using AIPBackend.Models;

namespace AIPBackend.Services
{
    public interface ICustomerAssignmentService
    {
        Task<List<int>> GetAssignedCustomerIdsAsync(string userId);
        Task AssignCustomersToUserAsync(string userId, List<int> customerIds);
        Task AddCustomerToUserAsync(string userId, int customerId);
        Task RemoveCustomerFromUserAsync(string userId, int customerId);
        Task ClearCustomerAssignmentsAsync(string userId);
        Task<bool> UserHasCustomerAsync(string userId, int customerId);
        Task<List<ApplicationUser>> GetUsersByCustomerIdAsync(int customerId);
        Task MigrateFromUserCustomerAssignmentsTableAsync();
    }
}
