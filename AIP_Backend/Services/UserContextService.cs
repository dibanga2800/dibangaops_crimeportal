using System.Security.Claims;
using AIPBackend.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AIPBackend.Services
{
	public interface IUserContextService
	{
		UserRequestContext GetCurrentContext();
		void EnsureCanAccessCustomer(int customerId);
		void EnsureCanAccessRecord(int customerId, string? createdByUserId);
	}

	public sealed record UserRequestContext
	{
		public string UserId { get; init; } = string.Empty;
		public string Role { get; init; } = string.Empty;
		public string PageAccessRole { get; init; } = string.Empty;
		public int? CustomerId { get; init; }
		public IReadOnlyCollection<int> AssignedCustomerIds { get; init; } = Array.Empty<int>();
		public bool IsAdministrator => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);
		public bool IsOfficer => string.Equals(Role, "AdvantageOneOfficer", StringComparison.OrdinalIgnoreCase) ||
		                        string.Equals(Role, "AdvantageOneHOOfficer", StringComparison.OrdinalIgnoreCase);
		public bool IsCustomer => string.Equals(Role, "CustomerSiteManager", StringComparison.OrdinalIgnoreCase) ||
		                          string.Equals(Role, "CustomerHOManager", StringComparison.OrdinalIgnoreCase);
	}

	public class UserContextService : IUserContextService
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private UserRequestContext? _cachedContext;

		public UserContextService(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}

		public UserRequestContext GetCurrentContext()
		{
			if (_cachedContext != null)
			{
				return _cachedContext;
			}

			var principal = _httpContextAccessor.HttpContext?.User;
			if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
			{
				throw new ForbiddenAccessException("User context is not available.");
			}

			var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ForbiddenAccessException("Authenticated user is missing identifier claim.");
			}

			var role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
			var pageAccessRole = principal.Claims.FirstOrDefault(c => c.Type == "PageAccessRole")?.Value ?? role;
			var customerIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value;
			int? customerId = null;
			if (int.TryParse(customerIdClaim, out var parsedCustomerId))
			{
				customerId = parsedCustomerId;
			}

			var assignedClaim = principal.Claims.FirstOrDefault(c => c.Type == "AssignedCustomerIds")?.Value;
			var assignedIds = ParseAssignedIds(assignedClaim);

			_cachedContext = new UserRequestContext
			{
				UserId = userId,
				Role = role,
				PageAccessRole = pageAccessRole,
				CustomerId = customerId,
				AssignedCustomerIds = assignedIds
			};

			return _cachedContext;
		}

		public void EnsureCanAccessCustomer(int customerId)
		{
			var context = GetCurrentContext();
			if (context.IsAdministrator || customerId <= 0)
			{
				return;
			}

			if (context.IsCustomer)
			{
				if (context.CustomerId.HasValue && context.CustomerId.Value == customerId)
				{
					return;
				}
				throw new ForbiddenAccessException("You do not have permission to access this customer.");
			}

			if (context.IsOfficer)
			{
				if (context.AssignedCustomerIds.Contains(customerId))
				{
					return;
				}
				throw new ForbiddenAccessException("This customer is not assigned to you.");
			}

			throw new ForbiddenAccessException("Current role is not allowed to access this customer.");
		}

		public void EnsureCanAccessRecord(int customerId, string? createdByUserId)
		{
			var context = GetCurrentContext();
			EnsureCanAccessCustomer(customerId);

			if (context.IsOfficer)
			{
				if (!string.Equals(createdByUserId, context.UserId, StringComparison.Ordinal))
				{
					throw new ForbiddenAccessException("You can only view or modify records you created.");
				}
			}
		}

		private static IReadOnlyCollection<int> ParseAssignedIds(string? claimValue)
		{
			if (string.IsNullOrWhiteSpace(claimValue))
			{
				return Array.Empty<int>();
			}

			var parts = claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var ids = parts.Select(part => int.TryParse(part, out var value) ? value : (int?)null)
				.Where(value => value.HasValue)
				.Select(value => value!.Value)
				.Distinct()
				.ToArray();
			return ids;
		}
	}
}

