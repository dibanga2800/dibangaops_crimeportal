using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AIPBackend.Services.Auth;

/// <summary>
/// HMAC-SHA256 hasher for short-lived email OTP codes, keyed per deployment and bound to user id.
/// Supports legacy plaintext values until they expire (codes issued before hashing rollout).
/// </summary>
public sealed class PendingTwoFactorCodeHasher : IPendingTwoFactorCodeHasher
{
	private readonly byte[] _hmacKey;

	public PendingTwoFactorCodeHasher(IConfiguration configuration)
	{
		var pepper = configuration["TwoFactor:CodeHashKey"]
			?? configuration["Jwt:Key"]
			?? throw new InvalidOperationException("TwoFactor:CodeHashKey or Jwt:Key must be configured.");

		_hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes(pepper));
	}

	public string Hash(string code, string userId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(code);
		ArgumentException.ThrowIfNullOrWhiteSpace(userId);

		var normalizedCode = NormalizeCode(code);
		var payload = $"{userId}:{normalizedCode}";
		var hash = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(payload));
		return Convert.ToHexString(hash);
	}

	public bool Verify(string code, string userId, string storedValue)
	{
		if (string.IsNullOrWhiteSpace(storedValue))
		{
			return false;
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(code);
		ArgumentException.ThrowIfNullOrWhiteSpace(userId);

		if (IsLegacyPlaintextCode(storedValue))
		{
			return FixedTimeEquals(NormalizeCode(code), storedValue.Trim());
		}

		var expected = Hash(code, userId);
		return FixedTimeEquals(expected, storedValue);
	}

	private static bool IsLegacyPlaintextCode(string storedValue)
	{
		var trimmed = storedValue.Trim();
		return trimmed.Length is >= 4 and <= 12 && trimmed.All(char.IsDigit);
	}

	private static string NormalizeCode(string code) => code.Trim();

	private static bool FixedTimeEquals(string left, string right)
	{
		var leftBytes = Encoding.UTF8.GetBytes(left);
		var rightBytes = Encoding.UTF8.GetBytes(right);
		return leftBytes.Length == rightBytes.Length
			&& CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
	}
}
