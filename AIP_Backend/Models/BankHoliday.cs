using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	public class BankHoliday
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int OfficerId { get; set; } // Officer user ID (Employee model removed)

		[Required]
		public DateTime HolidayDate { get; set; }

		[Required]
		public DateTime DateOfRequest { get; set; } = DateTime.UtcNow;

		public int? AuthorisedByEmployeeId { get; set; } // Authorized by user ID (Employee model removed)

		public DateTime? DateAuthorised { get; set; }

		[Required]
		[MaxLength(20)]
		public string Status { get; set; } = "pending"; // pending, authorized, declined

		[MaxLength(1000)]
		public string? Reason { get; set; }

		[Required]
		public bool Archived { get; set; } = false;

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		[ForeignKey(nameof(CreatedBy))]
		public virtual ApplicationUser? CreatedByUser { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		[ForeignKey(nameof(UpdatedBy))]
		public virtual ApplicationUser? UpdatedByUser { get; set; }
	}
}

