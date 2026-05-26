using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AIPBackend.Services.Security;

public sealed class SafeOutboundContentFetcher : ISafeOutboundContentFetcher
{
	private const int DefaultMaxBytes = 10 * 1024 * 1024;

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IWebHostEnvironment _environment;
	private readonly ILogger<SafeOutboundContentFetcher> _logger;
	private readonly long _maxBytes;

	public SafeOutboundContentFetcher(
		IHttpClientFactory httpClientFactory,
		IWebHostEnvironment environment,
		IConfiguration configuration,
		ILogger<SafeOutboundContentFetcher> logger)
	{
		_httpClientFactory = httpClientFactory;
		_environment = environment;
		_logger = logger;
		_maxBytes = configuration.GetValue<long?>("OutboundFetch:MaxResponseBytes") ?? DefaultMaxBytes;
	}

	public async Task<byte[]?> TryFetchBytesAsync(string url, CancellationToken cancellationToken = default)
	{
		var allowHttp = _environment.IsDevelopment();
		var validatedUri = await OutboundUrlValidator.ValidateFetchUrlAsync(url, allowHttp, cancellationToken);
		if (validatedUri == null)
		{
			_logger.LogWarning("Blocked outbound fetch for URL (policy or DNS check failed).");
			return null;
		}

		var client = _httpClientFactory.CreateClient("SafeOutbound");
		using var request = new HttpRequestMessage(HttpMethod.Get, validatedUri);
		using var response = await client.SendAsync(
			request,
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		if (response.Content.Headers.ContentLength is > 0 and var length && length > _maxBytes)
		{
			_logger.LogWarning("Blocked outbound fetch: Content-Length {Length} exceeds limit.", length);
			return null;
		}

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var memory = new MemoryStream();
		var buffer = new byte[81920];
		long total = 0;
		int read;
		while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
		{
			total += read;
			if (total > _maxBytes)
			{
				_logger.LogWarning("Blocked outbound fetch: response exceeded {MaxBytes} bytes.", _maxBytes);
				return null;
			}

			memory.Write(buffer, 0, read);
		}

		return memory.ToArray();
	}
}
