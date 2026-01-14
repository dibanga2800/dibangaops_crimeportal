#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
	using System.Text.Json.Serialization;

namespace AIPBackend.Repositories
{
	public class AlertRuleRepository : IAlertRuleRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<AlertRuleRepository> _logger;
			private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				NumberHandling = JsonNumberHandling.AllowReadingFromString
			};

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
			// Normalize inputs early so all downstream checks are consistent
			var normalizedIncidentType = (incidentType ?? string.Empty).Trim();
			var normalizedDescription = description ?? string.Empty;

			// Get all active rules
			var activeRules = await GetActiveRulesAsync();
			
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("🚨 ALERT RULE CHECK STARTING");
			_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
			_logger.LogInformation("📌 Incident ID: {IncidentId}", incidentId);
			_logger.LogInformation("📌 Incident Type (normalized): '{IncidentType}'", normalizedIncidentType);
			_logger.LogInformation("📌 Description (first 100 chars): '{Description}'", 
				normalizedDescription.Length > 100 ? normalizedDescription.Substring(0, 100) + "..." : normalizedDescription);
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
				var ruleIncidentTypes = DeserializeStringList(rule.IncidentTypes, "IncidentTypes", rule.AlertRuleId);

				// Work with a normalized view (trim + case-insensitive)
				var normalizedRuleIncidentTypes = ruleIncidentTypes
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Select(t => t.Trim())
					.ToList();

				_logger.LogInformation("📋 INCIDENT TYPE CHECK:");
				_logger.LogInformation("   - Rule has {Count} incident type(s): [{Types}]", 
					normalizedRuleIncidentTypes.Count, 
					normalizedRuleIncidentTypes.Count > 0 ? string.Join(", ", normalizedRuleIncidentTypes) : "NONE (matches all types)");
				_logger.LogInformation("   - Incident type to match: '{IncidentType}'", normalizedIncidentType);

				if (normalizedRuleIncidentTypes.Count > 0)
				{
					// Rule has specific incident types - must match one of them
					// Use flexible matching similar to keyword "any" condition:
					// 1. Try exact match first (most specific)
					// 2. Try "starts with" match (handles suffixes like " - Saved", " - Pending")
					// 3. Try "contains" match (handles cases where incident type contains rule type anywhere)
					// This makes incident type matching robust like keyword matching
					bool matches = normalizedRuleIncidentTypes.Contains(normalizedIncidentType, StringComparer.OrdinalIgnoreCase);
					
					if (!matches)
					{
						// Try bidirectional "starts with" - handles "Arrest" matching "Arrest - Saved"
						matches = normalizedRuleIncidentTypes.Any(ruleType => 
							normalizedIncidentType.StartsWith(ruleType, StringComparison.OrdinalIgnoreCase) ||
							ruleType.StartsWith(normalizedIncidentType, StringComparison.OrdinalIgnoreCase));
					}
					
					if (!matches)
					{
						// Try "contains" match (like keyword "any" condition) - most flexible
						// Handles cases where rule type appears anywhere in incident type
						matches = normalizedRuleIncidentTypes.Any(ruleType => 
							normalizedIncidentType.Contains(ruleType, StringComparison.OrdinalIgnoreCase) ||
							ruleType.Contains(normalizedIncidentType, StringComparison.OrdinalIgnoreCase));
					}
					
					if (matches)
					{
						incidentTypeMatches = true;
						_logger.LogInformation("   ✅ MATCH: Incident type '{IncidentType}' matched rule's incident types [{RuleTypes}]", 
							normalizedIncidentType, string.Join(", ", normalizedRuleIncidentTypes));
					}
					else
					{
						_logger.LogWarning("   ❌ NO MATCH: Incident type '{IncidentType}' NOT in rule types [{RuleTypes}] - SKIPPING RULE",
							normalizedIncidentType, string.Join(", ", normalizedRuleIncidentTypes));
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
				var ruleKeywords = DeserializeStringList(rule.Keywords, "Keywords", rule.AlertRuleId);

				_logger.LogInformation("🔑 KEYWORD CHECK:");
				_logger.LogInformation("   - Rule has {Count} keyword(s): [{Keywords}]", 
					ruleKeywords.Count,
					ruleKeywords.Count > 0 ? string.Join(", ", ruleKeywords) : "NONE (no keyword filtering)");
				_logger.LogInformation("   - Trigger condition: {Condition}", rule.TriggerCondition ?? "any");

				if (ruleKeywords.Count > 0)
				{
					// For "any" condition, don't skip if description is empty - incident type might still match
					// For "all" or "exact-match", keywords are required
					if (string.IsNullOrWhiteSpace(normalizedDescription))
					{
						if (!string.Equals(rule.TriggerCondition, "any", StringComparison.OrdinalIgnoreCase))
						{
							_logger.LogWarning("   ❌ NO MATCH: Description is empty but keywords are required for trigger condition '{Condition}' - SKIPPING RULE",
								rule.TriggerCondition);
							continue;
						}
						else
						{
							_logger.LogInformation("   ⚠️ Description is empty, but trigger condition is 'any' - will check if incident type matches");
						}
					}

					if (!string.IsNullOrWhiteSpace(normalizedDescription))
					{
						_logger.LogInformation("   - Searching in: '{Description}'", 
							normalizedDescription.Length > 100 ? normalizedDescription.Substring(0, 100) + "..." : normalizedDescription);
					}

					var descriptionLower = string.IsNullOrWhiteSpace(normalizedDescription) 
						? string.Empty 
						: normalizedDescription.ToLower().Trim();
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

					if (shouldTrigger)
					{
						keywordMatches = true;
						_logger.LogInformation("   ✅ MATCH: Trigger condition '{Condition}' satisfied with {MatchedCount} matching keyword(s)",
							rule.TriggerCondition, matchedKeywords.Count);
					}
					else
					{
						// For "any" condition, don't skip yet - let OR logic handle it (incident type might still match)
						// For "all" or "exact-match", we need keywords to match (AND logic)
						if (!string.Equals(rule.TriggerCondition, "any", StringComparison.OrdinalIgnoreCase))
						{
							var reason = rule.TriggerCondition?.ToLower() switch
							{
								"all" => $"Need all {ruleKeywords.Count} keywords, found {matchedKeywords.Count}",
								"exact-match" => "Need exact phrase match",
								_ => $"Need at least 1 keyword, found {matchedKeywords.Count}"
							};
							_logger.LogWarning("   ❌ NO MATCH: Trigger condition '{Condition}' not satisfied. {Reason} - SKIPPING RULE",
								rule.TriggerCondition, reason);
							continue;
						}
						else
						{
							_logger.LogInformation("   ⚠️ No keywords matched, but trigger condition is 'any' - will check if incident type matches");
						}
					}
				}
				else
				{
					// No keywords configured - automatically matches
					keywordMatches = true;
					_logger.LogInformation("   ✅ MATCH: Rule has no keywords configured, matches ALL incidents");
				}

				// Determine if rule should trigger based on trigger condition
				// For "any" condition: match if EITHER incident type OR keywords match (OR logic)
				// For "all" or "exact-match": BOTH incident type AND keywords must match (AND logic)
				bool shouldTriggerRule = false;
				string triggerReason = string.Empty;
				
				if (string.Equals(rule.TriggerCondition, "any", StringComparison.OrdinalIgnoreCase))
				{
					// OR logic: match if incident type OR keywords match
					shouldTriggerRule = incidentTypeMatches || keywordMatches;
					if (shouldTriggerRule)
					{
						if (incidentTypeMatches && keywordMatches)
							triggerReason = "Incident type AND keywords matched";
						else if (incidentTypeMatches)
							triggerReason = "Incident type matched (keywords not required or not configured)";
						else
							triggerReason = "Keywords matched (incident type not required or matched all)";
					}
				}
				else
				{
					// AND logic: both must match (for "all" and "exact-match")
					shouldTriggerRule = incidentTypeMatches && keywordMatches;
					if (shouldTriggerRule)
						triggerReason = "Incident type AND keywords both matched";
				}
				
				if (!shouldTriggerRule)
				{
					var missingConditions = new List<string>();
					if (!incidentTypeMatches) missingConditions.Add("incident type");
					if (!keywordMatches) missingConditions.Add("keywords");
					_logger.LogWarning("   ❌ NO MATCH: Rule '{RuleName}' did not satisfy trigger condition '{Condition}'. Missing: {Missing}",
						rule.Name, rule.TriggerCondition ?? "any", string.Join(" and ", missingConditions));
					continue;
				}
				
				// If we reached here, conditions are satisfied
				_logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
				_logger.LogInformation("🎯 RESULT: Rule '{RuleName}' (ID: {RuleId}) FULLY MATCHED!", rule.Name, rule.AlertRuleId);
				_logger.LogInformation("   ✅ Trigger Condition: {Condition}", rule.TriggerCondition ?? "any");
				_logger.LogInformation("   ✅ Reason: {Reason}", triggerReason);
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

		/// <summary>
		/// Safely deserialize a JSON string representing a list of strings.
		/// Returns an empty list on null/empty/invalid JSON and logs any issues
		/// without breaking alert evaluation for other rules.
		/// </summary>
		private List<string> DeserializeStringList(string? json, string propertyName, int ruleId)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return new List<string>();
			}

			try
			{
				var result = JsonSerializer.Deserialize<List<string>>(json, _jsonOptions);
				if (result == null)
				{
					_logger.LogWarning("AlertRuleRepository: {Property} JSON was null after deserialization for rule {RuleId}. Raw: {Json}", propertyName, ruleId, json);
					return new List<string>();
				}

				return result;
			}
			catch (JsonException ex)
			{
				_logger.LogError(ex, "AlertRuleRepository: Failed to deserialize {Property} for rule {RuleId}. Raw JSON: {Json}", propertyName, ruleId, json);
				return new List<string>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "AlertRuleRepository: Unexpected error deserializing {Property} for rule {RuleId}. Raw JSON: {Json}", propertyName, ruleId, json);
				return new List<string>();
			}
		}
	}
}
