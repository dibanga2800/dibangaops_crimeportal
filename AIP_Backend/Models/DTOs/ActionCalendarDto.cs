using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class ActionCalendarDto
    {
        public int ActionCalendarId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
        public string TaskStatus { get; set; } = string.Empty;
        public string PriorityLevel { get; set; } = string.Empty;
        public string AssignTo { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public DateTime? ReminderDate { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime? DateModified { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
    }

    public class CreateActionCalendarDto
    {
        [Required]
        [MaxLength(255)]
        public string TaskTitle { get; set; } = string.Empty;

        public string? TaskDescription { get; set; }

        [Required]
        [MaxLength(100)]
        public string TaskStatus { get; set; } = "pending";

        [Required]
        [MaxLength(50)]
        public string PriorityLevel { get; set; } = string.Empty;

        [Required]
        public string AssignTo { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public bool IsRecurring { get; set; }

        public DateTime? ReminderDate { get; set; }
    }

    public class UpdateActionCalendarDto
    {
        [Required]
        [MaxLength(255)]
        public string TaskTitle { get; set; } = string.Empty;

        public string? TaskDescription { get; set; }

        [Required]
        [MaxLength(100)]
        public string TaskStatus { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PriorityLevel { get; set; } = string.Empty;

        [Required]
        public string AssignTo { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public bool IsRecurring { get; set; }

        public DateTime? ReminderDate { get; set; }
    }

    public class ActionCalendarResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ActionCalendarDto Data { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ActionCalendarsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ActionCalendarDto> Data { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class ActionCalendarStatisticsDto
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }
        public int Blocked { get; set; }
        public int HighPriority { get; set; }
        public int DueToday { get; set; }
        public int Overdue { get; set; }
    }

    public class ActionCalendarStatusUpdateDto
    {
        public int ActionCalendarStatusUpdateId { get; set; }
        public int ActionCalendarId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime UpdateDate { get; set; }
        public string? UpdatedBy { get; set; }
        public string UpdatedByUserName { get; set; } = string.Empty;
    }

    public class CreateActionCalendarStatusUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public class ActionCalendarStatusUpdateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ActionCalendarStatusUpdateDto Data { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ActionCalendarStatusUpdatesResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ActionCalendarStatusUpdateDto> Data { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
