#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class RegionDto
    {
        public int RegionID { get; set; }
        public int fkCustomerID { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public string? RegionDescription { get; set; }
        public bool RecordIsDeletedYN { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DateModified { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class RegionCreateRequestDto
    {
        [Required]
        public int fkCustomerID { get; set; }

        [Required]
        [MaxLength(100)]
        public string RegionName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RegionDescription { get; set; }
    }

    public class RegionUpdateRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string RegionName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RegionDescription { get; set; }
    }



    public class SiteDto
    {
        public int SiteID { get; set; }
        public int fkCustomerID { get; set; }
        public int fkRegionID { get; set; }
        public bool CoreSiteYN { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? SINNumber { get; set; }
        public string? LocationType { get; set; }
        public string? BuildingName { get; set; }
        public string? NumberandStreet { get; set; }
        public string? VillageOrSuburb { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        public string? TelephoneNumber { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string? Details { get; set; }
        public DateTime? SiteSurveyComplete { get; set; }
        public DateTime? AssignmentInstructionsIssued { get; set; }
        public DateTime? RiskAssessmentIssued { get; set; }
        public bool RecordIsDeletedYN { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? DateModified { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class SiteCreateRequestDto
    {
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

        [MaxLength(10)]
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

        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SiteUpdateRequestDto
    {
        public bool CoreSiteYN { get; set; } = false;

        [MaxLength(100)]
        public string? LocationName { get; set; }

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

        [MaxLength(10)]
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

        [Required]
        [MaxLength(450)]
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
