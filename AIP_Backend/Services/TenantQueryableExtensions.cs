#nullable enable

using AIPBackend.Models;

namespace AIPBackend.Services
{
	internal static class TenantQueryableExtensions
	{
		public static IQueryable<Site> ApplyTenantScope(
			this IQueryable<Site> query,
			TenantCustomerFilter customerFilter,
			UserRequestContext context)
		{
			if (!customerFilter.Unrestricted)
			{
				if (customerFilter.SingleCustomerId.HasValue)
				{
					query = query.Where(s => s.fkCustomerID == customerFilter.SingleCustomerId.Value);
				}
				else if (customerFilter.CustomerIds.Count > 0)
				{
					query = query.Where(s => customerFilter.CustomerIds.Contains(s.fkCustomerID));
				}
			}
			else if (customerFilter.SingleCustomerId.HasValue)
			{
				query = query.Where(s => s.fkCustomerID == customerFilter.SingleCustomerId.Value);
			}

			if (context.AccessibleSiteIds.Count > 0)
			{
				var siteIds = context.AccessibleSiteIds
					.Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
					.Where(id => id.HasValue)
					.Select(id => id!.Value)
					.ToList();

				if (siteIds.Count > 0)
				{
					query = query.Where(s => siteIds.Contains(s.SiteID));
				}
			}

			return query;
		}

		public static IQueryable<Region> ApplyTenantScope(
			this IQueryable<Region> query,
			TenantCustomerFilter customerFilter)
		{
			if (!customerFilter.Unrestricted)
			{
				if (customerFilter.SingleCustomerId.HasValue)
				{
					query = query.Where(r => r.fkCustomerID == customerFilter.SingleCustomerId.Value);
				}
				else if (customerFilter.CustomerIds.Count > 0)
				{
					query = query.Where(r => customerFilter.CustomerIds.Contains(r.fkCustomerID));
				}
			}
			else if (customerFilter.SingleCustomerId.HasValue)
			{
				query = query.Where(r => r.fkCustomerID == customerFilter.SingleCustomerId.Value);
			}

			return query;
		}

		public static IQueryable<Customer> ApplyTenantScope(
			this IQueryable<Customer> query,
			TenantCustomerFilter customerFilter)
		{
			if (!customerFilter.Unrestricted)
			{
				if (customerFilter.SingleCustomerId.HasValue)
				{
					query = query.Where(c => c.CustomerId == customerFilter.SingleCustomerId.Value);
				}
				else if (customerFilter.CustomerIds.Count > 0)
				{
					query = query.Where(c => customerFilter.CustomerIds.Contains(c.CustomerId));
				}
			}
			else if (customerFilter.SingleCustomerId.HasValue)
			{
				query = query.Where(c => c.CustomerId == customerFilter.SingleCustomerId.Value);
			}

			return query;
		}
	}
}
