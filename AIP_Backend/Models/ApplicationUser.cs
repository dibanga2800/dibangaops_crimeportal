using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AIPBackend.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Basic Information (from frontend BaseUser interface)
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Role and Access Information
        private string _role = string.Empty;
        private string _pageAccessRole = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role
        {
            get => _role;
            set => _role = NormalizeRole(value);
        }

        [Required]
        [MaxLength(50)]
        public string PageAccessRole
        {
            get => _pageAccessRole;
            set => _pageAccessRole = NormalizeRole(value);
        }

        private static string NormalizeRole(string? role)
        {
            return string.IsNullOrWhiteSpace(role)
                ? string.Empty
                : role.Trim().ToLowerInvariant();
        }

        // Additional User Information
        [MaxLength(255)]
        public string? Signature { get; set; }

        [MaxLength(50)]
        public string? SignatureCode { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        // Direct Customer Assignment - Foreign key to Customer table (for Customer users)
        // This replaces UserCompany string mapping and provides direct access to customerId
        public int? CustomerId { get; set; }

        // Customer Assignment - JSON array of customer IDs (for AdvantageOne officers)
        [MaxLength(4000)] // Reasonable limit for JSON array
        public string? AssignedCustomerIds { get; set; }

        // Status and Audit Fields
        public bool RecordIsDeleted { get; set; } = false;
        public bool RecordIsDeletedYN { get; set; } = false; // Soft delete flag

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Authentication and Security Fields
        public DateTime? LastLoginAt { get; set; }

        public int LoginAttempts { get; set; } = 0;

        public DateTime? LockoutUntil { get; set; }

        // Computed Properties
        [NotMapped]
        public string FullName 
        { 
            get => $"{FirstName} {LastName}".Trim();
            set { } // Empty setter for EF compatibility
        }

        // Employee Linking Fields (removed - Employee model deleted)
        public int? EmployeeId { get; set; }
        public virtual ICollection<ApplicationUser> CreatedUsers { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<ApplicationUser> UpdatedUsers { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<ApplicationUser> CreatedByUsers { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<ApplicationUser> UpdatedByUsers { get; set; } = new List<ApplicationUser>();

        // Customer Assignment (for AdvantageOne users) - Keep for backward compatibility during migration
        public virtual ICollection<UserCustomerAssignment> UserCustomerAssignments { get; set; } = new List<UserCustomerAssignment>();

        // Navigation Property to Customer (for Customer users)
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        // Helper methods for working with AssignedCustomerIds
        [NotMapped]
        public List<int> CustomerIds
        {
            get
            {
                if (string.IsNullOrEmpty(AssignedCustomerIds))
                    return new List<int>();

                try
                {
                    return JsonSerializer.Deserialize<List<int>>(AssignedCustomerIds) ?? new List<int>();
                }
                catch
                {
                    return new List<int>();
                }
            }
            set
            {
                AssignedCustomerIds = value?.Count > 0 
                    ? JsonSerializer.Serialize(value) 
                    : null;
            }
        }

        public void AddCustomerId(int customerId)
        {
            var customerIds = CustomerIds;
            if (!customerIds.Contains(customerId))
            {
                customerIds.Add(customerId);
                CustomerIds = customerIds;
            }
        }

        public void RemoveCustomerId(int customerId)
        {
            var customerIds = CustomerIds;
            if (customerIds.Contains(customerId))
            {
                customerIds.Remove(customerId);
                CustomerIds = customerIds;
            }
        }

        public bool HasCustomerId(int customerId)
        {
            return CustomerIds.Contains(customerId);
        }

        public void ClearCustomerIds()
        {
            AssignedCustomerIds = null;
        }
    }
}
