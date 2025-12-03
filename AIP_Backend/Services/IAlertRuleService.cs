#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	public interface IAlertRuleService
	{
		Task<AlertRuleListResponseDto> GetPagedAsync(
			string? search,
			string? ruleType,
			bool? isActive,
			int? customerId,
			int page,
			int pageSize);

		Task<AlertRuleDto> GetByIdAsync(int id);
		Task<AlertRuleDto> CreateAsync(CreateAlertRuleDto dto, string currentUserId);
		Task<AlertRuleDto> UpdateAsync(int id, UpdateAlertRuleDto dto, string currentUserId);
		Task DeleteAsync(int id);
		Task<AlertRuleDto> ToggleActiveAsync(int id, bool isActive, string currentUserId);
		Task CheckIncidentForAlertsAsync(int incidentId);
	}
}
