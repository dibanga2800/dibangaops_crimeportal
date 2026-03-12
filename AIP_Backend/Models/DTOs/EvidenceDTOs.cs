#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	public class EvidenceItemDto
	{
		public int EvidenceItemId { get; set; }
		public int IncidentId { get; set; }
		public string Barcode { get; set; } = string.Empty;
		public string EvidenceType { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? StorageLocation { get; set; }
		public string Status { get; set; } = string.Empty;
		public string RegisteredAt { get; set; } = string.Empty;
		public string? RegisteredBy { get; set; }
		public List<EvidenceCustodyEventDto> CustodyEvents { get; set; } = new();
	}

	public class EvidenceCustodyEventDto
	{
		public int CustodyEventId { get; set; }
		public string EventType { get; set; } = string.Empty;
		public string? Notes { get; set; }
		public string? Location { get; set; }
		public string EventTimestamp { get; set; } = string.Empty;
		public string PerformedBy { get; set; } = string.Empty;
		public string? PerformedByName { get; set; }
	}

	public class RegisterEvidenceDto
	{
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
	}

	public class RecordCustodyEventDto
	{
		[Required]
		[MaxLength(50)]
		public string EventType { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? Notes { get; set; }

		[MaxLength(200)]
		public string? Location { get; set; }
	}

	public class BarcodeScanDto
	{
		[Required]
		[MaxLength(100)]
		public string Barcode { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? ScanLocation { get; set; }
	}

	public class BarcodeScanResultDto
	{
		public bool Found { get; set; }
		public EvidenceItemDto? EvidenceItem { get; set; }
		public int? IncidentId { get; set; }
		public string? IncidentType { get; set; }
		public string? SiteName { get; set; }
		public string Message { get; set; } = string.Empty;
	}

	public class EvidenceListResponseDto
	{
		public bool Success { get; set; } = true;
		public List<EvidenceItemDto> Data { get; set; } = new();
		public int TotalCount { get; set; }
	}
}
