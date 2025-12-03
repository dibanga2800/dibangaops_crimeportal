#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class Site
    {
        [Key]
        public int SiteID { get; set; }

        [Required]
        public int fkCustomerID { get; set; }

        [Required]
        public int fkRegionID { get; set; }

        public bool CoreSiteYN { get; set; } = false;

        [Required]
        [MaxLength(100)]
        public string LocationName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? SINNumber { get; set; }

        [MaxLength(50)]
        public string? LocationType { get; set; }

        [MaxLength(100)]
        public string? BuildingName { get; set; }

        [MaxLength(200)]
        public string? NumberandStreet { get; set; }

        [MaxLength(100)]
        public string? VillageOrSuburb { get; set; }

        [MaxLength(100)]
        public string? Town { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(20)]
        public string? Postcode { get; set; }

        [MaxLength(20)]
        public string? TelephoneNumber { get; set; }

        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        public DateTime? SiteSurveyComplete { get; set; }
        public DateTime? AssignmentInstructionsIssued { get; set; }
        public DateTime? RiskAssessmentIssued { get; set; }

        public bool RecordIsDeletedYN { get; set; } = false;

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? DateModified { get; set; }

        [MaxLength(450)]
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        [ForeignKey("fkCustomerID")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("fkRegionID")]
        public virtual Region Region { get; set; } = null!;
    }
}
