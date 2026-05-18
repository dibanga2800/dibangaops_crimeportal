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
		/// <summary>Throws when a non-administrator has no customer scope configured.</summary>
		void EnsureHasTenantScope();
		/// <summary>Resolves effective customer filter for list/detail queries.</summary>
		TenantCustomerFilter ResolveCustomerFilter(int? requestedCustomerId);
		/// <summary>Resolves effective site filter; may be null when not site-scoped.</summary>
		string? ResolveSiteFilter(string? requestedSiteId);
	}

	/// <summary>Customer filter applied to EF queries for tenant-scoped users.</summary>
	public sealed record TenantCustomerFilter
	{
		/// <summary>When true, no customer restriction (administrator).</summary>
		public bool Unrestricted { get; init; }
		/// <summary>Single customer when scoped to one tenant.</summary>
		public int? SingleCustomerId { get; init; }
		/// <summary>Allowed customers when user has multiple assignments.</summary>
		public IReadOnlyCollection<int> CustomerIds { get; init; } = Array.Empty<int>();
	}

	public sealed record UserRequestContext
	{
		public string UserId { get; init; } = string.Empty;
		public string Role { get; init; } = string.Empty;
		public string PageAccessRole { get; init; } = string.Empty;
		public int? CustomerId { get; init; }
		public IReadOnlyCollection<int> AssignedCustomerIds { get; init; } = Array.Empty<int>();
		public string? PrimarySiteId { get; init; }
		public IReadOnlyCollection<string> AssignedSiteIds { get; init; } = Array.Empty<string>();

		// Derived access scope
		public IReadOnlyCollection<int> AccessibleCustomerIds { get; init; } = Array.Empty<int>();
		public IReadOnlyCollection<string> AccessibleSiteIds { get; init; } = Array.Empty<string>();

		public bool IsAdministrator => string.Equals(Role, "administrator", StringComparison.OrdinalIgnoreCase);
		public bool IsManager => string.Equals(Role, "manager", StringComparison.OrdinalIgnoreCase);
		public bool IsOfficer => string.Equals(Role, "security-officer", StringComparison.OrdinalIgnoreCase);
		public bool IsStore => string.Equals(Role, "store", StringComparison.OrdinalIgnoreCase);
		/// <summary>Store / customer-portal users (not managers or officers with a customer link).</summary>
		public bool IsCustomer => IsStore;
		public bool HasAssignedCustomers => AssignedCustomerIds.Count > 0;
		public bool HasAssignedSites => AssignedSiteIds.Count > 0;
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

			var assignedCustomerClaim = principal.Claims.FirstOrDefault(c => c.Type == "AssignedCustomerIds")?.Value;
			var assignedIds = ParseAssignedCustomerIds(assignedCustomerClaim);

			var primarySiteId = principal.Claims.FirstOrDefault(c => c.Type == "PrimarySiteId")?.Value;
			var assignedSiteClaim = principal.Claims.FirstOrDefault(c => c.Type == "AssignedSiteIds")?.Value;
			var assignedSiteIds = ParseAssignedSiteIds(assignedSiteClaim);

			// Compute accessible scope
			IReadOnlyCollection<int> accessibleCustomerIds;
			IReadOnlyCollection<string> accessibleSiteIds;

			if (string.Equals(role, "administrator", StringComparison.OrdinalIgnoreCase))
			{
				// Admins are globally scoped - empty collections mean "no restriction"
				accessibleCustomerIds = Array.Empty<int>();
				accessibleSiteIds = Array.Empty<string>();
			}
			else if (string.Equals(role, "manager", StringComparison.OrdinalIgnoreCase))
			{
				if (customerId.HasValue)
				{
					accessibleCustomerIds = new[] { customerId.Value };
				}
				else if (assignedIds.Count > 0)
				{
					accessibleCustomerIds = assignedIds;
				}
				else
				{
					accessibleCustomerIds = Array.Empty<int>();
				}

				// Managers are not restricted by site within their customers
				accessibleSiteIds = Array.Empty<string>();
			}
			else if (string.Equals(role, "security-officer", StringComparison.OrdinalIgnoreCase))
			{
				if (assignedIds.Count > 0)
				{
					accessibleCustomerIds = assignedIds;
				}
				else if (customerId.HasValue)
				{
					accessibleCustomerIds = new[] { customerId.Value };
				}
				else
				{
					accessibleCustomerIds = Array.Empty<int>();
				}

				// Officers can optionally be scoped to specific sites
				accessibleSiteIds = assignedSiteIds.Count > 0 ? assignedSiteIds : Array.Empty<string>();
			}
			else if (string.Equals(role, "store", StringComparison.OrdinalIgnoreCase))
			{
				// Store users must be tied to a single customer + single primary site
				if (customerId.HasValue && !string.IsNullOrWhiteSpace(primarySiteId))
				{
					accessibleCustomerIds = new[] { customerId.Value };
					accessibleSiteIds = new[] { primarySiteId };
				}
				else
				{
					accessibleCustomerIds = Array.Empty<int>();
					accessibleSiteIds = Array.Empty<string>();
				}
			}
			else
			{
				// Default: use assigned customers if any; otherwise fall back to direct customerId
				if (assignedIds.Count > 0)
				{
					accessibleCustomerIds = assignedIds;
				}
				else if (customerId.HasValue)
				{
					accessibleCustomerIds = new[] { customerId.Value };
				}
				else
				{
					accessibleCustomerIds = Array.Empty<int>();
				}

				accessibleSiteIds = Array.Empty<string>();
			}

			_cachedContext = new UserRequestContext
			{
				UserId = userId,
				Role = role,
				PageAccessRole = pageAccessRole,
				CustomerId = customerId,
				AssignedCustomerIds = assignedIds,
				PrimarySiteId = primarySiteId,
				AssignedSiteIds = assignedSiteIds,
				AccessibleCustomerIds = accessibleCustomerIds,
				AccessibleSiteIds = accessibleSiteIds
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

			// Primary customer for customer-linked users
			if (context.IsCustomer && context.CustomerId.HasValue && context.CustomerId.Value == customerId)
				return;

			// Derived access scope for all non-admin roles
			if (context.AccessibleCustomerIds.Count > 0 && context.AccessibleCustomerIds.Contains(customerId))
				return;

			throw new ForbiddenAccessException("You do not have permission to access this customer.");
		}

		public void EnsureCanAccessRecord(int customerId, string? createdByUserId)
		{
			var context = GetCurrentContext();
			EnsureCanAccessCustomer(customerId);

			if ((context.IsStore || context.IsOfficer) && !context.IsAdministrator && !context.IsManager)
			{
				if (!string.Equals(createdByUserId, context.UserId, StringComparison.Ordinal))
				{
					throw new ForbiddenAccessException("You can only view or modify records you created.");
				}
			}
		}

		public void EnsureHasTenantScope()
		{
			var context = GetCurrentContext();
			if (context.IsAdministrator)
			{
				return;
			}

			if (context.AccessibleCustomerIds.Count > 0)
			{
				return;
			}

			throw new ForbiddenAccessException("Your account is not assigned to any customer. Contact an administrator.");
		}

		public TenantCustomerFilter ResolveCustomerFilter(int? requestedCustomerId)
		{
			var context = GetCurrentContext();
			if (context.IsAdministrator)
			{
				return new TenantCustomerFilter
				{
					Unrestricted = true,
					SingleCustomerId = requestedCustomerId
				};
			}

			EnsureHasTenantScope();

			if (context.IsCustomer && context.CustomerId.HasValue)
			{
				return new TenantCustomerFilter { SingleCustomerId = context.CustomerId.Value };
			}

			if (context.AccessibleCustomerIds.Count == 1)
			{
				var only = context.AccessibleCustomerIds.First();
				if (requestedCustomerId.HasValue && requestedCustomerId.Value != only)
				{
					throw new ForbiddenAccessException("You do not have permission to access this customer.");
				}

				return new TenantCustomerFilter { SingleCustomerId = only };
			}

			if (requestedCustomerId.HasValue)
			{
				if (!context.AccessibleCustomerIds.Contains(requestedCustomerId.Value))
				{
					throw new ForbiddenAccessException("You do not have permission to access this customer.");
				}

				return new TenantCustomerFilter { SingleCustomerId = requestedCustomerId.Value };
			}

			return new TenantCustomerFilter { CustomerIds = context.AccessibleCustomerIds };
		}

		public string? ResolveSiteFilter(string? requestedSiteId)
		{
			var context = GetCurrentContext();
			if (context.IsAdministrator || context.AccessibleSiteIds.Count == 0)
			{
				return requestedSiteId;
			}

			if (!string.IsNullOrWhiteSpace(requestedSiteId))
			{
				if (!context.AccessibleSiteIds.Contains(requestedSiteId))
				{
					throw new ForbiddenAccessException("You do not have permission to access this site.");
				}

				return requestedSiteId;
			}

			return context.AccessibleSiteIds.First();
		}

		private static IReadOnlyCollection<int> ParseAssignedCustomerIds(string? claimValue)
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

		private static IReadOnlyCollection<string> ParseAssignedSiteIds(string? claimValue)
		{
			if (string.IsNullOrWhiteSpace(claimValue))
			{
				return Array.Empty<string>();
			}

			// Sites may be stored as a comma-separated list or as a JSON array string
			try
			{
				// Try JSON array first
				var asJson = System.Text.Json.JsonSerializer.Deserialize<List<string>>(claimValue);
				if (asJson != null && asJson.Count > 0)
				{
					return asJson
						.Where(s => !string.IsNullOrWhiteSpace(s))
						.Select(s => s.Trim())
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToArray();
				}
			}
			catch
			{
				// Fallback to comma-separated string
			}

			var parts = claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var ids = parts
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Select(p => p.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
			return ids;
		}
	}
}

