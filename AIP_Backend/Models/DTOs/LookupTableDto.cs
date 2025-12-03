using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class LookupTableCreateRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Value { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    public class LookupTableUpdateRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Value { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class LookupTableResponseDto
    {
        public int LookupId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Code { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class LookupTableListResponseDto
    {
        public int LookupId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Code { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
