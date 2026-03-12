#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	public class AlertEscalationService : IAlertEscalationService
	{
		private readonly ApplicationDbContext _context;
		private readonly IEmailService _emailService;
		private readonly ILogger<AlertEscalationService> _logger;

		public AlertEscalationService(
			ApplicationDbContext context,
			IEmailService emailService,
			ILogger<AlertEscalationService> logger)
		{
			_context = context;
			_emailService = emailService;
			_logger = logger;
		}

		public async Task<AlertInstanceDto> CreateAlertAsync(
			int alertRuleId, int? incidentId, string severity, string message, string? matchDetails = null)
		{
			var alert = new AlertInstance
			{
				AlertRuleId = alertRuleId,
				IncidentId = incidentId,
				Severity = severity,
				Status = "new",
				Message = message,
				MatchDetails = matchDetails,
				CreatedAt = DateTime.UtcNow,
				EscalationLevel = 0
			};

			_context.Set<AlertInstance>().Add(alert);
			await _context.SaveChangesAsync();

			_logger.LogInformation("Alert instance {AlertId} created for rule {RuleId}, severity={Severity}",
				alert.AlertInstanceId, alertRuleId, severity);

			return await GetByIdAsync(alert.AlertInstanceId);
		}

		public async Task<AlertInstanceListResponseDto> GetAlertsAsync(
			string? status, string? severity, int? customerId, int page, int pageSize)
		{
			try
			{
				var query = _context.Set<AlertInstance>()
					.Include(a => a.AlertRule)
					.AsQueryable();

				if (!string.IsNullOrWhiteSpace(status))
					query = query.Where(a => a.Status == status);

				if (!string.IsNullOrWhiteSpace(severity))
					query = query.Where(a => a.Severity == severity);

				if (customerId.HasValue)
					query = query.Where(a => a.AlertRule.CustomerId == customerId.Value);

				var totalCount = await query.CountAsync();
				var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

				var alerts = await query
					.OrderByDescending(a => a.CreatedAt)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync();

				return new AlertInstanceListResponseDto
				{
					Success = true,
					Data = alerts.Select(MapToDto).ToList(),
					Pagination = new PaginationInfoDto
					{
						CurrentPage = page,
						PageSize = pageSize,
						TotalCount = totalCount,
					TotalPages = totalPages,
					HasPrevious = page > 1,
					HasNext = page < totalPages
				}
			};
			}
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208)
			{
				_logger.LogWarning("AlertInstances table not yet created. Run migration script 5-add-evidence-and-alert-instances.sql");
				return new AlertInstanceListResponseDto
				{
					Success = true,
					Data = new List<AlertInstanceDto>(),
					Pagination = new PaginationInfoDto
					{
						CurrentPage = page, PageSize = pageSize, TotalCount = 0,
						TotalPages = 0, HasPrevious = false, HasNext = false
					}
				};
			}
		}

		public async Task<AlertInstanceDto> GetByIdAsync(int alertInstanceId)
		{
			var alert = await _context.Set<AlertInstance>()
				.Include(a => a.AlertRule)
				.FirstOrDefaultAsync(a => a.AlertInstanceId == alertInstanceId);

			if (alert == null)
				throw new KeyNotFoundException($"Alert instance {alertInstanceId} not found");

			return MapToDto(alert);
		}

		public async Task<AlertInstanceDto> AcknowledgeAsync(
			int alertInstanceId, string userId, AcknowledgeAlertDto? dto = null)
		{
			var alert = await _context.Set<AlertInstance>()
				.Include(a => a.AlertRule)
				.FirstOrDefaultAsync(a => a.AlertInstanceId == alertInstanceId);

			if (alert == null)
				throw new KeyNotFoundException($"Alert instance {alertInstanceId} not found");

			if (alert.Status != "new")
				throw new InvalidOperationException($"Alert can only be acknowledged from 'new' status. Current: {alert.Status}");

			alert.Status = "acknowledged";
			alert.AcknowledgedAt = DateTime.UtcNow;
			alert.AcknowledgedBy = userId;

			await _context.SaveChangesAsync();

			_logger.LogInformation("Alert {AlertId} acknowledged by {UserId}", alertInstanceId, userId);

			return MapToDto(alert);
		}

		public async Task<AlertInstanceDto> EscalateAsync(
			int alertInstanceId, string userId, EscalateAlertDto dto)
		{
			var alert = await _context.Set<AlertInstance>()
				.Include(a => a.AlertRule)
				.FirstOrDefaultAsync(a => a.AlertInstanceId == alertInstanceId);

			if (alert == null)
				throw new KeyNotFoundException($"Alert instance {alertInstanceId} not found");

			if (alert.Status == "resolved")
				throw new InvalidOperationException("Cannot escalate a resolved alert");

			alert.Status = "escalated";
			alert.EscalatedAt = DateTime.UtcNow;
			alert.EscalatedTo = dto.EscalateTo;
			alert.EscalationLevel++;

			await _context.SaveChangesAsync();

			try
			{
				await _emailService.SendEmailAsync(
					dto.EscalateTo,
					$"[DibangOps] Escalated Alert: {alert.AlertRule?.Name ?? "Alert"}",
					BuildEscalationEmailBody(alert, dto.Notes),
					isHtml: true);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to send escalation email for alert {AlertId}", alertInstanceId);
			}

			_logger.LogInformation("Alert {AlertId} escalated to {EscalateTo} (level {Level}) by {UserId}",
				alertInstanceId, dto.EscalateTo, alert.EscalationLevel, userId);

			return MapToDto(alert);
		}

		public async Task<AlertInstanceDto> ResolveAsync(
			int alertInstanceId, string userId, ResolveAlertDto dto)
		{
			var alert = await _context.Set<AlertInstance>()
				.Include(a => a.AlertRule)
				.FirstOrDefaultAsync(a => a.AlertInstanceId == alertInstanceId);

			if (alert == null)
				throw new KeyNotFoundException($"Alert instance {alertInstanceId} not found");

			alert.Status = "resolved";
			alert.ResolvedAt = DateTime.UtcNow;
			alert.ResolvedBy = userId;
			alert.ResolutionNotes = dto.ResolutionNotes;

			await _context.SaveChangesAsync();

			_logger.LogInformation("Alert {AlertId} resolved by {UserId}", alertInstanceId, userId);

			return MapToDto(alert);
		}

		public async Task<AlertSummaryDto> GetSummaryAsync(int? customerId = null)
		{
			try
			{
				var query = _context.Set<AlertInstance>()
					.Include(a => a.AlertRule)
					.AsQueryable();

				if (customerId.HasValue)
					query = query.Where(a => a.AlertRule.CustomerId == customerId.Value);

				var active = await query.Where(a => a.Status != "resolved").ToListAsync();
				var resolvedToday = await query
					.Where(a => a.Status == "resolved" && a.ResolvedAt != null && a.ResolvedAt.Value.Date == DateTime.UtcNow.Date)
					.CountAsync();

				var recent = await query
					.OrderByDescending(a => a.CreatedAt)
					.Take(5)
					.ToListAsync();

				return new AlertSummaryDto
				{
					TotalActive = active.Count,
					NewCount = active.Count(a => a.Status == "new"),
					AcknowledgedCount = active.Count(a => a.Status == "acknowledged"),
					EscalatedCount = active.Count(a => a.Status == "escalated"),
					ResolvedTodayCount = resolvedToday,
					RecentAlerts = recent.Select(MapToDto).ToList()
				};
			}
			catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208)
			{
				_logger.LogWarning("AlertInstances table not yet created. Run migration script 5-add-evidence-and-alert-instances.sql");
				return new AlertSummaryDto
				{
					TotalActive = 0, NewCount = 0, AcknowledgedCount = 0,
					EscalatedCount = 0, ResolvedTodayCount = 0,
					RecentAlerts = new List<AlertInstanceDto>()
				};
			}
		}

		private static AlertInstanceDto MapToDto(AlertInstance alert)
		{
			return new AlertInstanceDto
			{
				AlertInstanceId = alert.AlertInstanceId,
				AlertRuleId = alert.AlertRuleId,
				AlertRuleName = alert.AlertRule?.Name,
				IncidentId = alert.IncidentId,
				Severity = alert.Severity,
				Status = alert.Status,
				Message = alert.Message,
				MatchDetails = alert.MatchDetails,
				CreatedAt = alert.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				AcknowledgedAt = alert.AcknowledgedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				AcknowledgedBy = alert.AcknowledgedBy,
				EscalatedAt = alert.EscalatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				EscalatedTo = alert.EscalatedTo,
				ResolvedAt = alert.ResolvedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				ResolvedBy = alert.ResolvedBy,
				ResolutionNotes = alert.ResolutionNotes,
				EscalationLevel = alert.EscalationLevel
			};
		}

		private static string BuildEscalationEmailBody(AlertInstance alert, string? notes)
		{
			return $@"
			<html><body style='font-family: Arial, sans-serif;'>
				<h2 style='color: #dc2626;'>Alert Escalation</h2>
				<p>An alert has been escalated and requires your attention.</p>
				<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>
					<tr><td style='padding: 8px; border: 1px solid #e5e7eb; font-weight: bold;'>Alert Rule</td>
						<td style='padding: 8px; border: 1px solid #e5e7eb;'>{alert.AlertRule?.Name ?? "N/A"}</td></tr>
					<tr><td style='padding: 8px; border: 1px solid #e5e7eb; font-weight: bold;'>Severity</td>
						<td style='padding: 8px; border: 1px solid #e5e7eb;'>{alert.Severity}</td></tr>
					<tr><td style='padding: 8px; border: 1px solid #e5e7eb; font-weight: bold;'>Escalation Level</td>
						<td style='padding: 8px; border: 1px solid #e5e7eb;'>{alert.EscalationLevel}</td></tr>
					<tr><td style='padding: 8px; border: 1px solid #e5e7eb; font-weight: bold;'>Message</td>
						<td style='padding: 8px; border: 1px solid #e5e7eb;'>{alert.Message ?? "N/A"}</td></tr>
					{(string.IsNullOrWhiteSpace(notes) ? "" : $@"<tr><td style='padding: 8px; border: 1px solid #e5e7eb; font-weight: bold;'>Notes</td>
						<td style='padding: 8px; border: 1px solid #e5e7eb;'>{notes}</td></tr>")}
				</table>
				<p style='margin-top: 16px; color: #6b7280; font-size: 12px;'>DibangOps™ — AI-Driven Enterprise Security Intelligence Platform</p>
			</body></html>";
		}
	}
}
