#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Represents a security incident report
	/// </summary>
	public class Incident
	{
		[Key]
		public int IncidentId { get; set; }

		// Core identification
		[Required]
		public int CustomerId { get; set; }

		[MaxLength(100)]
		public string? SiteId { get; set; }

		[MaxLength(100)]
		public string? RegionId { get; set; }

		// Location information
		[Required]
		[MaxLength(200)]
		[Column("StoreName")]
		public string StoreName { get; set; } = string.Empty;

		[MaxLength(200)]
		public string? RegionName { get; set; }

		[MaxLength(500)]
		public string? Location { get; set; }

		// Personnel information
		[Required]
		[MaxLength(200)]
		[Column("StaffMemberName")]
		public string StaffMemberName { get; set; } = string.Empty;

		[MaxLength(100)]
		[Column("StaffMemberRole")]
		public string? StaffMemberRole { get; set; }

		[MaxLength(50)]
		public string? OfficerType { get; set; }

		[MaxLength(200)]
		public string? DutyManagerName { get; set; }

		[MaxLength(200)]
		public string? AssignedTo { get; set; }

		// Time information
		[Required]
		public DateTime DateOfIncident { get; set; }

		[MaxLength(20)]
		public string? TimeOfIncident { get; set; }

		[Required]
		public DateTime DateInputted { get; set; } = DateTime.UtcNow;

		// Incident classification
		[Required]
		[MaxLength(100)]
		public string IncidentType { get; set; } = string.Empty;

		[MaxLength(50)]
		public string? ActionCode { get; set; }

		/// <summary>
		/// AI-assisted incident category (e.g. Shoplifting, Assault, Fraud).
		/// This is derived from free-text description and other signals.
		/// </summary>
		[MaxLength(100)]
		public string? IncidentCategory { get; set; }

		/// <summary>
		/// Confidence score for <see cref="IncidentCategory"/> between 0 and 1.
		/// </summary>
		public double? IncidentCategoryConfidence { get; set; }

		/// <summary>
		/// Machine-derived risk level (low/medium/high) independent of the manual priority field.
		/// </summary>
		[MaxLength(20)]
		public string? RiskLevel { get; set; }

		/// <summary>
		/// Normalised numeric risk score between 0 and 1.
		/// </summary>
		public double? RiskScore { get; set; }

		/// <summary>
		/// Version of the classifier that produced the current AI fields.
		/// </summary>
		[MaxLength(50)]
		public string? ClassificationVersion { get; set; }

		// Store incident involved categories as JSON array
		[MaxLength(2000)]
		public string? IncidentInvolved { get; set; }

		// Description and details
		[MaxLength(5000)]
		public string? Description { get; set; }

		[MaxLength(5000)]
		public string? IncidentDetails { get; set; }

		[MaxLength(5000)]
		public string? StoreComments { get; set; }

		// Financial information
		[Column(TypeName = "decimal(18,2)")]
		public decimal? TotalValueRecovered { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal? ValueRecovered { get; set; }

		public int? QuantityRecovered { get; set; }

		// Police involvement
		public bool PoliceInvolvement { get; set; } = false;

		[MaxLength(100)]
		public string? UrnNumber { get; set; }

		[MaxLength(100)]
		public string? CrimeRefNumber { get; set; }

		[MaxLength(100)]
		public string? PoliceId { get; set; }

		// Status tracking
		[MaxLength(50)]
		public string Status { get; set; } = "pending";

		[MaxLength(50)]
		public string? Priority { get; set; }

		[MaxLength(2000)]
		public string? ActionTaken { get; set; }

		public bool EvidenceAttached { get; set; } = false;

		// Store witness statements as JSON array
		[MaxLength(5000)]
		public string? WitnessStatements { get; set; }

		// Store involved parties as JSON array
		[MaxLength(2000)]
		public string? InvolvedParties { get; set; }

		[MaxLength(100)]
		public string? ReportNumber { get; set; }

		// Offender information
		[MaxLength(100)]
		public string? OffenderId { get; set; }

		[MaxLength(200)]
		public string? OffenderName { get; set; }

		[MaxLength(50)]
		public string? OffenderSex { get; set; }

		[MaxLength(50)]
		public string? Gender { get; set; }

		public DateTime? OffenderDOB { get; set; }

		[MaxLength(200)]
		public string? OffenderPlaceOfBirth { get; set; }

		[MaxLength(500)]
		public string? OffenderMarks { get; set; }

		public bool OffenderDetailsVerified { get; set; } = false;

		[MaxLength(100)]
		public string? VerificationMethod { get; set; }

		[Column(TypeName = "nvarchar(max)")]
		public string? VerificationEvidenceImage { get; set; }

		// Offender address fields
		[MaxLength(100)]
		public string? OffenderHouseName { get; set; }

		[MaxLength(200)]
		public string? OffenderNumberAndStreet { get; set; }

		[MaxLength(100)]
		public string? OffenderVillageOrSuburb { get; set; }

		[MaxLength(100)]
		public string? OffenderTown { get; set; }

		[MaxLength(100)]
		public string? OffenderCounty { get; set; }

		[MaxLength(20)]
		public string? OffenderPostCode { get; set; }

		// Store modus operandi as JSON array
		[MaxLength(2000)]
		public string? ModusOperandi { get; set; }

		// Special fields
		[MaxLength(2000)]
		public string? ArrestSaveComment { get; set; }

		// Audit Fields
		[Column("RecordIsDeletedYN")]
		public bool RecordIsDeletedYN { get; set; } = false;

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual ApplicationUser? CreatedByUser { get; set; }

		public DateTime? UpdatedAt { get; set; }

		[MaxLength(450)]
		public string? UpdatedBy { get; set; }

		[ForeignKey("UpdatedBy")]
		public virtual ApplicationUser? UpdatedByUser { get; set; }

		// Navigation Properties
		[ForeignKey("CustomerId")]
		public virtual Customer Customer { get; set; } = null!;

		// Collection of stolen items
		public virtual ICollection<StolenItem> StolenItems { get; set; } = new List<StolenItem>();
	}
}

