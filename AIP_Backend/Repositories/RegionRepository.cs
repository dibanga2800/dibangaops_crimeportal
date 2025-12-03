#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Repositories
{
    public interface IRegionRepository
    {
        Task<IEnumerable<Region>> GetAllAsync();
        Task<IEnumerable<Region>> GetByCustomerIdAsync(int customerId);
        Task<Region?> GetByIdAsync(int regionId);
        Task<Region> CreateAsync(Region region);
        Task<Region> UpdateAsync(Region region);
        Task DeleteAsync(int regionId);
        Task<bool> ExistsAsync(int regionId);
    }

    public class RegionRepository : IRegionRepository
    {
        private readonly ApplicationDbContext _context;

        public RegionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Region>> GetAllAsync()
        {
            return await _context.Regions
                .Include(r => r.Customer)
                .Include(r => r.Sites)
                .Where(r => !r.RecordIsDeletedYN)
                .OrderBy(r => r.Customer.CompanyName)
                .ThenBy(r => r.RegionName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Region>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Regions
                .Include(r => r.Customer)
                .Include(r => r.Sites)
                .Where(r => r.fkCustomerID == customerId && !r.RecordIsDeletedYN)
                .OrderBy(r => r.RegionName)
                .ToListAsync();
        }

        public async Task<Region?> GetByIdAsync(int regionId)
        {
            return await _context.Regions
                .Include(r => r.Customer)
                .Include(r => r.Sites)
                .FirstOrDefaultAsync(r => r.RegionID == regionId && !r.RecordIsDeletedYN);
        }

        public async Task<Region> CreateAsync(Region region)
        {
            _context.Regions.Add(region);
            await _context.SaveChangesAsync();
            return region;
        }

        public async Task<Region> UpdateAsync(Region region)
        {
            region.DateModified = DateTime.UtcNow;
            _context.Regions.Update(region);
            await _context.SaveChangesAsync();
            return region;
        }

        public async Task DeleteAsync(int regionId)
        {
            var region = await _context.Regions.FindAsync(regionId);
            if (region != null)
            {
                region.RecordIsDeletedYN = true;
                region.DateModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int regionId)
        {
            return await _context.Regions.AnyAsync(r => r.RegionID == regionId && !r.RecordIsDeletedYN);
        }
    }
}
