using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class UserSoftDeleteService : IUserSoftDeleteService
    {
        private readonly ApplicationDbContext _context;

        public UserSoftDeleteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SoftDeleteUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.RecordIsDeletedYN = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.RecordIsDeletedYN = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HardDeleteUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ApplicationUser>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.RecordIsDeletedYN == false)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetDeletedUsersAsync()
        {
            return await _context.Users
                .Where(u => u.RecordIsDeletedYN == true)
                .ToListAsync();
        }

        public async Task<bool> IsUserDeletedAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.RecordIsDeletedYN == true;
        }
    }
}
