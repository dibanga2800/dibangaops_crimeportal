#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a holiday/leave request made by an employee
	/// </summary>
	public class HolidayRequest
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int EmployeeId { get; set; }

		[Required]
		public DateTime StartDate { get; set; }

		[Required]
		public DateTime EndDate { get; set; }

		[Required]
		public DateTime ReturnToWorkDate { get; set; }

		[Required]
		public DateTime DateOfRequest { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? AuthorisedBy { get; set; } // Optional - only set during admin approval

		public DateTime? DateAuthorised { get; set; }

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } = "pending"; // pending, approved, denied

	[MaxLength(1000)]
	public string? Comment { get; set; } // Original comment from employee when creating request

	[MaxLength(1000)]
	public string? Reason { get; set; } // Reason provided by admin when approving/denying

	[Required]
	public int TotalDays { get; set; }

	public int? DaysLeftYTD { get; set; } // Days left for employee (out of 28 YTD)

		[Required]
		public bool Archived { get; set; } = false;

		// Audit fields
		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		// Navigation properties (Employee model removed - keeping EmployeeId for data integrity)

		[ForeignKey("AuthorisedBy")]
		public virtual ApplicationUser? AuthorisedByUser { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual ApplicationUser? CreatedByUser { get; set; }

		[ForeignKey("UpdatedBy")]
		public virtual ApplicationUser? UpdatedByUser { get; set; }
	}
}

