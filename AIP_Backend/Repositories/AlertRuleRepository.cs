#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIPBackend.Repositories
{
	public class AlertRuleRepository : IAlertRuleRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<AlertRuleRepository> _logger;

		public AlertRuleRepository(
			ApplicationDbContext context,
			ILogger<AlertRuleRepository> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<(List<AlertRule> Rules, int Total)> GetPagedAsync(
			string? search,
			string? ruleType,
			bool? isActive,
			int? customerId,
			int page,
			int pageSize)
		{
			var query = _context.AlertRules
				.Where(r => !r.IsDeleted)
				.AsQueryable();

			// Apply filters
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(r => r.Name.Contains(search));
			}

			if (!string.IsNullOrWhiteSpace(ruleType))
			{
				query = query.Where(r => r.RuleType == ruleType);
			}

			if (isActive.HasValue)
			{
				query = query.Where(r => r.IsActive == isActive.Value);
			}

			if (customerId.HasValue)
			{
				query = query.Where(r => r.CustomerId == customerId.Value);
			}

			var total = await query.CountAsync();

			var rules = await query
				.OrderByDescending(r => r.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (rules, total);
		}

		public async Task<AlertRule?> GetByIdAsync(int id)
		{
			return await _context.AlertRules
				.Where(r => !r.IsDeleted)
				.FirstOrDefaultAsync(r => r.AlertRuleId == id);
		}

		public async Task<AlertRule> CreateAsync(AlertRule rule)
		{
			_context.AlertRules.Add(rule);
			await _context.SaveChangesAsync();
			return rule;
		}

		public async Task<AlertRule> UpdateAsync(AlertRule rule)
		{
			rule.UpdatedAt = DateTime.UtcNow;
			_context.AlertRules.Update(rule);
			await _context.SaveChangesAsync();
			return rule;
		}

		public async Task DeleteAsync(int id)
		{
			var rule = await GetByIdAsync(id);
			if (rule != null)
			{
				rule.IsDeleted = true;
				rule.UpdatedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<List<AlertRule>> GetActiveRulesAsync()
		{
			return await _context.AlertRules
				.Where(r => !r.IsDeleted && r.IsActive)
				.ToListAsync();
		}

		public async Task<List<AlertRule>> GetActiveRulesByTypeAsync(string ruleType)
		{
			return await _context.AlertRules
				.Where(r => !r.IsDeleted && r.IsActive && r.RuleType == ruleType)
				.ToListAsync();
		}

		public async Task<List<AlertRule>> GetActiveRulesForIncidentAsync(
			int incidentId, 
			string incidentType, 
			string description)
		{
			// Get all active rules
			var activeRules = await GetActiveRulesAsync();
			
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("🚨 ALERT RULE CHECK STARTING");
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("📌 Incident ID: {IncidentId}", incidentId);
			_logger.LogInformation("📌 Incident Type: '{IncidentType}'", incidentType);
			_logger.LogInformation("📌 Description (first 100 chars): '{Description}'", 
				description.Length > 100 ? description.Substring(0, 100) + "..." : description);
			_logger.LogInformation("📌 Total Active Rules to Check: {Count}", activeRules.Count);
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

			// Filter rules that match the incident
			var matchingRules = new List<AlertRule>();

			foreach (var rule in activeRules)
			{
				_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
				_logger.LogInformation("🔍 Checking rule '{RuleName}' (ID: {RuleId}) - Type: {RuleType}", 
					rule.Name, rule.AlertRuleId, rule.RuleType);
				
				bool incidentTypeMatches = false;
				bool keywordMatches = false;
				
				// Check if incident type matches
				var ruleIncidentTypes = string.IsNullOrEmpty(rule.IncidentTypes)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.IncidentTypes) ?? new List<string>();

				_logger.LogInformation("📋 INCIDENT TYPE CHECK:");
				_logger.LogInformation("   - Rule has {Count} incident type(s): [{Types}]", 
					ruleIncidentTypes.Count, 
					ruleIncidentTypes.Count > 0 ? string.Join(", ", ruleIncidentTypes) : "NONE (matches all types)");
				_logger.LogInformation("   - Incident type to match: '{IncidentType}'", incidentType);

				if (ruleIncidentTypes.Count > 0)
				{
					// Rule has specific incident types - must match one of them
					if (ruleIncidentTypes.Contains(incidentType, StringComparer.OrdinalIgnoreCase))
					{
						incidentTypeMatches = true;
						_logger.LogInformation("   ✅ MATCH: Incident type '{IncidentType}' found in rule's incident types", incidentType);
					}
					else
					{
						_logger.LogWarning("   ❌ NO MATCH: Incident type '{IncidentType}' NOT in rule types [{RuleTypes}] - SKIPPING RULE",
							incidentType, string.Join(", ", ruleIncidentTypes));
						continue;
					}
				}
				else
				{
					// Rule has no incident types configured - matches all incidents
					incidentTypeMatches = true;
					_logger.LogInformation("   ✅ MATCH: Rule has no incident types configured, matches ALL incident types");
				}

				// Check if keywords match based on trigger condition
				var ruleKeywords = string.IsNullOrEmpty(rule.Keywords)
					? new List<string>()
					: JsonSerializer.Deserialize<List<string>>(rule.Keywords) ?? new List<string>();

				_logger.LogInformation("🔑 KEYWORD CHECK:");
				_logger.LogInformation("   - Rule has {Count} keyword(s): [{Keywords}]", 
					ruleKeywords.Count,
					ruleKeywords.Count > 0 ? string.Join(", ", ruleKeywords) : "NONE (no keyword filtering)");
				_logger.LogInformation("   - Trigger condition: {Condition}", rule.TriggerCondition ?? "any");

				if (ruleKeywords.Count > 0)
				{
					// Skip if description is empty or null
					if (string.IsNullOrWhiteSpace(description))
					{
						_logger.LogWarning("   ❌ NO MATCH: Description is empty but keywords are required - SKIPPING RULE");
						continue;
					}

					_logger.LogInformation("   - Searching in: '{Description}'", 
						description.Length > 100 ? description.Substring(0, 100) + "..." : description);

					var descriptionLower = description.ToLower().Trim();
					var matchedKeywords = ruleKeywords
						.Where(k => !string.IsNullOrWhiteSpace(k) && descriptionLower.Contains(k.ToLower().Trim()))
						.ToList();

					_logger.LogInformation("   - Found {MatchedCount}/{TotalCount} matching keywords: [{Matched}]",
						matchedKeywords.Count, ruleKeywords.Count, 
						matchedKeywords.Count > 0 ? string.Join(", ", matchedKeywords) : "NONE");

					bool shouldTrigger = rule.TriggerCondition?.ToLower() switch
					{
						"any" => matchedKeywords.Count > 0,
						"all" => matchedKeywords.Count == ruleKeywords.Count,
						"exact-match" => descriptionLower == string.Join(" ", ruleKeywords.Select(k => k.ToLower().Trim())),
						_ => matchedKeywords.Count > 0 // Default to "any" if trigger condition is not recognized
					};

					if (!shouldTrigger)
					{
						var reason = rule.TriggerCondition?.ToLower() switch
						{
							"any" => $"Need at least 1 keyword, found {matchedKeywords.Count}",
							"all" => $"Need all {ruleKeywords.Count} keywords, found {matchedKeywords.Count}",
							"exact-match" => "Need exact phrase match",
							_ => $"Need at least 1 keyword, found {matchedKeywords.Count}"
						};
						_logger.LogWarning("   ❌ NO MATCH: Trigger condition '{Condition}' not satisfied. {Reason} - SKIPPING RULE",
							rule.TriggerCondition, reason);
						continue;
					}

					keywordMatches = true;
					_logger.LogInformation("   ✅ MATCH: Trigger condition '{Condition}' satisfied with {MatchedCount} matching keyword(s)",
						rule.TriggerCondition, matchedKeywords.Count);
				}
				else
				{
					// No keywords configured - automatically matches
					keywordMatches = true;
					_logger.LogInformation("   ✅ MATCH: Rule has no keywords configured, matches ALL incidents");
				}

				// If we reached here, all conditions are satisfied
				_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
				_logger.LogInformation("🎯 RESULT: Rule '{RuleName}' (ID: {RuleId}) FULLY MATCHED!", rule.Name, rule.AlertRuleId);
				_logger.LogInformation("   ✅ Incident Type: MATCHED");
				_logger.LogInformation("   ✅ Keywords: MATCHED");
				_logger.LogInformation("   🚨 ACTION: Alert will be triggered for incident {IncidentId}", incidentId);
				_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
				
				matchingRules.Add(rule);
			}

			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("📊 ALERT RULE CHECK COMPLETED");
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("📌 Incident ID: {IncidentId}", incidentId);
			_logger.LogInformation("📌 Rules Checked: {TotalCount}", activeRules.Count);
			_logger.LogInformation("📌 Rules Matched: {MatchedCount}", matchingRules.Count);
			
			if (matchingRules.Count > 0)
			{
				_logger.LogInformation("🎯 Matched Rules:");
				foreach (var matchedRule in matchingRules)
				{
					_logger.LogInformation("   - '{RuleName}' (ID: {RuleId})", matchedRule.Name, matchedRule.AlertRuleId);
				}
			}
			else
			{
				_logger.LogInformation("⚠️ No alert rules matched this incident");
			}
			
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

			return matchingRules;
		}
	}
}
