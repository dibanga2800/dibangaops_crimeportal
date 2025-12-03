#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class RegionService : IRegionService
    {
        private readonly ApplicationDbContext _context;

        public RegionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RegionDto>> GetRegionsAsync(int page = 1, int pageSize = 10, string? search = null, int? customerId = null)
        {
            var query = _context.Regions.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.RegionName.Contains(search) || r.RegionDescription!.Contains(search));
            }

            if (customerId.HasValue)
            {
                query = query.Where(r => r.fkCustomerID == customerId.Value);
            }

            // Only return non-deleted regions
            query = query.Where(r => !r.RecordIsDeletedYN);

            var regions = await query
                .OrderBy(r => r.RegionName) // Add default ordering to fix EF warning
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RegionDto
                {
                    RegionID = r.RegionID,
                    fkCustomerID = r.fkCustomerID,
                    RegionName = r.RegionName,
                    RegionDescription = r.RegionDescription,
                    RecordIsDeletedYN = r.RecordIsDeletedYN,
                    DateCreated = r.DateCreated,
                    CreatedBy = r.CreatedBy,
                    DateModified = r.DateModified,
                    ModifiedBy = r.ModifiedBy
                })
                .ToListAsync();

            return regions;
        }

        public async Task<RegionDto?> GetRegionByIdAsync(int id)
        {
            var region = await _context.Regions
                .FirstOrDefaultAsync(r => r.RegionID == id && !r.RecordIsDeletedYN);

            if (region == null)
                return null;

            return new RegionDto
            {
                RegionID = region.RegionID,
                fkCustomerID = region.fkCustomerID,
                RegionName = region.RegionName,
                RegionDescription = region.RegionDescription,
                RecordIsDeletedYN = region.RecordIsDeletedYN,
                DateCreated = region.DateCreated,
                CreatedBy = region.CreatedBy,
                DateModified = region.DateModified,
                ModifiedBy = region.ModifiedBy
            };
        }

        public async Task<List<RegionDto>> GetRegionsByCustomerAsync(int customerId)
        {
            var regions = await _context.Regions
                .Where(r => r.fkCustomerID == customerId && !r.RecordIsDeletedYN)
                .Select(r => new RegionDto
                {
                    RegionID = r.RegionID,
                    fkCustomerID = r.fkCustomerID,
                    RegionName = r.RegionName,
                    RegionDescription = r.RegionDescription,
                    RecordIsDeletedYN = r.RecordIsDeletedYN,
                    DateCreated = r.DateCreated,
                    CreatedBy = r.CreatedBy,
                    DateModified = r.DateModified,
                    ModifiedBy = r.ModifiedBy
                })
                .ToListAsync();

            return regions;
        }

        public async Task<RegionDto> CreateRegionAsync(RegionCreateRequestDto createRegionDto, string createdBy)
        {
            var region = new Region
            {
                fkCustomerID = createRegionDto.fkCustomerID,
                RegionName = createRegionDto.RegionName,
                RegionDescription = createRegionDto.RegionDescription,
                RecordIsDeletedYN = false,
                DateCreated = DateTime.UtcNow,
                CreatedBy = createdBy,
                DateModified = null,
                ModifiedBy = null
            };

            _context.Regions.Add(region);
            await _context.SaveChangesAsync();

            return new RegionDto
            {
                RegionID = region.RegionID,
                fkCustomerID = region.fkCustomerID,
                RegionName = region.RegionName,
                RegionDescription = region.RegionDescription,
                RecordIsDeletedYN = region.RecordIsDeletedYN,
                DateCreated = region.DateCreated,
                CreatedBy = region.CreatedBy,
                DateModified = region.DateModified,
                ModifiedBy = region.ModifiedBy
            };
        }

        public async Task<RegionDto?> UpdateRegionAsync(int id, RegionUpdateRequestDto updateRegionDto, string modifiedBy)
        {
            var region = await _context.Regions
                .FirstOrDefaultAsync(r => r.RegionID == id && !r.RecordIsDeletedYN);

            if (region == null)
                return null;

            region.RegionName = updateRegionDto.RegionName;
            region.RegionDescription = updateRegionDto.RegionDescription;
            region.DateModified = DateTime.UtcNow;
            region.ModifiedBy = modifiedBy;

            await _context.SaveChangesAsync();

            return new RegionDto
            {
                RegionID = region.RegionID,
                fkCustomerID = region.fkCustomerID,
                RegionName = region.RegionName,
                RegionDescription = region.RegionDescription,
                RecordIsDeletedYN = region.RecordIsDeletedYN,
                DateCreated = region.DateCreated,
                CreatedBy = region.CreatedBy,
                DateModified = region.DateModified,
                ModifiedBy = region.ModifiedBy
            };
        }

        public async Task<bool> DeleteRegionAsync(int id)
        {
            var region = await _context.Regions
                .FirstOrDefaultAsync(r => r.RegionID == id && !r.RecordIsDeletedYN);

            if (region == null)
                return false;

            // Soft delete
            region.RecordIsDeletedYN = true;
            region.DateModified = DateTime.UtcNow;
            region.ModifiedBy = "system"; // TODO: Get from current user

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
