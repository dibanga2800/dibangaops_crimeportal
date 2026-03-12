using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading;

namespace AIPBackend.Services
{
    public interface IPageAccessService
    {
        Task<PageAccessSettingsDto> GetPageAccessSettingsAsync();
        Task<PageAccessSettingsDto> UpdatePageAccessSettingsAsync(UpdatePageAccessSettingsRequestDto request, string currentUserId);
        Task<List<PageAccessDto>> GetAllPagesAsync();
        Task<PageAccessDto?> GetPageByIdAsync(int id);
        Task<PageAccessDto?> GetPageByPageIdAsync(string pageId);
        Task<PageAccessDto?> CreatePageAsync(CreatePageAccessRequestDto request, string currentUserId);
        Task<PageAccessDto?> UpdatePageAsync(int id, CreatePageAccessRequestDto request, string currentUserId);
        Task<bool> DeletePageAsync(int id, string currentUserId);
        Task<List<RolePageAccessDto>> GetRolePageAccessAsync(string roleName);
        Task<bool> UpdateRolePageAccessAsync(UpdatePageAccessRequestDto request, string currentUserId);
        Task<bool> CheckUserAccessAsync(string userId, string pagePath);
        Task<PageAccessStatisticsDto> GetPageAccessStatisticsAsync();
        Task<bool> InitializeDefaultPageAccessAsync(string currentUserId);
        Task<SyncResultDto> SyncPagesFromDefinitionsAsync(SyncPagesRequestDto request, string currentUserId);
    }

