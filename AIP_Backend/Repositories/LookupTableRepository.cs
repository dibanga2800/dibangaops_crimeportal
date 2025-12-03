using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Repositories
{
    public interface ILookupTableRepository
    {
        Task<IEnumerable<LookupTable>> GetAllAsync();
        Task<IEnumerable<LookupTable>> GetByCategoryAsync(string category);
        Task<LookupTable?> GetByIdAsync(int lookupId);
        Task<LookupTable> CreateAsync(LookupTable lookupTable);
        Task<LookupTable> UpdateAsync(LookupTable lookupTable);
        Task<bool> DeleteAsync(int lookupId);
        Task<bool> ExistsAsync(int lookupId);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }

    public class LookupTableRepository : ILookupTableRepository
    {
        private readonly ApplicationDbContext _context;

        public LookupTableRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LookupTable>> GetAllAsync()
        {
            return await _context.LookupTables
                .Where(lt => !lt.RecordIsDeletedYN)
                .OrderBy(lt => lt.Category)
                .ThenBy(lt => lt.SortOrder)
                .ThenBy(lt => lt.Value)
                .ToListAsync();
        }

        public async Task<IEnumerable<LookupTable>> GetByCategoryAsync(string category)
        {
            return await _context.LookupTables
                .Where(lt => lt.Category == category && !lt.RecordIsDeletedYN && lt.IsActive)
                .OrderBy(lt => lt.SortOrder)
                .ThenBy(lt => lt.Value)
                .ToListAsync();
        }

        public async Task<LookupTable?> GetByIdAsync(int lookupId)
        {
            return await _context.LookupTables
                .FirstOrDefaultAsync(lt => lt.LookupId == lookupId && !lt.RecordIsDeletedYN);
        }

        public async Task<LookupTable> CreateAsync(LookupTable lookupTable)
        {
            _context.LookupTables.Add(lookupTable);
            await _context.SaveChangesAsync();
            return lookupTable;
        }

        public async Task<LookupTable> UpdateAsync(LookupTable lookupTable)
        {
            lookupTable.UpdatedAt = DateTime.UtcNow;
            _context.LookupTables.Update(lookupTable);
            await _context.SaveChangesAsync();
            return lookupTable;
        }

        public async Task<bool> DeleteAsync(int lookupId)
        {
            var lookupTable = await GetByIdAsync(lookupId);
            if (lookupTable == null)
                return false;

            lookupTable.RecordIsDeletedYN = true;
            lookupTable.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int lookupId)
        {
            return await _context.LookupTables
                .AnyAsync(lt => lt.LookupId == lookupId && !lt.RecordIsDeletedYN);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _context.LookupTables
                .Where(lt => !lt.RecordIsDeletedYN)
                .Select(lt => lt.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
