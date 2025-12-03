using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    /// <summary>
    /// DTO for employee registration (create) operations
    /// </summary>
    public class EmployeeRegistrationRequest
    {
        // Required fields
        [Required(ErrorMessage = "Employee number is required")]
        [MaxLength(50)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [MaxLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee status is required")]
        [MaxLength(100)]
        public string EmployeeStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employment type is required")]
        [MaxLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        // Optional fields
        [MaxLength(100)]
        public string? AipAccessLevel { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(255)]
        [EmailAddress]
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

        public bool? DrivingLicenceCopyTaken { get; set; }

        public bool? SixMonthlyCheck { get; set; }

        // Checks and References
        public bool? GraydonCheckAuthorised { get; set; }

        [MaxLength(500)]
        public string? GraydonCheckDetails { get; set; }

        public bool? InitialOralReferencesComplete { get; set; }

        public DateTime? InitialOralReferencesDate { get; set; }

        public bool? WrittenRefsComplete { get; set; }

        public DateTime? WrittenRefsCompleteDate { get; set; }

        public bool? QuickStarterFormCompleted { get; set; }

        // Employment Documentation Status
        [MaxLength(100)]
        public string? WorkingTimeDirective { get; set; }

        public bool? WorkingTimeDirectiveComplete { get; set; }

        public bool? ContractOfEmploymentSigned { get; set; }

        public bool? PhotoTaken { get; set; }

        [MaxLength(500)]
        public string? PhotoFile { get; set; }

        public bool? IdCardIssued { get; set; }

        public bool? EquipmentIssued { get; set; }

        public bool? UniformIssued { get; set; }

        public bool? NextOfKinDetailsComplete { get; set; }

        [MaxLength(50)]
        public string? PeopleHoursPin { get; set; }

        // Training and Induction
        public DateTime? FullRotasIssued { get; set; }

        public DateTime? InductionAndTrainingBooked { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Trainer { get; set; }
    }

    /// <summary>
    /// DTO for employee update operations - all fields optional
    /// </summary>
    public class EmployeeUpdateRequest
    {
        [MaxLength(50)]
        public string? EmployeeNumber { get; set; }

        [MaxLength(50)]
        public string? Title { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? Surname { get; set; }

        public DateTime? StartDate { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        [MaxLength(100)]
        public string? EmployeeStatus { get; set; }

        [MaxLength(50)]
        public string? EmploymentType { get; set; }

        [MaxLength(100)]
        public string? AipAccessLevel { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        [MaxLength(255)]
        [EmailAddress]
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

        public bool? DrivingLicenceCopyTaken { get; set; }

        public bool? SixMonthlyCheck { get; set; }

        // Checks and References
        public bool? GraydonCheckAuthorised { get; set; }

        [MaxLength(500)]
        public string? GraydonCheckDetails { get; set; }

        public bool? InitialOralReferencesComplete { get; set; }

        public DateTime? InitialOralReferencesDate { get; set; }

        public bool? WrittenRefsComplete { get; set; }

        public DateTime? WrittenRefsCompleteDate { get; set; }

        public bool? QuickStarterFormCompleted { get; set; }

        // Employment Documentation Status
        [MaxLength(100)]
        public string? WorkingTimeDirective { get; set; }

        public bool? WorkingTimeDirectiveComplete { get; set; }

        public bool? ContractOfEmploymentSigned { get; set; }

        public bool? PhotoTaken { get; set; }

        [MaxLength(500)]
        public string? PhotoFile { get; set; }

        public bool? IdCardIssued { get; set; }

        public bool? EquipmentIssued { get; set; }

        public bool? UniformIssued { get; set; }

        public bool? NextOfKinDetailsComplete { get; set; }

        [MaxLength(50)]
        public string? PeopleHoursPin { get; set; }

        // Training and Induction
        public DateTime? FullRotasIssued { get; set; }

        public DateTime? InductionAndTrainingBooked { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Trainer { get; set; }
    }

    /// <summary>
    /// DTO for employee registration response (simple response after create)
    /// </summary>
    public class EmployeeRegistrationResponse
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for detailed employee response (full details)
    /// </summary>
    public class EmployeeDetailResponse
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public DateTime StartDate { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? EmployeeStatus { get; set; }
        public string? EmploymentType { get; set; }
        public string? AipAccessLevel { get; set; }
        public string? Region { get; set; }
        public string? Email { get; set; }
        public string? ContactNumber { get; set; }
        public string? HouseName { get; set; }
        public string? NumberAndStreet { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? PostCode { get; set; }
        public string? SiaLicenceType { get; set; }
        public DateTime? SiaLicenceExpiry { get; set; }
        public bool? IsSiaLicenceExpired { get; set; }
        public bool? IsSiaLicenceExpiringSoon { get; set; }
        public string? Nationality { get; set; }
        public string? RightToWorkCondition { get; set; }
        public string? DrivingLicenceType { get; set; }
        public DateTime? DateDLChecked { get; set; }
        public bool? DrivingLicenceCopyTaken { get; set; }
        public bool? SixMonthlyCheck { get; set; }
        public bool? GraydonCheckAuthorised { get; set; }
        public string? GraydonCheckDetails { get; set; }
        public bool? InitialOralReferencesComplete { get; set; }
        public DateTime? InitialOralReferencesDate { get; set; }
        public bool? WrittenRefsComplete { get; set; }
        public DateTime? WrittenRefsCompleteDate { get; set; }
        public bool? QuickStarterFormCompleted { get; set; }
        public string? WorkingTimeDirective { get; set; }
        public bool? WorkingTimeDirectiveComplete { get; set; }
        public bool? ContractOfEmploymentSigned { get; set; }
        public bool? PhotoTaken { get; set; }
        public string? PhotoFile { get; set; }
        public bool? IdCardIssued { get; set; }
        public bool? EquipmentIssued { get; set; }
        public bool? UniformIssued { get; set; }
        public bool? NextOfKinDetailsComplete { get; set; }
        public string? PeopleHoursPin { get; set; }
        public DateTime? FullRotasIssued { get; set; }
        public DateTime? InductionAndTrainingBooked { get; set; }
        public string? Location { get; set; }
        public string? Trainer { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// DTO for employee list response (limited fields for list views)
    /// </summary>
    public class EmployeeListResponse
    {
        public int EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployeeStatus { get; set; } = string.Empty;
        public string? EmploymentType { get; set; }
        public string? Email { get; set; }
        public DateTime StartDate { get; set; }
        public string? SiaLicenceType { get; set; }
        public DateTime? SiaLicenceExpiry { get; set; }
        public bool IsSiaLicenceExpired { get; set; }
        public bool IsSiaLicenceExpiringSoon { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for paginated list of employees
    /// </summary>
    public class PaginatedEmployeeResponse
    {
        public List<EmployeeListResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
