using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
    public class UserCustomerAssignment
    {
        [Key]
        public int UserCustomerAssignmentId { get; set; }

        [Required]
        [MaxLength(450)]
        public required string UserId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        // Audit Fields
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(450)]
        public required string CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }
    }
}
