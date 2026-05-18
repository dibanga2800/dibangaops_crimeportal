using AIPBackend.Services.Auth;

namespace AIPBackend.Tests;

public class TwoFactorPolicyTests
{
	[Theory]
	[InlineData("administrator")]
	[InlineData("manager")]
	public void RequiresLoginTwoFactor_mandatory_roles_always_require_2fa(string role)
	{
		Assert.True(TwoFactorPolicy.RequiresLoginTwoFactor(new[] { role }, twoFactorEnabledOnAccount: false));
	}

	[Theory]
	[InlineData("store")]
	[InlineData("security-officer")]
	[InlineData("officer")]
	public void RequiresLoginTwoFactor_store_and_officer_only_when_opted_in(string role)
	{
		Assert.False(TwoFactorPolicy.RequiresLoginTwoFactor(new[] { role }, twoFactorEnabledOnAccount: false));
		Assert.True(TwoFactorPolicy.RequiresLoginTwoFactor(new[] { role }, twoFactorEnabledOnAccount: true));
	}

	[Fact]
	public void RequiresLoginTwoFactor_manager_wins_when_combined_with_store()
	{
		Assert.True(TwoFactorPolicy.RequiresLoginTwoFactor(
			new[] { "store", "manager" },
			twoFactorEnabledOnAccount: false));
	}

	[Fact]
	public void CanUserDisableTwoFactor_false_for_mandatory_roles()
	{
		Assert.False(TwoFactorPolicy.CanUserDisableTwoFactor(new[] { "manager" }));
		Assert.True(TwoFactorPolicy.CanUserDisableTwoFactor(new[] { "store" }));
	}
}
