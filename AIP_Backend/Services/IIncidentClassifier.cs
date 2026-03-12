#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Abstraction for AI-assisted incident classification.
	/// Current implementation: rule-based. Swap for ML/AI provider later.
	/// </summary>
	public interface IIncidentClassifier
	{
		Task<IncidentClassificationResultDto> ClassifyAsync(IncidentClassificationRequestDto request);
	}
}
