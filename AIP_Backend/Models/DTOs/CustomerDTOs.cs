#nullable enable

using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class CustomerSummaryDto
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CustomerType { get; set; }
        public string? Region { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerDetailResponseDto
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyNumber { get; set; } = string.Empty;
        public string? VatNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CustomerType { get; set; }
        public string? Region { get; set; }
        public string? PageAssignments { get; set; }
        
        // Address fields
        public string? Building { get; set; }
        public string? Street { get; set; }
        public string? Village { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        
        // Contact fields
        public string? ContactTitle { get; set; }
        public string? ContactForename { get; set; }
        public string? ContactSurname { get; set; }
        public string? ContactPosition { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class CustomerCreateRequestDto
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        public string CompanyNumber { get; set; } = string.Empty;
        
        public string? VatNumber { get; set; }
        public string? Status { get; set; }
        public string? CustomerType { get; set; }
        public string? Region { get; set; }
        public string? PageAssignments { get; set; }
        
        // Address fields
        public string? Building { get; set; }
        public string? Street { get; set; }
        public string? Village { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        
        // Contact fields
        public string? ContactTitle { get; set; }
        public string? ContactForename { get; set; }
        public string? ContactSurname { get; set; }
        public string? ContactPosition { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        
        public string? CreatedBy { get; set; }
    }

    public class CustomerUpdateRequestDto
    {
        public string? CompanyName { get; set; }
        public string? CompanyNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? Status { get; set; }
        public string? CustomerType { get; set; }
        public string? Region { get; set; }
        public string? PageAssignments { get; set; }
        
        // Address fields
        public string? Building { get; set; }
        public string? Street { get; set; }
        public string? Village { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        
        // Contact fields
        public string? ContactTitle { get; set; }
        public string? ContactForename { get; set; }
        public string? ContactSurname { get; set; }
        public string? ContactPosition { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        
        public string? UpdatedBy { get; set; }
    }

    public class CustomerListResponseDto
    {
        public List<CustomerDetailResponseDto> Customers { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class CustomerStatisticsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public Dictionary<string, int> CustomersByRegion { get; set; } = new();
        public Dictionary<string, int> CustomersByType { get; set; } = new();
    }

    public class CustomerPageAssignmentsDto
    {
        [Required]
        public string PageAssignments { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
    }
}
