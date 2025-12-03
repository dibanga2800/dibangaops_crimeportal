using AIPBackend.Models;

namespace AIPBackend.Services
{
    public interface IUserSoftDeleteService
    {
        Task<bool> SoftDeleteUserAsync(string userId);
        Task<bool> RestoreUserAsync(string userId);
        Task<bool> HardDeleteUserAsync(string userId);
        Task<List<ApplicationUser>> GetActiveUsersAsync();
        Task<List<ApplicationUser>> GetDeletedUsersAsync();
        Task<bool> IsUserDeletedAsync(string userId);
    }
}
