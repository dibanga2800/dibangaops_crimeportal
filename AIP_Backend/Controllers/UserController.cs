using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "administrator,manager")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new user account with optional employee linking
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<AdminUserResponseDto>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("Creating user account for {Username} with role {Role}", 
                    request.Username, request.Role);
                _logger.LogInformation("CreateUser request - CustomerId: {CustomerId}, EmployeeId: {EmployeeId}", 
                    request.CustomerId, request.EmployeeId);

                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                {
                    return BadRequest(new { error = "First name and last name are required" });
                }

                // Check if username already exists
                var existingUser = await _userManager.FindByNameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "Username already exists" });
                }

                // Check if email already exists
                existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "Email already exists" });
                }

                // Note: Employee validation removed (Employee model deleted)
                // EmployeeId field in ApplicationUser is kept for legacy data compatibility
                if (request.Role == "Employee" && request.EmployeeId.HasValue)
                {
                    // Employee validation skipped - Employee model deleted
                    // Employee linking is no longer supported
                }

                // Create new user
                var newUser = new ApplicationUser
                {
                    UserName = request.Username,
                    Email = request.Email,
                    EmailConfirmed = true, // Auto-confirm for admin-created accounts
                    PhoneNumber = request.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    JobTitle = request.JobTitle,
                    Signature = request.Signature,
                    SignatureCode = request.SignatureCode,
                    RecordIsDeleted = request.RecordIsDeleted,
                    RecordIsDeletedYN = request.RecordIsDeleted,
                    Role = request.Role ?? string.Empty,
                    PageAccessRole = string.IsNullOrWhiteSpace(request.PageAccessRole)
                        ? (request.Role ?? string.Empty)
                        : request.PageAccessRole,
                    PrimarySiteId = string.IsNullOrWhiteSpace(request.PrimarySiteId)
                        ? null
                        : request.PrimarySiteId
                };

                // Set CustomerId if provided (customer-linked users have a direct CustomerId)
                if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                {
                    var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                    if (customer != null)
                    {
                        newUser.CustomerId = request.CustomerId.Value;
                        _logger.LogInformation("Setting CustomerId {CustomerId} for user {UserId}", request.CustomerId.Value, newUser.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Customer with ID {CustomerId} not found. CustomerId not set.", request.CustomerId.Value);
                    }
                }

                var createResult = await _userManager.CreateAsync(newUser, request.Password);
                if (!createResult.Succeeded)
                {
                    return BadRequest(new { 
                        error = "Failed to create user account",
                        details = createResult.Errors.Select(e => e.Description)
                    });
                }

                // Assign role
                // Normalize role to lowercase for storage (backend stores roles in lowercase)
                var normalizedRoleForStorage = request.Role?.ToLowerInvariant() ?? "user";
                var roleResult = await _userManager.AddToRoleAsync(newUser, normalizedRoleForStorage);
                if (!roleResult.Succeeded)
                {
                    // Clean up user if role assignment fails
                    await _userManager.DeleteAsync(newUser);
                    return BadRequest(new { 
                        error = "Failed to assign role to user",
                        details = roleResult.Errors.Select(e => e.Description)
                    });
                }

                // Link to employee if provided
                if (request.EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FindAsync(request.EmployeeId.Value);
                    if (employee != null)
                    {
                        // Check if employee is already linked to another user
                        if (employee.UserId != null)
                        {
                            await _userManager.DeleteAsync(newUser);
                            return BadRequest(new { error = "Employee is already linked to another user account" });
                        }

                        // Link both sides of the relationship
                        newUser.EmployeeId = request.EmployeeId.Value;
                        employee.UserId = newUser.Id;
                        employee.DateModified = DateTime.UtcNow;
                        employee.ModifiedBy = newUser.Id; // Use the new user's ID as the modifier

                        await _userManager.UpdateAsync(newUser);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Linked new User {UserId} to Employee {EmployeeId}", newUser.Id, request.EmployeeId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Employee {EmployeeId} not found during user creation", request.EmployeeId.Value);
                    }
                }

                // Assign customers
                if (request.AssignedCustomerIds != null && request.AssignedCustomerIds.Any())
                {
                    // Use provided customer assignments
                    foreach (var customerId in request.AssignedCustomerIds)
                    {
                        var customer = await _context.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            var assignment = new UserCustomerAssignment
                            {
                                UserId = newUser.Id,
                                CustomerId = customerId,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = newUser.Id // Self-reference for now
                            };
                            _context.UserCustomerAssignments.Add(assignment);
                        }
                        else
                        {
                            _logger.LogWarning("Customer with ID {CustomerId} not found for user {UserId}", customerId, newUser.Id);
                        }
                    }

                    // Persist customer list in JSON column for backward compatibility
                    newUser.CustomerIds = request.AssignedCustomerIds;
                    _logger.LogInformation("Assigned user {UserId} to {CustomerCount} specified customers", 
                        newUser.Id, request.AssignedCustomerIds.Count);
                }
                else if (!newUser.CustomerId.HasValue)
                {
                    // For platform users without a direct CustomerId, assign all customers
                    var customers = await _context.Customers.ToListAsync();
                    var assignments = customers.Select(c => new UserCustomerAssignment
                    {
                        UserId = newUser.Id,
                        CustomerId = c.CustomerId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = newUser.Id
                    }).ToList();

                    _context.UserCustomerAssignments.AddRange(assignments);
                    
                    _logger.LogInformation("Assigned user {UserId} to all {CustomerCount} customers (platform user)", 
                        newUser.Id, customers.Count);
                }
                
                await _context.SaveChangesAsync();

                // Assign sites if provided
                if (request.AssignedSiteIds != null && request.AssignedSiteIds.Any())
                {
                    newUser.SiteIds = request.AssignedSiteIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    _logger.LogInformation("Assigned user {UserId} to {SiteCount} specified sites", 
                        newUser.Id, newUser.SiteIds.Count);
                    await _userManager.UpdateAsync(newUser);
                }

                // Return created user
                var userResponse = new AdminUserResponseDto
                {
                    Id = newUser.Id,
                    Username = newUser.UserName,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Email = newUser.Email,
                    Role = request.Role ?? string.Empty,
                    PageAccessRole = newUser.PageAccessRole ?? string.Empty,
                    Signature = newUser.Signature,
                    SignatureCode = newUser.SignatureCode,
                    JobTitle = newUser.JobTitle,
                    ProfilePicture = newUser.ProfilePicture,
                    CustomerId = newUser.CustomerId,
                    RecordIsDeleted = newUser.RecordIsDeleted,
                    IsActive = newUser.IsActive,
                    CreatedAt = newUser.CreatedAt,
                    UpdatedAt = newUser.UpdatedAt,
                    CreatedBy = newUser.CreatedBy,
                    UpdatedBy = newUser.UpdatedBy,
                    LastLoginAt = newUser.LastLoginAt,
                    PhoneNumber = newUser.PhoneNumber,
                    EmailConfirmed = newUser.EmailConfirmed,
                    EmployeeId = request.EmployeeId,
                    PrimarySiteId = newUser.PrimarySiteId,
                    AssignedCustomerIds = request.AssignedCustomerIds ?? new List<int>(),
                    AssignedCustomerNames = new List<string>(),
                    AssignedSiteIds = newUser.SiteIds.ToList()
                };

                _logger.LogInformation("User account created successfully: {UserId}", newUser.Id);
                return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user account");
                return StatusCode(500, new { error = "An error occurred while creating the user account" });
            }
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminUserResponseDto>> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var identityRole = roles.FirstOrDefault() ?? "User";
                var effectiveRole = string.IsNullOrWhiteSpace(user.Role)
                    ? identityRole
                    : user.Role;

                // Get assigned customers for this user
                var customerAssignments = await _context.UserCustomerAssignments
                    .Where(uca => uca.UserId == user.Id)
                    .Include(uca => uca.Customer)
                    .ToListAsync();

                var userResponse = new AdminUserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = effectiveRole,
                    PageAccessRole = user.PageAccessRole ?? string.Empty,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    JobTitle = user.JobTitle,
                    ProfilePicture = user.ProfilePicture,
                    CustomerId = user.CustomerId,
                    RecordIsDeleted = user.RecordIsDeleted,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PrimarySiteId = user.PrimarySiteId,
                    AssignedCustomerIds = customerAssignments.Select(uca => uca.CustomerId).ToList(),
                    AssignedCustomerNames = customerAssignments
                        .Where(uca => uca.Customer != null)
                        .Select(uca => uca.Customer!.CompanyName)
                        .ToList(),
                    AssignedSiteIds = user.SiteIds.ToList()
                };

                // Note: Employee information lookup removed (Employee model deleted)
                // EmployeeId is stored directly in ApplicationUser
                if (user.EmployeeId.HasValue)
                {
                    userResponse.EmployeeId = user.EmployeeId.Value;
                    userResponse.EmployeeName = $"User #{user.EmployeeId.Value}"; // Fallback name
                }

                // Get customer name if CustomerId is set (for customer users)
                if (user.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId.Value);
                    if (customer != null)
                    {
                        userResponse.CustomerName = customer.CompanyName;
                    }
                }

                // Get assigned customers
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the user" });
            }
        }

        /// <summary>
        /// Gets all users with pagination
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<UserListResponseDto>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Filter out soft-deleted users
                var query = _userManager.Users
                    .Where(u => !u.RecordIsDeleted && !u.RecordIsDeletedYN)
                    .AsQueryable();
                var totalCount = await query.CountAsync();

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userResponses = new List<AdminUserResponseDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var identityRole = roles.FirstOrDefault() ?? "User";
                    var effectiveRole = string.IsNullOrWhiteSpace(user.Role)
                        ? identityRole
                        : user.Role;

                    var userResponse = new AdminUserResponseDto
                    {
                        Id = user.Id,
                        Username = user.UserName ?? "",
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? "",
                        Role = effectiveRole,
                        PageAccessRole = user.PageAccessRole,
                        Signature = user.Signature,
                        SignatureCode = user.SignatureCode,
                        JobTitle = user.JobTitle,
                        ProfilePicture = user.ProfilePicture,
                        CustomerId = user.CustomerId,
                        RecordIsDeleted = user.RecordIsDeleted,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt,
                        CreatedBy = user.CreatedBy,
                        UpdatedBy = user.UpdatedBy,
                        LastLoginAt = user.LastLoginAt,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        PrimarySiteId = user.PrimarySiteId,
                        AssignedCustomerIds = new List<int>(),
                        AssignedCustomerNames = new List<string>(),
                        AssignedSiteIds = user.SiteIds.ToList()
                    };

                    // Note: Employee information lookup removed (Employee model deleted)
                    if (user.EmployeeId.HasValue)
                    {
                        userResponse.EmployeeId = user.EmployeeId.Value;
                        userResponse.EmployeeName = $"User #{user.EmployeeId.Value}"; // Fallback name
                    }

                    // Get customer name if CustomerId is set (for customer users)
                    if (user.CustomerId.HasValue)
                    {
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId.Value);
                        if (customer != null)
                        {
                            userResponse.CustomerName = customer.CompanyName;
                        }
                    }

                    // Get assigned customers
                    var customerAssignments = await _context.UserCustomerAssignments
                        .Where(uca => uca.UserId == user.Id)
                        .Include(uca => uca.Customer)
                        .ToListAsync();

                    userResponse.AssignedCustomerIds = customerAssignments.Select(uca => uca.CustomerId).ToList();
                    userResponse.AssignedCustomerNames = customerAssignments
                        .Where(uca => uca.Customer != null)
                        .Select(uca => uca.Customer!.CompanyName)
                        .ToList();

                    userResponses.Add(userResponse);
                }

                var response = new UserListResponseDto
                {
                    Users = userResponses,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new { error = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Gets users by role
        /// </summary>
        [HttpGet("by-role/{role}")]
        public async Task<ActionResult<List<AdminUserResponseDto>>> GetUsersByRole(string role)
        {
            try
            {
                var allUsersInRole = await _userManager.GetUsersInRoleAsync(role);
                // Filter out soft-deleted users
                var usersInRole = allUsersInRole.Where(u => !u.RecordIsDeleted && !u.RecordIsDeletedYN).ToList();
                var userResponses = new List<AdminUserResponseDto>();

                foreach (var user in usersInRole)
                {
                    // Get assigned customers for this user
                    var customerAssignments = await _context.UserCustomerAssignments
                        .Where(uca => uca.UserId == user.Id)
                        .Include(uca => uca.Customer)
                        .ToListAsync();

                    var userResponse = new AdminUserResponseDto
                    {
                        Id = user.Id,
                        Username = user.UserName ?? "",
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? "",
                        Role = role,
                        PageAccessRole = user.PageAccessRole,
                        Signature = user.Signature,
                        SignatureCode = user.SignatureCode,
                        JobTitle = user.JobTitle,
                        ProfilePicture = user.ProfilePicture,
                        CustomerId = user.CustomerId,
                        RecordIsDeleted = user.RecordIsDeleted,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt,
                        CreatedBy = user.CreatedBy,
                        UpdatedBy = user.UpdatedBy,
                        LastLoginAt = user.LastLoginAt,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        PrimarySiteId = user.PrimarySiteId,
                        AssignedCustomerIds = customerAssignments.Select(uca => uca.CustomerId).ToList(),
                        AssignedCustomerNames = customerAssignments
                            .Where(uca => uca.Customer != null)
                            .Select(uca => uca.Customer!.CompanyName)
                            .ToList(),
                        AssignedSiteIds = user.SiteIds.ToList()
                    };

                    // Note: Employee information lookup removed (Employee model deleted)
                    if (user.EmployeeId.HasValue)
                    {
                        userResponse.EmployeeId = user.EmployeeId.Value;
                        userResponse.EmployeeName = $"User #{user.EmployeeId.Value}"; // Fallback name
                    }

                    // Get customer name if CustomerId is set (for customer users)
                    if (user.CustomerId.HasValue)
                    {
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId.Value);
                        if (customer != null)
                        {
                            userResponse.CustomerName = customer.CompanyName;
                        }
                    }

                    userResponses.Add(userResponse);
                }

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                return StatusCode(500, new { error = "An error occurred while retrieving users by role" });
            }
        }

        /// <summary>
        /// Gets unlinked employees (employees without user accounts)
        /// </summary>
        [HttpGet("unlinked-employees")]
        public async Task<ActionResult<List<EmployeeResponseDto>>> GetUnlinkedEmployees()
        {
            try
            {
                _logger.LogInformation("Fetching unlinked employees (employees without user accounts)");

                // Query employees where UserId is null (not linked to any user account)
                var unlinkedEmployees = await _context.Employees
                    .Where(e => e.UserId == null && 
                                e.RecordIsDeletedYN == false &&
                                e.EmployeeStatus.ToLower() == "active")
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.Surname)
                    .Select(e => new EmployeeResponseDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeNumber = e.EmployeeNumber,
                        FirstName = e.FirstName,
                        Surname = e.Surname,
                        Email = e.Email ?? string.Empty,
                        Position = e.Position,
                        EmployeeStatus = e.EmployeeStatus,
                        UserId = e.UserId
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} unlinked employees", unlinkedEmployees.Count);
                return Ok(unlinkedEmployees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unlinked employees");
                return StatusCode(500, new { error = "An error occurred while retrieving unlinked employees" });
            }
        }

        /// <summary>
        /// Gets linked employees (employees with user accounts)
        /// </summary>
        [HttpGet("linked-employees")]
        public async Task<ActionResult<List<EmployeeResponseDto>>> GetLinkedEmployees()
        {
            try
            {
                _logger.LogInformation("Fetching linked employees (employees with user accounts)");

                // Query employees where UserId is not null (linked to a user account)
                var linkedEmployees = await _context.Employees
                    .Where(e => e.UserId != null && 
                                e.RecordIsDeletedYN == false &&
                                e.EmployeeStatus.ToLower() == "active")
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.Surname)
                    .Select(e => new EmployeeResponseDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeNumber = e.EmployeeNumber,
                        FirstName = e.FirstName,
                        Surname = e.Surname,
                        Email = e.Email ?? string.Empty,
                        Position = e.Position,
                        EmployeeStatus = e.EmployeeStatus,
                        UserId = e.UserId
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} linked employees", linkedEmployees.Count);
                return Ok(linkedEmployees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting linked employees");
                return StatusCode(500, new { error = "An error occurred while retrieving linked employees" });
            }
        }

        /// <summary>
        /// Validates if a username is available
        /// </summary>
        [HttpGet("validate-username")]
        public async Task<ActionResult<ValidationResponseDto>> ValidateUsername([FromQuery] string username)
        {
            try
            {
                var existingUser = await _userManager.FindByNameAsync(username);
                var available = existingUser == null;

                return Ok(new ValidationResponseDto
                {
                    Available = available,
                    Message = available ? "Username is available" : "Username already exists"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating username: {Username}", username);
                return StatusCode(500, new { error = "An error occurred while validating username" });
            }
        }

        /// <summary>
        /// Validates if an email is available
        /// </summary>
        [HttpGet("validate-email")]
        public async Task<ActionResult<ValidationResponseDto>> ValidateEmail([FromQuery] string email)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                var available = existingUser == null;

                return Ok(new ValidationResponseDto
                {
                    Available = available,
                    Message = available ? "Email is available" : "Email already exists"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email: {Email}", email);
                return StatusCode(500, new { error = "An error occurred while validating email" });
            }
        }

        /// <summary>
        /// Links an existing user to an employee
        /// </summary>
        [HttpPost("{userId}/link-employee")]
        public async Task<ActionResult<AdminUserResponseDto>> LinkToEmployee(string userId, [FromBody] LinkEmployeeRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Check if employee exists
                var employee = await _context.Employees.FindAsync(request.EmployeeId);
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found" });
                }

                // Check if employee is already linked to another user
                if (employee.UserId != null && employee.UserId != userId)
                {
                    return BadRequest(new { error = "Employee is already linked to another user account" });
                }

                // Update both sides of the relationship
                user.EmployeeId = request.EmployeeId;
                employee.UserId = userId;
                employee.DateModified = DateTime.UtcNow;
                employee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Linked User {UserId} to Employee {EmployeeId}", userId, request.EmployeeId);

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var userResponse = new AdminUserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = role,
                    PageAccessRole = user.PageAccessRole ?? string.Empty,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    JobTitle = user.JobTitle,
                    ProfilePicture = user.ProfilePicture,
                    CustomerId = user.CustomerId,
                    RecordIsDeleted = user.RecordIsDeleted,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    EmployeeId = user.EmployeeId,
                    EmployeeName = user.EmployeeId.HasValue ? $"User #{user.EmployeeId.Value}" : null
                };

                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking user {UserId} to employee", userId);
                return StatusCode(500, new { error = "An error occurred while linking user to employee" });
            }
        }

        /// <summary>
        /// Unlinks a user from an employee
        /// </summary>
        [HttpPost("{userId}/unlink-employee")]
        public async Task<ActionResult<AdminUserResponseDto>> UnlinkFromEmployee(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Find and unlink the employee (both sides of the relationship)
                if (user.EmployeeId.HasValue)
                {
                    var employee = await _context.Employees.FindAsync(user.EmployeeId.Value);
                    if (employee != null)
                    {
                        employee.UserId = null;
                        employee.DateModified = DateTime.UtcNow;
                        employee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        await _context.SaveChangesAsync();
                    }

                    user.EmployeeId = null;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Unlinked User {UserId} from Employee {EmployeeId}", userId, employee?.EmployeeId);
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var userResponse = new AdminUserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = role,
                    PageAccessRole = user.PageAccessRole ?? string.Empty,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    JobTitle = user.JobTitle,
                    CustomerId = user.CustomerId,
                    RecordIsDeleted = user.RecordIsDeleted,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed
                };

                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking user {UserId} from employee", userId);
                return StatusCode(500, new { error = "An error occurred while unlinking user from employee" });
            }
        }

        /// <summary>
        /// Updates a user account
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AdminUserResponseDto>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("🔵 [UpdateUser] Starting update for user {UserId}", id);
                _logger.LogInformation("🔵 [UpdateUser] Request received - CustomerId: {CustomerId}, Role: {Role}, EmployeeId: {EmployeeId}", 
                    request.CustomerId?.ToString() ?? "null", request.Role ?? "null", request.EmployeeId?.ToString() ?? "null");

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("🔴 [UpdateUser] User {UserId} not found", id);
                    return NotFound(new { error = "User not found" });
                }

                _logger.LogInformation("🔵 [UpdateUser] User found - Current CustomerId: {CustomerId}, Current Role: {Role}", 
                    user.CustomerId?.ToString() ?? "null", user.PageAccessRole ?? "null");

                // Update basic information
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                {
                    user.FirstName = request.FirstName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.LastName))
                {
                    user.LastName = request.LastName.Trim();
                }

                if (!string.IsNullOrEmpty(request.Username) && request.Username != user.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(request.Username);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        return BadRequest(new { error = "Username already exists" });
                    }
                    user.UserName = request.Username;
                }

                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        return BadRequest(new { error = "Email already exists" });
                    }
                    user.Email = request.Email;
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                }

                if (request.JobTitle != null)
                {
                    user.JobTitle = request.JobTitle;
                }

                if (request.ClearProfilePicture == true)
                {
                    user.ProfilePicture = null;
                }
                else if (request.ProfilePicture != null)
                {
                    user.ProfilePicture = request.ProfilePicture;
                }

                if (request.Signature != null)
                {
                    user.Signature = request.Signature;
                }

                if (request.SignatureCode != null)
                {
                    user.SignatureCode = request.SignatureCode;
                }

                if (request.PageAccessRole != null)
                {
                    user.PageAccessRole = request.PageAccessRole;
                }

                // Update primary site for store users or when explicitly provided
                if (request.PrimarySiteId != null)
                {
                    user.PrimarySiteId = string.IsNullOrWhiteSpace(request.PrimarySiteId)
                        ? null
                        : request.PrimarySiteId;
                }

                if (request.RecordIsDeleted.HasValue)
                {
                    user.RecordIsDeleted = request.RecordIsDeleted.Value;
                    user.RecordIsDeletedYN = request.RecordIsDeleted.Value;
                }

                // Update role first if specified (before updating CustomerId)
                string finalRole;
                if (!string.IsNullOrEmpty(request.Role))
                {
                    var requestedRole = request.Role.Trim();
                    var requestedRoleLower = requestedRole.ToLowerInvariant();

                    var existingRoles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation("🟡 [UpdateUser] Changing role from {OldRole} to {NewRole} for user {UserId}",
                        string.Join(", ", existingRoles), requestedRoleLower, user.Id);
                    await _userManager.RemoveFromRolesAsync(user, existingRoles);

                    // Always keep ApplicationUser.Role in normalized (lowercase) form
                    user.Role = requestedRoleLower;

                    // Only touch Identity roles if the target role actually exists
                    if (await _roleManager.RoleExistsAsync(requestedRoleLower))
                    {
                        await _userManager.AddToRoleAsync(user, requestedRoleLower);
                        finalRole = requestedRoleLower;
                        _logger.LogInformation("✅ [UpdateUser] Role updated to {Role} for user {UserId}",
                            requestedRoleLower, user.Id);
                    }
                    else
                    {
                        finalRole = requestedRoleLower;
                        _logger.LogWarning("🔴 [UpdateUser] Requested role {Role} does not exist in Identity. " +
                                           "Updated ApplicationUser.Role but skipped AddToRoleAsync.",
                            requestedRoleLower);
                    }
                }
                else
                {
                    // Use current role if not being changed
                    var currentUserRoles = await _userManager.GetRolesAsync(user);
                    finalRole = (currentUserRoles.FirstOrDefault() ?? user.Role ?? "user").ToLowerInvariant();
                    _logger.LogInformation("🟡 [UpdateUser] Role not changed, using current role: {Role} for user {UserId}", finalRole, user.Id);
                }

                // Normalize finalRole to lowercase for comparison (backend stores roles in lowercase)
                finalRole = finalRole?.ToLowerInvariant() ?? "user";
                
                // Update CustomerId based on final role (after role update)
                _logger.LogInformation("🟡 [UpdateUser] Processing CustomerId update - Final Role: {Role} (normalized), Request CustomerId: {CustomerId}, Current CustomerId: {CurrentCustomerId}", 
                    finalRole, request.CustomerId?.ToString() ?? "null", user.CustomerId?.ToString() ?? "null");
                
                if (request.CustomerId.HasValue)
                {
                    _logger.LogInformation("🟡 [UpdateUser] Processing CustomerId update for user");
                    // For customer roles, update CustomerId if provided
                    if (request.CustomerId.HasValue)
                    {
                        if (request.CustomerId.Value > 0)
                        {
                            _logger.LogInformation("🟡 [UpdateUser] Validating customer exists - CustomerId: {CustomerId}", request.CustomerId.Value);
                            // Validate that the customer exists
                            var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                            if (customer != null)
                            {
                                var oldCustomerId = user.CustomerId;
                                user.CustomerId = request.CustomerId.Value;
                                _logger.LogInformation("✅ [UpdateUser] CustomerId updated from {OldCustomerId} to {NewCustomerId} for customer user {UserId}", 
                                    oldCustomerId?.ToString() ?? "null", request.CustomerId.Value, user.Id);
                            }
                            else
                            {
                                _logger.LogWarning("🔴 [UpdateUser] Customer with ID {CustomerId} not found. CustomerId not updated for user {UserId}.", 
                                    request.CustomerId.Value, user.Id);
                            }
                        }
                        else
                        {
                            // Set to null if explicitly set to 0
                            var oldCustomerId = user.CustomerId;
                            user.CustomerId = null;
                            _logger.LogInformation("✅ [UpdateUser] CustomerId set to null (was {OldCustomerId}) for customer user {UserId}", 
                                oldCustomerId?.ToString() ?? "null", user.Id);
                    }
                }
                else
                {
                        _logger.LogInformation("🟡 [UpdateUser] CustomerId not provided in request, preserving existing CustomerId: {CustomerId} for user {UserId}", 
                            user.CustomerId?.ToString() ?? "null", user.Id);
                    }
                    // If CustomerId is not provided but user is a customer role, preserve existing CustomerId
                    // (Don't change it if not explicitly provided in the request)
                }
                else
                {
                    _logger.LogInformation("🟡 [UpdateUser] User has AdvantageOne role, clearing CustomerId if present");
                    // For AdvantageOne roles, always set CustomerId to null
                    if (user.CustomerId.HasValue)
                        {
                        var oldCustomerId = user.CustomerId;
                        user.CustomerId = null;
                        _logger.LogInformation("✅ [UpdateUser] CustomerId cleared (was {OldCustomerId}) for AdvantageOne user {UserId}", 
                            oldCustomerId.Value, user.Id);
                    }
                    else
                    {
                        _logger.LogInformation("🟡 [UpdateUser] CustomerId already null for AdvantageOne user {UserId}", user.Id);
                    }
                }
                
                _logger.LogInformation("🟡 [UpdateUser] After CustomerId processing - User.CustomerId: {CustomerId}", user.CustomerId?.ToString() ?? "null");

                // Update password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new { 
                            error = "Failed to update password",
                            details = result.Errors.Select(e => e.Description)
                        });
                    }
                }

                // Handle employee linking/unlinking
                if (request.EmployeeId.HasValue)
                {
                    // User is being linked to an employee
                    var newEmployee = await _context.Employees.FindAsync(request.EmployeeId.Value);
                    if (newEmployee != null)
                    {
                        // Check if employee is already linked to another user
                        if (newEmployee.UserId != null && newEmployee.UserId != user.Id)
                        {
                            return BadRequest(new { error = "Employee is already linked to another user account" });
                        }

                        // If user was previously linked to a different employee, unlink the old one
                        if (user.EmployeeId.HasValue && user.EmployeeId.Value != request.EmployeeId.Value)
                        {
                            var oldEmployee = await _context.Employees.FindAsync(user.EmployeeId.Value);
                            if (oldEmployee != null && oldEmployee.UserId == user.Id)
                            {
                                oldEmployee.UserId = null;
                                oldEmployee.DateModified = DateTime.UtcNow;
                                oldEmployee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            }
                        }

                        // Link both sides of the relationship
                        user.EmployeeId = request.EmployeeId.Value;
                        newEmployee.UserId = user.Id;
                        newEmployee.DateModified = DateTime.UtcNow;
                        newEmployee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Linked User {UserId} to Employee {EmployeeId}", user.Id, request.EmployeeId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Employee {EmployeeId} not found during user update", request.EmployeeId.Value);
                    }
                }
                else if (request.EmployeeId == null && user.EmployeeId.HasValue)
                {
                    // User is being unlinked from employee
                    var employee = await _context.Employees.FindAsync(user.EmployeeId.Value);
                    if (employee != null && employee.UserId == user.Id)
                    {
                        employee.UserId = null;
                        employee.DateModified = DateTime.UtcNow;
                        employee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        await _context.SaveChangesAsync();
                    }
                    
                    user.EmployeeId = null;
                    _logger.LogInformation("Unlinked User {UserId} from Employee", user.Id);
                }

                // Update customer assignments if provided
                if (request.AssignedCustomerIds != null)
                {
                    // Remove existing customer assignments
                    var existingAssignments = await _context.UserCustomerAssignments
                        .Where(uca => uca.UserId == user.Id)
                        .ToListAsync();
                    _context.UserCustomerAssignments.RemoveRange(existingAssignments);

                    // Add new customer assignments
                    foreach (var customerId in request.AssignedCustomerIds)
                    {
                        var customer = await _context.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            var assignment = new UserCustomerAssignment
                            {
                                UserId = user.Id,
                                CustomerId = customerId,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = user.Id // Using the current user as creator
                            };
                            _context.UserCustomerAssignments.Add(assignment);
                        }
                        else
                        {
                            _logger.LogWarning("Customer with ID {CustomerId} not found for user {UserId}", customerId, user.Id);
                        }
                    }
                    user.CustomerIds = request.AssignedCustomerIds;
                    _logger.LogInformation("Updated customer assignments for user {UserId}: {CustomerCount} customers", user.Id, request.AssignedCustomerIds.Count);
                }

                // Update site assignments if provided
                if (request.AssignedSiteIds != null)
                {
                    user.SiteIds = request.AssignedSiteIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _logger.LogInformation("Updated site assignments for user {UserId}: {SiteCount} sites",
                        user.Id, user.SiteIds.Count);
                }

                _logger.LogInformation("🟡 [UpdateUser] Saving changes to database for user {UserId}", user.Id);
                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ [UpdateUser] Changes saved successfully for user {UserId}", user.Id);

                var roles = await _userManager.GetRolesAsync(user);
                var identityRoleForResponse = roles.FirstOrDefault() ?? "User";
                var effectiveRoleForResponse = string.IsNullOrWhiteSpace(user.Role)
                    ? identityRoleForResponse
                    : user.Role;
                _logger.LogInformation("🟡 [UpdateUser] Building response - Final Role: {Role}, Final CustomerId: {CustomerId}", 
                    effectiveRoleForResponse, user.CustomerId?.ToString() ?? "null");

                var userResponse = new AdminUserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = effectiveRoleForResponse,
                    PageAccessRole = user.PageAccessRole ?? string.Empty,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    JobTitle = user.JobTitle,
                    CustomerId = user.CustomerId,
                    RecordIsDeleted = user.RecordIsDeleted,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PrimarySiteId = user.PrimarySiteId,
                    AssignedSiteIds = user.SiteIds.ToList()
                };

                // Note: Employee information retrieval removed (Employee model deleted)
                // EmployeeId is stored directly in ApplicationUser for legacy compatibility
                if (user.EmployeeId.HasValue)
                {
                    userResponse.EmployeeId = user.EmployeeId.Value;
                }

                // Get customer name if CustomerId is set (for customer users)
                if (user.CustomerId.HasValue)
                {
                    _logger.LogInformation("🟡 [UpdateUser] Fetching customer name for CustomerId: {CustomerId}", user.CustomerId.Value);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == user.CustomerId.Value);
                    if (customer != null)
                    {
                        userResponse.CustomerName = customer.CompanyName;
                        _logger.LogInformation("✅ [UpdateUser] Customer name found: {CustomerName} for CustomerId: {CustomerId}", 
                            customer.CompanyName, user.CustomerId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("🔴 [UpdateUser] Customer not found for CustomerId: {CustomerId}", user.CustomerId.Value);
                    }
                }
                else
                {
                    _logger.LogInformation("🟡 [UpdateUser] No CustomerId set, CustomerName will be null");
                }
                
                _logger.LogInformation("🟡 [UpdateUser] Response CustomerId: {CustomerId}, Response CustomerName: {CustomerName}", 
                    userResponse.CustomerId?.ToString() ?? "null", userResponse.CustomerName ?? "null");

                // Get assigned customers
                var customerAssignments = await _context.UserCustomerAssignments
                    .Where(uca => uca.UserId == user.Id)
                    .Include(uca => uca.Customer)
                    .ToListAsync();

                userResponse.AssignedCustomerIds = customerAssignments.Select(uca => uca.CustomerId).ToList();
                userResponse.AssignedCustomerNames = customerAssignments
                    .Where(uca => uca.Customer != null)
                    .Select(uca => uca.Customer!.CompanyName)
                    .ToList();

                _logger.LogInformation("✅ [UpdateUser] User account updated successfully: {UserId} - Final CustomerId: {CustomerId}, Final CustomerName: {CustomerName}", 
                    id, userResponse.CustomerId?.ToString() ?? "null", userResponse.CustomerName ?? "null");
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user account: {UserId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the user account" });
            }
        }

        /// <summary>
        /// Deletes a user account (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            try
            {
                _logger.LogInformation("Soft deleting user account {UserId}", id);

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Check if already deleted
                if (user.RecordIsDeleted || user.RecordIsDeletedYN)
                {
                    return BadRequest(new { error = "User account is already deleted" });
                }

                // Check if user is linked to an employee and unlink first
                var linkedEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == id);
                
                if (linkedEmployee != null)
                {
                    _logger.LogInformation("User {UserId} is linked to Employee {EmployeeId}. Unlinking before deletion.", 
                        id, linkedEmployee.EmployeeId);
                    
                    // Unlink employee by setting UserId to null
                    linkedEmployee.UserId = null;
                    linkedEmployee.DateModified = DateTime.UtcNow;
                    linkedEmployee.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Employee {EmployeeId} unlinked from User {UserId}", 
                        linkedEmployee.EmployeeId, id);
                }

                // Soft delete: Mark as deleted and deactivate
                user.RecordIsDeleted = true;
                user.RecordIsDeletedYN = true;
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { 
                        error = "Failed to delete user account",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.LogInformation("User account soft deleted successfully: {UserId}", id);
                return Ok(new { message = "User account deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user account: {UserId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the user account" });
            }
        }

        /// <summary>
        /// Test endpoint to verify user data serialization
        /// </summary>
        [HttpGet("test-serialization")]
        public async Task<ActionResult> TestSerialization()
        {
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound("No users found");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var userResponse = new AdminUserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    Role = role,
                    PageAccessRole = user.PageAccessRole,
                    Signature = user.Signature,
                    SignatureCode = user.SignatureCode,
                    JobTitle = user.JobTitle,
                    CustomerId = user.CustomerId,
                    RecordIsDeleted = user.RecordIsDeleted,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginAt,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed
                };

                _logger.LogInformation("Test Serialization - User from DB: FirstName='{FirstName}', LastName='{LastName}'", 
                    user.FirstName, user.LastName);
                _logger.LogInformation("Test Serialization - DTO: FirstName='{FirstName}', LastName='{LastName}'", 
                    userResponse.FirstName, userResponse.LastName);

                return Ok(new { 
                    message = "Test serialization",
                    userFromDb = new { user.FirstName, user.LastName },
                    userResponse = userResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test serialization");
                return StatusCode(500, new { error = "Test failed" });
            }
        }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PageAccessRole { get; set; }
        public int? EmployeeId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? Signature { get; set; }
        public string? SignatureCode { get; set; }
        public bool RecordIsDeleted { get; set; } = false;
        public List<int>? AssignedCustomerIds { get; set; }
        public int? CustomerId { get; set; } // For Customer users - direct foreign key to Customer table

        // Store / site assignment
        public string? PrimarySiteId { get; set; }
        public List<string>? AssignedSiteIds { get; set; }
    }

    public class AdminUserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PageAccessRole { get; set; } = string.Empty;
        public string? Signature { get; set; }
        public string? SignatureCode { get; set; }
        public string? JobTitle { get; set; }
        public string? ProfilePicture { get; set; }
        public int? CustomerId { get; set; } // For Customer users - direct foreign key to Customer table
        public string? CustomerName { get; set; } // Company name for Customer users
        public bool RecordIsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        
        // Employee linking fields
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        
        // Customer assignment fields
        public List<int> AssignedCustomerIds { get; set; } = new();
        public List<string> AssignedCustomerNames { get; set; } = new();

        // Store / site assignment fields
        public string? PrimarySiteId { get; set; }
        public List<string> AssignedSiteIds { get; set; } = new();
    }

    public class UserListResponseDto
    {
        public List<AdminUserResponseDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class EmployeeResponseDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployeeStatus { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    public class ValidationResponseDto
    {
        public bool Available { get; set; }
        public string? Message { get; set; }
    }

    public class LinkEmployeeRequest
    {
        public int EmployeeId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public int? EmployeeId { get; set; }
        public string? JobTitle { get; set; }
        public string? Signature { get; set; }
        public string? SignatureCode { get; set; }
            public string? ProfilePicture { get; set; }
        public bool? ClearProfilePicture { get; set; }
        public string? PageAccessRole { get; set; }
        public bool? RecordIsDeleted { get; set; }
        public List<int>? AssignedCustomerIds { get; set; }
        public int? CustomerId { get; set; } // For Customer users - direct foreign key to Customer table

        // Store / site assignment
        public string? PrimarySiteId { get; set; }
        public List<string>? AssignedSiteIds { get; set; }
    }
}
