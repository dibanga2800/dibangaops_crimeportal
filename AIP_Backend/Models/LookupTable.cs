using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class LookupTable
    {
        [Key]
        public int LookupId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Category { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Value { get; set; }

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Audit Fields
        public bool RecordIsDeletedYN { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("UpdatedBy")]
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }
}
