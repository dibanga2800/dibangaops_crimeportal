using AIPBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JwtService> _logger;
        private readonly SigningCredentials _signingCredentials;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public JwtService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
        {
            try
            {
                                 var claims = new List<Claim>
                 {
                     new Claim(ClaimTypes.NameIdentifier, user.Id),
                     new Claim(ClaimTypes.Name, user.UserName ?? ""),
                     new Claim(ClaimTypes.Email, user.Email ?? ""),
                     new Claim(ClaimTypes.GivenName, user.FirstName),
                     new Claim(ClaimTypes.Surname, user.LastName),
                     new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                     new Claim("JobTitle", user.JobTitle ?? ""),
                     new Claim("IsActive", user.IsActive.ToString()),
                     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                 };

                // Add roles to claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                if (user.CustomerId.HasValue)
                {
                    claims.Add(new Claim("CustomerId", user.CustomerId.Value.ToString()));
                }

                if (!string.IsNullOrWhiteSpace(user.PageAccessRole))
                {
                    claims.Add(new Claim("PageAccessRole", user.PageAccessRole));
                }

                var assignedCustomerIds = user.CustomerIds;
                if (assignedCustomerIds.Count > 0)
                {
                    claims.Add(new Claim("AssignedCustomerIds", string.Join(',', assignedCustomerIds)));
                }

                // Store / site-level claims for role-based scoping
                if (!string.IsNullOrWhiteSpace(user.PrimarySiteId))
                {
                    claims.Add(new Claim("PrimarySiteId", user.PrimarySiteId));
                }

                var assignedSiteIds = user.SiteIds;
                if (assignedSiteIds.Count > 0)
                {
                    claims.Add(new Claim("AssignedSiteIds", string.Join(',', assignedSiteIds)));
                }

				var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
                var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
                var expirationMinutes = _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60";
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(expirationMinutes)),
                    signingCredentials: _signingCredentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        public bool ValidateRefreshToken(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return false;

                // Basic validation - check if it's a valid base64 string
                try
                {
                    Convert.FromBase64String(refreshToken);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                return false;
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingCredentials.Key,
                    ValidateLifetime = false // We want to get the principal from expired token
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting principal from expired token");
                throw;
            }
        }

        public Task<bool> IsRefreshTokenValidAsync(string refreshToken, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(userId))
                    return Task.FromResult(false);

                if (!ValidateRefreshToken(refreshToken))
                    return Task.FromResult(false);

                var refreshTokenHash = HashRefreshToken(refreshToken);
                return _userManager.Users
                    .AnyAsync(u =>
                        u.Id == userId &&
                        u.IsActive &&
                        u.RefreshTokenHash == refreshTokenHash &&
                        u.RefreshTokenExpiresAtUtc.HasValue &&
                        u.RefreshTokenExpiresAtUtc > DateTime.UtcNow &&
                        !u.RefreshTokenRevokedAtUtc.HasValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
                return Task.FromResult(false);
            }
        }

        public async Task<ApplicationUser?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (!ValidateRefreshToken(refreshToken))
                {
                    return null;
                }

                var refreshTokenHash = HashRefreshToken(refreshToken);
                return await _userManager.Users
                    .Where(u =>
                        u.IsActive &&
                        u.RefreshTokenHash == refreshTokenHash &&
                        u.RefreshTokenExpiresAtUtc.HasValue &&
                        u.RefreshTokenExpiresAtUtc > DateTime.UtcNow &&
                        !u.RefreshTokenRevokedAtUtc.HasValue)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving user from refresh token");
                return null;
            }
        }

        public async Task<bool> StoreRefreshTokenAsync(ApplicationUser user, string refreshToken)
        {
            try
            {
                if (user == null || string.IsNullOrWhiteSpace(refreshToken))
                {
                    return false;
                }

                var refreshTokenExpiryDays = Convert.ToInt32(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
                user.RefreshTokenHash = HashRefreshToken(refreshToken);
                user.RefreshTokenCreatedAtUtc = DateTime.UtcNow;
                user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
                user.RefreshTokenRevokedAtUtc = null;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing refresh token for user {UserId}", user.Id);
                return false;
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(ApplicationUser user)
        {
            try
            {
                user.RefreshTokenRevokedAtUtc = DateTime.UtcNow;
                user.RefreshTokenHash = null;
                user.RefreshTokenCreatedAtUtc = null;
                user.RefreshTokenExpiresAtUtc = null;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token for user {UserId}", user.Id);
                return false;
            }
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }

        public Task<string?> GetUserIdFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                return Task.FromResult(userIdClaim?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID from token");
                return Task.FromResult<string?>(null);
            }
        }

        public Task<IList<string>> GetUserRolesFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var roleClaims = jwtToken.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
                return Task.FromResult<IList<string>>(roleClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles from token");
                return Task.FromResult<IList<string>>(new List<string>());
            }
        }
    }
}
