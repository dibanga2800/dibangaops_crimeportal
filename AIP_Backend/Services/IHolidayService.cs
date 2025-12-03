using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IHolidayService
    {
        /// <summary>
        /// Submit a new holiday request
        /// </summary>
        /// <param name="request">Holiday request details</param>
        /// <param name="employeeId">Employee ID submitting the request</param>
        /// <param name="currentUserId">Current user ID</param>
        /// <returns>Holiday request response</returns>
        Task<HolidayRequestResponseDto> SubmitHolidayRequestAsync(HolidayRequestSubmissionDto request, int employeeId, string currentUserId);

        /// <summary>
        /// Approve a holiday request
        /// </summary>
        /// <param name="requestId">Holiday request ID</param>
        /// <param name="approvedBy">User ID who approved the request</param>
        /// <param name="comments">Approval comments</param>
        /// <returns>True if approved successfully</returns>
        Task<bool> ApproveHolidayRequestAsync(int requestId, string approvedBy, string? comments = null);

        /// <summary>
        /// Reject a holiday request
        /// </summary>
        /// <param name="requestId">Holiday request ID</param>
        /// <param name="rejectedBy">User ID who rejected the request</param>
        /// <param name="rejectionReason">Reason for rejection</param>
        /// <returns>True if rejected successfully</returns>
        Task<bool> RejectHolidayRequestAsync(int requestId, string rejectedBy, string rejectionReason);

        /// <summary>
        /// Cancel a holiday request
        /// </summary>
        /// <param name="requestId">Holiday request ID</param>
        /// <param name="cancelledBy">User ID who cancelled the request</param>
        /// <param name="cancellationReason">Reason for cancellation</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelHolidayRequestAsync(int requestId, string cancelledBy, string? cancellationReason = null);

        /// <summary>
        /// Modify a holiday request
        /// </summary>
        /// <param name="requestId">Holiday request ID</param>
        /// <param name="modifiedRequest">Modified request details</param>
        /// <param name="modifiedBy">User ID who modified the request</param>
        /// <returns>Updated holiday request</returns>
        Task<HolidayRequestResponseDto> ModifyHolidayRequestAsync(int requestId, HolidayRequestSubmissionDto modifiedRequest, string modifiedBy);

        /// <summary>
        /// Get holiday request by ID
        /// </summary>
        /// <param name="requestId">Holiday request ID</param>
        /// <returns>Holiday request details</returns>
        Task<HolidayRequestResponseDto> GetHolidayRequestByIdAsync(int requestId);

        /// <summary>
        /// Get all holiday requests for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>List of holiday requests</returns>
        Task<IEnumerable<HolidayRequestResponseDto>> GetHolidayRequestsByEmployeeAsync(int employeeId);

        /// <summary>
        /// Get all pending holiday requests for approval
        /// </summary>
        /// <param name="managerId">Manager ID</param>
        /// <returns>List of pending holiday requests</returns>
        Task<IEnumerable<HolidayRequestResponseDto>> GetPendingHolidayRequestsAsync(int managerId);

        /// <summary>
        /// Get holiday balance for an employee
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>Holiday balance details</returns>
        Task<HolidayBalanceDto> GetHolidayBalanceAsync(int employeeId);

        /// <summary>
        /// Check for holiday request conflicts
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="employeeId">Employee ID (optional, for excluding own requests)</param>
        /// <returns>List of conflicting employees</returns>
        Task<IEnumerable<string>> CheckHolidayConflictsAsync(DateTime startDate, DateTime endDate, int? employeeId = null);

        /// <summary>
        /// Send reminder emails for pending requests
        /// </summary>
        /// <param name="daysThreshold">Number of days to trigger reminder</param>
        /// <returns>Number of reminders sent</returns>
        Task<int> SendPendingRequestRemindersAsync(int daysThreshold = 3);
    }

    // DTOs for holiday requests
    public class HolidayRequestSubmissionDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfDays { get; set; }
        public string HolidayType { get; set; } = string.Empty; // Annual, Sick, Personal, etc.
        public string Reason { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public bool IsHalfDay { get; set; }
        public string HalfDayType { get; set; } = string.Empty; // Morning, Afternoon
    }

    public class HolidayRequestResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfDays { get; set; }
        public string HolidayType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected, Cancelled
        public string ApprovedBy { get; set; } = string.Empty;
        public string RejectedBy { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsHalfDay { get; set; }
        public string HalfDayType { get; set; } = string.Empty;
    }

    public class HolidayBalanceDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
        public int PendingDays { get; set; }
        public DateTime YearStart { get; set; }
        public DateTime YearEnd { get; set; }
        public List<HolidayRequestResponseDto> Requests { get; set; } = new List<HolidayRequestResponseDto>();
    }
}
