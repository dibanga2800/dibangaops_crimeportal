namespace AIPBackend.Services.Security;

public sealed class ImageReferenceContentResolver : IImageReferenceContentResolver
{
	private readonly ISafeOutboundContentFetcher _fetcher;

	public ImageReferenceContentResolver(ISafeOutboundContentFetcher fetcher)
	{
		_fetcher = fetcher;
	}

	public async Task<byte[]?> ResolveAsync(string? imageReference, CancellationToken cancellationToken = default)
	{
		var inlineBytes = TryDecodeBase64DataUrl(imageReference);
		if (inlineBytes is { Length: > 0 })
		{
			return inlineBytes;
		}

		if (string.IsNullOrWhiteSpace(imageReference))
		{
			return null;
		}

		return await _fetcher.TryFetchBytesAsync(imageReference, cancellationToken);
	}

	private static byte[]? TryDecodeBase64DataUrl(string? value)
	{
		if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var comma = value.IndexOf(',');
		if (comma < 0)
		{
			return null;
		}

		try
		{
			return Convert.FromBase64String(value[(comma + 1)..]);
		}
		catch
		{
			return null;
		}
	}
}
