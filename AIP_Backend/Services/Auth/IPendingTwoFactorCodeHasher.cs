namespace AIPBackend.Services.Auth;

/// <summary>
/// Hashes and verifies pending email-based two-factor codes at rest (never store plaintext in the database).
/// </summary>
public interface IPendingTwoFactorCodeHasher
{
	string Hash(string code, string userId);

	bool Verify(string code, string userId, string storedValue);
}
