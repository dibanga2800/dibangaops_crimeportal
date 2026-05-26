using System.Net;
using System.Net.Sockets;

namespace AIPBackend.Services.Security;

/// <summary>
/// Validates outbound HTTP(S) URLs to reduce SSRF risk (private networks, metadata endpoints, etc.).
/// </summary>
public static class OutboundUrlValidator
{
	private static readonly HashSet<string> BlockedHostnames = new(StringComparer.OrdinalIgnoreCase)
	{
		"localhost",
		"metadata.google.internal",
		"metadata.goog",
	};

	public static async Task<Uri?> ValidateFetchUrlAsync(
		string url,
		bool allowHttp,
		CancellationToken cancellationToken = default)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			return null;
		}

		if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
		    !(allowHttp && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}

		if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
		{
			return null;
		}

		if (uri.Port is < 0 or > 65535)
		{
			return null;
		}

		if (BlockedHostnames.Contains(uri.Host))
		{
			return null;
		}

		if (IPAddress.TryParse(uri.Host, out var literalIp))
		{
			return IsRestrictedAddress(literalIp) ? null : uri;
		}

		IPAddress[] addresses;
		try
		{
			addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
		}
		catch (SocketException)
		{
			return null;
		}

		if (addresses.Length == 0)
		{
			return null;
		}

		if (addresses.Any(IsRestrictedAddress))
		{
			return null;
		}

		return uri;
	}

	internal static bool IsRestrictedAddress(IPAddress address)
	{
		if (IPAddress.IsLoopback(address))
		{
			return true;
		}

		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			var bytes = address.GetAddressBytes();
			return bytes[0] switch
			{
				0 => true,
				10 => true,
				127 => true,
				169 when bytes[1] == 254 => true,
				172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
				192 when bytes[1] == 168 => true,
				100 when bytes[1] >= 64 && bytes[1] <= 127 => true,
				_ => false,
			};
		}

		if (address.AddressFamily == AddressFamily.InterNetworkV6)
		{
			if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
			{
				return true;
			}

			var bytes = address.GetAddressBytes();
			// Unique local (fc00::/7)
			if ((bytes[0] & 0xfe) == 0xfc)
			{
				return true;
			}
		}

		return false;
	}
}
