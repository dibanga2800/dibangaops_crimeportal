#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Thin wrapper around Azure OpenAI for incident-related prompts.
	/// This abstraction keeps HTTP details out of domain services.
	/// </summary>
	public interface IAzureOpenAiClient
	{
		Task<IncidentClassificationResultDto> ClassifyIncidentAsync(
			IncidentClassificationRequestDto request,
			CancellationToken cancellationToken = default);
	}
}

