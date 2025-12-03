#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IRegionService
    {
        Task<List<RegionDto>> GetRegionsAsync(int page = 1, int pageSize = 10, string? search = null, int? customerId = null);
        Task<RegionDto?> GetRegionByIdAsync(int id);
        Task<List<RegionDto>> GetRegionsByCustomerAsync(int customerId);
        Task<RegionDto> CreateRegionAsync(RegionCreateRequestDto createRegionDto, string createdBy);
        Task<RegionDto?> UpdateRegionAsync(int id, RegionUpdateRequestDto updateRegionDto, string modifiedBy);
        Task<bool> DeleteRegionAsync(int id);
    }
}
