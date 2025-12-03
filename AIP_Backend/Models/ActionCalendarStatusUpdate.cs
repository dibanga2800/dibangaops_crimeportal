using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class ActionCalendarStatusUpdate
    {
        [Key]
        public int ActionCalendarStatusUpdateId { get; set; }

        public int ActionCalendarId { get; set; }

        [ForeignKey("ActionCalendarId")]
        public virtual ActionCalendar? ActionCalendar { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Status { get; set; }

        public string? Comment { get; set; }

        public DateTime UpdateDate { get; set; }

        public string? UpdatedBy { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }
} 