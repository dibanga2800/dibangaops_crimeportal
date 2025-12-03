using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Repositories;

namespace AIPBackend.Services
{
    public interface ILookupTableService
    {
        Task<IEnumerable<LookupTableListResponseDto>> GetAllAsync();
        Task<IEnumerable<LookupTableListResponseDto>> GetByCategoryAsync(string category);
        Task<LookupTableResponseDto?> GetByIdAsync(int lookupId);
        Task<LookupTableResponseDto> CreateAsync(LookupTableCreateRequestDto request, string createdBy);
        Task<LookupTableResponseDto?> UpdateAsync(int lookupId, LookupTableUpdateRequestDto request, string updatedBy);
        Task<bool> DeleteAsync(int lookupId);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }

    public class LookupTableService : ILookupTableService
    {
        private readonly ILookupTableRepository _lookupTableRepository;

        public LookupTableService(ILookupTableRepository lookupTableRepository)
        {
            _lookupTableRepository = lookupTableRepository;
        }

        public async Task<IEnumerable<LookupTableListResponseDto>> GetAllAsync()
        {
            var lookupTables = await _lookupTableRepository.GetAllAsync();
            return lookupTables.Select(MapToLookupTableListResponseDto);
        }

        public async Task<IEnumerable<LookupTableListResponseDto>> GetByCategoryAsync(string category)
        {
            var lookupTables = await _lookupTableRepository.GetByCategoryAsync(category);
            return lookupTables.Select(MapToLookupTableListResponseDto);
        }

        public async Task<LookupTableResponseDto?> GetByIdAsync(int lookupId)
        {
            var lookupTable = await _lookupTableRepository.GetByIdAsync(lookupId);
            if (lookupTable == null)
                return null;

            return MapToLookupTableResponseDto(lookupTable);
        }

        public async Task<LookupTableResponseDto> CreateAsync(LookupTableCreateRequestDto request, string createdBy)
        {
            var lookupTable = new LookupTable
            {
                Category = request.Category,
                Value = request.Value,
                Description = request.Description ?? string.Empty,
                Code = request.Code ?? string.Empty,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedBy = createdBy
            };

            var created = await _lookupTableRepository.CreateAsync(lookupTable);
            return MapToLookupTableResponseDto(created);
        }

        public async Task<LookupTableResponseDto?> UpdateAsync(int lookupId, LookupTableUpdateRequestDto request, string updatedBy)
        {
            var lookupTable = await _lookupTableRepository.GetByIdAsync(lookupId);
            if (lookupTable == null)
                return null;

            lookupTable.Category = request.Category;
            lookupTable.Value = request.Value;
            lookupTable.Description = request.Description ?? string.Empty;
            lookupTable.Code = request.Code ?? string.Empty;
            lookupTable.SortOrder = request.SortOrder;
            lookupTable.IsActive = request.IsActive;
            lookupTable.UpdatedBy = updatedBy;

            var updated = await _lookupTableRepository.UpdateAsync(lookupTable);
            return MapToLookupTableResponseDto(updated);
        }

        public async Task<bool> DeleteAsync(int lookupId)
        {
            return await _lookupTableRepository.DeleteAsync(lookupId);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _lookupTableRepository.GetCategoriesAsync();
        }

        private static LookupTableResponseDto MapToLookupTableResponseDto(LookupTable lookupTable)
        {
            return new LookupTableResponseDto
            {
                LookupId = lookupTable.LookupId,
                Category = lookupTable.Category,
                Value = lookupTable.Value,
                Description = lookupTable.Description,
                Code = lookupTable.Code,
                SortOrder = lookupTable.SortOrder,
                IsActive = lookupTable.IsActive,
                CreatedAt = lookupTable.CreatedAt,
                UpdatedAt = lookupTable.UpdatedAt
            };
        }

        private static LookupTableListResponseDto MapToLookupTableListResponseDto(LookupTable lookupTable)
        {
            return new LookupTableListResponseDto
            {
                LookupId = lookupTable.LookupId,
                Category = lookupTable.Category,
                Value = lookupTable.Value,
                Description = lookupTable.Description,
                Code = lookupTable.Code,
                SortOrder = lookupTable.SortOrder,
                IsActive = lookupTable.IsActive
            };
        }
    }
}
