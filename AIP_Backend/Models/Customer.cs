#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CompanyNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? VatNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "active";

        [MaxLength(50)]
        public string? CustomerType { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        // Address fields
        [MaxLength(100)]
        public string? Building { get; set; }

        [MaxLength(200)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? Village { get; set; }

        [MaxLength(100)]
        public string? Town { get; set; }

        [MaxLength(100)]
        public string? County { get; set; }

        [MaxLength(10)]
        public string? Postcode { get; set; }

        // Contact fields
        [MaxLength(20)]
        public string? ContactTitle { get; set; }

        [MaxLength(50)]
        public string? ContactForename { get; set; }

        [MaxLength(50)]
        public string? ContactSurname { get; set; }

        [MaxLength(100)]
        public string? ContactPosition { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        // Page assignments stored as JSON
        [MaxLength(4000)]
        public string? PageAssignments { get; set; }

        // Audit Fields
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<UserCustomerAssignment> UserCustomerAssignments { get; set; } = new List<UserCustomerAssignment>();
        public virtual ICollection<Region> Regions { get; set; } = new List<Region>();
        public virtual ICollection<Site> Sites { get; set; } = new List<Site>();
        public virtual ICollection<CustomerPageAccess> CustomerPageAccesses { get; set; } = new List<CustomerPageAccess>();
    }
}
