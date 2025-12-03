#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AIPBackend.Services
{
	public class HolidayRequestService : IHolidayRequestService
	{
		private readonly IHolidayRequestRepository _repository;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IHolidayEmailService _emailService;
		private readonly ILogger<HolidayRequestService> _logger;

		public HolidayRequestService(
			IHolidayRequestRepository repository,
			UserManager<ApplicationUser> userManager,
			IHolidayEmailService emailService,
			ILogger<HolidayRequestService> logger)
		{
			_repository = repository;
			_userManager = userManager;
			_emailService = emailService;
			_logger = logger;
		}

		public async Task<AIPBackend.Models.DTOs.HolidayRequestDto> GetByIdAsync(int id)
		{
			try
			{
				var holidayRequest = await _repository.GetByIdAsync(id);
				if (holidayRequest == null)
				{
					throw new KeyNotFoundException($"Holiday request with ID {id} not found.");
				}

				return MapToDto(holidayRequest);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting holiday request with ID {Id}", id);
				throw;
			}
		}

		public async Task<HolidayRequestListResponseDto> GetAllAsync(
			string? search = null,
			string? status = null,
			bool? archived = null,
			int page = 1,
			int limit = 10)
		{
			try
			{
				var holidayRequests = await _repository.GetFilteredAsync(
					search: search,
					status: status,
					archived: archived,
					page: page,
					limit: limit);

				var total = await _repository.GetTotalCountAsync(
					search: search,
					status: status,
					archived: archived);

				return new HolidayRequestListResponseDto
				{
					Data = holidayRequests.Select(MapToDto).ToList(),
					Total = total,
					Page = page,
					Limit = limit
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting holiday requests");
				throw;
			}
		}

		public async Task<AIPBackend.Models.DTOs.HolidayRequestDto> CreateAsync(CreateHolidayRequestDto request, string currentUserId)
		{
			try
			{
				// Note: EmployeeId validation removed (Employee model deleted)
				// OfficerId is stored as-is in HolidayRequest

				// Parse dates
				if (!DateTime.TryParse(request.StartDate, out var startDate))
				{
					throw new ArgumentException("Invalid StartDate format.");
				}

				if (!DateTime.TryParse(request.EndDate, out var endDate))
				{
					throw new ArgumentException("Invalid EndDate format.");
				}

				if (!DateTime.TryParse(request.ReturnToWorkDate, out var returnToWorkDate))
				{
					throw new ArgumentException("Invalid ReturnToWorkDate format.");
				}

				// Validate dates
				if (endDate < startDate)
				{
					throw new ArgumentException("EndDate cannot be before StartDate.");
				}

				if (returnToWorkDate <= endDate)
				{
					throw new ArgumentException("ReturnToWorkDate must be after EndDate.");
				}

				// Calculate total days (business days)
				var totalDays = CalculateBusinessDays(startDate, endDate);

				// Validate AuthorisedBy if provided
				string? authorisedBy = null;
				if (!string.IsNullOrWhiteSpace(request.AuthorisedBy))
				{
					var user = await _userManager.FindByIdAsync(request.AuthorisedBy);
					if (user == null)
					{
						throw new ArgumentException($"User with ID '{request.AuthorisedBy}' not found. Cannot set AuthorisedBy.");
					}
					authorisedBy = request.AuthorisedBy;
				}

				var holidayRequest = new HolidayRequest
				{
					EmployeeId = request.OfficerId,
					StartDate = startDate,
					EndDate = endDate,
					ReturnToWorkDate = returnToWorkDate,
					DateOfRequest = DateTime.UtcNow,
					AuthorisedBy = authorisedBy,
					DateAuthorised = null,
					Status = "pending",
					Comment = request.Comment,
					TotalDays = totalDays,
					Archived = false,
					CreatedAt = DateTime.UtcNow,
					CreatedBy = currentUserId,
					UpdatedAt = null,
					UpdatedBy = null
				};

				var created = await _repository.CreateAsync(holidayRequest);

				// Send email notification to manager (if manager email can be determined)
				try
				{
					// Try to get manager email - for now, we'll skip if not available
					// In a full implementation, you'd get this from user's manager relationship
					_logger.LogInformation("Holiday request created for user with EmployeeId {EmployeeId}. Email notification skipped (manager email not configured).", request.OfficerId);
				}
				catch (Exception emailEx)
				{
					// Log but don't fail the creation if email fails
					_logger.LogWarning(emailEx, "Failed to send email notification for new holiday request");
				}

				return MapToDto(created);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating holiday request");
				throw;
			}
		}

		public async Task<AIPBackend.Models.DTOs.HolidayRequestDto> UpdateAsync(int id, UpdateHolidayRequestDto request, string currentUserId)
		{
			try
			{
				var holidayRequest = await _repository.GetByIdAsync(id)
					?? throw new KeyNotFoundException($"Holiday request with ID {id} not found.");

				// Update employee if provided
				if (request.OfficerId.HasValue)
				{
					holidayRequest.EmployeeId = request.OfficerId.Value; // Store as-is (legacy EmployeeId field)
				}

				// Update dates if provided
				if (!string.IsNullOrWhiteSpace(request.StartDate) && DateTime.TryParse(request.StartDate, out var startDate))
				{
					holidayRequest.StartDate = startDate;
				}

				if (!string.IsNullOrWhiteSpace(request.EndDate) && DateTime.TryParse(request.EndDate, out var endDate))
				{
					holidayRequest.EndDate = endDate;
				}

				if (!string.IsNullOrWhiteSpace(request.ReturnToWorkDate) && DateTime.TryParse(request.ReturnToWorkDate, out var returnToWorkDate))
				{
					holidayRequest.ReturnToWorkDate = returnToWorkDate;
				}

				// Track status change for email notification
				var oldStatus = holidayRequest.Status.ToLower();
				var statusChanged = false;
				var newStatus = oldStatus;

				// Update status and handle date authorized
				if (!string.IsNullOrWhiteSpace(request.Status))
				{
					newStatus = request.Status.ToLower();
					statusChanged = oldStatus != newStatus;
					holidayRequest.Status = newStatus;
					
					// Auto-set date authorized and authorisedBy if status changed to approved/denied
					if ((newStatus == "approved" || newStatus == "denied") && statusChanged)
					{
						if (holidayRequest.DateAuthorised == null)
						{
							holidayRequest.DateAuthorised = DateTime.UtcNow;
						}
						// Set the current user as the authorizer
						holidayRequest.AuthorisedBy = currentUserId;
					}
				}

				if (!string.IsNullOrWhiteSpace(request.DateAuthorised) && DateTime.TryParse(request.DateAuthorised, out var dateAuthorised))
				{
					holidayRequest.DateAuthorised = dateAuthorised;
				}

				// Update AuthorisedBy if explicitly provided (validate user exists)
				// Note: This allows override, but typically AuthorisedBy is set automatically above
				if (!string.IsNullOrWhiteSpace(request.AuthorisedBy))
				{
					var user = await _userManager.FindByIdAsync(request.AuthorisedBy);
					if (user == null)
					{
						throw new ArgumentException($"User with ID '{request.AuthorisedBy}' not found. Cannot set AuthorisedBy.");
					}
					holidayRequest.AuthorisedBy = request.AuthorisedBy;
				}

				if (request.Comment != null)
				{
					holidayRequest.Comment = request.Comment;
				}

				if (request.Reason != null)
				{
					holidayRequest.Reason = request.Reason;
				}

				if (request.DaysLeftYTD.HasValue)
				{
					holidayRequest.DaysLeftYTD = request.DaysLeftYTD.Value;
				}

				// Recalculate total days
				holidayRequest.TotalDays = CalculateBusinessDays(holidayRequest.StartDate, holidayRequest.EndDate);
				holidayRequest.UpdatedAt = DateTime.UtcNow;
				holidayRequest.UpdatedBy = currentUserId;

				var updated = await _repository.UpdateAsync(holidayRequest);

				// Send email notification if status changed to approved/denied
				if (statusChanged && (newStatus == "approved" || newStatus == "denied"))
				{
					try
					{
						// Try to find user by EmployeeId (legacy field)
						var allUsers = _userManager.Users.ToList();
						var user = allUsers.FirstOrDefault(u => u.EmployeeId == holidayRequest.EmployeeId);
						
						if (user != null && !string.IsNullOrWhiteSpace(user.Email))
						{
							var authorizer = await _userManager.FindByIdAsync(currentUserId);
							var authorizerName = authorizer?.FullName ?? authorizer?.Email ?? "Administrator";

							if (newStatus == "approved")
							{
								// Email sending temporarily disabled - EmployeeDetailResponseDto removed
								_logger.LogInformation("Holiday request approved for user {UserId} (email notification skipped)", user.Id);
							}
							else if (newStatus == "denied")
							{
								// Email sending temporarily disabled - EmployeeDetailResponseDto removed
								_logger.LogInformation("Holiday request denied for user {UserId} (email notification skipped)", user.Id);
							}
						}
					}
					catch (Exception emailEx)
					{
						// Log but don't fail the update if email fails
						_logger.LogWarning(emailEx, "Failed to send email notification for holiday request {Id}", id);
					}
				}

				return MapToDto(updated);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating holiday request with ID {Id}", id);
				throw;
			}
		}

		public async Task<bool> DeleteAsync(int id)
		{
			try
			{
				return await _repository.DeleteAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting holiday request with ID {Id}", id);
				throw;
			}
		}

		/// <summary>
		/// Sets the archived status of a holiday request
		/// </summary>
		private async Task<AIPBackend.Models.DTOs.HolidayRequestDto> SetArchiveStatusAsync(int id, bool archived, string currentUserId)
		{
			var holidayRequest = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Holiday request with ID {id} not found.");

			holidayRequest.Archived = archived;
			holidayRequest.UpdatedAt = DateTime.UtcNow;
			holidayRequest.UpdatedBy = currentUserId;

			var updated = await _repository.UpdateAsync(holidayRequest);
			return MapToDto(updated);
		}

		public async Task<AIPBackend.Models.DTOs.HolidayRequestDto> ArchiveAsync(int id, string currentUserId)
		{
			try
			{
				return await SetArchiveStatusAsync(id, true, currentUserId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error archiving holiday request with ID {Id}", id);
				throw;
			}
		}

		public async Task<AIPBackend.Models.DTOs.HolidayRequestDto> UnarchiveAsync(int id, string currentUserId)
		{
			try
			{
				return await SetArchiveStatusAsync(id, false, currentUserId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error unarchiving holiday request with ID {Id}", id);
				throw;
			}
		}

		private static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
		{
			var count = 0;
			var currentDate = startDate.Date;

			while (currentDate <= endDate.Date)
			{
				// Count Monday through Friday as business days
				if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
				{
					count++;
				}
				currentDate = currentDate.AddDays(1);
			}

			return count;
		}

	/// <summary>
	/// Maps a HolidayRequest entity to a HolidayRequestDto
	/// </summary>
	private AIPBackend.Models.DTOs.HolidayRequestDto MapToDto(HolidayRequest holidayRequest)
	{
		// Note: Employee navigation removed - using EmployeeId field directly
		// User lookup would need to be done separately if needed
		var officerName = $"User #{holidayRequest.EmployeeId}";

		// Get authorizer name from navigation property if available
		var authorisedByName = string.Empty;
		if (holidayRequest.AuthorisedByUser != null)
		{
			authorisedByName = !string.IsNullOrWhiteSpace(holidayRequest.AuthorisedByUser.FullName)
				? holidayRequest.AuthorisedByUser.FullName
				: holidayRequest.AuthorisedByUser.Email ?? string.Empty;
		}
		else if (!string.IsNullOrWhiteSpace(holidayRequest.AuthorisedBy))
		{
			// Fallback to ID if user not loaded
			authorisedByName = holidayRequest.AuthorisedBy;
		}

		return new AIPBackend.Models.DTOs.HolidayRequestDto
		{
			Id = holidayRequest.Id.ToString(),
			EmployeeId = holidayRequest.EmployeeId,
			OfficerId = holidayRequest.EmployeeId.ToString(),
			OfficerName = officerName,
			StartDate = holidayRequest.StartDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			EndDate = holidayRequest.EndDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			ReturnToWorkDate = holidayRequest.ReturnToWorkDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			DateOfRequest = holidayRequest.DateOfRequest.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			AuthorisedBy = authorisedByName, // Return name instead of ID
			DateAuthorised = holidayRequest.DateAuthorised?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			Status = holidayRequest.Status,
			Comment = holidayRequest.Comment,
			Reason = holidayRequest.Reason,
			TotalDays = holidayRequest.TotalDays,
			DaysLeftYTD = holidayRequest.DaysLeftYTD,
			Archived = holidayRequest.Archived,
			CreatedAt = holidayRequest.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			CreatedBy = holidayRequest.CreatedBy,
			UpdatedAt = holidayRequest.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
			UpdatedBy = holidayRequest.UpdatedBy
		};
	}
	}
}

