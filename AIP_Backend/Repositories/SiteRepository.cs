#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Repositories
{
    public interface ISiteRepository
    {
        Task<IEnumerable<Site>> GetAllAsync();
        Task<IEnumerable<Site>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Site>> GetByRegionIdAsync(int regionId);
        Task<Site?> GetByIdAsync(int siteId);
        Task<Site> CreateAsync(Site site);
        Task<Site> UpdateAsync(Site site);
        Task DeleteAsync(int siteId);
        Task<bool> ExistsAsync(int siteId);
    }

    public class SiteRepository : ISiteRepository
    {
        private readonly ApplicationDbContext _context;

        public SiteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Site>> GetAllAsync()
        {
            return await _context.Sites
                .Include(s => s.Customer)
                .Include(s => s.Region)
                .Where(s => !s.RecordIsDeletedYN)
                .OrderBy(s => s.Customer.CompanyName)
                .ThenBy(s => s.Region.RegionName)
                .ThenBy(s => s.LocationName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Site>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Sites
                .Include(s => s.Customer)
                .Include(s => s.Region)
                .Where(s => s.fkCustomerID == customerId && !s.RecordIsDeletedYN)
                .OrderBy(s => s.Region.RegionName)
                .ThenBy(s => s.LocationName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Site>> GetByRegionIdAsync(int regionId)
        {
            return await _context.Sites
                .Include(s => s.Customer)
                .Include(s => s.Region)
                .Where(s => s.fkRegionID == regionId && !s.RecordIsDeletedYN)
                .OrderBy(s => s.Customer.CompanyName)
                .ThenBy(s => s.LocationName)
                .ToListAsync();
        }

        public async Task<Site?> GetByIdAsync(int siteId)
        {
            return await _context.Sites
                .Include(s => s.Customer)
                .Include(s => s.Region)
                .FirstOrDefaultAsync(s => s.SiteID == siteId && !s.RecordIsDeletedYN);
        }

        public async Task<Site> CreateAsync(Site site)
        {
            _context.Sites.Add(site);
            await _context.SaveChangesAsync();
            return site;
        }

        public async Task<Site> UpdateAsync(Site site)
        {
            site.DateModified = DateTime.UtcNow;
            _context.Sites.Update(site);
            await _context.SaveChangesAsync();
            return site;
        }

        public async Task DeleteAsync(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site != null)
            {
                site.RecordIsDeletedYN = true;
                site.DateModified = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int siteId)
        {
            return await _context.Sites.AnyAsync(s => s.SiteID == siteId && !s.RecordIsDeletedYN);
        }
    }
}
