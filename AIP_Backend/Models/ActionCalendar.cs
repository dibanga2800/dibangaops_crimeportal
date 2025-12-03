using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class ActionCalendar
    {
        [Key]
        public int ActionCalendarId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string TaskTitle { get; set; }

        public string? TaskDescription { get; set; }

        [Required]
        [MaxLength(100)]
        public required string TaskStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public required string PriorityLevel { get; set; }

        [Required]
        public required string AssignTo { get; set; }

        [ForeignKey("AssignTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public bool IsRecurring { get; set; }

        public DateTime? ReminderDate { get; set; }

        public bool RecordIsDeletedYN { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        public required string CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        public DateTime? DateModified { get; set; }

        public string? ModifiedBy { get; set; }

        [ForeignKey("ModifiedBy")]
        public virtual ApplicationUser? ModifiedByUser { get; set; }
    }
} 