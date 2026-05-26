namespace AIPBackend.Services.Security;

/// <summary>
/// Resolves incident/offender image references (data URLs or safe remote HTTPS URLs) to raw bytes.
/// </summary>
public interface IImageReferenceContentResolver
{
	Task<byte[]?> ResolveAsync(string? imageReference, CancellationToken cancellationToken = default);
}
