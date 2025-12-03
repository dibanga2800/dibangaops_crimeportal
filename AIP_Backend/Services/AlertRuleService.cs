#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;
using System.Text;
using System.Text.Json;

namespace AIPBackend.Services
{
	public class AlertRuleService : IAlertRuleService
	{
		private readonly IAlertRuleRepository _repository;
		private readonly IIncidentRepository _incidentRepository;
		private readonly IEmailService _emailService;
		private readonly ILogger<AlertRuleService> _logger;

		public AlertRuleService(
			IAlertRuleRepository repository,
			IIncidentRepository incidentRepository,
			IEmailService emailService,
			ILogger<AlertRuleService> logger)
		{
			_repository = repository;
			_incidentRepository = incidentRepository;
			_emailService = emailService;
			_logger = logger;
		}

		public async Task<AlertRuleListResponseDto> GetPagedAsync(
			string? search,
			string? ruleType,
			bool? isActive,
			int? customerId,
			int page,
			int pageSize)
		{
			var (rules, total) = await _repository.GetPagedAsync(
				search, 
				ruleType, 
				isActive, 
				customerId,
				page, 
				pageSize);

			var dtos = rules.Select(MapToDto).ToList();
			var totalPages = (int)Math.Ceiling(total / (double)pageSize);

			return new AlertRuleListResponseDto
			{
				Data = dtos,
				Total = total,
				Page = page,
				PageSize = pageSize,
				TotalPages = totalPages
			};
		}

		public async Task<AlertRuleDto> GetByIdAsync(int id)
		{
			var rule = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Alert rule with ID {id} not found.");

			return MapToDto(rule);
		}

		public async Task<AlertRuleDto> CreateAsync(CreateAlertRuleDto dto, string currentUserId)
		{
			var rule = new AlertRule
			{
				Name = dto.Name,
				RuleType = dto.RuleType,
				Keywords = dto.Keywords.Count > 0 ? JsonSerializer.Serialize(dto.Keywords) : null,
				IncidentTypes = dto.IncidentTypes.Count > 0 ? JsonSerializer.Serialize(dto.IncidentTypes) : null,
				LpmRegion = dto.LpmRegion,
				RegionId = dto.RegionId,
				TriggerCondition = dto.TriggerCondition,
				Channels = dto.Channels.Count > 0 ? JsonSerializer.Serialize(dto.Channels) : null,
				EmailRecipients = dto.EmailRecipients.Count > 0 ? JsonSerializer.Serialize(dto.EmailRecipients) : null,
				IsActive = dto.IsActive,
				CustomerId = dto.CustomerId,
				SiteId = dto.SiteId,
				CreatedAt = DateTime.UtcNow,
				CreatedBy = currentUserId
			};

			var created = await _repository.CreateAsync(rule);
			_logger.LogInformation("Alert rule created: {RuleName} (ID: {RuleId}) by user {UserId}", 
				rule.Name, rule.AlertRuleId, currentUserId);

			return MapToDto(created);
		}

		public async Task<AlertRuleDto> UpdateAsync(int id, UpdateAlertRuleDto dto, string currentUserId)
		{
			var existing = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Alert rule with ID {id} not found.");

			// Update fields if provided
			if (!string.IsNullOrWhiteSpace(dto.Name))
			{
				existing.Name = dto.Name;
			}

			if (dto.Keywords != null)
			{
				existing.Keywords = dto.Keywords.Count > 0 ? JsonSerializer.Serialize(dto.Keywords) : null;
			}

			if (dto.IncidentTypes != null)
			{
				existing.IncidentTypes = dto.IncidentTypes.Count > 0 ? JsonSerializer.Serialize(dto.IncidentTypes) : null;
			}

			if (dto.LpmRegion != null)
			{
				existing.LpmRegion = dto.LpmRegion;
			}

			if (dto.RegionId.HasValue)
			{
				existing.RegionId = dto.RegionId;
			}

			if (!string.IsNullOrWhiteSpace(dto.TriggerCondition))
			{
				existing.TriggerCondition = dto.TriggerCondition;
			}

			if (dto.Channels != null)
			{
				existing.Channels = dto.Channels.Count > 0 ? JsonSerializer.Serialize(dto.Channels) : null;
			}

			if (dto.EmailRecipients != null)
			{
				existing.EmailRecipients = dto.EmailRecipients.Count > 0 ? JsonSerializer.Serialize(dto.EmailRecipients) : null;
			}

			if (dto.IsActive.HasValue)
			{
				existing.IsActive = dto.IsActive.Value;
			}

			if (dto.CustomerId.HasValue)
			{
				existing.CustomerId = dto.CustomerId;
			}

			if (dto.SiteId.HasValue)
			{
				existing.SiteId = dto.SiteId;
			}

			existing.UpdatedBy = currentUserId;

			var updated = await _repository.UpdateAsync(existing);
			_logger.LogInformation("Alert rule updated: {RuleName} (ID: {RuleId}) by user {UserId}",
				updated.Name, updated.AlertRuleId, currentUserId);

			return MapToDto(updated);
		}

