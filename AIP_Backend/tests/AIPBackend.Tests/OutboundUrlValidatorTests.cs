using AIPBackend.Services.Security;

namespace AIPBackend.Tests;

public class OutboundUrlValidatorTests
{
	[Theory]
	[InlineData("https://93.184.216.34/image.jpg")]
	[InlineData("https://1.1.1.1/asset")]
	public async Task ValidateFetchUrlAsync_allows_public_https(string url)
	{
		var uri = await OutboundUrlValidator.ValidateFetchUrlAsync(url, allowHttp: false);
		Assert.NotNull(uri);
	}

	[Theory]
	[InlineData("http://127.0.0.1/")]
	[InlineData("https://localhost/secret")]
	[InlineData("https://169.254.169.254/latest/meta-data/")]
	[InlineData("ftp://example.com/file")]
	public async Task ValidateFetchUrlAsync_blocks_restricted_or_invalid_urls(string url)
	{
		var uri = await OutboundUrlValidator.ValidateFetchUrlAsync(url, allowHttp: true);
		Assert.Null(uri);
	}

}