    public class PageAccessService : IPageAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PageAccessService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] AdvantageOneOfficerCustomerPageIds = Array.Empty<string>();

        public PageAccessService(ApplicationDbContext context, ILogger<PageAccessService> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Validates and returns a valid user ID. If the provided user ID doesn't exist,
        /// attempts to find an admin user. Returns null if no valid user is found.
        /// </summary>
        private async Task<string?> GetValidUserIdAsync(string? userId)
        {
            // If userId is null or empty, try to find an admin user
            if (string.IsNullOrWhiteSpace(userId) || userId == "System")
            {
                _logger.LogWarning("Invalid or system user ID provided ({UserId}), attempting to find admin user", userId);
                
                // Try to find an admin user
                var adminUsers = await _userManager.GetUsersInRoleAsync("administrator");
                if (adminUsers.Any())
                {
                    var adminId = adminUsers.First().Id;
                    _logger.LogInformation("Using admin user {AdminId} as fallback", adminId);
                    return adminId;
                }
                
                // If no admin user exists, return null (FK allows nulls)
                _logger.LogWarning("No admin user found, will use null for CreatedBy/UpdatedBy");
                return null;
            }

            // Validate that the provided user ID exists
            var userExists = await _userManager.FindByIdAsync(userId);
            if (userExists != null)
            {
                return userId;
            }

            _logger.LogWarning("User ID {UserId} does not exist, attempting to find admin user", userId);
            
            // Fallback to admin user
            var fallbackAdmins = await _userManager.GetUsersInRoleAsync("administrator");
            if (fallbackAdmins.Any())
            {
                var fallbackId = fallbackAdmins.First().Id;
                _logger.LogInformation("Using admin user {AdminId} as fallback", fallbackId);
                return fallbackId;
            }

            _logger.LogWarning("No admin user found, will use null for CreatedBy/UpdatedBy");
            return null;
        }

        public async Task<PageAccessSettingsDto> GetPageAccessSettingsAsync()
        {
            try
			{
				_logger.LogInformation("Getting page access settings");

				// Use a timeout to prevent hanging if database is slow/unavailable
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				
				var pagesQuery = _context.PageAccesses
					.Where(p => p.IsActive && !string.IsNullOrEmpty(p.PageId) && !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Path))
					.OrderBy(p => p.SortOrder)
					.ThenBy(p => p.Title)
					.Select(p => new PageAccessDto
					{
						Id = p.Id,
						PageId = p.PageId,
						Title = p.Title,
						Path = p.Path,
						Category = p.Category ?? string.Empty,
						Description = p.Description ?? string.Empty,
						IsActive = p.IsActive,
						SortOrder = p.SortOrder,
						CreatedAt = p.CreatedAt,
						CreatedBy = p.CreatedBy ?? string.Empty,
						UpdatedAt = p.UpdatedAt,
						UpdatedBy = p.UpdatedBy ?? string.Empty
					});

				var pages = await pagesQuery.ToListAsync(cts.Token);

				string[] requiredPageIds =
				{
					"incident-report",
					"incident-graph",
					"crime-intelligence"
				};

				var missingRequiredPages = requiredPageIds
					.Where(id => pages.All(p => !string.Equals(p.PageId, id, StringComparison.OrdinalIgnoreCase)))
					.ToList();

				if (missingRequiredPages.Any())
				{
					_logger.LogWarning("Detected {MissingCount} missing critical page definitions. Settings will continue using existing database state. Run manual sync if these pages should exist. Missing: {MissingPages}",
						missingRequiredPages.Count,
						string.Join(", ", missingRequiredPages));
				}

                // Get role page access mappings with NULL handling
				var rolePageAccess = await _context.RolePageAccesses
                    .Where(rpa => rpa.HasAccess && !string.IsNullOrEmpty(rpa.RoleName))
                    .Include(rpa => rpa.PageAccess)
                    .Where(rpa => rpa.PageAccess.IsActive && !string.IsNullOrEmpty(rpa.PageAccess.PageId))
                    .GroupBy(rpa => rpa.RoleName)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(rpa => rpa.PageAccess.PageId).ToArray(),
						cts.Token
                    );

                // Log what was retrieved from database
                if (rolePageAccess.ContainsKey("AdvantageOneOfficer"))
                {
                    var officerPages = rolePageAccess["AdvantageOneOfficer"];
                }

                // If no role page access found, try auto-initializing and re-query (ensures DB is seeded before using in-memory defaults)
                if (!rolePageAccess.Any())
                {
                    _logger.LogWarning("No role page access found in database. Attempting to initialize defaults and re-query.");
                    try
                    {
                        await InitializeDefaultPageAccessAsync("System");
                        rolePageAccess = await _context.RolePageAccesses
                            .Where(rpa => rpa.HasAccess && !string.IsNullOrEmpty(rpa.RoleName))
                            .Include(rpa => rpa.PageAccess)
                            .Where(rpa => rpa.PageAccess.IsActive && !string.IsNullOrEmpty(rpa.PageAccess.PageId))
                            .GroupBy(rpa => rpa.RoleName)
                            .ToDictionaryAsync(g => g.Key, g => g.Select(rpa => rpa.PageAccess.PageId).ToArray(), CancellationToken.None);
                        if (!rolePageAccess.Any())
                            return GetDefaultPageAccessSettings();
                    }
                    catch (Exception initEx)
                    {
                        _logger.LogError(initEx, "Failed to auto-initialize page access; returning default settings");
                        return GetDefaultPageAccessSettings();
                    }
                }

                // If no pages found in database, return default settings
                if (!pages.Any())
                {
                    _logger.LogWarning("No pages found in database, returning default page access settings");
                    return GetDefaultPageAccessSettings();
                }

                return new PageAccessSettingsDto
                {
                    PageAccessByRole = rolePageAccess,
                    AvailablePages = pages
                };
            }
            catch (OperationCanceledException)
			{
				_logger.LogWarning("Page access settings query timed out, returning default settings");
				return GetDefaultPageAccessSettings();
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page access settings, returning default settings");
                return GetDefaultPageAccessSettings();
            }
        }

        public async Task<PageAccessSettingsDto> UpdatePageAccessSettingsAsync(UpdatePageAccessSettingsRequestDto request, string currentUserId)
        {
            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Updating page access settings for user {CurrentUserId}", currentUserId);
                _logger.LogInformation("Received {RoleCount} roles to update", request.PageAccessByRole.Count);

                // Verify request is not null or empty
                if (request.PageAccessByRole == null || !request.PageAccessByRole.Any())
                {
                    _logger.LogWarning("UpdatePageAccessSettingsAsync called with null or empty PageAccessByRole");
                    throw new ArgumentException("PageAccessByRole cannot be null or empty", nameof(request));
                }

                // Clear existing role page access
                var deletedCount = await _context.RolePageAccesses.ExecuteDeleteAsync();
                _logger.LogInformation("Deleted {DeletedCount} existing role page access records", deletedCount);

                // Add new role page access mappings
                var rolePageAccesses = new List<RolePageAccess>();
                var missingPages = new List<string>();
                var totalPageIds = 0;

                foreach (var kvp in request.PageAccessByRole)
                {
                    // Normalize role name to lowercase for consistency
                    var roleName = kvp.Key.ToLowerInvariant();
                    var pageIds = kvp.Value;
                    totalPageIds += pageIds.Length;
                    _logger.LogInformation("Processing role {RoleName} with {PageCount} pages", roleName, pageIds.Length);

                    foreach (var pageIdentifier in pageIds)
                    {
                        if (string.IsNullOrWhiteSpace(pageIdentifier))
                        {
                            _logger.LogWarning("Empty or null page identifier encountered for role {RoleName}, skipping", roleName);
                            continue;
                        }

                        // Try to find page by Title or PageId
                        // SQL Server default collation is case-insensitive, so == should work
                        var normalizedIdentifier = pageIdentifier.Trim().ToLower();

                        var page = await _context.PageAccesses
                            .Where(p => p.IsActive && (!string.IsNullOrEmpty(p.PageId)))
                            .Select(p => new { Page = p, NormalizedPageId = p.PageId.ToLower(), NormalizedTitle = p.Title.ToLower() })
                            .Where(p => p.NormalizedPageId == normalizedIdentifier || p.NormalizedTitle == normalizedIdentifier)
                            .FirstOrDefaultAsync();
                        
                        if (page != null)
                        {
                            // Page found and is active (we already filtered for IsActive)
                            rolePageAccesses.Add(new RolePageAccess
                            {
                                RoleName = roleName,
                                PageAccessId = page.Page.Id,
                                HasAccess = true,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = currentUserId,
                                PagePath = page.Page.Path
                            });
                            
                            // Log which identifier was used (helpful for debugging)
                            if (page.Page.Title == pageIdentifier && page.Page.PageId != pageIdentifier)
                            {
                                _logger.LogInformation("Page matched by Title '{Title}' (PageId: '{PageId}') for role {RoleName}", 
                                    page.Page.Title, page.Page.PageId, roleName);
                            }
                            else if (page.Page.PageId == pageIdentifier && page.Page.Title != pageIdentifier)
                            {
                                _logger.LogInformation("Page matched by PageId '{PageId}' (Title: '{Title}') for role {RoleName}", 
                                    page.Page.PageId, page.Page.Title, roleName);
                            }
                        }
                        else
                        {
                            missingPages.Add($"{roleName}:{pageIdentifier}");
                            _logger.LogWarning("Page '{PageIdentifier}' not found in database for role {RoleName}. Searched by both Title and PageId.", 
                                pageIdentifier, roleName);
                            
                            // Log similar page titles for debugging (only if enabled to avoid performance issues)
                            try
                            {
                                if (pageIdentifier.Length >= 3 && pageIdentifier.Length <= 100)
                                {
                                    var prefix = pageIdentifier.Substring(0, Math.Min(10, pageIdentifier.Length));
                                    // Get all pages first, then filter in memory to avoid EF translation issues
                                    var allActivePages = await _context.PageAccesses
                                        .Where(p => p.IsActive)
                                        .Select(p => new { p.Title, p.PageId })
                                        .ToListAsync();
                                    
                                    var similarPages = allActivePages
                                        .Where(p => p.Title.Contains(prefix, StringComparison.OrdinalIgnoreCase) || 
                                                    p.PageId.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                                        .Select(p => $"'{p.Title}' (PageId: {p.PageId})")
                                        .Take(5)
                                        .ToList();
                                    
                                    if (similarPages.Any())
                                    {
                                        _logger.LogWarning("Similar pages found: {SimilarPages}", string.Join(", ", similarPages));
                                    }
                                }
                            }
                            catch (Exception logEx)
                            {
                                // Don't fail the whole operation if logging similar pages fails
                                _logger.LogWarning(logEx, "Could not retrieve similar pages for debugging");
                            }
                        }
                    }
                }

                if (missingPages.Any())
                {
                    _logger.LogWarning("Missing {MissingCount} pages in database: {MissingPages}", 
                        missingPages.Count, string.Join(", ", missingPages));
                }

                if (rolePageAccesses.Count == 0)
                {
                    // Get sample of what exists in database for comparison
                    var samplePages = await _context.PageAccesses
                        .Where(p => p.IsActive)
                        .Select(p => $"'{p.Title}' (PageId: {p.PageId})")
                        .Take(5)
                        .ToListAsync();
                    
                    var errorMessage = $"No valid page access records to save. " +
                        $"Total pages attempted: {totalPageIds}, " +
                        $"Missing pages: {missingPages.Count}. " +
                        $"Sample pages in database: {string.Join(", ", samplePages)}. " +
                        $"First 10 missing: {string.Join(", ", missingPages.Take(10))}";
                    
                    _logger.LogError(errorMessage);
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException(errorMessage);
                }

                _logger.LogInformation("Adding {AccessCount} role page access records to database", rolePageAccesses.Count);
                _context.RolePageAccesses.AddRange(rolePageAccesses);
                
                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved {SavedCount} role page access records to database", savedCount);

                if (savedCount == 0 && rolePageAccesses.Count > 0)
                {
                    _logger.LogError("SaveChangesAsync returned 0 but {ExpectedCount} records were added. This may indicate a database issue.", 
                        rolePageAccesses.Count);
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Failed to save {rolePageAccesses.Count} role page access records to the database.");
                }

                // Commit the transaction
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully");

                // Clear any cached data to ensure we read fresh from database
                // This ensures we get the committed data, not cached/stale data
                _context.ChangeTracker.Clear();

                // Return updated settings - read fresh from database after commit
                var updatedSettings = await GetPageAccessSettingsAsync();
                
                // Log what was actually retrieved for officer role
                if (updatedSettings.PageAccessByRole.ContainsKey("AdvantageOneOfficer"))
                {
                    var officerPages = updatedSettings.PageAccessByRole["AdvantageOneOfficer"];
                }
                
                _logger.LogInformation("Settings update completed. Retrieved {RoleCount} roles with {TotalPages} total pages", 
                    updatedSettings.PageAccessByRole.Count, 
                    updatedSettings.AvailablePages.Count);
                
                return updatedSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page access settings");
                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogInformation("Transaction rolled back due to error");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
                throw;
            }
        }

        public async Task<List<PageAccessDto>> GetAllPagesAsync()
        {
            try
            {
                return await _context.PageAccesses
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Title)
                    .Select(p => new PageAccessDto
                    {
                        Id = p.Id,
                        PageId = p.PageId,
                        Title = p.Title,
                        Path = p.Path,
                        Category = p.Category,
                        Description = p.Description,
                        IsActive = p.IsActive,
                        SortOrder = p.SortOrder,
                        CreatedAt = p.CreatedAt,
                        CreatedBy = p.CreatedBy,
                        UpdatedAt = p.UpdatedAt,
                        UpdatedBy = p.UpdatedBy
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all pages");
                throw;
            }
        }

        public async Task<PageAccessDto?> GetPageByIdAsync(int id)
        {
            try
            {
                var page = await _context.PageAccesses.FindAsync(id);
                if (page == null)
                    return null;

                return new PageAccessDto
                {
                    Id = page.Id,
                    PageId = page.PageId,
                    Title = page.Title,
                    Path = page.Path,
                    Category = page.Category,
                    Description = page.Description,
                    IsActive = page.IsActive,
                    SortOrder = page.SortOrder,
                    CreatedAt = page.CreatedAt,
                    CreatedBy = page.CreatedBy,
                    UpdatedAt = page.UpdatedAt,
                    UpdatedBy = page.UpdatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page by ID {PageId}", id);
                throw;
            }
        }

        public async Task<PageAccessDto?> GetPageByPageIdAsync(string pageId)
        {
            try
            {
                var page = await _context.PageAccesses.FirstOrDefaultAsync(p => p.PageId == pageId);
                if (page == null)
                    return null;

                return new PageAccessDto
                {
                    Id = page.Id,
                    PageId = page.PageId,
                    Title = page.Title,
                    Path = page.Path,
                    Category = page.Category,
                    Description = page.Description,
                    IsActive = page.IsActive,
                    SortOrder = page.SortOrder,
                    CreatedAt = page.CreatedAt,
                    CreatedBy = page.CreatedBy,
                    UpdatedAt = page.UpdatedAt,
                    UpdatedBy = page.UpdatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page by PageId {PageId}", pageId);
                throw;
            }
        }

        public async Task<PageAccessDto?> CreatePageAsync(CreatePageAccessRequestDto request, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Creating new page: {PageId}", request.PageId);

                var page = new PageAccess
                {
                    PageId = request.PageId,
                    Title = request.Title,
                    Path = request.Path,
                    Category = request.Category,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                _context.PageAccesses.Add(page);
                await _context.SaveChangesAsync();

                return await GetPageByIdAsync(page.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page: {PageId}", request.PageId);
                throw;
            }
        }

        public async Task<PageAccessDto?> UpdatePageAsync(int id, CreatePageAccessRequestDto request, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Updating page: {PageId}", id);

                var page = await _context.PageAccesses.FindAsync(id);
                if (page == null)
                    throw new ArgumentException($"Page with ID {id} not found");

                page.PageId = request.PageId;
                page.Title = request.Title;
                page.Path = request.Path;
                page.Category = request.Category;
                page.Description = request.Description;
                page.IsActive = request.IsActive;
                page.SortOrder = request.SortOrder;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                return await GetPageByIdAsync(page.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page: {PageId}", id);
                throw;
            }
        }

        public async Task<bool> DeletePageAsync(int id, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Deleting page: {PageId}", id);

                var page = await _context.PageAccesses.FindAsync(id);
                if (page == null)
                    return false;

                _context.PageAccesses.Remove(page);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page: {PageId}", id);
                throw;
            }
        }

        public async Task<List<RolePageAccessDto>> GetRolePageAccessAsync(string roleName)
        {
            try
            {
                return await _context.RolePageAccesses
                    .Where(rpa => rpa.RoleName == roleName)
                    .Include(rpa => rpa.PageAccess)
                    .Select(rpa => new RolePageAccessDto
                    {
                        Id = rpa.Id,
                        RoleName = rpa.RoleName,
                        PageAccessId = rpa.PageAccessId,
                        PagePath = rpa.PagePath,
                        HasAccess = rpa.HasAccess,
                        CreatedAt = rpa.CreatedAt,
                        CreatedBy = rpa.CreatedBy,
                        UpdatedAt = rpa.UpdatedAt,
                        UpdatedBy = rpa.UpdatedBy,
                        PageAccess = new PageAccessDto
                        {
                            Id = rpa.PageAccess.Id,
                            PageId = rpa.PageAccess.PageId,
                            Title = rpa.PageAccess.Title,
                            Path = rpa.PageAccess.Path,
                            Category = rpa.PageAccess.Category,
                            Description = rpa.PageAccess.Description,
                            IsActive = rpa.PageAccess.IsActive,
                            SortOrder = rpa.PageAccess.SortOrder,
                            CreatedAt = rpa.PageAccess.CreatedAt,
                            CreatedBy = rpa.PageAccess.CreatedBy,
                            UpdatedAt = rpa.PageAccess.UpdatedAt,
                            UpdatedBy = rpa.PageAccess.UpdatedBy
                        }
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role page access for role: {RoleName}", roleName);
                throw;
            }
        }

        public async Task<bool> UpdateRolePageAccessAsync(UpdatePageAccessRequestDto request, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Updating role page access for role: {RoleName}", request.RoleName);

                // Remove existing access for this role
                // Normalize role name to lowercase for consistency
                var normalizedRoleName = request.RoleName.ToLowerInvariant();
                
                var existingAccess = await _context.RolePageAccesses
                    .Where(rpa => rpa.RoleName == normalizedRoleName)
                    .ToListAsync();

                _context.RolePageAccesses.RemoveRange(existingAccess);

                // Add new access
                var rolePageAccesses = new List<RolePageAccess>();

                foreach (var pageId in request.PageIds)
                {
                    var page = await _context.PageAccesses.FirstOrDefaultAsync(p => p.PageId == pageId);
                    if (page != null)
                    {
                        rolePageAccesses.Add(new RolePageAccess
                        {
                            RoleName = normalizedRoleName,
                            PageAccessId = page.Id,
                            HasAccess = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            PagePath = page.Path
                        });
                    }
                }

                _context.RolePageAccesses.AddRange(rolePageAccesses);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role page access for role: {RoleName}", request.RoleName);
                throw;
            }
        }

        public async Task<bool> CheckUserAccessAsync(string userId, string pagePath)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return false;

                // Get user roles from Identity system
                var userRoles = await _userManager.GetRolesAsync(user);

                // Administrator has access to all pages (case-insensitive check)
                if (userRoles.Any(r => r.Equals("administrator", StringComparison.OrdinalIgnoreCase)))
                    return true;

                var page = await _context.PageAccesses.FirstOrDefaultAsync(p => p.Path == pagePath && p.IsActive);
                if (page == null)
                    return false;

                var hasAccess = await _context.RolePageAccesses
                    .AnyAsync(rpa => userRoles.Contains(rpa.RoleName) && 
                                   rpa.PageAccessId == page.Id && 
                                   rpa.HasAccess);

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user access for user {UserId} to page {PagePath}", userId, pagePath);
                return false;
            }
        }

        public async Task<PageAccessStatisticsDto> GetPageAccessStatisticsAsync()
        {
            try
            {
                var totalPages = await _context.PageAccesses.CountAsync();
                var activePages = await _context.PageAccesses.CountAsync(p => p.IsActive);
                var totalRoles = await _context.RolePageAccesses.Select(rpa => rpa.RoleName).Distinct().CountAsync();

                var pagesByCategory = await _context.PageAccesses
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.Category ?? "Uncategorized")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                var accessByRole = await _context.RolePageAccesses
                    .Where(rpa => rpa.HasAccess)
                    .GroupBy(rpa => rpa.RoleName)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());

                return new PageAccessStatisticsDto
                {
                    TotalPages = totalPages,
                    ActivePages = activePages,
                    TotalRoles = totalRoles,
                    PagesByCategory = pagesByCategory,
                    AccessByRole = accessByRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page access statistics");
                throw;
            }
        }

        private PageAccessSettingsDto GetDefaultPageAccessSettings()
        {
            var defaultPages = new List<PageAccessDto>
            {
                new PageAccessDto { Id = 1, PageId = "dashboard", Title = "Dashboard", Path = "/dashboard", Category = "Main", Description = "Main dashboard", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 2, PageId = "action-calendar", Title = "Action Calendar", Path = "/action-calendar", Category = "Action Calendar", Description = "Task calendar and management", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 3, PageId = "profile", Title = "Profile", Path = "/profile", Category = "Settings", Description = "User profile", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 4, PageId = "settings", Title = "Settings", Path = "/settings", Category = "Settings", Description = "Application settings", IsActive = true, SortOrder = 4, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 5, PageId = "user-setup", Title = "User Setup", Path = "/administration/user-setup", Category = "Administration", Description = "User management and setup", IsActive = true, SortOrder = 5, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 6, PageId = "employee-registration", Title = "Employee Registration", Path = "/administration/employee-registration", Category = "Administration", Description = "Employee registration and management", IsActive = true, SortOrder = 6, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 7, PageId = "customer-setup", Title = "Customer Setup", Path = "/administration/customer-setup", Category = "Administration", Description = "Customer, region, and site management", IsActive = true, SortOrder = 7, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 8, PageId = "incident-report", Title = "Incident Report", Path = "/operations/incident-report", Category = "Operations", Description = "Report security incidents", IsActive = true, SortOrder = 8, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 9, PageId = "incident-graph", Title = "Incident Graph", Path = "/operations/incident-graph", Category = "Operations", Description = "Visualize incident data", IsActive = true, SortOrder = 9, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 10, PageId = "crime-intelligence", Title = "Crime Intelligence", Path = "/operations/crime-intelligence", Category = "Operations", Description = "Crime intelligence and analysis", IsActive = true, SortOrder = 10, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 11, PageId = "alert-rules", Title = "Alert Rules", Path = "/operations/alert-rules", Category = "Operations", Description = "Configure alert rules for crime intelligence", IsActive = true, SortOrder = 11, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" },
                new PageAccessDto { Id = 12, PageId = "data-analytics-hub", Title = "Data Analytics Hub", Path = "/analytics/data-analytics-hub", Category = "Analytics", Description = "Data analytics and insights", IsActive = true, SortOrder = 12, CreatedAt = DateTime.UtcNow, CreatedBy = "System", UpdatedAt = null, UpdatedBy = "" }
            };

            var defaultPageAccessByRole = new Dictionary<string, string[]>
            {
                ["administrator"] = defaultPages.Select(p => p.PageId).ToArray(),
                ["manager"] = defaultPages.Where(p => 
                    p.Category != "Customer" || 
                    p.PageId == "customer-views-config").Select(p => p.PageId).ToArray(),
                ["store"] = defaultPages.Where(p => 
                    p.Category == "Main" || 
                    p.Category == "Operations" ||
                    AdvantageOneOfficerCustomerPageIds.Contains(p.PageId, StringComparer.OrdinalIgnoreCase))
                    .Select(p => p.PageId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };

            return new PageAccessSettingsDto
            {
                PageAccessByRole = defaultPageAccessByRole,
                AvailablePages = defaultPages
            };
        }

        /// <summary>
        /// Initializes default page access settings in the database.
        /// 
        /// ARCHITECTURE: Database-First Approach
        /// This method is the PRIMARY way pages are managed. It is:
        /// - Called automatically on backend startup (Program.cs)
        /// - Called during database seeding (DataSeedingService)
        /// - Fully idempotent (safe to call multiple times)
        /// 
        /// Pages are NOT synced from frontend - the database is the single source of truth.
        /// This ensures enterprise-grade reliability and prevents sync failures from breaking functionality.
        /// 
        /// To add new pages: Update the defaultPages list below and deploy the backend.
        /// </summary>
        public async Task<bool> InitializeDefaultPageAccessAsync(string currentUserId)
        {
            try
            {
                _logger.LogInformation("Initializing default page access settings (database-first approach)");

                // Validate and get a valid user ID (or null if no valid user exists)
                var validUserId = await GetValidUserIdAsync(currentUserId);
                if (string.IsNullOrWhiteSpace(validUserId))
                {
                    validUserId = "system";
                }

                var defaultPages = new List<PageAccess>
                {
                    // Main/Dashboard
                    new PageAccess { PageId = "dashboard", Title = "Dashboard", Path = "/dashboard", Category = "Dashboard", Description = "Main dashboard", SortOrder = 1 },
                    new PageAccess { PageId = "action-calendar", Title = "Action Calendar", Path = "/action-calendar", Category = "Action Calendar", Description = "Task calendar and management", SortOrder = 2 },
                    new PageAccess { PageId = "profile", Title = "Profile", Path = "/profile", Category = "Settings", Description = "User profile", SortOrder = 3 },
                    new PageAccess { PageId = "settings", Title = "Settings", Path = "/settings", Category = "Settings", Description = "Application settings", SortOrder = 4 },

                    // Administration
                    new PageAccess { PageId = "user-setup", Title = "User Setup", Path = "/administration/user-setup", Category = "Administration", Description = "User management and setup", SortOrder = 10 },
                    new PageAccess { PageId = "employee-registration", Title = "Employee Registration", Path = "/administration/employee-registration", Category = "Administration", Description = "Employee registration", SortOrder = 11 },
                    new PageAccess { PageId = "customer-setup", Title = "Customer Setup", Path = "/administration/customer-setup", Category = "Administration", Description = "Customer management", SortOrder = 12 },

                    // Operations
                    new PageAccess { PageId = "incident-report", Title = "Incident Report", Path = "/operations/incident-report", Category = "Operations", Description = "Report security incidents", SortOrder = 20 },
                    new PageAccess { PageId = "incident-graph", Title = "Incident Graph", Path = "/operations/incident-graph", Category = "Operations", Description = "Visualize incident data", SortOrder = 21 },
                    new PageAccess { PageId = "crime-intelligence", Title = "Crime Intelligence", Path = "/operations/crime-intelligence", Category = "Operations", Description = "Crime intelligence and analysis", SortOrder = 22 },
                    new PageAccess { PageId = "alert-rules", Title = "Alert Rules", Path = "/operations/alert-rules", Category = "Operations", Description = "Configure alert rules for crime intelligence", SortOrder = 23 },
                    
                    // Analytics
                    new PageAccess { PageId = "data-analytics-hub", Title = "Data Analytics Hub", Path = "/analytics/data-analytics-hub", Category = "Analytics", Description = "Data analytics and insights", SortOrder = 30 }
                };

				var expectedPageCount = defaultPages.Count;
				var existingPageCount = await _context.PageAccesses.CountAsync();

				if (existingPageCount >= expectedPageCount)
				{
					_logger.LogInformation(
						"Existing page count matches or exceeds expected ({ExistingCount}/{ExpectedCount}); ensuring any missing defaults are added",
						existingPageCount,
						expectedPageCount);
				}
				else if (existingPageCount > 0)
				{
					_logger.LogWarning(
						"Partial page data exists ({ExistingCount}/{ExpectedCount}). Attempting to clean up and recreate.",
						existingPageCount,
						expectedPageCount);

					// Clear existing page access data to avoid conflicts
					await _context.RolePageAccesses.ExecuteDeleteAsync();
					await _context.PageAccesses.ExecuteDeleteAsync();
					await _context.SaveChangesAsync();

					_logger.LogInformation("Cleared existing page access data, will recreate all pages");
				}

				var existingPages = await _context.PageAccesses.ToListAsync();
				var existingPagesDict = existingPages
					.ToDictionary(p => p.PageId, p => p, StringComparer.OrdinalIgnoreCase);

				var pagesToAdd = new List<PageAccess>();
				var pagesToUpdate = new List<PageAccess>();
				var updatedCount = 0;

				foreach (var defaultPage in defaultPages)
				{
					if (existingPagesDict.TryGetValue(defaultPage.PageId, out var existingPage))
					{
						// Update existing page if path, title, category, or description has changed
						var needsUpdate = false;
						
						if (existingPage.Path != defaultPage.Path)
						{
							_logger.LogInformation("Updating page {PageId} path: {OldPath} -> {NewPath}", 
								defaultPage.PageId, existingPage.Path, defaultPage.Path);
							existingPage.Path = defaultPage.Path;
							needsUpdate = true;
						}
						
						if (existingPage.Title != defaultPage.Title)
						{
							_logger.LogInformation("Updating page {PageId} title: {OldTitle} -> {NewTitle}", 
								defaultPage.PageId, existingPage.Title, defaultPage.Title);
							existingPage.Title = defaultPage.Title;
							needsUpdate = true;
						}
						
						if (existingPage.Category != defaultPage.Category)
						{
							_logger.LogInformation("Updating page {PageId} category: {OldCategory} -> {NewCategory}", 
								defaultPage.PageId, existingPage.Category, defaultPage.Category);
							existingPage.Category = defaultPage.Category;
							needsUpdate = true;
						}
						
						if (existingPage.SortOrder != defaultPage.SortOrder)
						{
							existingPage.SortOrder = defaultPage.SortOrder;
							needsUpdate = true;
						}
						
						// Update description if provided and different
						if (!string.IsNullOrEmpty(defaultPage.Description) && 
						    existingPage.Description != defaultPage.Description)
						{
							existingPage.Description = defaultPage.Description;
							needsUpdate = true;
						}
						
						// Ensure page is active
						if (!existingPage.IsActive)
						{
							existingPage.IsActive = true;
							needsUpdate = true;
						}
						
						if (needsUpdate)
						{
							existingPage.UpdatedAt = DateTime.UtcNow;
							existingPage.UpdatedBy = validUserId;
							pagesToUpdate.Add(existingPage);
							updatedCount++;
						}
					}
					else
					{
						// Create new page
						var newPage = new PageAccess
						{
							PageId = defaultPage.PageId,
							Title = defaultPage.Title,
							Path = defaultPage.Path,
							Category = defaultPage.Category ?? "Uncategorized",
							Description = defaultPage.Description,
							SortOrder = defaultPage.SortOrder,
							IsActive = true,
							CreatedAt = DateTime.UtcNow,
							CreatedBy = validUserId
						};
						pagesToAdd.Add(newPage);
					}
				}

				if (pagesToAdd.Any())
				{
					_logger.LogInformation("Creating {PageCount} new default pages", pagesToAdd.Count);
					_context.PageAccesses.AddRange(pagesToAdd);
				}

				if (pagesToUpdate.Any())
				{
					_logger.LogInformation("Updating {PageCount} existing pages", pagesToUpdate.Count);
					_context.PageAccesses.UpdateRange(pagesToUpdate);
				}

				if (pagesToAdd.Any() || pagesToUpdate.Any())
				{
					await _context.SaveChangesAsync();
					_logger.LogInformation("Page sync completed: {Created} created, {Updated} updated", 
						pagesToAdd.Count, updatedCount);
				}
				else
				{
					_logger.LogInformation("No page changes required - all pages are up to date");
				}

				// Only seed default role access if none exists
				var existingRoleAccessCount = await _context.RolePageAccesses.CountAsync();
				if (existingRoleAccessCount == 0)
				{
					var allPages = await _context.PageAccesses.ToListAsync();
					var roleAccesses = new List<RolePageAccess>();
					var roleAccessSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

					// Admin gets access to all pages
				foreach (var page in allPages)
				{
					var key = $"admin|{page.Id}";
						if (roleAccessSet.Contains(key)) continue;

						roleAccesses.Add(new RolePageAccess
						{
						RoleName = "administrator",
							PageAccessId = page.Id,
							HasAccess = true,
							CreatedAt = DateTime.UtcNow,
							CreatedBy = validUserId,
							PagePath = page.Path
						});
						roleAccessSet.Add(key);
					}

					// Manager gets access to most pages
					var managerPages = allPages.Where(p =>
						p.Category != "Customer" ||
						p.PageId == "customer-reporting-page" ||
						p.PageId == "customer-views-config").ToList();

				foreach (var page in managerPages)
				{
					var key = $"manager|{page.Id}";
						if (roleAccessSet.Contains(key)) continue;

						roleAccesses.Add(new RolePageAccess
						{
						RoleName = "manager",
							PageAccessId = page.Id,
							HasAccess = true,
							CreatedAt = DateTime.UtcNow,
							CreatedBy = validUserId,
							PagePath = page.Path
						});
						roleAccessSet.Add(key);
					}

					// Security Officer gets incident-report by default; admin can enable additional pages via Settings
					var securityOfficerPageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
					{
						"dashboard", "profile", "incident-report"
					};
					var securityOfficerPages = allPages.Where(p =>
						securityOfficerPageIds.Contains(p.PageId)).ToList();

					foreach (var page in securityOfficerPages)
					{
						var key = $"security-officer|{page.Id}";
						if (roleAccessSet.Contains(key)) continue;

						roleAccesses.Add(new RolePageAccess
						{
							RoleName = "security-officer",
							PageAccessId = page.Id,
							HasAccess = true,
							CreatedAt = DateTime.UtcNow,
							CreatedBy = validUserId,
							PagePath = page.Path
						});
						roleAccessSet.Add(key);
					}

					// Store user: ONLY incident page under Operations (no configurable extras)
					var storePageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
					{
						"dashboard", "profile", "incident-report"
					};
					var storePages = allPages.Where(p =>
						storePageIds.Contains(p.PageId)).ToList();

					foreach (var page in storePages)
					{
						var key = $"store|{page.Id}";
						if (roleAccessSet.Contains(key)) continue;

						roleAccesses.Add(new RolePageAccess
						{
							RoleName = "store",
							PageAccessId = page.Id,
							HasAccess = true,
							CreatedAt = DateTime.UtcNow,
							CreatedBy = validUserId,
							PagePath = page.Path
						});
						roleAccessSet.Add(key);
					}

					if (roleAccesses.Any())
					{
						_context.RolePageAccesses.AddRange(roleAccesses);
						await _context.SaveChangesAsync();
						_logger.LogInformation("Created {RoleAccessCount} default role access records", roleAccesses.Count);
					}
					else
					{
						_logger.LogInformation("No default role access records required");
					}
				}
				else
				{
					_logger.LogInformation("Existing role access records detected ({RoleAccessCount}); preserving admin settings", existingRoleAccessCount);
					
					// Normalize existing RolePageAccess records to lowercase
					await NormalizeRolePageAccessRoleNamesAsync(validUserId);
					
					// Ensure officer role has default access (for new role migration)
					await EnsureOfficerRoleAccessAsync(validUserId);
				}

                _logger.LogInformation("Default page access settings initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default page access settings");
                throw;
            }
        }

        /// <summary>
        /// Normalizes all RolePageAccess RoleName values to lowercase to ensure consistency.
        /// This handles existing records that may have been created with uppercase role names.
        /// </summary>
        private async Task NormalizeRolePageAccessRoleNamesAsync(string validUserId)
        {
            try
            {
                _logger.LogInformation("Normalizing RolePageAccess role names to lowercase...");

                // Get all RolePageAccess records
                var allRecords = await _context.RolePageAccesses.ToListAsync();
                
                if (!allRecords.Any())
                {
                    _logger.LogInformation("No RolePageAccess records to normalize");
                    return;
                }

                var updatedCount = 0;
                var deletedCount = 0;
                var recordsToDelete = new List<RolePageAccess>();

                // Group records by normalized role name and page access ID to find duplicates
                var normalizedGroups = allRecords
                    .GroupBy(rpa => new 
                    { 
                        NormalizedRoleName = rpa.RoleName.ToLowerInvariant(), 
                        PageAccessId = rpa.PageAccessId 
                    })
                    .ToList();

                foreach (var group in normalizedGroups)
                {
                    var records = group.ToList();
                    
                    // If all records in the group already have lowercase names, skip
                    if (records.All(r => r.RoleName == group.Key.NormalizedRoleName))
                    {
                        continue;
                    }

                    // Find the record with lowercase name (if exists) or the first one to keep
                    var recordToKeep = records.FirstOrDefault(r => r.RoleName == group.Key.NormalizedRoleName)
                        ?? records.First();

                    // Update records that need normalization
                    foreach (var record in records)
                    {
                        if (record.Id == recordToKeep.Id)
                        {
                            // This is the record we're keeping
                            if (record.RoleName != group.Key.NormalizedRoleName)
                            {
                                record.RoleName = group.Key.NormalizedRoleName;
                                record.UpdatedAt = DateTime.UtcNow;
                                record.UpdatedBy = validUserId;
                                updatedCount++;
                            }
                        }
                        else
                        {
                            // This is a duplicate, mark for deletion
                            recordsToDelete.Add(record);
                            deletedCount++;
                        }
                    }
                }

                // Remove duplicates
                if (recordsToDelete.Any())
                {
                    _context.RolePageAccesses.RemoveRange(recordsToDelete);
                    _logger.LogInformation("Marked {Count} duplicate RolePageAccess records for deletion", recordsToDelete.Count);
                }

                // Save changes
                if (updatedCount > 0 || deletedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Normalized RolePageAccess role names: {Updated} updated, {Deleted} duplicates removed", 
                        updatedCount, deletedCount);
                }
                else
                {
                    _logger.LogInformation("All RolePageAccess records already have lowercase role names");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing RolePageAccess role names");
                throw;
            }
        }

        /// <summary>
        /// Ensures the officer role has default page access (dashboard, profile, incident-report).
        /// Called when role access exists but officer role may be missing (e.g. after adding officer role).
        /// </summary>
        private async Task EnsureOfficerRoleAccessAsync(string validUserId)
        {
            try
            {
                var securityOfficerAccessCount = await _context.RolePageAccesses
                    .Where(rpa => rpa.RoleName == "security-officer" && rpa.HasAccess)
                    .CountAsync();
                
                if (securityOfficerAccessCount > 0)
                {
                    _logger.LogInformation("Security Officer role already has {Count} page access records", securityOfficerAccessCount);
                    return;
                }

                _logger.LogInformation("Adding default page access for security-officer role");
                var allPages = await _context.PageAccesses.Where(p => p.IsActive).ToListAsync();
                var securityOfficerPageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "dashboard", "profile", "incident-report"
                };
                var securityOfficerPages = allPages.Where(p => securityOfficerPageIds.Contains(p.PageId ?? "")).ToList();

                foreach (var page in securityOfficerPages)
                {
                    _context.RolePageAccesses.Add(new RolePageAccess
                    {
                        RoleName = "security-officer",
                        PageAccessId = page.Id,
                        HasAccess = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = validUserId,
                        PagePath = page.Path
                    });
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} default page access records for security-officer role", securityOfficerPages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring officer role access");
                // Don't throw - this is a best-effort migration
            }
        }

        /// <summary>
        /// Syncs pages from frontend definitions to the database.
        /// 
        /// NOTE: This is a LEGACY/OPTIONAL method. The primary way to manage pages is via
        /// InitializeDefaultPageAccessAsync which runs on backend startup (database-first approach).
        /// 
        /// This method is kept for:
        /// - Manual page updates via admin UI
        /// - Migration scenarios
        /// - Development convenience
        /// 
        /// For production, prefer updating InitializeDefaultPageAccessAsync and deploying.
        /// </summary>
        public async Task<SyncResultDto> SyncPagesFromDefinitionsAsync(SyncPagesRequestDto request, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Syncing {PageCount} pages from frontend definitions (legacy method - prefer database-first approach)", request.Pages.Count);

                var existingPages = await _context.PageAccesses.ToListAsync();
                var existingPagesDict = existingPages
                    .ToDictionary(p => p.PageId, p => p, StringComparer.OrdinalIgnoreCase);

                var pagesToAdd = new List<PageAccess>();
                var pagesToUpdate = new List<PageAccess>();
                var createdCount = 0;
                var updatedCount = 0;

                foreach (var pageDef in request.Pages)
                {
                    if (existingPagesDict.TryGetValue(pageDef.PageId, out var existingPage))
                    {
                        // Update existing page if path, title, category, or description has changed
                        var needsUpdate = false;
                        
                        if (existingPage.Path != pageDef.Path)
                        {
                            _logger.LogInformation("Updating page {PageId} path: {OldPath} -> {NewPath}", 
                                pageDef.PageId, existingPage.Path, pageDef.Path);
                            existingPage.Path = pageDef.Path;
                            needsUpdate = true;
                        }
                        
                        if (existingPage.Title != pageDef.Title)
                        {
                            _logger.LogInformation("Updating page {PageId} title: {OldTitle} -> {NewTitle}", 
                                pageDef.PageId, existingPage.Title, pageDef.Title);
                            existingPage.Title = pageDef.Title;
                            needsUpdate = true;
                        }
                        
                        // Determine category - auto-detect Customer category from path if not provided
                        string? newCategory = pageDef.Category;
                        if (string.IsNullOrWhiteSpace(newCategory) && 
                            !string.IsNullOrWhiteSpace(pageDef.Path) && 
                            pageDef.Path.ToLower().StartsWith("/customer"))
                        {
                            newCategory = "Customer";
                            _logger.LogInformation("Auto-detected Customer category for page {PageId} based on path {Path}", 
                                pageDef.PageId, pageDef.Path);
                        }
                        
                        if (existingPage.Category != newCategory)
                        {
                            _logger.LogInformation("Updating page {PageId} category: {OldCategory} -> {NewCategory}", 
                                pageDef.PageId, existingPage.Category, newCategory);
                            existingPage.Category = newCategory;
                            needsUpdate = true;
                        }
                        
                        if (existingPage.SortOrder != pageDef.SortOrder)
                        {
                            existingPage.SortOrder = pageDef.SortOrder;
                            needsUpdate = true;
                        }
                        
                        // Update description if provided and different
                        if (!string.IsNullOrEmpty(pageDef.Description) && 
                            existingPage.Description != pageDef.Description)
                        {
                            existingPage.Description = pageDef.Description;
                            needsUpdate = true;
                        }
                        
                        // Ensure page is active
                        if (!existingPage.IsActive)
                        {
                            existingPage.IsActive = true;
                            needsUpdate = true;
                        }
                        
                        if (needsUpdate)
                        {
                            existingPage.UpdatedAt = DateTime.UtcNow;
                            existingPage.UpdatedBy = currentUserId;
                            pagesToUpdate.Add(existingPage);
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Determine category - auto-detect Customer category from path if not provided
                        string? category = pageDef.Category;
                        if (string.IsNullOrWhiteSpace(category) && 
                            !string.IsNullOrWhiteSpace(pageDef.Path) && 
                            pageDef.Path.ToLower().StartsWith("/customer"))
                        {
                            category = "Customer";
                            _logger.LogInformation("Auto-detected Customer category for page {PageId} based on path {Path}", 
                                pageDef.PageId, pageDef.Path);
                        }
                        
                        // Create new page
                        var newPage = new PageAccess
                        {
                            PageId = pageDef.PageId,
                            Title = pageDef.Title,
                            Path = pageDef.Path,
                            Category = category ?? "Uncategorized",
                            Description = pageDef.Description,
                            SortOrder = pageDef.SortOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId
                        };
                        pagesToAdd.Add(newPage);
                        createdCount++;
                    }
                }

                if (pagesToAdd.Any())
                {
                    _logger.LogInformation("Creating {PageCount} new pages from sync", pagesToAdd.Count);
                    _context.PageAccesses.AddRange(pagesToAdd);
                }

                if (pagesToUpdate.Any())
                {
                    _logger.LogInformation("Updating {PageCount} existing pages from sync", pagesToUpdate.Count);
                    _context.PageAccesses.UpdateRange(pagesToUpdate);
                }

                if (pagesToAdd.Any() || pagesToUpdate.Any())
                {
                    await _context.SaveChangesAsync();
                }

                var message = $"Sync completed: {createdCount} created, {updatedCount} updated, {request.Pages.Count} total";
                _logger.LogInformation(message);

                return new SyncResultDto
                {
                    Created = createdCount,
                    Updated = updatedCount,
                    Total = request.Pages.Count,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing pages from definitions");
                throw;
            }
        }
    }
}
