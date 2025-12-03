#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models
{
    public class Region
    {
        [Key]
        public int RegionID { get; set; }

        [Required]
        public int fkCustomerID { get; set; }

        [Required]
        [MaxLength(100)]
        public string RegionName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RegionDescription { get; set; }

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
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<Site> Sites { get; set; } = new List<Site>();
    }
}
