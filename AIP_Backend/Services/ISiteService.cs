#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface ISiteService
    {
        Task<List<SiteDto>> GetSitesAsync(int page = 1, int pageSize = 10, string? search = null, int? customerId = null, int? regionId = null);
        Task<SiteDto?> GetSiteByIdAsync(int id);
        Task<List<SiteDto>> GetSitesByCustomerAsync(int customerId);
        Task<List<SiteDto>> GetSitesByRegionAsync(int regionId);
        Task<SiteDto> CreateSiteAsync(SiteCreateRequestDto createSiteDto, string createdBy);
        Task<SiteDto?> UpdateSiteAsync(int id, SiteUpdateRequestDto updateSiteDto, string modifiedBy);
        Task<bool> DeleteSiteAsync(int id);
    }
}
