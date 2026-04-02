using AIPBackend.Models;
using System.Security.Claims;

namespace AIPBackend.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        bool ValidateRefreshToken(string refreshToken);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<bool> IsRefreshTokenValidAsync(string refreshToken, string userId);
        Task<ApplicationUser?> GetUserByRefreshTokenAsync(string refreshToken);
        Task<bool> StoreRefreshTokenAsync(ApplicationUser user, string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(ApplicationUser user);
        Task<string?> GetUserIdFromTokenAsync(string token);
        Task<IList<string>> GetUserRolesFromTokenAsync(string token);
    }
}
