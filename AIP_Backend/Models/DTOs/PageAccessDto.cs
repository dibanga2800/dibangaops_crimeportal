using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class PageAccessDto
    {
        public int Id { get; set; }
        public string PageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class RolePageAccessDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int PageAccessId { get; set; }
        public string? PagePath { get; set; }
        public bool HasAccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public PageAccessDto PageAccess { get; set; } = new();
    }

    public class PageAccessSettingsDto
    {
        public Dictionary<string, string[]> PageAccessByRole { get; set; } = new Dictionary<string, string[]>();
        public List<PageAccessDto> AvailablePages { get; set; } = new List<PageAccessDto>();
    }

    public class UpdatePageAccessRequestDto
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;

        [Required]
        public List<string> PageIds { get; set; } = new List<string>();
    }

    public class CreatePageAccessRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string PageId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdatePageAccessSettingsRequestDto
    {
        [Required]
        public Dictionary<string, string[]> PageAccessByRole { get; set; } = new Dictionary<string, string[]>();
    }

    public class PageAccessStatisticsDto
    {
        public int TotalPages { get; set; }
        public int ActivePages { get; set; }
        public int TotalRoles { get; set; }
        public Dictionary<string, int> PagesByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AccessByRole { get; set; } = new Dictionary<string, int>();
    }

    public class CustomerPageAccessResponseDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<PageAccessDto> AvailablePages { get; set; } = new List<PageAccessDto>();
        public string[] AssignedPageIds { get; set; } = Array.Empty<string>();
    }

    public class UpdateCustomerPageAccessRequestDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public List<string> PageIds { get; set; } = new List<string>();
    }

    public class SyncPageDefinitionDto
    {
        [Required]
        [MaxLength(100)]
        public string PageId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    public class SyncPagesRequestDto
    {
        [Required]
        public List<SyncPageDefinitionDto> Pages { get; set; } = new List<SyncPageDefinitionDto>();
    }

    public class SyncResultDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Total { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
