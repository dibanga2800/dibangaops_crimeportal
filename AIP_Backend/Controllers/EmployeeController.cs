using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "administrator,manager")]
    public class EmployeeController : ControllerBase
    {
        private static readonly Regex GeneratedEmployeeNumberPattern = new(@"^EMP(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ApplicationDbContext context,
            ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all employees with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<PaginatedEmployeeResponse>>> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? position = null,
            [FromQuery] string? region = null)
        {
            try
            {
                var query = _context.Employees
                    .Where(e => !e.RecordIsDeletedYN)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(e =>
                        e.EmployeeNumber.Contains(search) ||
                        e.FirstName.Contains(search) ||
                        e.Surname.Contains(search) ||
                        (e.Email != null && e.Email.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(e => e.EmployeeStatus == status);
                }

                if (!string.IsNullOrWhiteSpace(position))
                {
                    query = query.Where(e => e.Position == position);
                }

                if (!string.IsNullOrWhiteSpace(region))
                {
                    query = query.Where(e => e.Region == region);
                }

                var totalCount = await query.CountAsync();

                var employeesList = await query
                    .OrderBy(e => e.Surname)
                    .ThenBy(e => e.FirstName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var employees = employeesList.Select(e => new EmployeeListResponse
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeNumber = e.EmployeeNumber,
                    FullName = e.FullName,
                    Position = e.Position,
                    EmployeeStatus = e.EmployeeStatus,
                    EmploymentType = e.EmploymentType,
                    Email = e.Email,
                    StartDate = e.StartDate,
                    SiaLicenceExpiry = e.SiaLicenceExpiry,
                    IsSiaLicenceExpired = e.IsSiaLicenceExpired,
                    IsSiaLicenceExpiringSoon = e.IsSiaLicenceExpiringSoon,
                    UserId = e.UserId,
                    CreatedAt = e.DateCreated
                }).ToList();

                var response = new PaginatedEmployeeResponse
                {
                    Items = employees,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return Ok(new ApiResponseDto<PaginatedEmployeeResponse>
                {
                    Success = true,
                    Message = "Employees retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new ApiResponseDto<PaginatedEmployeeResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employees",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get active employees for assignment/selection
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponseDto<List<EmployeeListResponse>>>> GetActiveEmployees()
        {
            try
            {
                var employeesList = await _context.Employees
                    .Include(e => e.User)  // Include user to check if active
                    .Where(e => !e.RecordIsDeletedYN && e.EmployeeStatus == "Active")
                    .OrderBy(e => e.Surname)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                var employees = employeesList.Select(e => new EmployeeListResponse
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeNumber = e.EmployeeNumber,
                    FullName = e.FullName,
                    Position = e.Position,
                    EmployeeStatus = e.EmployeeStatus,
                    EmploymentType = e.EmploymentType,
                    Email = e.Email,
                    StartDate = e.StartDate,
                    SiaLicenceExpiry = e.SiaLicenceExpiry,
                    IsSiaLicenceExpired = e.IsSiaLicenceExpired,
                    IsSiaLicenceExpiringSoon = e.IsSiaLicenceExpiringSoon,
                    // Only include UserId if the user is active (not soft-deleted)
                    UserId = e.User != null && !e.User.RecordIsDeleted && !e.User.RecordIsDeletedYN && e.User.IsActive ? e.UserId : null,
                    CreatedAt = e.DateCreated
                }).ToList();

                return Ok(new ApiResponseDto<List<EmployeeListResponse>>
                {
                    Success = true,
                    Message = "Active employees retrieved successfully",
                    Data = employees
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active employees");
                return StatusCode(500, new ApiResponseDto<List<EmployeeListResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving active employees",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get employee by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDto<EmployeeDetailResponse>>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.EmployeeId == id && !e.RecordIsDeletedYN)
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new ApiResponseDto<EmployeeDetailResponse>
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                var response = new EmployeeDetailResponse
                {
                    Id = employee.EmployeeId,
                    EmployeeNumber = employee.EmployeeNumber,
                    Title = employee.Title,
                    FirstName = employee.FirstName,
                    Surname = employee.Surname,
                    FullName = employee.FullName,
                    StartDate = employee.StartDate,
                    Position = employee.Position,
                    EmployeeStatus = employee.EmployeeStatus,
                    EmploymentType = employee.EmploymentType,
                    AipAccessLevel = employee.AipAccessLevel,
                    Region = employee.Region,
                    Email = employee.Email,
                    ContactNumber = employee.ContactNumber,
                    HouseName = employee.HouseName,
                    NumberAndStreet = employee.NumberAndStreet,
                    Town = employee.Town,
                    County = employee.County,
                    PostCode = employee.PostCode,
                    SiaLicenceExpiry = employee.SiaLicenceExpiry,
                    IsSiaLicenceExpired = employee.IsSiaLicenceExpired,
                    IsSiaLicenceExpiringSoon = employee.IsSiaLicenceExpiringSoon,
                    Nationality = employee.Nationality,
                    RightToWorkCondition = employee.RightToWorkCondition,
                    UserId = employee.UserId,
                    Username = employee.User?.UserName,
                    CreatedAt = employee.DateCreated,
                    CreatedBy = employee.CreatedBy,
                    UpdatedAt = employee.DateModified,
                    UpdatedBy = employee.ModifiedBy
                };

                return Ok(new ApiResponseDto<EmployeeDetailResponse>
                {
                    Success = true,
                    Message = "Employee retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponseDto<EmployeeDetailResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the employee",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get employee statistics for dashboard cards
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponseDto<EmployeeStatisticsResponse>>> GetEmployeeStatistics()
        {
            try
            {
                var employees = await _context.Employees
                    .AsNoTracking()
                    .Where(e => !e.RecordIsDeletedYN)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var response = new EmployeeStatisticsResponse
                {
                    TotalEmployees = employees.Count,
                    ActiveEmployees = employees.Count(e => string.Equals(e.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase)),
                    InactiveEmployees = employees.Count(e => !string.Equals(e.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase)),
                    NewEmployeesThisMonth = employees.Count(e => e.DateCreated >= monthStart),
                    EmployeesByPosition = employees
                        .Where(e => !string.IsNullOrWhiteSpace(e.Position))
                        .GroupBy(e => e.Position)
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    EmployeesByRegion = employees
                        .Where(e => !string.IsNullOrWhiteSpace(e.Region))
                        .GroupBy(e => e.Region!)
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(new ApiResponseDto<EmployeeStatisticsResponse>
                {
                    Success = true,
                    Message = "Employee statistics retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee statistics");
                return StatusCode(500, new ApiResponseDto<EmployeeStatisticsResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving employee statistics",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Register a new employee
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponseDto<EmployeeRegistrationResponse>>> RegisterEmployee(
            [FromBody] EmployeeRegistrationRequest request)
        {
            try
            {
                var employeeNumber = string.IsNullOrWhiteSpace(request.EmployeeNumber)
                    ? await GenerateNextEmployeeNumberAsync()
                    : request.EmployeeNumber.Trim();

                _logger.LogInformation("Registering new employee: {EmployeeNumber}", employeeNumber);

                // Validate unique employee number
                var existingEmployee = await _context.Employees
                    .Where(e => e.EmployeeNumber == employeeNumber && !e.RecordIsDeletedYN)
                    .FirstOrDefaultAsync();

                if (existingEmployee != null)
                {
                    return BadRequest(new ApiResponseDto<EmployeeRegistrationResponse>
                    {
                        Success = false,
                        Message = "Employee number already exists",
                        Errors = new List<string> { $"Employee number {employeeNumber} is already in use" }
                    });
                }

                // Validate unique email if provided
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var existingEmail = await _context.Employees
                        .Where(e => e.Email == request.Email && !e.RecordIsDeletedYN)
                        .FirstOrDefaultAsync();

                    if (existingEmail != null)
                    {
                        return BadRequest(new ApiResponseDto<EmployeeRegistrationResponse>
                        {
                            Success = false,
                            Message = "Email already exists",
                            Errors = new List<string> { $"Email {request.Email} is already in use" }
                        });
                    }
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var employee = new Employee
                {
                    EmployeeNumber = employeeNumber,
                    Title = request.Title,
                    FirstName = request.FirstName,
                    Surname = request.Surname,
                    StartDate = request.StartDate,
                    Position = request.Position,
                    EmployeeStatus = request.EmployeeStatus,
                    EmploymentType = request.EmploymentType,
                    AipAccessLevel = request.AipAccessLevel,
                    Region = request.Region,
                    Email = request.Email,
                    ContactNumber = request.ContactNumber,
                    HouseName = request.HouseName,
                    NumberAndStreet = request.NumberAndStreet,
                    Town = request.Town,
                    County = request.County,
                    PostCode = request.PostCode,
                    SiaLicenceExpiry = request.SiaLicenceExpiry,
                    Nationality = request.Nationality,
                    RightToWorkCondition = request.RightToWorkCondition,
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = userId ?? "system",
                    RecordIsDeletedYN = false
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee registered successfully: {EmployeeId}", employee.EmployeeId);

                var response = new EmployeeRegistrationResponse
                {
                    Id = employee.EmployeeId,
                    EmployeeNumber = employee.EmployeeNumber,
                    FirstName = employee.FirstName,
                    Surname = employee.Surname,
                    Email = employee.Email,
                    Position = employee.Position,
                    Status = employee.EmployeeStatus,
                    CreatedAt = employee.DateCreated
                };

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, new ApiResponseDto<EmployeeRegistrationResponse>
                {
                    Success = true,
                    Message = "Employee registered successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering employee");
                return StatusCode(500, new ApiResponseDto<EmployeeRegistrationResponse>
                {
                    Success = false,
                    Message = "An error occurred while registering the employee",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update an employee
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponseDto<EmployeeDetailResponse>>> UpdateEmployee(
            int id,
            [FromBody] EmployeeUpdateRequest request)
        {
            try
            {
                var employee = await _context.Employees
                    .Where(e => e.EmployeeId == id && !e.RecordIsDeletedYN)
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new ApiResponseDto<EmployeeDetailResponse>
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                // Validate unique employee number if changed
                if (!string.IsNullOrWhiteSpace(request.EmployeeNumber) && request.EmployeeNumber != employee.EmployeeNumber)
                {
                    var existingEmployee = await _context.Employees
                        .Where(e => e.EmployeeNumber == request.EmployeeNumber && e.EmployeeId != id && !e.RecordIsDeletedYN)
                        .FirstOrDefaultAsync();

                    if (existingEmployee != null)
                    {
                        return BadRequest(new ApiResponseDto<EmployeeDetailResponse>
                        {
                            Success = false,
                            Message = "Employee number already exists"
                        });
                    }
                }

                // Validate unique email if changed
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != employee.Email)
                {
                    var existingEmail = await _context.Employees
                        .Where(e => e.Email == request.Email && e.EmployeeId != id && !e.RecordIsDeletedYN)
                        .FirstOrDefaultAsync();

                    if (existingEmail != null)
                    {
                        return BadRequest(new ApiResponseDto<EmployeeDetailResponse>
                        {
                            Success = false,
                            Message = "Email already exists"
                        });
                    }
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(request.EmployeeNumber)) employee.EmployeeNumber = request.EmployeeNumber;
                if (!string.IsNullOrWhiteSpace(request.Title)) employee.Title = request.Title;
                if (!string.IsNullOrWhiteSpace(request.FirstName)) employee.FirstName = request.FirstName;
                if (!string.IsNullOrWhiteSpace(request.Surname)) employee.Surname = request.Surname;
                if (request.StartDate.HasValue) employee.StartDate = request.StartDate.Value;
                if (!string.IsNullOrWhiteSpace(request.Position)) employee.Position = request.Position;
                if (!string.IsNullOrWhiteSpace(request.EmployeeStatus)) employee.EmployeeStatus = request.EmployeeStatus;
                if (!string.IsNullOrWhiteSpace(request.EmploymentType)) employee.EmploymentType = request.EmploymentType;
                if (request.AipAccessLevel != null) employee.AipAccessLevel = request.AipAccessLevel;
                if (request.Region != null) employee.Region = request.Region;
                if (request.Email != null) employee.Email = request.Email;
                if (request.ContactNumber != null) employee.ContactNumber = request.ContactNumber;
                if (request.HouseName != null) employee.HouseName = request.HouseName;
                if (request.NumberAndStreet != null) employee.NumberAndStreet = request.NumberAndStreet;
                if (request.Town != null) employee.Town = request.Town;
                if (request.County != null) employee.County = request.County;
                if (request.PostCode != null) employee.PostCode = request.PostCode;
                if (request.SiaLicenceExpiry.HasValue) employee.SiaLicenceExpiry = request.SiaLicenceExpiry;
                if (request.Nationality != null) employee.Nationality = request.Nationality;
                if (request.RightToWorkCondition != null) employee.RightToWorkCondition = request.RightToWorkCondition;
                employee.DateModified = DateTime.UtcNow;
                employee.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee updated successfully: {EmployeeId}", id);

                // Return updated employee details
                return await GetEmployee(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponseDto<EmployeeDetailResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating the employee",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete an employee (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Where(e => e.EmployeeId == id && !e.RecordIsDeletedYN)
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Employee not found"
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Soft delete
                employee.RecordIsDeletedYN = true;
                employee.DateModified = DateTime.UtcNow;
                employee.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee deleted successfully: {EmployeeId}", id);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Employee deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the employee",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private async Task<string> GenerateNextEmployeeNumberAsync()
        {
            var employeeNumbers = await _context.Employees
                .AsNoTracking()
                .Where(e => !e.RecordIsDeletedYN && e.EmployeeNumber.StartsWith("EMP"))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            var nextNumber = employeeNumbers
                .Select(value => GeneratedEmployeeNumberPattern.Match(value))
                .Where(match => match.Success && int.TryParse(match.Groups[1].Value, out _))
                .Select(match => int.Parse(match.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"EMP{nextNumber:D6}";
        }
    }
}
