#nullable enable

using AIPBackend.Models;

namespace AIPBackend.Repositories
{
	public interface IAlertRuleRepository
	{
		Task<(List<AlertRule> Rules, int Total)> GetPagedAsync(
			string? search, 
			string? ruleType, 
			bool? isActive, 
			int? customerId,
			int page, 
			int pageSize);

		Task<AlertRule?> GetByIdAsync(int id);
		Task<AlertRule> CreateAsync(AlertRule rule);
		Task<AlertRule> UpdateAsync(AlertRule rule);
		Task DeleteAsync(int id);
		Task<List<AlertRule>> GetActiveRulesAsync();
		Task<List<AlertRule>> GetActiveRulesByTypeAsync(string ruleType);
		Task<List<AlertRule>> GetActiveRulesForIncidentAsync(int incidentId, string incidentType, string description);
	}
}
