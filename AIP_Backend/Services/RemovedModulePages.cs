namespace AIPBackend.Services
{
	/// <summary>
	/// Identifies page-access rows for modules and routes that no longer exist in the application.
	/// </summary>
	internal static class RemovedModulePages
	{
		private static readonly HashSet<string> ProtectedPageIds = new(StringComparer.OrdinalIgnoreCase)
		{
			"dashboard",
			"profile",
			"settings",
			"alert-rules",
			"data-analytics-hub",
			"user-setup",
			"employee-registration",
			"customer-setup",
			"barcode-catalog-import",
			"product-catalog",
			"incident-report",
			"incident-graph",
			"crime-intelligence",
			"customer-incident-report",
			"customer-incident-graph",
			"customer-crime-intelligence",
		};

		private static readonly HashSet<string> RemovedPageIds = new(StringComparer.OrdinalIgnoreCase)
		{
			"action-calendar",
			"bank-holiday",
			"bank-holidays",
			"holiday-request",
			"holiday-requests",
			"daily-occurrence-book",
			"daily-occurrence",
			"occurrence-book",
			"mystery-shopper",
			"site-visit",
			"officer-support",
			"manager-support",
			"safe-duress",
			"safe-duress-words",
			"crm",
			"contacts",
			"contacts-crm",
			"management-customer-reporting",
			"customer-reporting-page",
			"customer-reporting",
			"customer-satisfaction-report",
			"satisfaction-report",
			"customer-views-config",
			"be-safe-be-secure",
			"daily-activity-report",
			"daily-activity-reports",
			"incident-list",
			"stock-management",
			"employee-activity",
		};

		internal static bool IsRemoved(string? pageId, string? path)
		{
			if (string.IsNullOrWhiteSpace(pageId) && string.IsNullOrWhiteSpace(path))
			{
				return false;
			}

			var id = (pageId ?? string.Empty).Trim().ToLowerInvariant();
			var route = (path ?? string.Empty).Trim().ToLowerInvariant();

			if (!string.IsNullOrEmpty(id) && ProtectedPageIds.Contains(id))
			{
				return false;
			}

			if (!string.IsNullOrEmpty(id) && RemovedPageIds.Contains(id))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(id))
			{
				if (id.StartsWith("compliance-", StringComparison.Ordinal) ||
				    id.StartsWith("management-", StringComparison.Ordinal) ||
				    id.StartsWith("recruitment-", StringComparison.Ordinal))
				{
					return true;
				}

				if (ContainsAny(id,
					    "holiday",
					    "bank-holiday",
					    "occurrence",
					    "mystery-shopper",
					    "site-visit",
					    "officer-support",
					    "manager-support",
					    "safe-duress",
					    "customer-reporting",
					    "customer-satisfaction",
					    "satisfaction-report",
					    "action-calendar",
					    "incident-list",
					    "employee-activity",
					    "daily-activity",
					    "be-safe"))
				{
					return true;
				}

				if (id is "crm" or "contacts" or "contacts-crm")
				{
					return true;
				}
			}

			if (string.IsNullOrEmpty(route))
			{
				return false;
			}

			if (route.StartsWith("/compliance/", StringComparison.Ordinal) ||
			    route.StartsWith("/management/", StringComparison.Ordinal) ||
			    route.StartsWith("/recruitment/", StringComparison.Ordinal) ||
			    route.StartsWith("/crm", StringComparison.Ordinal) ||
			    route == "/contacts" ||
			    route.StartsWith("/contacts/", StringComparison.Ordinal))
			{
				return true;
			}

			return ContainsAny(route,
				"/action-calendar",
				"/holiday",
				"/bank-holiday",
				"/mystery-shopper",
				"/site-visit",
				"/daily-occurrence",
				"/occurrence-book",
				"/officer-support",
				"/manager-support",
				"/safe-duress",
				"/customer-reporting",
				"/customer-satisfaction",
				"/satisfaction-report",
				"/incident-list",
				"/employee-activity",
				"/stock-management",
				"/daily-activity-report",
				"/be-safe-be-secure");
		}

		private static bool ContainsAny(string value, params string[] needles) =>
			needles.Any(needle => value.Contains(needle, StringComparison.Ordinal));
	}
}