		public async Task DeleteAsync(int id)
		{
			await _repository.DeleteAsync(id);
			_logger.LogInformation("Alert rule deleted: ID {RuleId}", id);
		}

		public async Task<AlertRuleDto> ToggleActiveAsync(int id, bool isActive, string currentUserId)
		{
			var rule = await _repository.GetByIdAsync(id)
				?? throw new KeyNotFoundException($"Alert rule with ID {id} not found.");

			rule.IsActive = isActive;
			rule.UpdatedBy = currentUserId;

			var updated = await _repository.UpdateAsync(rule);
			_logger.LogInformation("Alert rule {Status}: {RuleName} (ID: {RuleId}) by user {UserId}",
				isActive ? "activated" : "deactivated", updated.Name, updated.AlertRuleId, currentUserId);

			return MapToDto(updated);
		}

		public async Task CheckIncidentForAlertsAsync(int incidentId)
		{
			try
			{
				// Get incident directly from repository to bypass user context checks
				// (this runs in background task with no HTTP context)
				var incident = await _incidentRepository.GetByIdWithItemsAsync(incidentId);
				if (incident == null)
				{
					_logger.LogWarning("Incident not found for alert check: ID {IncidentId}", incidentId);
					return;
				}

				// Combine Description and IncidentDetails for keyword matching
				var fullDescription = string.Join(" ", new[] { 
					incident.Description ?? string.Empty, 
					incident.IncidentDetails ?? string.Empty 
				}.Where(s => !string.IsNullOrWhiteSpace(s)));

				_logger.LogInformation("Checking incident {IncidentId} - Type: {Type}, Description: {Desc}",
					incidentId, incident.IncidentType, fullDescription.Substring(0, Math.Min(100, fullDescription.Length)));

				// Get matching alert rules
				var matchingRules = await _repository.GetActiveRulesForIncidentAsync(
					incidentId,
					incident.IncidentType ?? string.Empty,
					fullDescription);

				if (!matchingRules.Any())
				{
					_logger.LogDebug("No matching alert rules for incident {IncidentId}", incidentId);
					return;
				}

				_logger.LogInformation("Found {Count} matching alert rules for incident {IncidentId}",
					matchingRules.Count, incidentId);

				// Trigger each matching rule
				foreach (var rule in matchingRules)
				{
					await TriggerAlertAsync(rule, incident, incidentId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking incident {IncidentId} for alerts", incidentId);
			}
		}

	private async Task TriggerAlertAsync(AlertRule rule, Incident incident, int incidentId)
	{
		try
		{
			_logger.LogInformation("🚨 Triggering alert rule: {RuleName} (ID: {RuleId}) for incident {IncidentId}",
				rule.Name, rule.AlertRuleId, incidentId);

			var channels = string.IsNullOrEmpty(rule.Channels)
				? new List<string>()
				: JsonSerializer.Deserialize<List<string>>(rule.Channels) ?? new List<string>();

			var emailRecipients = string.IsNullOrEmpty(rule.EmailRecipients)
				? new List<string>()
				: JsonSerializer.Deserialize<List<string>>(rule.EmailRecipients) ?? new List<string>();

			_logger.LogInformation("Alert channels configured: [{Channels}], Email recipients: [{Recipients}]",
				string.Join(", ", channels), string.Join(", ", emailRecipients));

			// Send email notifications if email channel is enabled and recipients are configured
			if (channels.Contains("email") && emailRecipients.Count > 0)
			{
				_logger.LogInformation("📧 Sending alert email to {Count} recipients...", emailRecipients.Count);
				await SendAlertEmailAsync(rule, incident, incidentId, emailRecipients);
			}
			else if (channels.Contains("email") && emailRecipients.Count == 0)
			{
				_logger.LogWarning("⚠️ Email channel is enabled but no recipients configured for rule {RuleName}", rule.Name);
			}
			else if (!channels.Contains("email"))
			{
				_logger.LogInformation("Email channel not enabled for rule {RuleName}, skipping email notification", rule.Name);
			}

			// TODO: Add in-app notification logic here if needed
			if (channels.Contains("in-app"))
			{
				_logger.LogInformation("In-app notification for alert rule {RuleName} would be triggered here", rule.Name);
				// Future: Implement in-app notification system
			}

			// Update rule trigger statistics
			rule.LastTriggered = DateTime.UtcNow;
			rule.TriggerCount++;
			await _repository.UpdateAsync(rule);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error triggering alert rule {RuleId} for incident {IncidentId}",
				rule.AlertRuleId, incidentId);
		}
	}

	private async Task SendAlertEmailAsync(AlertRule rule, Incident incident, int incidentId, List<string> recipients)
	{
		try
		{
			_logger.LogInformation("📧 Preparing to send alert email for rule '{RuleName}' to {RecipientCount} recipients: [{Recipients}]",
				rule.Name, recipients.Count, string.Join(", ", recipients));

			var subject = $"🚨 Security Alert: {rule.Name}";
			
			// Parse date for formatting
			DateTime incidentDate = incident.DateOfIncident;
			
			var sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"en\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"UTF-8\">");
			sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
			sb.AppendLine("<style>");
			sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
			sb.AppendLine(".email-wrapper { width: 100%; background-color: #f4f4f4; padding: 20px 0; }");
			sb.AppendLine(".container { width: 100%; max-width: 100%; margin: 0 auto; background-color: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
			sb.AppendLine("@media (min-width: 768px) { .container { width: 50%; max-width: 800px; } }");
			sb.AppendLine(".header { background-color: #dc2626; color: #ffffff; padding: 30px 20px; text-align: center; }");
			sb.AppendLine(".header h1 { margin: 0; font-size: 24px; font-weight: 600; }");
			sb.AppendLine(".header p { margin: 10px 0 0 0; font-size: 14px; opacity: 0.9; }");
			sb.AppendLine(".content { padding: 30px 20px; }");
			sb.AppendLine(".alert-message { font-size: 16px; margin-bottom: 30px; color: #dc2626; font-weight: 500; padding: 15px; background-color: #fee2e2; border-left: 4px solid #dc2626; border-radius: 4px; }");
			sb.AppendLine(".details-table { width: 100%; border-collapse: collapse; margin: 20px 0; background-color: #f9fafb; }");
			sb.AppendLine(".details-table th { background-color: #83BB26; color: #ffffff; padding: 12px; text-align: left; font-weight: 600; font-size: 14px; }");
			sb.AppendLine(".details-table td { padding: 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
			sb.AppendLine(".details-table tr:last-child td { border-bottom: none; }");
			sb.AppendLine(".details-table tr:nth-child(even) { background-color: #ffffff; }");
			sb.AppendLine(".priority-high { color: #dc2626; font-weight: bold; }");
			sb.AppendLine(".priority-medium { color: #f59e0b; font-weight: bold; }");
			sb.AppendLine(".priority-low { color: #10b981; font-weight: bold; }");
			sb.AppendLine(".note { background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; font-size: 14px; }");
			sb.AppendLine(".rule-info { background-color: #e8f5d1; border-left: 4px solid #83BB26; padding: 15px; margin: 20px 0; font-size: 14px; }");
			sb.AppendLine(".footer { background-color: #f9fafb; padding: 20px; text-align: center; font-size: 14px; color: #6b7280; border-top: 1px solid #e5e7eb; }");
			sb.AppendLine(".footer p { margin: 5px 0; }");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<div class=\"email-wrapper\">");
			sb.AppendLine("<div class=\"container\">");
			
			// Header
			sb.AppendLine("<div class=\"header\">");
			sb.AppendLine("<h1>🚨 Security Alert Triggered</h1>");
			sb.AppendLine($"<p>Alert Rule: {rule.Name}</p>");
			sb.AppendLine("</div>");
			
			// Content
			sb.AppendLine("<div class=\"content\">");
			sb.AppendLine("<div class=\"alert-message\">");
			sb.AppendLine("<strong>⚠️ Immediate Attention Required:</strong> An incident has been reported that matches your alert criteria.");
			sb.AppendLine("</div>");
			
			// Incident Details Table
			sb.AppendLine("<table class=\"details-table\">");
			sb.AppendLine("<tr><th colspan=\"2\">Incident Details</th></tr>");
			sb.AppendLine($"<tr><td><strong>Incident ID</strong></td><td>{incident.IncidentId}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Type</strong></td><td>{incident.IncidentType}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Site</strong></td><td>{incident.SiteName}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Region</strong></td><td>{incident.RegionName ?? "N/A"}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Date</strong></td><td>{incidentDate:dd/MM/yyyy}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Time</strong></td><td>{incident.TimeOfIncident ?? "N/A"}</td></tr>");
			sb.AppendLine($"<tr><td><strong>Reported By</strong></td><td>{incident.OfficerName}</td></tr>");
			
			// Priority with color coding
			var priorityClass = incident.Priority?.ToLower() switch
			{
				"high" => "priority-high",
				"medium" => "priority-medium",
				"low" => "priority-low",
				_ => ""
			};
			sb.AppendLine($"<tr><td><strong>Priority</strong></td><td><span class=\"{priorityClass}\">{incident.Priority?.ToUpper() ?? "N/A"}</span></td></tr>");
			sb.AppendLine($"<tr><td><strong>Description</strong></td><td>{incident.Description ?? "N/A"}</td></tr>");
			
			// Value Recovered (if applicable)
			if (incident.TotalValueRecovered.HasValue && incident.TotalValueRecovered > 0)
			{
				sb.AppendLine($"<tr><td><strong>Value Recovered</strong></td><td>£{incident.TotalValueRecovered:N2}</td></tr>");
			}
			
			sb.AppendLine("</table>");
			
			// Rule Information
			sb.AppendLine("<div class=\"rule-info\">");
			sb.AppendLine("<strong>Alert Rule Information:</strong><br/>");
			sb.AppendLine($"<strong>Rule Name:</strong> {rule.Name}<br/>");
			sb.AppendLine($"<strong>Rule Type:</strong> {rule.RuleType.ToUpper()}<br/>");
			sb.AppendLine($"<strong>Trigger Condition:</strong> {rule.TriggerCondition}<br/>");
			
			// Show matched criteria
			if (!string.IsNullOrEmpty(rule.IncidentTypes))
			{
				var incidentTypes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rule.IncidentTypes);
				if (incidentTypes != null && incidentTypes.Any())
				{
					sb.AppendLine($"<strong>Matching Incident Types:</strong> {string.Join(", ", incidentTypes)}<br/>");
				}
			}
			
			if (!string.IsNullOrEmpty(rule.Keywords))
			{
				var keywords = System.Text.Json.JsonSerializer.Deserialize<List<string>>(rule.Keywords);
				if (keywords != null && keywords.Any())
				{
					sb.AppendLine($"<strong>Matching Keywords:</strong> {string.Join(", ", keywords)}");
				}
			}
			sb.AppendLine("</div>");
			
			// Action Note
			sb.AppendLine("<div class=\"note\">");
			sb.AppendLine("<strong>Recommended Action:</strong> Please review this incident in the Advantage One Security portal and take appropriate action based on your organization's security protocols.");
			sb.AppendLine("</div>");
			
			sb.AppendLine("</div>");
			
			// Footer
			sb.AppendLine("<div class=\"footer\">");
			sb.AppendLine("<p>This is an automated notification from the <strong>Advantage One Security</strong> Alert System.</p>");
			sb.AppendLine("<p>Please do not reply to this message.</p>");
			sb.AppendLine("<p>&copy; " + DateTime.Now.Year + " Advantage One Security. All rights reserved.</p>");
			sb.AppendLine("</div>");
			
			sb.AppendLine("</div>");
			sb.AppendLine("</div>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");

			var body = sb.ToString();

			_logger.LogInformation("📤 Sending email via EmailService...");

			var success = await _emailService.SendEmailAsync(
				recipients,
				subject,
				body,
				isHtml: true,
				fromName: "Advantage One Security - Alert System");

			if (success)
			{
				_logger.LogInformation("✅✅✅ Alert email sent successfully for rule '{RuleName}' to {RecipientCount} recipients: [{Recipients}]",
					rule.Name, recipients.Count, string.Join(", ", recipients));
			}
			else
			{
				_logger.LogError("❌❌❌ Failed to send alert email for rule '{RuleName}' to {RecipientCount} recipients: [{Recipients}]",
					rule.Name, recipients.Count, string.Join(", ", recipients));
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending alert email for rule {RuleId}", rule.AlertRuleId);
			throw;
		}
	}

		private AlertRuleDto MapToDto(AlertRule rule)
		{
			return new AlertRuleDto
			{
				AlertRuleId = rule.AlertRuleId,
				Name = rule.Name,
				RuleType = rule.RuleType,
				Keywords = string.IsNullOrEmpty(rule.Keywords)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.Keywords) ?? new List<string>(),
				IncidentTypes = string.IsNullOrEmpty(rule.IncidentTypes)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.IncidentTypes) ?? new List<string>(),
				LpmRegion = rule.LpmRegion,
				RegionId = rule.RegionId,
				TriggerCondition = rule.TriggerCondition,
				Channels = string.IsNullOrEmpty(rule.Channels)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.Channels) ?? new List<string>(),
				EmailRecipients = string.IsNullOrEmpty(rule.EmailRecipients)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.EmailRecipients) ?? new List<string>(),
				IsActive = rule.IsActive,
				CustomerId = rule.CustomerId,
				SiteId = rule.SiteId,
				CreatedAt = rule.CreatedAt,
				CreatedBy = rule.CreatedBy,
				UpdatedAt = rule.UpdatedAt,
				UpdatedBy = rule.UpdatedBy,
				LastTriggered = rule.LastTriggered,
				TriggerCount = rule.TriggerCount
			};
		}
	}
}
