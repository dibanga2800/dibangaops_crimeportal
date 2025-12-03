#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface ICustomerService
    {
        Task<CustomerListResponseDto> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? region = null);
        Task<CustomerDetailResponseDto?> GetCustomerByIdAsync(int id);
        Task<CustomerDetailResponseDto> CreateCustomerAsync(CustomerCreateRequestDto request);
        Task<CustomerDetailResponseDto> UpdateCustomerAsync(int id, CustomerUpdateRequestDto request);
        Task DeleteCustomerAsync(int id);
        Task<CustomerStatisticsDto> GetCustomerStatisticsAsync();
        Task<CustomerDetailResponseDto> UpdatePageAssignmentsAsync(int id, CustomerPageAssignmentsDto request);
        Task<CustomerListResponseDto> SearchCustomersAsync(string query);
        Task<CustomerListResponseDto> GetCustomersByRegionAsync(string region);
        Task<CustomerListResponseDto> GetCustomersByStatusAsync(string status);
    }
}
