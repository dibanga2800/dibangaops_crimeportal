#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using AIPBackend.Repositories.Models;
using AIPBackend.Exceptions;
using AIPBackend.Services.Security;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service implementation for Incident operations
	/// </summary>
	public class IncidentService : IIncidentService
	{
		private const int MinPageSize = 1;
		private const int MaxPageSize = 100;
		/// <summary>
		/// Maximum wall-clock time the API thread will wait for the configured
		/// classifier (Azure OpenAI) before falling back to the deterministic
		/// rule-based path inline. Keeps Create/Update responses snappy even when
		/// the LLM endpoint is sluggish; the LLM-refined fields can still land on
		/// the row later via the backfill loop.
		/// </summary>
		private static readonly TimeSpan InlineClassificationTimeout = TimeSpan.FromMilliseconds(750);

		private readonly IIncidentRepository _repository;
		private readonly ISiteRepository _siteRepository;
		private readonly ILogger<IncidentService> _logger;
		private readonly IUserContextService _userContext;
		private readonly IServiceProvider _serviceProvider;
		private readonly IIncidentClassifier _classifier;
		private readonly RuleBasedIncidentClassifier _ruleBasedFallback;
		private readonly IIncidentImageStorageService _incidentImageStorageService;
		private readonly IImageReferenceContentResolver _imageReferenceContentResolver;

		public IncidentService(
			IIncidentRepository repository,
			ISiteRepository siteRepository,
			ILogger<IncidentService> logger,
			IUserContextService userContext,
			IServiceProvider serviceProvider,
			IIncidentClassifier classifier,
			RuleBasedIncidentClassifier ruleBasedFallback,
			IIncidentImageStorageService incidentImageStorageService,
			IImageReferenceContentResolver imageReferenceContentResolver)
		{
			_repository = repository;
			_siteRepository = siteRepository;
			_logger = logger;
			_userContext = userContext;
			_serviceProvider = serviceProvider;
			_classifier = classifier;
			_ruleBasedFallback = ruleBasedFallback;
			_incidentImageStorageService = incidentImageStorageService;
			_imageReferenceContentResolver = imageReferenceContentResolver;
		}

		public async Task<IncidentResponseDto> GetByIdAsync(string id)
		{
			if (!int.TryParse(id, out var incidentId))
			{
				throw new ArgumentException("Invalid incident ID format", nameof(id));
			}

			var incident = await _repository.GetByIdWithItemsAsync(incidentId);
			if (incident == null)
			{
				throw new KeyNotFoundException($"Incident with ID {id} not found");
			}

			_userContext.EnsureCanAccessRecord(incident.CustomerId, incident.CreatedBy);

				return new IncidentResponseDto
			{
				Data = MapToDto(incident),
				Success = true,
				Message = "Incident retrieved successfully"
			};
		}

		public async Task<IncidentsResponseDto> GetIncidentsAsync(GetIncidentsQueryDto query)
		{
			query.Page = query.Page < 1 ? 1 : query.Page;
			query.PageSize = Math.Clamp(query.PageSize, MinPageSize, MaxPageSize);

			DateTime? fromDate = null;
			DateTime? toDate = null;

			if (!string.IsNullOrWhiteSpace(query.FromDate))
			{
				if (DateTime.TryParse(query.FromDate, out var parsedFromDate))
				{
					fromDate = parsedFromDate;
				}
			}

			if (!string.IsNullOrWhiteSpace(query.ToDate))
			{
				if (DateTime.TryParse(query.ToDate, out var parsedToDate))
				{
					toDate = parsedToDate;
				}
			}

			int? requestedCustomerId = null;
			if (!string.IsNullOrWhiteSpace(query.CustomerId) && int.TryParse(query.CustomerId, out var parsedCustomerId))
			{
				requestedCustomerId = parsedCustomerId;
			}

			var customerFilter = _userContext.ResolveCustomerFilter(requestedCustomerId);
			int? customerId = customerFilter.SingleCustomerId;
			IReadOnlyCollection<int>? customerIds = customerFilter.Unrestricted
				? null
				: customerFilter.CustomerIds.Count > 0 ? customerFilter.CustomerIds : null;

			if (customerId.HasValue)
			{
				query.CustomerId = customerId.Value.ToString();
			}

			var siteIdFilter = _userContext.ResolveSiteFilter(query.SiteId);
			if (siteIdFilter != null)
			{
				query.SiteId = siteIdFilter;
			}

			var regionIdFilter = string.IsNullOrWhiteSpace(query.RegionId) ? null : query.RegionId;

			var (incidents, totalCount) = await _repository.GetPagedAsync(
				page: query.Page,
				pageSize: query.PageSize,
				search: query.Search,
				customerId: customerId,
				customerIds: customerIds,
				siteId: siteIdFilter,
				regionId: regionIdFilter,
				incidentType: query.IncidentType,
				status: query.Status,
				fromDate: fromDate,
				toDate: toDate,
				createdByUserId: null);

			var summary = await _repository.GetSummaryAsync(
				search: query.Search,
				customerId: customerId,
				customerIds: customerIds,
				siteId: siteIdFilter,
				regionId: regionIdFilter,
				incidentType: query.IncidentType,
				status: query.Status,
				fromDate: fromDate,
				toDate: toDate,
				createdByUserId: null);

			var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

			return new IncidentsResponseDto
			{
				Data = incidents.Select(MapToDto).ToList(),
				Pagination = new PaginationInfoDto
				{
					CurrentPage = query.Page,
					TotalPages = totalPages,
					PageSize = query.PageSize,
					TotalCount = totalCount,
					HasPrevious = query.Page > 1,
					HasNext = query.Page < totalPages
				},
				Summary = summary
			};
		}

		public async Task<IncidentResponseDto> CreateAsync(UpsertIncidentDto dto, string? userId = null)
		{
			_userContext.EnsureCanAccessCustomer(dto.CustomerId);
			var context = _userContext.GetCurrentContext();

			await EnrichLocationMetadataAsync(dto);
			var storedImage = await _incidentImageStorageService.PersistVerificationImageAsync(dto.VerificationEvidenceImage);
			dto.VerificationEvidenceImage = storedImage.StoredReference;

			var incident = MapToEntity(dto);
			incident.CreatedBy = context.UserId;
			incident.CreatedAt = DateTime.UtcNow;
			incident.DateInputted = DateTime.UtcNow;

			// Calculate total value recovered from stolen items if not provided
			if (!incident.TotalValueRecovered.HasValue && incident.StolenItems.Any())
			{
				incident.TotalValueRecovered = incident.StolenItems.Sum(item => item.TotalAmount);
			}

		// Classify inline before the INSERT so the persisted row already carries
		// the AI fields and the API response returns up-to-date insight values.
		await ApplyClassificationInlineAsync(incident);

		var created = await _repository.CreateAsync(incident);

		_logger.LogInformation("Incident created with ID {IncidentId} by user {UserId}", created.IncidentId, context.UserId);

		_ = Task.Run(async () =>
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();

				var alertRuleService = scope.ServiceProvider.GetService<IAlertRuleService>();
				if (alertRuleService != null)
				{
					await alertRuleService.CheckIncidentForAlertsAsync(created.IncidentId);
				}

				var offenderRecognition = scope.ServiceProvider.GetService<IOffenderRecognitionService>();
				if (offenderRecognition != null && !string.IsNullOrWhiteSpace(created.VerificationEvidenceImage))
				{
					var imageBytes = storedImage.ImageBytes ?? await _imageReferenceContentResolver.ResolveAsync(created.VerificationEvidenceImage, CancellationToken.None);
					if (imageBytes != null && imageBytes.Length > 0)
					{
						await offenderRecognition.IndexVerificationEvidenceAsync(
							created.IncidentId,
							imageBytes,
							created.OffenderName ?? $"Incident-{created.IncidentId}",
							created.OffenderId,
							CancellationToken.None);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in post-create processing for incident {IncidentId}", created.IncidentId);
			}
		});

		return new IncidentResponseDto
		{
			Data = MapToDto(created),
			Success = true,
			Message = "Incident created successfully"
		};
	}

		public async Task<IncidentResponseDto> UpdateAsync(string id, UpsertIncidentDto dto, string? userId = null)
		{
			if (!int.TryParse(id, out var incidentId))
			{
				throw new ArgumentException("Invalid incident ID format", nameof(id));
			}

			var existing = await _repository.GetByIdWithItemsAsync(incidentId);
			if (existing == null)
			{
				throw new KeyNotFoundException($"Incident with ID {id} not found");
			}

			_userContext.EnsureCanAccessRecord(existing.CustomerId, existing.CreatedBy);
			_userContext.EnsureCanAccessCustomer(dto.CustomerId);

			await EnrichLocationMetadataAsync(dto);
			var storedImage = await _incidentImageStorageService.PersistVerificationImageAsync(dto.VerificationEvidenceImage);
			dto.VerificationEvidenceImage = storedImage.StoredReference;

			var beforeFingerprint = SnapshotForClassification(existing);

			UpdateEntityFromDto(existing, dto);
			var context = _userContext.GetCurrentContext();
			existing.UpdatedBy = context.UserId;
			existing.UpdatedAt = DateTime.UtcNow;

			// Calculate total value recovered from stolen items if not provided
			if (!existing.TotalValueRecovered.HasValue && existing.StolenItems.Any())
			{
				existing.TotalValueRecovered = existing.StolenItems.Sum(item => item.TotalAmount);
			}

		var afterFingerprint = SnapshotForClassification(existing);

		// Re-classify inline when any risk-relevant field changed so the API
		// response carries fresh AI insight values without waiting for the
		// hourly backfill pass to catch up.
		if (!beforeFingerprint.Equals(afterFingerprint))
		{
			await ApplyClassificationInlineAsync(existing);
		}

		var updated = await _repository.UpdateAsync(existing);

		_logger.LogInformation("Incident updated with ID {IncidentId} by user {UserId}", updated.IncidentId, context.UserId);

		// Check for matching alert rules and send notifications (async fire-and-forget)
		_ = Task.Run(async () =>
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var alertRuleService = scope.ServiceProvider.GetService<IAlertRuleService>();
				if (alertRuleService != null)
				{
					await alertRuleService.CheckIncidentForAlertsAsync(updated.IncidentId);
				}

				var offenderRecognition = scope.ServiceProvider.GetService<IOffenderRecognitionService>();
				if (offenderRecognition != null && !string.IsNullOrWhiteSpace(updated.VerificationEvidenceImage))
				{
					var imageBytes = storedImage.ImageBytes ?? await _imageReferenceContentResolver.ResolveAsync(updated.VerificationEvidenceImage, CancellationToken.None);
					if (imageBytes != null && imageBytes.Length > 0)
					{
						await offenderRecognition.IndexVerificationEvidenceAsync(
							updated.IncidentId,
							imageBytes,
							updated.OffenderName ?? $"Incident-{updated.IncidentId}",
							updated.OffenderId,
							CancellationToken.None);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking incident {IncidentId} for alert rules", updated.IncidentId);
			}
		});

		return new IncidentResponseDto
		{
			Data = MapToDto(updated),
			Success = true,
			Message = "Incident updated successfully"
		};
	}

		public async Task<bool> DeleteAsync(string id)
		{
			if (!int.TryParse(id, out var incidentId))
			{
				throw new ArgumentException("Invalid incident ID format", nameof(id));
			}

			var existing = await _repository.GetByIdAsync(incidentId);
			if (existing == null)
			{
				return false;
			}

			_userContext.EnsureCanAccessRecord(existing.CustomerId, existing.CreatedBy);

			var result = await _repository.DeleteAsync(incidentId);
			if (result)
			{
				_logger.LogInformation("Incident deleted with ID {IncidentId}", incidentId);
			}
			return result;
		}

		public async Task<List<IncidentDto>> GetAllForStatsAsync(int? customerId = null, string? siteId = null, string? regionId = null)
		{
			var customerFilter = _userContext.ResolveCustomerFilter(customerId);
			customerId = customerFilter.SingleCustomerId;
			IReadOnlyCollection<int>? customerIds = customerFilter.Unrestricted
				? null
				: customerFilter.CustomerIds.Count > 0 ? customerFilter.CustomerIds : null;
			siteId = _userContext.ResolveSiteFilter(siteId);

			var incidents = await _repository.GetAllForStatsAsync(customerId, customerIds, siteId, regionId);

			return incidents.Select(MapToDto).ToList();
		}

		public async Task<RepeatOffenderSearchResponseDto> SearchRepeatOffendersAsync(RepeatOffenderSearchQueryDto query)
		{
			var context = _userContext.GetCurrentContext();
			if (!context.IsAdministrator)
			{
				_userContext.EnsureHasTenantScope();
			}

			if (string.IsNullOrWhiteSpace(query.Name) &&
				string.IsNullOrWhiteSpace(query.Marks) &&
				string.IsNullOrWhiteSpace(query.DateOfBirth))
			{
				throw new ArgumentException("Provide at least one search criteria (name, date of birth, or marks)");
			}

			DateTime? parsedDob = null;
			if (!string.IsNullOrWhiteSpace(query.DateOfBirth) &&
				DateTime.TryParse(query.DateOfBirth, out var dob))
			{
				parsedDob = dob.Date;
			}

			var filter = new RepeatOffenderSearchFilter
			{
				Name = query.Name,
				Marks = query.Marks,
				DateOfBirth = parsedDob,
				Page = query.Page < 1 ? 1 : query.Page,
				PageSize = query.PageSize < 1 ? 10 : query.PageSize,
				AccessibleCustomerIds = context.IsAdministrator ? Array.Empty<int>() : context.AccessibleCustomerIds,
				AccessibleSiteIds = context.IsAdministrator ? Array.Empty<string>() : context.AccessibleSiteIds
			};

			var (results, totalCount) = await _repository.SearchRepeatOffendersAsync(filter);
			var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

			var dto = new RepeatOffenderSearchResponseDto
			{
				Success = true,
				Data = results.Select(MapRepeatOffenderResultToDto).ToList(),
				Pagination = new PaginationInfoDto
				{
					CurrentPage = filter.Page,
					PageSize = filter.PageSize,
					TotalCount = totalCount,
					TotalPages = totalPages,
					HasPrevious = filter.Page > 1,
					HasNext = filter.Page < totalPages
				}
			};

			return dto;
		}

		public async Task<CrimeIntelligenceResponseDto> GetCrimeInsightsAsync(CrimeIntelligenceQueryDto query)
		{
			if (query.CustomerId <= 0)
			{
				throw new ArgumentException("CustomerId is required", nameof(query.CustomerId));
			}

			_userContext.EnsureCanAccessCustomer(query.CustomerId);
			var context = _userContext.GetCurrentContext();

			var effectiveQuery = new CrimeIntelligenceQueryDto
			{
				CustomerId = query.CustomerId,
				SiteId = query.SiteId,
				RegionId = query.RegionId,
				StartDate = query.StartDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				EndDate = query.EndDate ?? DateTime.UtcNow
			};

			if (!context.IsAdministrator && context.AccessibleSiteIds.Count > 0)
			{
				if (!string.IsNullOrWhiteSpace(effectiveQuery.SiteId) &&
					!context.AccessibleSiteIds.Contains(effectiveQuery.SiteId))
				{
					throw new ForbiddenAccessException("You do not have permission to access this site.");
				}
			}

			var incidents = await _repository.GetIncidentsWithDetailsAsync(effectiveQuery);
			if (!context.IsAdministrator && context.AccessibleSiteIds.Count > 0)
			{
				incidents = incidents
					.Where(i => !string.IsNullOrWhiteSpace(i.SiteId) && context.AccessibleSiteIds.Contains(i.SiteId))
					.ToList();
			}

			if (!incidents.Any())
			{
				return new CrimeIntelligenceResponseDto
				{
					Message = "No incident data available for the selected filters.",
					GeneratedAt = DateTime.UtcNow
				};
			}

			var totalIncidents = incidents.Count;
			var totalRecoveredValue = incidents.Sum(IncidentFinancials.GetRecoveredValue);
			var totalLostValue = incidents.Sum(IncidentFinancials.GetLostValue);
			var hasEstimatedLossValues = incidents.Any(i => !i.TotalLostValue.HasValue);
			var distinctStores = incidents
				.Where(i => !string.IsNullOrWhiteSpace(i.StoreName))
				.Select(i => i.StoreName!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Count();

			var incidentTypeGroups = incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unspecified" : i.IncidentType)
				.Select(g => new CrimeInsightListItemDto
				{
					Name = g.Key,
					Count = g.Count(),
					Value = g.Sum(IncidentFinancials.GetRecoveredValue),
					Percentage = Math.Round((double)g.Count() / totalIncidents * 100, 1)
				})
				.OrderByDescending(g => g.Count)
				.Take(6)
				.ToList();

			var storeGroups = incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.StoreName) ? "Unassigned Site" : i.StoreName!)
				.Select(g => new CrimeInsightListItemDto
				{
					Name = g.Key,
					Count = g.Count(),
					Value = g.Sum(IncidentFinancials.GetRecoveredValue),
					Percentage = Math.Round((double)g.Count() / totalIncidents * 100, 1)
				})
				.OrderByDescending(g => g.Count)
				.Take(20) // Increased from 6 to 20 to support pagination
				.ToList();

			var regionGroups = incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.RegionName) ? "Unassigned Region" : i.RegionName!)
				.Select(g => new CrimeInsightListItemDto
				{
					Name = g.Key,
					Count = g.Count(),
					Value = g.Sum(IncidentFinancials.GetRecoveredValue),
					Percentage = Math.Round((double)g.Count() / totalIncidents * 100, 1)
				})
				.OrderByDescending(g => g.Count)
				.Take(20) // Increased from 6 to 20 to support pagination
				.ToList();

			var stolenItems = incidents
				.Where(i => i.StolenItems != null && i.StolenItems.Any())
				.SelectMany(i => i.StolenItems.Select(item => (item, incident: i)))
				.ToList();

			var totalItems = stolenItems.Sum(x => x.item.Quantity);
			var topProducts = stolenItems
				.GroupBy(x => string.IsNullOrWhiteSpace(x.item.ProductName)
					? string.IsNullOrWhiteSpace(x.item.Category) ? "Unspecified Product" : x.item.Category!
					: x.item.ProductName!)
				.Select(g => new CrimeInsightListItemDto
				{
					Name = g.Key,
					Count = g.Sum(x => x.item.Quantity),
					Value = g.Sum(x => x.item.TotalAmount),
					Percentage = totalItems > 0
						? Math.Round((double)g.Sum(x => x.item.Quantity) / totalItems * 100, 1)
						: 0
				})
				.OrderByDescending(g => g.Count)
				.Take(10)
				.ToList();

			var timeBuckets = CalculateTimeBuckets(incidents, totalIncidents);

			var hotProduct = BuildHotProductInsight(topProducts, stolenItems);

			var heroMetrics = BuildHeroMetrics(
				totalIncidents,
				totalRecoveredValue,
				totalLostValue,
				hasEstimatedLossValues,
				distinctStores,
				incidentTypeGroups,
				storeGroups);

			return new CrimeIntelligenceResponseDto
			{
				HeroMetrics = heroMetrics,
				TopIncidentTypes = incidentTypeGroups,
				TopStores = storeGroups,
				TopProducts = topProducts,
				TopRegions = regionGroups,
				TimeBuckets = timeBuckets,
				HotProduct = hotProduct,
				GeneratedAt = DateTime.UtcNow
			};
		}

		public async Task<IncidentGraphAnalyticsResponseDto> GetIncidentGraphAnalyticsAsync(IncidentGraphAnalyticsQueryDto query)
		{
			if (query.CustomerId <= 0)
			{
				throw new ArgumentException("CustomerId is required", nameof(query.CustomerId));
			}

			_userContext.EnsureCanAccessCustomer(query.CustomerId);
			var context = _userContext.GetCurrentContext();

			DateTime? fromDate = null;
			DateTime? toDate = null;
			if (!string.IsNullOrWhiteSpace(query.FromDate) && DateTime.TryParse(query.FromDate, out var parsedFrom))
			{
				fromDate = parsedFrom;
			}

			if (!string.IsNullOrWhiteSpace(query.ToDate) && DateTime.TryParse(query.ToDate, out var parsedTo))
			{
				toDate = parsedTo;
			}

			var effectiveQuery = new CrimeIntelligenceQueryDto
			{
				CustomerId = query.CustomerId,
				RegionId = query.RegionId,
				StartDate = fromDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				EndDate = toDate ?? DateTime.UtcNow
			};

			var incidents = await _repository.GetIncidentsWithDetailsAsync(effectiveQuery);
			if (!context.IsAdministrator && context.AccessibleSiteIds.Count > 0)
			{
				incidents = incidents
					.Where(i => !string.IsNullOrWhiteSpace(i.SiteId) && context.AccessibleSiteIds.Contains(i.SiteId))
					.ToList();
			}

			incidents = incidents
				.Where(i => PassesOfficerFilter(i, query.OfficerType))
				.ToList();

			if (!incidents.Any())
			{
				return new IncidentGraphAnalyticsResponseDto
				{
					Message = "No incident data available for the selected filters.",
					Totals = new IncidentGraphTotalsDto()
				};
			}

			var graphType = string.IsNullOrWhiteSpace(query.GraphType) ? "value" : query.GraphType.Trim().ToLowerInvariant();
			var grouped = new Dictionary<string, IncidentGraphLocationDto>(StringComparer.OrdinalIgnoreCase);

			foreach (var incident in incidents)
			{
				var location = string.IsNullOrWhiteSpace(incident.StoreName)
					? string.IsNullOrWhiteSpace(incident.SiteId) ? "Unknown Location" : incident.SiteId
					: incident.StoreName;

				if (!grouped.TryGetValue(location, out var entry))
				{
					entry = new IncidentGraphLocationDto
					{
						Location = location,
						SiteId = incident.SiteId ?? string.Empty,
						SiteName = incident.StoreName ?? location,
						RegionId = incident.RegionId ?? string.Empty,
						RegionName = incident.RegionName ?? string.Empty,
						CustomerName = string.Empty,
					};
					grouped[location] = entry;
				}

				entry.Count += 1;
				entry.Value += IncidentFinancials.GetRecoveredValue(incident);
				entry.LostValue += IncidentFinancials.GetLostValue(incident);
				entry.Quantity += incident.TotalRecoveredQuantity
					?? incident.QuantityRecovered
					?? 1;
			}

			var locations = grouped.Values
				.OrderByDescending(l => graphType switch
				{
					"lost" => l.LostValue,
					"quantity" => l.Quantity,
					"type" => l.Count,
					_ => l.Value
				})
				.ToList();

			var typeMap = incidents
				.GroupBy(i => string.IsNullOrWhiteSpace(i.IncidentType) ? "Unknown" : i.IncidentType)
				.Select(g => new IncidentGraphTypeCountDto { Type = g.Key, Count = g.Count() })
				.OrderByDescending(t => t.Count)
				.ToList();

			var totals = new IncidentGraphTotalsDto
			{
				TotalIncidents = incidents.Count,
				TotalValue = graphType switch
				{
					"lost" => incidents.Sum(IncidentFinancials.GetLostValue),
					"quantity" => locations.Sum(l => l.Quantity),
					"type" => incidents.Count,
					_ => incidents.Sum(IncidentFinancials.GetRecoveredValue)
				},
				TotalQuantity = locations.Sum(l => l.Quantity)
			};

			return new IncidentGraphAnalyticsResponseDto
			{
				Locations = locations,
				Totals = totals,
				Types = typeMap
			};
		}

		#region Mapping Methods

		private IncidentDto MapToDto(Incident incident)
		{
			ApplyComputedIncidentTotals(incident);

			// Parse JSON arrays
			List<string>? incidentInvolved = null;
			if (!string.IsNullOrWhiteSpace(incident.IncidentInvolved))
			{
				try
				{
					incidentInvolved = JsonSerializer.Deserialize<List<string>>(incident.IncidentInvolved);
				}
				catch (JsonException ex)
				{
					_logger.LogWarning(ex, "Failed to parse IncidentInvolved for incident {IncidentId}", incident.IncidentId);
				}
			}

			List<string>? witnessStatements = null;
			if (!string.IsNullOrWhiteSpace(incident.WitnessStatements))
			{
				try
				{
					witnessStatements = JsonSerializer.Deserialize<List<string>>(incident.WitnessStatements);
				}
				catch (JsonException ex)
				{
					_logger.LogWarning(ex, "Failed to parse WitnessStatements for incident {IncidentId}", incident.IncidentId);
				}
			}

			List<string>? involvedParties = null;
			if (!string.IsNullOrWhiteSpace(incident.InvolvedParties))
			{
				try
				{
					involvedParties = JsonSerializer.Deserialize<List<string>>(incident.InvolvedParties);
				}
				catch (JsonException ex)
				{
					_logger.LogWarning(ex, "Failed to parse InvolvedParties for incident {IncidentId}", incident.IncidentId);
				}
			}

			List<string>? modusOperandi = null;
			if (!string.IsNullOrWhiteSpace(incident.ModusOperandi))
			{
				try
				{
					modusOperandi = JsonSerializer.Deserialize<List<string>>(incident.ModusOperandi);
				}
				catch (JsonException ex)
				{
					_logger.LogWarning(ex, "Failed to parse ModusOperandi for incident {IncidentId}", incident.IncidentId);
				}
			}

			// Build offender address if any fields are present
			OffenderAddressDto? offenderAddress = null;
			if (!string.IsNullOrWhiteSpace(incident.OffenderNumberAndStreet) ||
				!string.IsNullOrWhiteSpace(incident.OffenderTown) ||
				!string.IsNullOrWhiteSpace(incident.OffenderPostCode))
			{
				offenderAddress = new OffenderAddressDto
				{
					HouseName = incident.OffenderHouseName,
					NumberAndStreet = incident.OffenderNumberAndStreet,
					VillageOrSuburb = incident.OffenderVillageOrSuburb,
					Town = incident.OffenderTown,
					County = incident.OffenderCounty,
					PostCode = incident.OffenderPostCode
				};
			}

				return new IncidentDto
			{
				Id = incident.IncidentId.ToString(),
				CustomerId = incident.CustomerId,
				CustomerName = incident.Customer?.CompanyName ?? string.Empty,
				StoreName = incident.StoreName,
				SiteId = incident.SiteId,
				RegionId = incident.RegionId,
				RegionName = incident.RegionName,
				Location = incident.Location,
				Store = incident.StoreName, // Legacy field
				StaffMemberName = incident.StaffMemberName,
				StaffMemberRole = incident.StaffMemberRole,
				OfficerType = incident.OfficerType,
				DutyManagerName = incident.DutyManagerName,
				AssignedTo = incident.AssignedTo,
				DateOfIncident = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				Date = incident.DateOfIncident.ToString("yyyy-MM-dd"), // Legacy field
				TimeOfIncident = incident.TimeOfIncident,
				DateInputted = incident.DateInputted.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				IncidentType = incident.IncidentType,
				Type = incident.IncidentType, // Legacy field
				ActionCode = incident.ActionCode,
				IncidentCategory = incident.IncidentCategory,
				IncidentCategoryConfidence = incident.IncidentCategoryConfidence,
				RiskLevel = incident.RiskLevel,
				RiskScore = incident.RiskScore,
				ClassificationVersion = incident.ClassificationVersion,
				IncidentInvolved = incidentInvolved,
				Description = incident.Description,
				IncidentDetails = incident.IncidentDetails,
				StoreComments = incident.StoreComments,
				TotalStolenValue = incident.TotalStolenValue,
				TotalRecoveredValue = incident.TotalRecoveredValue,
				TotalLostValue = incident.TotalLostValue,
				TotalRecoveredQuantity = incident.TotalRecoveredQuantity,
				TotalValueRecovered = incident.TotalValueRecovered,
				Value = incident.TotalValueRecovered, // Legacy field
				ValueRecovered = incident.ValueRecovered,
				QuantityRecovered = incident.QuantityRecovered,
				Amount = incident.TotalValueRecovered, // Legacy field
				Total = incident.TotalValueRecovered, // Legacy field
				StolenItems = incident.StolenItems?.Select(item => new StolenItemDto
				{
					Id = item.StolenItemId.ToString(),
					Category = item.Category,
					Description = item.Description,
					ProductName = item.ProductName,
					Cost = item.Cost,
					Quantity = item.Quantity,
					TotalAmount = item.TotalAmount,
					WasRecovered = item.WasRecovered,
					RecoveredQuantity = item.RecoveredQuantity,
					RecoveredAmount = item.RecoveredAmount,
					Barcode = item.Barcode
				}).ToList(),
				PoliceInvolvement = incident.PoliceInvolvement,
				UrnNumber = incident.UrnNumber,
				CrimeRefNumber = incident.CrimeRefNumber,
				PoliceID = incident.PoliceId,
				Status = incident.Status,
				Priority = incident.Priority,
				ActionTaken = incident.ActionTaken,
				EvidenceAttached = incident.EvidenceAttached,
				WitnessStatements = witnessStatements,
				InvolvedParties = involvedParties,
				ReportNumber = incident.ReportNumber,
				OffenderName = incident.OffenderName,
				OffenderSex = incident.OffenderSex,
				Gender = incident.Gender,
				OffenderDOB = incident.OffenderDOB?.ToString("yyyy-MM-dd"),
				OffenderPlaceOfBirth = incident.OffenderPlaceOfBirth,
				OffenderMarks = incident.OffenderMarks,
				OffenderDetailsVerified = incident.OffenderDetailsVerified,
				VerificationMethod = incident.VerificationMethod,
				VerificationEvidenceImage = incident.VerificationEvidenceImage,
				OffenderAddress = offenderAddress,
				OffenderId = incident.OffenderId,
				ModusOperandi = modusOperandi,
				ArrestSaveComment = incident.ArrestSaveComment
			};
		}

		private Incident MapToEntity(UpsertIncidentDto dto)
		{
			var incident = new Incident
			{
				CustomerId = dto.CustomerId,
				SiteId = dto.SiteId,
				RegionId = dto.RegionId,
				StoreName = dto.StoreName,
				RegionName = dto.RegionName,
				Location = dto.Location,
				StaffMemberName = dto.StaffMemberName,
				StaffMemberRole = dto.StaffMemberRole,
				OfficerType = dto.OfficerType,
				DutyManagerName = dto.DutyManagerName,
				AssignedTo = dto.AssignedTo,
				DateOfIncident = dto.DateOfIncident,
				TimeOfIncident = dto.TimeOfIncident,
				IncidentType = dto.IncidentType,
				ActionCode = dto.ActionCode,
				Description = dto.Description,
				IncidentDetails = dto.IncidentDetails,
				StoreComments = dto.StoreComments,
				TotalStolenValue = dto.TotalStolenValue,
				TotalRecoveredValue = dto.TotalRecoveredValue,
				TotalLostValue = dto.TotalLostValue,
				TotalRecoveredQuantity = dto.TotalRecoveredQuantity,
				TotalValueRecovered = dto.TotalValueRecovered,
				ValueRecovered = dto.ValueRecovered,
				QuantityRecovered = dto.QuantityRecovered,
				PoliceInvolvement = dto.PoliceInvolvement,
				UrnNumber = dto.UrnNumber,
				CrimeRefNumber = dto.CrimeRefNumber,
				PoliceId = dto.PoliceId,
				Status = dto.Status ?? "pending",
				Priority = dto.Priority,
				ActionTaken = dto.ActionTaken,
				EvidenceAttached = dto.EvidenceAttached,
				ReportNumber = dto.ReportNumber,
				OffenderName = dto.OffenderName,
				OffenderSex = dto.OffenderSex,
				Gender = dto.Gender,
				OffenderDOB = dto.OffenderDOB,
				OffenderPlaceOfBirth = dto.OffenderPlaceOfBirth,
				OffenderMarks = dto.OffenderMarks,
				OffenderDetailsVerified = dto.OffenderDetailsVerified,
				VerificationMethod = dto.VerificationMethod,
				VerificationEvidenceImage = dto.VerificationEvidenceImage,
				OffenderHouseName = dto.OffenderAddress?.HouseName,
				OffenderNumberAndStreet = dto.OffenderAddress?.NumberAndStreet,
				OffenderVillageOrSuburb = dto.OffenderAddress?.VillageOrSuburb,
				OffenderTown = dto.OffenderAddress?.Town,
				OffenderCounty = dto.OffenderAddress?.County,
				OffenderPostCode = dto.OffenderAddress?.PostCode,
				OffenderId = dto.OffenderId,
				ArrestSaveComment = dto.ArrestSaveComment
			};

			// Serialize JSON arrays
			if (dto.ModusOperandi != null && dto.ModusOperandi.Any())
			{
				incident.ModusOperandi = JsonSerializer.Serialize(dto.ModusOperandi);
			}

			if (dto.IncidentInvolved != null && dto.IncidentInvolved.Any())
			{
				incident.IncidentInvolved = JsonSerializer.Serialize(dto.IncidentInvolved);
			}

			if (dto.WitnessStatements != null && dto.WitnessStatements.Any())
			{
				incident.WitnessStatements = JsonSerializer.Serialize(dto.WitnessStatements);
			}

			if (dto.InvolvedParties != null && dto.InvolvedParties.Any())
			{
				incident.InvolvedParties = JsonSerializer.Serialize(dto.InvolvedParties);
			}

			// Map stolen items
			if (dto.StolenItems != null && dto.StolenItems.Any())
			{
				incident.StolenItems = dto.StolenItems.Select(item => new StolenItem
				{
					Category = item.Category,
					Description = item.Description,
					ProductName = item.ProductName,
					Cost = item.Cost,
					Quantity = item.Quantity,
					TotalAmount = item.TotalAmount,
					WasRecovered = item.WasRecovered,
					RecoveredQuantity = item.RecoveredQuantity,
					RecoveredAmount = item.RecoveredAmount,
					Barcode = item.Barcode,
					CreatedAt = DateTime.UtcNow
				}).ToList();
			}

			ApplyComputedIncidentTotals(incident);

			return incident;
		}

		private async Task EnrichLocationMetadataAsync(UpsertIncidentDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.SiteId))
			{
				return;
			}

			if (!int.TryParse(dto.SiteId, out var siteId))
			{
				_logger.LogWarning("IncidentService: Unable to parse SiteId '{SiteId}' to int when enriching location metadata.", dto.SiteId);
				return;
			}

			var site = await _siteRepository.GetByIdAsync(siteId);
			if (site == null)
			{
				_logger.LogWarning("IncidentService: Site with ID {SiteId} not found while enriching incident metadata.", siteId);
				return;
			}

			if (dto.CustomerId != site.fkCustomerID)
			{
				_logger.LogInformation("IncidentService: Overriding customerId {OriginalCustomerId} with site-owned customerId {SiteCustomerId} for site {SiteId}.", dto.CustomerId, site.fkCustomerID, siteId);
				dto.CustomerId = site.fkCustomerID;
			}

			dto.RegionId = site.fkRegionID.ToString(CultureInfo.InvariantCulture);
			dto.RegionName = site.Region?.RegionName ?? dto.RegionName;
			dto.StoreName = string.IsNullOrWhiteSpace(dto.StoreName) ? site.LocationName : dto.StoreName;
			dto.Location ??= site.LocationName;
		}

		private void UpdateEntityFromDto(Incident incident, UpsertIncidentDto dto)
		{
			incident.CustomerId = dto.CustomerId;
			incident.SiteId = dto.SiteId;
			incident.RegionId = dto.RegionId;
			incident.StoreName = dto.StoreName;
			incident.RegionName = dto.RegionName;
			incident.Location = dto.Location;
			incident.StaffMemberName = dto.StaffMemberName;
			incident.StaffMemberRole = dto.StaffMemberRole;
			incident.OfficerType = dto.OfficerType;
			incident.DutyManagerName = dto.DutyManagerName;
			incident.AssignedTo = dto.AssignedTo;
			incident.DateOfIncident = dto.DateOfIncident;
			incident.TimeOfIncident = dto.TimeOfIncident;
			incident.IncidentType = dto.IncidentType;
			incident.ActionCode = dto.ActionCode;
			incident.Description = dto.Description;
			incident.IncidentDetails = dto.IncidentDetails;
			incident.StoreComments = dto.StoreComments;
			incident.TotalStolenValue = dto.TotalStolenValue;
			incident.TotalRecoveredValue = dto.TotalRecoveredValue;
			incident.TotalLostValue = dto.TotalLostValue;
			incident.TotalRecoveredQuantity = dto.TotalRecoveredQuantity;
			incident.TotalValueRecovered = dto.TotalValueRecovered;
			incident.ValueRecovered = dto.ValueRecovered;
			incident.QuantityRecovered = dto.QuantityRecovered;
			incident.PoliceInvolvement = dto.PoliceInvolvement;
			incident.UrnNumber = dto.UrnNumber;
			incident.CrimeRefNumber = dto.CrimeRefNumber;
			incident.PoliceId = dto.PoliceId;
			incident.Status = dto.Status ?? incident.Status;
			incident.Priority = dto.Priority;
			incident.ActionTaken = dto.ActionTaken;
			incident.EvidenceAttached = dto.EvidenceAttached;
			incident.ReportNumber = dto.ReportNumber;
			incident.OffenderName = dto.OffenderName;
			incident.OffenderSex = dto.OffenderSex;
			incident.Gender = dto.Gender;
			incident.OffenderDOB = dto.OffenderDOB;
			incident.OffenderPlaceOfBirth = dto.OffenderPlaceOfBirth;
			incident.OffenderMarks = dto.OffenderMarks;
			incident.OffenderDetailsVerified = dto.OffenderDetailsVerified;
			incident.VerificationMethod = dto.VerificationMethod;
			incident.VerificationEvidenceImage = dto.VerificationEvidenceImage;
			incident.OffenderHouseName = dto.OffenderAddress?.HouseName;
			incident.OffenderNumberAndStreet = dto.OffenderAddress?.NumberAndStreet;
			incident.OffenderVillageOrSuburb = dto.OffenderAddress?.VillageOrSuburb;
			incident.OffenderTown = dto.OffenderAddress?.Town;
			incident.OffenderCounty = dto.OffenderAddress?.County;
			incident.OffenderPostCode = dto.OffenderAddress?.PostCode;
			incident.OffenderId = dto.OffenderId;
			incident.ArrestSaveComment = dto.ArrestSaveComment;

			// Serialize JSON arrays
			if (dto.ModusOperandi != null && dto.ModusOperandi.Any())
			{
				incident.ModusOperandi = JsonSerializer.Serialize(dto.ModusOperandi);
			}
			else
			{
				incident.ModusOperandi = null;
			}

			if (dto.IncidentInvolved != null)
			{
				incident.IncidentInvolved = dto.IncidentInvolved.Any()
					? JsonSerializer.Serialize(dto.IncidentInvolved)
					: null;
			}

			if (dto.WitnessStatements != null)
			{
				incident.WitnessStatements = dto.WitnessStatements.Any()
					? JsonSerializer.Serialize(dto.WitnessStatements)
					: null;
			}

			if (dto.InvolvedParties != null)
			{
				incident.InvolvedParties = dto.InvolvedParties.Any()
					? JsonSerializer.Serialize(dto.InvolvedParties)
					: null;
			}

			// Update stolen items - remove existing and add new ones
			// Note: In a production system, you might want to update existing items instead
			incident.StolenItems.Clear();
			if (dto.StolenItems != null && dto.StolenItems.Any())
			{
				foreach (var itemDto in dto.StolenItems)
				{
					incident.StolenItems.Add(new StolenItem
					{
						Category = itemDto.Category,
						Description = itemDto.Description,
						ProductName = itemDto.ProductName,
						Cost = itemDto.Cost,
						Quantity = itemDto.Quantity,
						TotalAmount = itemDto.TotalAmount,
						WasRecovered = itemDto.WasRecovered,
						RecoveredQuantity = itemDto.RecoveredQuantity,
						RecoveredAmount = itemDto.RecoveredAmount,
						Barcode = itemDto.Barcode,
						CreatedAt = DateTime.UtcNow
					});
				}
			}

			ApplyComputedIncidentTotals(incident);
		}

		private static void ApplyComputedIncidentTotals(Incident incident)
		{
			if (incident.StolenItems == null || !incident.StolenItems.Any())
			{
				return;
			}

			foreach (var item in incident.StolenItems)
			{
				item.Quantity = Math.Max(item.Quantity, 0);
				item.Cost = Math.Max(item.Cost, 0);
				item.TotalAmount = item.Cost * item.Quantity;

				if (!item.WasRecovered)
				{
					item.RecoveredQuantity = 0;
					item.RecoveredAmount = 0;
					continue;
				}

				item.RecoveredQuantity = Math.Clamp(item.RecoveredQuantity, 0, item.Quantity);
				item.RecoveredAmount = item.Cost * item.RecoveredQuantity;
			}

			var totalStolenValue = incident.StolenItems.Sum(item => item.TotalAmount);
			var totalRecoveredValue = incident.StolenItems.Sum(item => item.RecoveredAmount);
			var totalRecoveredQuantity = incident.StolenItems.Sum(item => item.RecoveredQuantity);
			var totalLostValue = totalStolenValue - totalRecoveredValue;

			incident.TotalStolenValue = totalStolenValue;
			incident.TotalRecoveredValue = totalRecoveredValue;
			incident.TotalLostValue = totalLostValue;
			incident.TotalRecoveredQuantity = totalRecoveredQuantity;

			// Keep legacy fields aligned during rollout.
			incident.TotalValueRecovered = totalRecoveredValue;
			incident.ValueRecovered = totalRecoveredValue;
			incident.QuantityRecovered = totalRecoveredQuantity;
		}

		private RepeatOffenderMatchDto MapRepeatOffenderResultToDto(RepeatOffenderRepositoryResult result)
		{
			OffenderAddressDto? offenderAddress = null;
			if (!string.IsNullOrWhiteSpace(result.NumberAndStreet) ||
				!string.IsNullOrWhiteSpace(result.Town) ||
				!string.IsNullOrWhiteSpace(result.PostCode))
			{
				offenderAddress = new OffenderAddressDto
				{
					HouseName = result.HouseName,
					NumberAndStreet = result.NumberAndStreet,
					VillageOrSuburb = result.VillageOrSuburb,
					Town = result.Town,
					County = result.County,
					PostCode = result.PostCode
				};
			}

			return new RepeatOffenderMatchDto
			{
				OffenderName = result.OffenderName,
				OffenderDOB = result.OffenderDOB?.ToString("yyyy-MM-dd"),
				Gender = result.Gender,
				OffenderMarks = result.OffenderMarks,
				OffenderPlaceOfBirth = result.OffenderPlaceOfBirth,
				OffenderAddress = offenderAddress,
				IncidentCount = result.IncidentCount,
				RecentIncidents = result.RecentIncidents.Select(incident => new RepeatOffenderIncidentSummaryDto
				{
					IncidentId = incident.IncidentId.ToString(),
					DateOfIncident = incident.DateOfIncident.ToString("yyyy-MM-dd"),
				SiteName = incident.StoreName,
				IncidentType = incident.IncidentType,
				Description = incident.Description,
				OffenderMarks = incident.OffenderMarks,
					OffenderDetailsVerified = incident.OffenderDetailsVerified,
					VerificationMethod = incident.VerificationMethod,
					VerificationEvidenceImage = incident.VerificationEvidenceImage
				}).ToList()
			};
		}

		private static decimal CalculateIncidentValue(Incident incident) =>
			IncidentFinancials.GetRecoveredValue(incident);

		private static string NormalizeOfficerRole(string? role) =>
			string.IsNullOrWhiteSpace(role)
				? string.Empty
				: role.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');

		private static bool PassesOfficerFilter(Incident incident, string? officerType)
		{
			if (string.IsNullOrWhiteSpace(officerType) || officerType.Equals("all", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			var role = NormalizeOfficerRole(incident.StaffMemberRole);
			if (string.IsNullOrEmpty(role))
			{
				return false;
			}

			return officerType.ToLowerInvariant() switch
			{
				"uniform" => role.Contains("uniform", StringComparison.Ordinal)
					|| role == "security officer"
					|| role == "officer",
				"detective" => role.Contains("detective", StringComparison.Ordinal),
				"store-user" => role == "store user"
					|| role == "store"
					|| role == "store colleague"
					|| role == "colleague",
				_ => true
			};
		}

		private static List<CrimeInsightMetricDto> BuildHeroMetrics(
			int totalIncidents,
			decimal totalRecoveredValue,
			decimal totalLostValue,
			bool hasEstimatedLossValues,
			int distinctStores,
			List<CrimeInsightListItemDto> incidentTypes,
			List<CrimeInsightListItemDto> stores)
		{
			var currencyFormat = CultureInfo.CreateSpecificCulture("en-GB");
			var recoveredFormatted = totalRecoveredValue.ToString("C0", currencyFormat);
			var lossFormatted = totalLostValue.ToString("C0", currencyFormat);
			var lossLabel = hasEstimatedLossValues ? "Est. loss" : "Loss";

			var metrics = new List<CrimeInsightMetricDto>
			{
				new CrimeInsightMetricDto
				{
					Title = "Total Incidents",
					Value = totalIncidents.ToString("N0"),
					Subtext = distinctStores > 0 ? $"{(totalIncidents / Math.Max(distinctStores, 1)):N1} per store" : "No store data",
					TrendIsPositive = false
				},
				new CrimeInsightMetricDto
				{
					Title = "Value Impact",
					Value = recoveredFormatted,
					Subtext = "Recovered / Estimated loss",
					ValueImpact = new CrimeInsightValueImpactDto
					{
						Recovered = recoveredFormatted,
						EstimatedLoss = lossFormatted,
						LossLabel = lossLabel
					},
					TrendIsPositive = totalLostValue <= totalRecoveredValue
				}
			};

			if (incidentTypes.Any())
			{
				var topType = incidentTypes.First();
				metrics.Add(new CrimeInsightMetricDto
				{
					Title = "Top Incident Type",
					Value = topType.Name,
					Subtext = $"{topType.Count:N0} reports ({topType.Percentage:N1}%)",
					TrendIsPositive = false
				});
			}

			if (stores.Any())
			{
				var hotStore = stores.First();
				metrics.Add(new CrimeInsightMetricDto
				{
					Title = "Hot Store",
					Value = hotStore.Name,
					Subtext = $"{hotStore.Count:N0} incidents ({hotStore.Percentage:N1}%)",
					TrendIsPositive = false
				});
			}

			return metrics;
		}

		private static List<CrimeInsightTimeBucketDto> CalculateTimeBuckets(List<Incident> incidents, int totalIncidents)
		{
			var buckets = new[]
			{
				new { Label = "00:00 - 05:59", Start = 0, End = 6 },
				new { Label = "06:00 - 11:59", Start = 6, End = 12 },
				new { Label = "12:00 - 17:59", Start = 12, End = 18 },
				new { Label = "18:00 - 23:59", Start = 18, End = 24 }
			};

			var bucketCounts = buckets.Select(bucket =>
			{
				var count = incidents.Count(incident =>
				{
					if (string.IsNullOrWhiteSpace(incident.TimeOfIncident))
					{
						return false;
					}

					if (!TimeSpan.TryParse(incident.TimeOfIncident, out var timeOfDay))
					{
						return false;
					}

					var hour = timeOfDay.Hours;
					return hour >= bucket.Start && hour < bucket.End;
				});

				return new CrimeInsightTimeBucketDto
				{
					Bucket = bucket.Label,
					Count = count,
					Percentage = totalIncidents > 0
						? Math.Round((double)count / totalIncidents * 100, 1)
						: 0
				};
			}).ToList();

			return bucketCounts;
		}

		private static CrimeInsightHotProductDto? BuildHotProductInsight(
			List<CrimeInsightListItemDto> topProducts,
			List<(StolenItem item, Incident incident)> stolenItems)
		{
			if (!topProducts.Any())
			{
				return null;
			}

			var leadingProduct = topProducts.First();
			var matchingItems = stolenItems
				.Where(x => string.Equals(
					string.IsNullOrWhiteSpace(x.item.ProductName)
						? x.item.Category
						: x.item.ProductName,
					leadingProduct.Name,
					StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (!matchingItems.Any())
			{
				return null;
			}

			var storeGroups = matchingItems
				.GroupBy(x => string.IsNullOrWhiteSpace(x.incident.StoreName) ? "Unassigned Site" : x.incident.StoreName!)
				.Select(g => new
				{
					Store = g.Key,
					Quantity = g.Sum(x => x.item.Quantity)
				})
				.OrderByDescending(g => g.Quantity)
				.FirstOrDefault();

			var timeBuckets = matchingItems
				.GroupBy(x =>
				{
					if (string.IsNullOrWhiteSpace(x.incident.TimeOfIncident) ||
						!TimeSpan.TryParse(x.incident.TimeOfIncident, out var time))
					{
						return "Unknown";
					}

					return time.Hours switch
					{
						>= 0 and < 6 => "Overnight",
						>= 6 and < 12 => "Morning",
						>= 12 and < 18 => "Afternoon",
						_ => "Evening"
					};
				})
				.Select(g => new
				{
					Bucket = g.Key,
					Quantity = g.Sum(x => x.item.Quantity)
				})
				.OrderByDescending(g => g.Quantity)
				.FirstOrDefault();

			var sampleItem = matchingItems.First().item;

			return new CrimeInsightHotProductDto
			{
				ProductName = leadingProduct.Name,
				Category = sampleItem.Category,
				Quantity = leadingProduct.Count,
				TotalValue = leadingProduct.Value ?? 0,
				MostTargetedStore = storeGroups?.Store,
				TypicalTime = timeBuckets?.Bucket
			};
		}

		#endregion

		#region Classification helpers

		/// <summary>
		/// Risk-relevant fields used to decide whether re-classification is needed on update.
		/// Uses a record so equality is structural.
		/// </summary>
		private sealed record ClassificationFingerprint(
			string IncidentType,
			string? Description,
			string? IncidentDetails,
			bool PoliceInvolvement,
			decimal? TotalLostValue,
			decimal? TotalValueRecovered,
			int StolenItemCount,
			bool HasOffenderName);

		private static ClassificationFingerprint SnapshotForClassification(Incident incident) => new(
			incident.IncidentType ?? string.Empty,
			incident.Description,
			incident.IncidentDetails,
			incident.PoliceInvolvement,
			incident.TotalLostValue,
			incident.TotalValueRecovered,
			incident.StolenItems?.Count ?? 0,
			!string.IsNullOrWhiteSpace(incident.OffenderName));

		/// <summary>
		/// Synchronously classifies the in-memory incident and applies the AI-derived
		/// fields directly on the entity. Called by Create/Update before persistence
		/// so the single INSERT/UPDATE already contains the AI insight values and
		/// the API response returns up-to-date data.
		///
		/// Race semantics: the configured classifier (Azure OpenAI when enabled) is
		/// given <see cref="InlineClassificationTimeout"/> to respond. If it does
		/// not respond in time, the deterministic rule-based classifier runs inline
		/// as a fast, always-available fallback and the result is tagged
		/// <c>"rule-based-fallback (inline-timeout)"</c> so the periodic backfill
		/// can later re-classify with the LLM once it recovers.
		/// </summary>
		private async Task ApplyClassificationInlineAsync(Incident incident)
		{
			var request = new IncidentClassificationRequestDto
			{
				IncidentId = incident.IncidentId,
				IncidentType = incident.IncidentType ?? string.Empty,
				Description = incident.Description,
				IncidentDetails = incident.IncidentDetails,
				TotalValueRecovered = incident.TotalValueRecovered,
				TotalLostValue = incident.TotalLostValue,
				PoliceInvolvement = incident.PoliceInvolvement,
				OffenderName = incident.OffenderName,
				StolenItemCount = incident.StolenItems?.Count ?? 0
			};

			IncidentClassificationResultDto classification;
			try
			{
				var classifyTask = _classifier.ClassifyAsync(request);
				var timeoutTask = Task.Delay(InlineClassificationTimeout);
				var winner = await Task.WhenAny(classifyTask, timeoutTask);

				if (winner == classifyTask)
				{
					classification = await classifyTask;
				}
				else
				{
					_logger.LogWarning(
						"Inline classifier exceeded {TimeoutMs}ms for incident {IncidentId}; using rule-based fallback",
						InlineClassificationTimeout.TotalMilliseconds,
						incident.IncidentId);

					classification = await _ruleBasedFallback.ClassifyAsync(request);
					classification.ClassifierVersion = "rule-based-fallback (inline-timeout)";

					// Observe the orphaned task so a late failure does not surface as
					// an unobserved task exception in process logs.
					_ = classifyTask.ContinueWith(
						t => _logger.LogWarning(
							t.Exception,
							"Inline classifier task faulted after fallback was applied for incident {IncidentId}",
							incident.IncidentId),
						TaskContinuationOptions.OnlyOnFaulted);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Inline classification failed for incident {IncidentId}; using rule-based fallback",
					incident.IncidentId);
				classification = await _ruleBasedFallback.ClassifyAsync(request);
				classification.ClassifierVersion = "rule-based-fallback (inline-error)";
			}

			incident.IncidentCategory = classification.SuggestedCategory;
			incident.IncidentCategoryConfidence = classification.Confidence;
			incident.RiskLevel = classification.RiskLevel;
			incident.RiskScore = classification.RiskScore;
			incident.ClassificationVersion = classification.ClassifierVersion;

			if (string.IsNullOrWhiteSpace(incident.Priority))
			{
				incident.Priority = classification.RiskLevel;
			}
		}

		#endregion
	}
}

