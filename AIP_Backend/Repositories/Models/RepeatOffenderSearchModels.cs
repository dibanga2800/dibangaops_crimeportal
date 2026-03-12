using System;
using System.Collections.Generic;

namespace AIPBackend.Repositories.Models
{
	public class RepeatOffenderSearchFilter
	{
		public string? Name { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string? Marks { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}

	public class RepeatOffenderRepositoryIncident
	{
		public int IncidentId { get; set; }
		public DateTime DateOfIncident { get; set; }
		public string StoreName { get; set; } = string.Empty;
		public string IncidentType { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? OffenderMarks { get; set; }
		public bool OffenderDetailsVerified { get; set; }
		public string? VerificationMethod { get; set; }
		public string? VerificationEvidenceImage { get; set; }
	}

	public class RepeatOffenderRepositoryResult
	{
		public string OffenderName { get; set; } = string.Empty;
		public DateTime? OffenderDOB { get; set; }
		public string? Gender { get; set; }
		public string? OffenderMarks { get; set; }
		public string? OffenderPlaceOfBirth { get; set; }
		public string? HouseName { get; set; }
		public string? NumberAndStreet { get; set; }
		public string? VillageOrSuburb { get; set; }
		public string? Town { get; set; }
		public string? County { get; set; }
		public string? PostCode { get; set; }
		public int IncidentCount { get; set; }
		public List<RepeatOffenderRepositoryIncident> RecentIncidents { get; set; } = new();
	}
}

