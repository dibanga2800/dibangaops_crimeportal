#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	/// <summary>
	/// DTO for Holiday Request (response)
	/// </summary>
	public class HolidayRequestDto
	{
		public string Id { get; set; } = string.Empty;
		public int EmployeeId { get; set; }
		public string OfficerId { get; set; } = string.Empty; // For frontend compatibility
		public string OfficerName { get; set; } = string.Empty;
		public string StartDate { get; set; } = string.Empty; // ISO date string
		public string EndDate { get; set; } = string.Empty; // ISO date string
		public string ReturnToWorkDate { get; set; } = string.Empty; // ISO date string
		public string DateOfRequest { get; set; } = string.Empty; // ISO date string
		public string AuthorisedBy { get; set; } = string.Empty;
			public string? DateAuthorised { get; set; } // ISO date string
		public string Status { get; set; } = "pending"; // pending, approved, denied
		public string? Comment { get; set; } // Original comment from employee
		public string? Reason { get; set; } // Reason provided by admin when approving/denying
		public int TotalDays { get; set; }
		public int? DaysLeftYTD { get; set; } // Days left for employee (out of 28 YTD)
		public bool Archived { get; set; } = false;
		public string CreatedAt { get; set; } = string.Empty;
		public string? CreatedBy { get; set; }
		public string? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
	}

	/// <summary>
	/// DTO for creating a new holiday request
	/// </summary>
	public class CreateHolidayRequestDto
	{
		[Required]
		public int OfficerId { get; set; } // EmployeeId

		[Required]
		public string StartDate { get; set; } = string.Empty; // ISO date string

		[Required]
		public string EndDate { get; set; } = string.Empty; // ISO date string

		[Required]
		public string ReturnToWorkDate { get; set; } = string.Empty; // ISO date string

		[MaxLength(450)]
		public string? AuthorisedBy { get; set; } // Optional - only set during admin approval

		[MaxLength(1000)]
		public string? Comment { get; set; }
	}

	/// <summary>
	/// DTO for updating an existing holiday request
	/// </summary>
	public class UpdateHolidayRequestDto
	{
		public int? OfficerId { get; set; } // EmployeeId
		public string? StartDate { get; set; } // ISO date string
		public string? EndDate { get; set; } // ISO date string
		public string? ReturnToWorkDate { get; set; } // ISO date string
		
		[MaxLength(450)]
		public string? AuthorisedBy { get; set; }
		
		[MaxLength(50)]
		public string? Status { get; set; } // pending, approved, denied
		
		public string? DateAuthorised { get; set; } // ISO date string
		
		[MaxLength(1000)]
		public string? Comment { get; set; } // Original comment from employee
		
		[MaxLength(1000)]
		public string? Reason { get; set; } // Reason provided by admin when approving/denying
		
		public int? DaysLeftYTD { get; set; } // Days left for employee (out of 28 YTD)
	}

	/// <summary>
	/// DTO for filtering holiday requests
	/// </summary>
	public class HolidayRequestFilterDto
	{
		public string? Search { get; set; }
		public string? Status { get; set; } // pending, approved, denied
		public bool? Archived { get; set; }
		public string? StartDateFrom { get; set; }
		public string? StartDateTo { get; set; }
		public int? EmployeeId { get; set; }
	}

	/// <summary>
	/// Response DTO for paginated list of holiday requests
	/// </summary>
	public class HolidayRequestListResponseDto
	{
		public List<HolidayRequestDto> Data { get; set; } = new();
		public int Total { get; set; }
		public int Page { get; set; }
		public int Limit { get; set; }
	}
}

