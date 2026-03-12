#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	public class EvidenceItem
	{
		[Key]
		public int EvidenceItemId { get; set; }

		[Required]
		public int IncidentId { get; set; }

		[Required]
		[MaxLength(100)]
		public string Barcode { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string EvidenceType { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Description { get; set; }

		[MaxLength(200)]
		public string? StorageLocation { get; set; }

		[Required]
		[MaxLength(50)]
		public string Status { get; set; } = "registered";

		public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? RegisteredBy { get; set; }

		[ForeignKey("IncidentId")]
		public virtual Incident Incident { get; set; } = null!;

		public virtual ICollection<EvidenceCustodyEvent> CustodyEvents { get; set; } = new List<EvidenceCustodyEvent>();
	}

	public class EvidenceCustodyEvent
	{
		[Key]
		public int CustodyEventId { get; set; }

		[Required]
		public int EvidenceItemId { get; set; }

		[Required]
		[MaxLength(50)]
		public string EventType { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Notes { get; set; }

		[MaxLength(200)]
		public string? Location { get; set; }

		[Required]
		public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;

		[Required]
		[MaxLength(450)]
		public string PerformedBy { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? PerformedByName { get; set; }

		[ForeignKey("EvidenceItemId")]
		public virtual EvidenceItem EvidenceItem { get; set; } = null!;
	}
}
