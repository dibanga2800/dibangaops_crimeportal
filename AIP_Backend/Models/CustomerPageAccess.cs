using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	public class CustomerPageAccess
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[ForeignKey(nameof(Customer))]
		public int CustomerId { get; set; }

		[Required]
		[ForeignKey(nameof(PageAccess))]
		public int PageAccessId { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string CreatedBy { get; set; } = string.Empty;

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		public virtual Customer Customer { get; set; } = null!;

		public virtual PageAccess PageAccess { get; set; } = null!;
	}
}

