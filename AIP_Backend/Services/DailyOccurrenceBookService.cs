#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service implementation for Daily Occurrence Book operations
	/// </summary>
	public class DailyOccurrenceBookService : IDailyOccurrenceBookService
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<DailyOccurrenceBookService> _logger;
		private static readonly IReadOnlyDictionary<string, string> _codeDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["A"] = "Arrest",
			["B"] = "Deter",
			["C"] = "Theft",
			["D"] = "Violent Behaviour",
			["E"] = "Abusive Behaviour",
			["F"] = "Ban from Store",
			["G"] = "Criminal Damage",
			["H"] = "Underage Purchase",
			["J"] = "Credit Card Fraud",
			["K"] = "Anti-Social Behaviour",
			["L"] = "Suspicious Behaviour",
			["M"] = "Other"
		};

		public DailyOccurrenceBookService(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			ILogger<DailyOccurrenceBookService> logger)
		{
			_context = context;
			_userManager = userManager;
			_logger = logger;
		}

		public async Task<DailyOccurrenceBookListResponseDto> GetOccurrencesAsync(int customerId, DailyOccurrenceBookFilterDto? filters = null)
		{
			try
			{
				_logger.LogInformation("Fetching occurrences for customer {CustomerId}", customerId);

				var query = _context.DailyOccurrenceBooks
					.Where(dob => dob.CustomerId == customerId)
					.Include(dob => dob.ReportedByUser)
					.Include(dob => dob.Customer)
					.AsQueryable();

				// Apply filters
				if (filters != null)
				{
					if (!string.IsNullOrWhiteSpace(filters.SiteId))
					{
						query = query.Where(dob => dob.SiteId == filters.SiteId);
					}

					if (!string.IsNullOrWhiteSpace(filters.DateFrom) && DateTime.TryParse(filters.DateFrom, out var dateFrom))
					{
						query = query.Where(dob => dob.OccurrenceDate >= dateFrom);
					}

					if (!string.IsNullOrWhiteSpace(filters.DateTo) && DateTime.TryParse(filters.DateTo, out var dateTo))
					{
						query = query.Where(dob => dob.OccurrenceDate <= dateTo);
					}

					if (!string.IsNullOrWhiteSpace(filters.StoreNumber))
					{
						var storeNumber = filters.StoreNumber.Trim().ToLower();
						query = query.Where(dob => dob.StoreNumber != null && dob.StoreNumber.ToLower() == storeNumber);
					}

					if (!string.IsNullOrWhiteSpace(filters.StoreName))
					{
						var storeName = filters.StoreName.Trim().ToLower();
						query = query.Where(dob => dob.StoreName != null && dob.StoreName.ToLower().Contains(storeName));
					}

					if (!string.IsNullOrWhiteSpace(filters.OfficerName))
					{
						var officerName = filters.OfficerName.Trim().ToLower();
						query = query.Where(dob => dob.OfficerName.ToLower().Contains(officerName));
					}

					if (!string.IsNullOrWhiteSpace(filters.Code))
					{
						var normalizedCodes = filters.Code.Split(',', StringSplitOptions.RemoveEmptyEntries)
							.Select(NormalizeCode)
							.ToList();

						if (normalizedCodes.Any())
						{
							query = query.Where(dob => normalizedCodes.Contains(dob.OccurrenceCode));
						}
					}

					if (!string.IsNullOrWhiteSpace(filters.ReportedBy))
					{
						query = query.Where(dob => dob.ReportedById == filters.ReportedBy);
					}

					if (!string.IsNullOrWhiteSpace(filters.Search))
					{
						var searchLower = filters.Search.ToLower();
						query = query.Where(dob =>
							(dob.StoreName ?? string.Empty).ToLower().Contains(searchLower) ||
							(dob.StoreNumber ?? string.Empty).ToLower().Contains(searchLower) ||
							dob.OfficerName.ToLower().Contains(searchLower) ||
							dob.Details.ToLower().Contains(searchLower));
					}
				}

				// Order by date and time (newest first)
				var occurrences = await query
					.OrderByDescending(dob => dob.OccurrenceDate)
					.ThenByDescending(dob => dob.OccurrenceTime)
					.ToListAsync();

				var dtos = occurrences.Select(MapToDto).ToList();

				// Calculate stats
				var stats = await CalculateStatsAsync(customerId, filters);

				_logger.LogInformation("Found {Count} occurrences for customer {CustomerId}", dtos.Count, customerId);

				return new DailyOccurrenceBookListResponseDto
				{
					Success = true,
					Data = dtos,
					Stats = stats,
					Message = $"Found {dtos.Count} occurrence(s)"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching occurrences for customer {CustomerId}", customerId);
				throw;
			}
		}

		public async Task<DailyOccurrenceBookResponseDto> GetByIdAsync(int customerId, string occurrenceId)
		{
			try
			{
				if (!int.TryParse(occurrenceId, out var id))
				{
					throw new ArgumentException("Invalid occurrence ID format", nameof(occurrenceId));
				}

				var occurrence = await _context.DailyOccurrenceBooks
					.Include(dob => dob.ReportedByUser)
					.Include(dob => dob.Customer)
					.FirstOrDefaultAsync(dob => dob.Id == id && dob.CustomerId == customerId);

				if (occurrence == null)
				{
					throw new KeyNotFoundException($"Occurrence with ID {occurrenceId} not found for customer {customerId}");
				}

				return new DailyOccurrenceBookResponseDto
				{
					Success = true,
					Data = MapToDto(occurrence),
					Message = "Occurrence retrieved successfully"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				throw;
			}
		}

		public async Task<DailyOccurrenceBookResponseDto> CreateAsync(CreateDailyOccurrenceBookRequestDto request, string currentUserId)
		{
			try
			{
				_logger.LogInformation("Creating occurrence for customer {CustomerId}", request.CustomerId);

				// Validate customer exists
				var customer = await _context.Customers.FindAsync(request.CustomerId);
				if (customer == null)
				{
					throw new ArgumentException($"Customer with ID {request.CustomerId} not found");
				}

				// Get reported by user info
				var reportedByUser = await _userManager.FindByIdAsync(currentUserId);
				if (reportedByUser == null)
				{
					throw new ArgumentException($"User with ID {currentUserId} not found");
				}

				// Get site name if siteId provided
				string? siteName = null;
				if (!string.IsNullOrWhiteSpace(request.SiteId) && int.TryParse(request.SiteId, out var siteIdInt))
				{
					var site = await _context.Sites.FindAsync(siteIdInt);
					siteName = site?.LocationName;
				}

				var normalizedCode = NormalizeCode(request.Code);
				var codeDescription = ResolveCodeDescription(normalizedCode);
				var occurrenceDate = ParseRequiredDate(request.Date, nameof(request.Date));
				var dateCommenced = ParseOptionalDate(request.DateCommenced, nameof(request.DateCommenced));
				var crimeReportCompletedAt = BuildDateTimeFromParts(
					string.IsNullOrWhiteSpace(request.CrimeReportCompletedDate) ? null : request.CrimeReportCompletedDate,
					string.IsNullOrWhiteSpace(request.CrimeReportCompletedTime) ? null : request.CrimeReportCompletedTime,
					"crime report completion");

				var occurrence = new DailyOccurrenceBook
				{
					CustomerId = request.CustomerId,
					SiteId = request.SiteId,
					SiteName = siteName,
					StoreName = !string.IsNullOrWhiteSpace(request.StoreName) ? request.StoreName.Trim() : siteName,
					StoreNumber = request.StoreNumber.Trim(),
					DateCommenced = dateCommenced,
					OccurrenceDate = occurrenceDate,
					OccurrenceTime = request.Time,
					OfficerName = request.OfficerName.Trim(),
					OccurrenceCode = normalizedCode,
					OccurrenceCodeDescription = codeDescription,
					CrimeReportCompletedAt = crimeReportCompletedAt,
					Details = request.Details,
					Signature = request.Signature.Trim(),
					ReportedById = currentUserId,
					ReportedByName = $"{reportedByUser.FirstName} {reportedByUser.LastName}".Trim(),
					ReportedByRole = reportedByUser.JobTitle ?? "User", // Use JobTitle instead of Employee.Position
					CreatedAt = DateTime.UtcNow,
					CreatedBy = currentUserId
				};

				_context.DailyOccurrenceBooks.Add(occurrence);
				await _context.SaveChangesAsync();

				_logger.LogInformation("Created occurrence {OccurrenceId} for customer {CustomerId}", occurrence.Id, request.CustomerId);

				// Reload with includes
				var created = await _context.DailyOccurrenceBooks
					.Include(dob => dob.ReportedByUser)
					.Include(dob => dob.Customer)
					.FirstOrDefaultAsync(dob => dob.Id == occurrence.Id);

				return new DailyOccurrenceBookResponseDto
				{
					Success = true,
					Data = MapToDto(created!),
					Message = "Occurrence created successfully"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating occurrence for customer {CustomerId}", request.CustomerId);
				throw;
			}
		}

		public async Task<DailyOccurrenceBookResponseDto> UpdateAsync(int customerId, string occurrenceId, UpdateDailyOccurrenceBookRequestDto request, string currentUserId)
		{
			try
			{
				if (!int.TryParse(occurrenceId, out var id))
				{
					throw new ArgumentException("Invalid occurrence ID format", nameof(occurrenceId));
				}

				var occurrence = await _context.DailyOccurrenceBooks
					.FirstOrDefaultAsync(dob => dob.Id == id && dob.CustomerId == customerId);

				if (occurrence == null)
				{
					throw new KeyNotFoundException($"Occurrence with ID {occurrenceId} not found for customer {customerId}");
				}

				_logger.LogInformation("Updating occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);

				// Update fields
				if (!string.IsNullOrWhiteSpace(request.StoreName))
				{
					occurrence.StoreName = request.StoreName.Trim();
				}

				if (!string.IsNullOrWhiteSpace(request.StoreNumber))
				{
					occurrence.StoreNumber = request.StoreNumber.Trim();
				}

				if (request.DateCommenced != null)
				{
					occurrence.DateCommenced = ParseOptionalDate(request.DateCommenced, nameof(request.DateCommenced));
				}

				if (!string.IsNullOrWhiteSpace(request.Date))
				{
					occurrence.OccurrenceDate = ParseRequiredDate(request.Date, nameof(request.Date));
				}

				if (!string.IsNullOrWhiteSpace(request.Time))
				{
					occurrence.OccurrenceTime = request.Time;
				}

				if (!string.IsNullOrWhiteSpace(request.OfficerName))
				{
					occurrence.OfficerName = request.OfficerName.Trim();
				}

				if (!string.IsNullOrWhiteSpace(request.Code))
				{
					var normalizedCode = NormalizeCode(request.Code);
					occurrence.OccurrenceCode = normalizedCode;
					occurrence.OccurrenceCodeDescription = ResolveCodeDescription(normalizedCode);
				}

				if (request.CrimeReportCompletedDate != null || request.CrimeReportCompletedTime != null)
				{
					occurrence.CrimeReportCompletedAt = BuildDateTimeFromParts(
						string.IsNullOrWhiteSpace(request.CrimeReportCompletedDate) ? null : request.CrimeReportCompletedDate,
						string.IsNullOrWhiteSpace(request.CrimeReportCompletedTime) ? null : request.CrimeReportCompletedTime,
						"crime report completion");
				}

				if (request.Details != null)
				{
					occurrence.Details = request.Details;
				}

				if (request.Signature != null)
				{
					occurrence.Signature = request.Signature;
				}

				occurrence.UpdatedAt = DateTime.UtcNow;
				occurrence.UpdatedBy = currentUserId;

				await _context.SaveChangesAsync();

				_logger.LogInformation("Updated occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);

				// Reload with includes
				var updated = await _context.DailyOccurrenceBooks
					.Include(dob => dob.ReportedByUser)
					.Include(dob => dob.Customer)
					.FirstOrDefaultAsync(dob => dob.Id == occurrence.Id);

				return new DailyOccurrenceBookResponseDto
				{
					Success = true,
					Data = MapToDto(updated!),
					Message = "Occurrence updated successfully"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				throw;
			}
		}

		public async Task<bool> DeleteAsync(int customerId, string occurrenceId, string currentUserId)
		{
			try
			{
				if (!int.TryParse(occurrenceId, out var id))
				{
					throw new ArgumentException("Invalid occurrence ID format", nameof(occurrenceId));
				}

				var occurrence = await _context.DailyOccurrenceBooks
					.FirstOrDefaultAsync(dob => dob.Id == id && dob.CustomerId == customerId);

				if (occurrence == null)
				{
					return false;
				}

				_context.DailyOccurrenceBooks.Remove(occurrence);
				await _context.SaveChangesAsync();

				_logger.LogInformation("Deleted occurrence {OccurrenceId} for customer {CustomerId} by user {UserId}", occurrenceId, customerId, currentUserId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting occurrence {OccurrenceId} for customer {CustomerId}", occurrenceId, customerId);
				throw;
			}
		}

		public async Task<DailyOccurrenceBookStatsDto> GetStatsAsync(int customerId, DailyOccurrenceBookFilterDto? filters = null)
		{
			try
			{
				return await CalculateStatsAsync(customerId, filters);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calculating stats for customer {CustomerId}", customerId);
				throw;
			}
		}

		private async Task<DailyOccurrenceBookStatsDto> CalculateStatsAsync(int customerId, DailyOccurrenceBookFilterDto? filters = null)
		{
			var query = _context.DailyOccurrenceBooks
				.Where(dob => dob.CustomerId == customerId)
				.AsQueryable();

			// Apply same filters as GetOccurrencesAsync
			if (filters != null)
			{
				if (!string.IsNullOrWhiteSpace(filters.SiteId))
				{
					query = query.Where(dob => dob.SiteId == filters.SiteId);
				}

				if (!string.IsNullOrWhiteSpace(filters.DateFrom) && DateTime.TryParse(filters.DateFrom, out var dateFrom))
				{
					query = query.Where(dob => dob.OccurrenceDate >= dateFrom);
				}

				if (!string.IsNullOrWhiteSpace(filters.DateTo) && DateTime.TryParse(filters.DateTo, out var dateTo))
				{
					query = query.Where(dob => dob.OccurrenceDate <= dateTo);
				}

				if (!string.IsNullOrWhiteSpace(filters.StoreNumber))
				{
					var storeNumber = filters.StoreNumber.Trim().ToLower();
					query = query.Where(dob => dob.StoreNumber != null && dob.StoreNumber.ToLower() == storeNumber);
				}

				if (!string.IsNullOrWhiteSpace(filters.StoreName))
				{
					var storeName = filters.StoreName.Trim().ToLower();
					query = query.Where(dob => dob.StoreName != null && dob.StoreName.ToLower().Contains(storeName));
				}

				if (!string.IsNullOrWhiteSpace(filters.OfficerName))
				{
					var officerName = filters.OfficerName.Trim().ToLower();
					query = query.Where(dob => dob.OfficerName.ToLower().Contains(officerName));
				}

				if (!string.IsNullOrWhiteSpace(filters.Code))
				{
					var normalizedCodes = filters.Code.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(NormalizeCode)
						.ToList();

					if (normalizedCodes.Any())
					{
						query = query.Where(dob => normalizedCodes.Contains(dob.OccurrenceCode));
					}
				}

				if (!string.IsNullOrWhiteSpace(filters.ReportedBy))
				{
					query = query.Where(dob => dob.ReportedById == filters.ReportedBy);
				}

				if (!string.IsNullOrWhiteSpace(filters.Search))
				{
					var searchLower = filters.Search.ToLower();
					query = query.Where(dob =>
						(dob.StoreName ?? string.Empty).ToLower().Contains(searchLower) ||
						(dob.StoreNumber ?? string.Empty).ToLower().Contains(searchLower) ||
						dob.OfficerName.ToLower().Contains(searchLower) ||
						dob.Details.ToLower().Contains(searchLower));
				}
			}

			var allOccurrences = await query.ToListAsync();
			var now = DateTime.UtcNow;
			var weekStart = now.AddDays(-(int)now.DayOfWeek);
			var monthStart = new DateTime(now.Year, now.Month, 1);

			var stats = new DailyOccurrenceBookStatsDto
			{
				TotalEntries = allOccurrences.Count,
				EntriesThisWeek = allOccurrences.Count(o => o.OccurrenceDate >= weekStart),
				EntriesThisMonth = allOccurrences.Count(o => o.OccurrenceDate >= monthStart),
				ByCode = allOccurrences
					.GroupBy(o => o.OccurrenceCode)
					.ToDictionary(g => g.Key, g => g.Count()),
				ByStore = allOccurrences
					.Where(o => !string.IsNullOrWhiteSpace(o.StoreNumber))
					.GroupBy(o => o.StoreNumber!)
					.ToDictionary(g => g.Key, g => g.Count())
			};

			return stats;
		}

		private DailyOccurrenceBookDto MapToDto(DailyOccurrenceBook occurrence)
		{
			var codeDescription = occurrence.OccurrenceCodeDescription;
			if (string.IsNullOrWhiteSpace(codeDescription) && _codeDescriptions.TryGetValue(occurrence.OccurrenceCode, out var derivedDescription))
			{
				codeDescription = derivedDescription;
			}

			var dto = new DailyOccurrenceBookDto
			{
				Id = occurrence.Id.ToString(),
				CustomerId = occurrence.CustomerId,
				SiteId = occurrence.SiteId,
				SiteName = occurrence.SiteName,
				StoreName = occurrence.StoreName,
				StoreNumber = occurrence.StoreNumber,
				DateCommenced = occurrence.DateCommenced?.ToString("yyyy-MM-dd"),
				Date = occurrence.OccurrenceDate.ToString("yyyy-MM-dd"),
				Time = occurrence.OccurrenceTime,
				OfficerName = occurrence.OfficerName,
				Code = occurrence.OccurrenceCode,
				CodeDescription = codeDescription ?? "Unspecified",
				CrimeReportCompletedDate = occurrence.CrimeReportCompletedAt?.ToString("yyyy-MM-dd"),
				CrimeReportCompletedTime = occurrence.CrimeReportCompletedAt?.ToString("HH:mm"),
				Details = occurrence.Details,
				Signature = occurrence.Signature,
				ReportedBy = new ReportedByDto
				{
					Id = occurrence.ReportedById,
					Name = occurrence.ReportedByName ?? "Unknown",
					Role = occurrence.ReportedByRole ?? "User",
					BadgeNumber = occurrence.ReportedByBadgeNumber
				},
				CreatedAt = occurrence.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				UpdatedAt = occurrence.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				CreatedBy = occurrence.CreatedBy,
				UpdatedBy = occurrence.UpdatedBy
			};

			return dto;
		}

		private static string NormalizeCode(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				throw new ArgumentException("Code is required", nameof(code));
			}

			return code.Trim().ToUpperInvariant();
		}

		private static string ResolveCodeDescription(string code)
		{
			var normalized = NormalizeCode(code);
			if (_codeDescriptions.TryGetValue(normalized, out var description))
			{
				return description;
			}

			throw new ArgumentException($"Unsupported code '{code}'. Valid codes: {string.Join(", ", _codeDescriptions.Keys)}", nameof(code));
		}

		private static DateTime ParseRequiredDate(string date, string propertyName)
		{
			if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
			{
				throw new ArgumentException($"Invalid date format for {propertyName}", propertyName);
			}

			return parsed;
		}

		private static DateTime? ParseOptionalDate(string? date, string propertyName)
		{
			if (string.IsNullOrWhiteSpace(date))
			{
				return null;
			}

			if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
			{
				throw new ArgumentException($"Invalid date format for {propertyName}", propertyName);
			}

			return parsed;
		}

		private static DateTime? BuildDateTimeFromParts(string? date, string? time, string contextName)
		{
			if (string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(time))
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
			{
				throw new ArgumentException($"Both date and time are required for {contextName}", contextName);
			}

			var dateTimeInput = $"{date} {time}";

			if (!DateTime.TryParse(dateTimeInput, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
			{
				throw new ArgumentException($"Invalid date/time combination for {contextName}", contextName);
			}

			return parsed;
		}
	}
}

