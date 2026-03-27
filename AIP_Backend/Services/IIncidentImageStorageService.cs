#nullable enable

namespace AIPBackend.Services
{
	public interface IIncidentImageStorageService
	{
		Task<IncidentImageStorageResult> PersistVerificationImageAsync(string? imageReference, CancellationToken cancellationToken = default);
	}
}
