using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIPBackend.Services
{
    public class CustomerAssignmentService : ICustomerAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public CustomerAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetAssignedCustomerIdsAsync(string userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                return new List<int>();

            return user.CustomerIds;
        }

        public async Task AssignCustomersToUserAsync(string userId, List<int> customerIds)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found or is deleted");

            user.CustomerIds = customerIds;
            await _context.SaveChangesAsync();
        }

        public async Task AddCustomerToUserAsync(string userId, int customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found or is deleted");

            user.AddCustomerId(customerId);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCustomerFromUserAsync(string userId, int customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found or is deleted");

            user.RemoveCustomerId(customerId);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCustomerAssignmentsAsync(string userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found or is deleted");

            user.ClearCustomerIds();
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserHasCustomerAsync(string userId, int customerId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.RecordIsDeletedYN == false);

            if (user == null)
                return false;

            return user.HasCustomerId(customerId);
        }

        public async Task<List<ApplicationUser>> GetUsersByCustomerIdAsync(int customerId)
        {
            return await _context.Users
                .Where(u => u.RecordIsDeletedYN == false &&
                           u.AssignedCustomerIds != null && 
                           u.AssignedCustomerIds.Contains(customerId.ToString()))
                .ToListAsync();
        }

        public async Task MigrateFromUserCustomerAssignmentsTableAsync()
        {
            // Get all users with existing customer assignments
            var usersWithAssignments = await _context.UserCustomerAssignments
                .GroupBy(uca => uca.UserId)
                .Select(g => new { UserId = g.Key, CustomerIds = g.Select(uca => uca.CustomerId).ToList() })
                .ToListAsync();

            foreach (var userAssignment in usersWithAssignments)
            {
                var user = await _context.Users.FindAsync(userAssignment.UserId);
                if (user != null)
                {
                    user.CustomerIds = userAssignment.CustomerIds;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
