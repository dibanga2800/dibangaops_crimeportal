namespace AIPBackend.Services.Security;

public interface ISafeOutboundContentFetcher
{
	Task<byte[]?> TryFetchBytesAsync(string url, CancellationToken cancellationToken = default);
}
