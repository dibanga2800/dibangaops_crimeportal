using System;
using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class UserCreateRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PageAccessRole { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Signature { get; set; }

        [MaxLength(50)]
        public string? SignatureCode { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        public bool IsActive { get; set; } = true;

        // For platform users (assigned to multiple customers)
        public int[] AssignedCustomerIds { get; set; } = new int[0];

        // For customer-linked users (direct single customer)
        public int? CustomerId { get; set; }

        // Store / site assignments
        // Primary site for store users
        [MaxLength(200)]
        public string? PrimarySiteId { get; set; }

        // Multiple sites for security officers or multi-site users
        public string[] AssignedSiteIds { get; set; } = Array.Empty<string>();
    }

    public class UserUpdateRequestDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; }

        [MaxLength(50)]
        public string? PageAccessRole { get; set; }

        [MaxLength(255)]
        public string? Signature { get; set; }

        [MaxLength(50)]
        public string? SignatureCode { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        public bool? IsActive { get; set; }

        // For platform users
        public int[] AssignedCustomerIds { get; set; } = new int[0];

        // For Customer users
        public int? CustomerId { get; set; }

        // Store / site assignments
        [MaxLength(200)]
        public string? PrimarySiteId { get; set; }

        public string[] AssignedSiteIds { get; set; } = Array.Empty<string>();
    }

    public class UserPasswordChangeRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UserPasswordResetRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class UserPasswordResetConfirmDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PageAccessRole { get; set; } = string.Empty;
        public string? Signature { get; set; }
        public string? SignatureCode { get; set; }
        public string? JobTitle { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; }
        public bool RecordIsDeleted { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public bool LoginAlertsEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int LoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public string[] Roles { get; set; } = new string[0]; // For compatibility with AuthController
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // For platform users
        public int[] AssignedCustomerIds { get; set; } = new int[0];

        // For Customer users
        public int? CustomerId { get; set; }

        // Store / site assignments
        public string? PrimarySiteId { get; set; }
        public string[] AssignedSiteIds { get; set; } = Array.Empty<string>();
    }

    public class UserListResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PageAccessRole { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public bool IsActive { get; set; }
        public bool RecordIsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // For platform users
        public int[] AssignedCustomerIds { get; set; } = new int[0];

        // For Customer users
        public int? CustomerId { get; set; }

        // Store / site assignments
        public string? PrimarySiteId { get; set; }
        public string[] AssignedSiteIds { get; set; } = Array.Empty<string>();
    }

    public class RoleResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string[] Permissions { get; set; } = new string[0];
    }

    public class RoleCreateRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public string[] PermissionIds { get; set; } = new string[0];
    }

    public class RoleUpdateRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public string[] PermissionIds { get; set; } = new string[0];
    }

    public class PermissionResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerResponseDto
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyNumber { get; set; } = string.Empty;
        public string VatNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
