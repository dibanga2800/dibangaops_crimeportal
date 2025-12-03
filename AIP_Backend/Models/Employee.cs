using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class Employee
    {
        // Primary Key
        [Key]
        public int EmployeeId { get; set; }

        // Basic Information (Required)
        [Required]
        [MaxLength(50)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EmployeeStatus { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        // Optional Fields
        [MaxLength(100)]
        public string? AipAccessLevel { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        // Address Information
        [MaxLength(100)]
        public string? HouseName { get; set; }

        [MaxLength(100)]
        public string? NumberAndStreet { get; set; }

        [MaxLength(100)]
        public string? Town { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(20)]
        public string? PostCode { get; set; }

        // SIA License Information
        [MaxLength(100)]
        public string? SiaLicenceType { get; set; }

        public DateTime? SiaLicenceExpiry { get; set; }

        // Personal Information
        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(255)]
        public string? RightToWorkCondition { get; set; }

        // Driving License Information
        [MaxLength(100)]
        public string? DrivingLicenceType { get; set; }

        public DateTime? DateDLChecked { get; set; }

        public bool DrivingLicenceCopyTakenYN { get; set; } = false;

        public bool SixMonthlyCheck { get; set; } = false;

        // Checks and References
        public bool GraydonCheckAuthorised { get; set; } = false;

        [MaxLength(500)]
        public string? GraydonCheckDetails { get; set; }

        public bool InitialOralReferencesComplete { get; set; } = false;

        public DateTime? InitialOralReferencesDate { get; set; }

        public bool WrittenRefsComplete { get; set; } = false;

        public DateTime? WrittenRefsCompleteDate { get; set; }

        public bool QuickStarterFormCompletedYN { get; set; } = false;

        // Employment Documentation Status
        [MaxLength(100)]
        public string? WorkingTimeDirective { get; set; }

        public bool WorkingTimeDirectiveComplete { get; set; } = false;

        public bool ContractOfEmploymentSignedYN { get; set; } = false;

        public bool PhotoTakenYN { get; set; } = false;

        [MaxLength(500)]
        public string? PhotoFile { get; set; }

        public bool IDCardIssuedYN { get; set; } = false;

        public bool EquipmentIssuedYN { get; set; } = false;

        public bool UniformIssuedYN { get; set; } = false;

        public bool NextOfKinDetailsComplete { get; set; } = false;

        [MaxLength(50)]
        public string? PeopleHoursPin { get; set; }

        // Training and Induction
        public DateTime? FullRotasIssued { get; set; }

        public DateTime? InductionAndTrainingBooked { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Trainer { get; set; }

        // User Linking (optional link to ApplicationUser)
        [MaxLength(450)]
        public string? UserId { get; set; }

        // Audit Fields
        public bool RecordIsDeletedYN { get; set; } = false;

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime? DateModified { get; set; }

        [MaxLength(450)]
        public string? ModifiedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string FullName
        {
            get => $"{FirstName} {Surname}".Trim();
            set { } // Empty setter for EF compatibility
        }

        [NotMapped]
        public bool IsSiaLicenceExpired
        {
            get
            {
                if (!SiaLicenceExpiry.HasValue) return false;
                return SiaLicenceExpiry.Value < DateTime.UtcNow;
            }
        }

        [NotMapped]
        public bool IsSiaLicenceExpiringSoon
        {
            get
            {
                if (!SiaLicenceExpiry.HasValue) return false;
                var daysUntilExpiry = (SiaLicenceExpiry.Value - DateTime.UtcNow).Days;
                return daysUntilExpiry > 0 && daysUntilExpiry <= 30; // Expiring within 30 days
            }
        }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        [ForeignKey("ModifiedBy")]
        public virtual ApplicationUser? ModifiedByUser { get; set; }
    }
}
