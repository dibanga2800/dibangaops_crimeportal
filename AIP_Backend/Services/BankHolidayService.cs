using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	public class BankHolidayService : IBankHolidayService
	{
		private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
		{
			"pending",
			"authorized",
			"declined"
		};

		private readonly IBankHolidayRepository _repository;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<BankHolidayService> _logger;
		private readonly IEmailService _emailService;

		public BankHolidayService(
			IBankHolidayRepository repository,
			UserManager<ApplicationUser> userManager,
			ILogger<BankHolidayService> logger,
			IEmailService emailService)
		{
			_repository = repository;
			_userManager = userManager;
			_logger = logger;
			_emailService = emailService;
		}

		public async Task<BankHolidayListResponseDto> GetPagedAsync(string? search, string? status, bool? archived, int page, int limit)
		{
			var (holidays, total) = await _repository.GetPagedAsync(search, status, archived, page, limit);
			var totalPages = (int)Math.Ceiling(total / (double)limit);

			var dtos = new List<BankHolidayDto>();
			foreach (var holiday in holidays)
			{
				dtos.Add(await MapToDtoAsync(holiday));
			}

			return new BankHolidayListResponseDto
			{
				Data = dtos,
				Total = total,
				Page = page,
				Limit = limit,
				TotalPages = totalPages
			};
		}

		public async Task<BankHolidayDto> GetByIdAsync(int id)
		{
			var holiday = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Bank holiday with ID {id} not found.");

			return await MapToDtoAsync(holiday);
		}

		public async Task<BankHolidayDto> CreateAsync(CreateBankHolidayDto dto, string currentUserId)
		{
			// Note: OfficerId is stored as int (legacy from Employee model)
			// For now, we'll use it directly - proper user lookup can be added later
			if (!DateTime.TryParse(dto.HolidayDate, out var holidayDate))
			{
				throw new ArgumentException("Invalid holiday date supplied.");
			}

			var bankHoliday = new BankHoliday
			{
				OfficerId = dto.OfficerId, // Store as-is (legacy int field)
				HolidayDate = holidayDate,
				DateOfRequest = DateTime.UtcNow,
				Reason = dto.Reason,
				Status = "pending",
				Archived = false,
				CreatedAt = DateTime.UtcNow,
				CreatedBy = currentUserId
			};

			var created = await _repository.CreateAsync(bankHoliday);
			return await MapToDtoAsync(created);
		}

		public async Task<BankHolidayDto> UpdateAsync(int id, UpdateBankHolidayDto dto, string currentUserId)
		{
			var existing = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Bank holiday with ID {id} not found.");

			var previousStatus = existing.Status;

			if (dto.OfficerId.HasValue)
			{
				existing.OfficerId = dto.OfficerId.Value; // Store as-is (legacy int field)
			}

			if (!string.IsNullOrWhiteSpace(dto.HolidayDate))
			{
				if (!DateTime.TryParse(dto.HolidayDate, out var parsedDate))
				{
					throw new ArgumentException("Invalid holiday date supplied.");
				}

				existing.HolidayDate = parsedDate;
			}

			if (!string.IsNullOrWhiteSpace(dto.Status))
			{
				var normalizedStatus = NormalizeStatus(dto.Status);
				ValidateStatus(normalizedStatus);
				existing.Status = normalizedStatus;
			}

			if (dto.AuthorisedByEmployeeId.HasValue)
			{
				existing.AuthorisedByEmployeeId = dto.AuthorisedByEmployeeId.Value; // Store as-is (legacy int field)
			}

			if (!string.IsNullOrWhiteSpace(dto.DateAuthorised))
			{
				if (!DateTime.TryParse(dto.DateAuthorised, out var parsedAuthorisedDate))
				{
					throw new ArgumentException("Invalid date authorised supplied.");
				}

				existing.DateAuthorised = parsedAuthorisedDate;
			}

			if (dto.Reason != null)
			{
				existing.Reason = dto.Reason;
			}

			if (dto.Archived.HasValue)
			{
				existing.Archived = dto.Archived.Value;
			}

			EnsureApprovalConsistency(existing);

			existing.UpdatedAt = DateTime.UtcNow;
			existing.UpdatedBy = currentUserId;

			var statusChangedToAuthorized = !previousStatus.Equals("authorized", StringComparison.OrdinalIgnoreCase)
				&& existing.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase);

			var updated = await _repository.UpdateAsync(existing);

			if (statusChangedToAuthorized)
			{
				await SendApprovalEmailAsync(updated);
			}

			return await MapToDtoAsync(updated);
		}

		public async Task<bool> DeleteAsync(int id)
		{
			return await _repository.DeleteAsync(id);
		}

		public async Task<BankHolidayDto> ArchiveAsync(int id, string currentUserId)
		{
			return await SetArchiveStatusAsync(id, true, currentUserId);
		}

		public async Task<BankHolidayDto> UnarchiveAsync(int id, string currentUserId)
		{
			return await SetArchiveStatusAsync(id, false, currentUserId);
		}

		private async Task<BankHolidayDto> SetArchiveStatusAsync(int id, bool archived, string currentUserId)
		{
			var existing = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Bank holiday with ID {id} not found.");

			existing.Archived = archived;
			existing.UpdatedAt = DateTime.UtcNow;
			existing.UpdatedBy = currentUserId;

			var updated = await _repository.UpdateAsync(existing);
			return await MapToDtoAsync(updated);
		}

		private static string NormalizeStatus(string status)
		{
			return status.Trim().ToLower() switch
			{
				"approved" => "authorized",
				"denied" => "declined",
				_ => status.Trim().ToLower()
			};
		}

		private static void ValidateStatus(string status)
		{
			if (!AllowedStatuses.Contains(status))
			{
				throw new ArgumentException($"Invalid status '{status}'. Allowed values: pending, authorized, declined.");
			}
		}

		private static void EnsureApprovalConsistency(BankHoliday holiday)
		{
			var isApprovedState = holiday.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase)
				|| holiday.Status.Equals("declined", StringComparison.OrdinalIgnoreCase);

			if (isApprovedState)
			{
				if (holiday.AuthorisedByEmployeeId == null)
				{
					throw new ArgumentException("AuthorisedByEmployeeId is required when approving or declining a bank holiday.");
				}

				if (!holiday.DateAuthorised.HasValue)
				{
					holiday.DateAuthorised = DateTime.UtcNow;
				}
			}
			else
			{
				holiday.DateAuthorised = null;
				holiday.AuthorisedByEmployeeId = null;
			}
		}

		private Task<BankHolidayDto> MapToDtoAsync(BankHoliday bankHoliday)
		{
			// Try to find user by EmployeeId field (legacy mapping)
			ApplicationUser? officer = null;
			if (bankHoliday.OfficerId > 0)
			{
				// Try to find user where EmployeeId matches (legacy field)
				var allUsers = _userManager.Users.ToList();
				officer = allUsers.FirstOrDefault(u => u.EmployeeId == bankHoliday.OfficerId);
			}
			
			var officerFullName = officer != null
				? $"{officer.FirstName} {officer.LastName}".Trim()
				: $"Officer #{bankHoliday.OfficerId}";

			string? managerFullName = null;
			if (bankHoliday.AuthorisedByEmployeeId.HasValue)
			{
				var allUsers = _userManager.Users.ToList();
				var manager = allUsers.FirstOrDefault(u => u.EmployeeId == bankHoliday.AuthorisedByEmployeeId.Value);
				managerFullName = manager != null
					? $"{manager.FirstName} {manager.LastName}".Trim()
					: null;
			}

			var dto = new BankHolidayDto
			{
				Id = bankHoliday.Id.ToString(),
				OfficerId = bankHoliday.OfficerId,
				OfficerName = officerFullName,
				OfficerNumber = string.Empty, // Employee number removed
				HolidayDate = bankHoliday.HolidayDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
				DateOfRequest = bankHoliday.DateOfRequest.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
				AuthorisedByEmployeeId = bankHoliday.AuthorisedByEmployeeId,
				AuthorisedByName = managerFullName,
				DateAuthorised = bankHoliday.DateAuthorised?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
				Status = bankHoliday.Status,
				Reason = bankHoliday.Reason,
				Archived = bankHoliday.Archived,
				CreatedAt = bankHoliday.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
				UpdatedAt = bankHoliday.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
			};

			return Task.FromResult(dto);
		}

		private async Task SendApprovalEmailAsync(BankHoliday holiday)
		{
			try
			{
				// Try to find user by EmployeeId (legacy field)
				var allUsers = _userManager.Users.ToList();
				var officer = allUsers.FirstOrDefault(u => u.EmployeeId == holiday.OfficerId);
				
				if (officer == null || string.IsNullOrWhiteSpace(officer.Email))
				{
					_logger.LogWarning("Cannot send bank holiday approval email because officer email is missing. OfficerId: {OfficerId}", holiday.OfficerId);
					return;
				}

				string approverName = "Manager";
				if (holiday.AuthorisedByEmployeeId.HasValue)
				{
					var approver = allUsers.FirstOrDefault(u => u.EmployeeId == holiday.AuthorisedByEmployeeId.Value);
					if (approver != null)
					{
						approverName = $"{approver.FirstName} {approver.LastName}".Trim();
					}
				}

				var subject = $"Bank Holiday Approved - {holiday.HolidayDate:dd MMM yyyy}";
				var body = BuildApprovalEmailBody(officer, holiday, approverName);

				var sent = await _emailService.SendEmailAsync(officer.Email, subject, body);
				if (sent)
				{
					_logger.LogInformation("Bank holiday approval email sent to {Email} for BankHolidayId {Id}", officer.Email, holiday.Id);
				}
				else
				{
					_logger.LogWarning("Bank holiday approval email failed to send to {Email} for BankHolidayId {Id}", officer.Email, holiday.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while sending bank holiday approval email for BankHolidayId {Id}", holiday.Id);
			}
		}

		private static string BuildApprovalEmailBody(ApplicationUser officer, BankHoliday holiday, string approverName)
		{
			var officerName = $"{officer.FirstName} {officer.LastName}".Trim();
			var reason = string.IsNullOrWhiteSpace(holiday.Reason) ? "No additional notes were provided." : holiday.Reason;

			return $@"
<html>
	<body style=""font-family: Arial, sans-serif;"">
		<h2 style=""color:#0f172a;"">Your Bank Holiday Has Been Approved</h2>
		<p>Hi {officerName},</p>
		<p>Your bank holiday request for <strong>{holiday.HolidayDate:dddd, dd MMMM yyyy}</strong> has been approved by <strong>{approverName}</strong>.</p>
		<p><strong>Manager notes:</strong><br/>{reason}</p>
		<p style=""margin-top:16px;"">
			If you have any questions, please contact your manager directly.
		</p>
		<p>Thank you,<br/>Crime Portal</p>
	</body>
</html>";
		}
	}
}

