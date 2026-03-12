#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service for alert lifecycle management: create, acknowledge, escalate, resolve.
	/// </summary>
	public interface IAlertEscalationService
	{
		Task<AlertInstanceDto> CreateAlertAsync(int alertRuleId, int? incidentId, string severity, string message, string? matchDetails = null);
		Task<AlertInstanceListResponseDto> GetAlertsAsync(string? status, string? severity, int? customerId, int page, int pageSize);
		Task<AlertInstanceDto> GetByIdAsync(int alertInstanceId);
		Task<AlertInstanceDto> AcknowledgeAsync(int alertInstanceId, string userId, AcknowledgeAlertDto? dto = null);
		Task<AlertInstanceDto> EscalateAsync(int alertInstanceId, string userId, EscalateAlertDto dto);
		Task<AlertInstanceDto> ResolveAsync(int alertInstanceId, string userId, ResolveAlertDto dto);
		Task<AlertSummaryDto> GetSummaryAsync(int? customerId = null);
	}
}
