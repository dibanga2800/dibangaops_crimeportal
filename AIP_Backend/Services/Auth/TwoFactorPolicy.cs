namespace AIPBackend.Services.Auth
{
	/// <summary>
	/// Login-time two-factor rules.
	/// Administrator and manager: mandatory on every login.
	/// Store and security-officer: only when the user enables TwoFactorEnabled on their profile.
	/// </summary>
	public static class TwoFactorPolicy
	{
		public static bool RequiresLoginTwoFactor(IEnumerable<string> roles, bool twoFactorEnabledOnAccount)
		{
			if (IsMandatoryRole(roles))
			{
				return true;
			}

			return twoFactorEnabledOnAccount;
		}

		public static bool IsMandatoryRole(IEnumerable<string> roles) =>
			roles.Any(IsMandatoryRole);

		public static bool IsMandatoryRole(string role) =>
			!string.IsNullOrWhiteSpace(role)
			&& (role.Equals("administrator", StringComparison.OrdinalIgnoreCase)
				|| role.Equals("manager", StringComparison.OrdinalIgnoreCase));

		/// <summary>
		/// Roles that may turn 2FA on/off from profile (not required by default).
		/// </summary>
		public static bool IsUserControlledTwoFactorRole(string role) =>
			!string.IsNullOrWhiteSpace(role)
			&& (role.Equals("store", StringComparison.OrdinalIgnoreCase)
				|| role.Equals("security-officer", StringComparison.OrdinalIgnoreCase)
				|| role.Equals("officer", StringComparison.OrdinalIgnoreCase));

		public static bool CanUserDisableTwoFactor(IEnumerable<string> roles) =>
			!IsMandatoryRole(roles);
	}
}
