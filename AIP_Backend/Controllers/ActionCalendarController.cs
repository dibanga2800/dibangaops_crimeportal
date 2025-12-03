using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActionCalendarController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActionCalendarController> _logger;
        private readonly IActionCalendarEmailService _actionCalendarEmailService;

        public ActionCalendarController(
            ApplicationDbContext context, 
            ILogger<ActionCalendarController> logger,
            IActionCalendarEmailService actionCalendarEmailService)
        {
            _context = context;
            _logger = logger;
            _actionCalendarEmailService = actionCalendarEmailService;
        }

        // GET: api/ActionCalendar
        [HttpGet]
        public async Task<ActionResult<ActionCalendarsResponseDto>> GetActionCalendars(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? assignee = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.ActionCalendars
                    .Include(a => a.AssignedUser)
                    .Include(a => a.CreatedByUser)
                    .Include(a => a.ModifiedByUser)
                    .Where(a => !a.RecordIsDeletedYN);

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(a => a.TaskStatus == status);

                if (!string.IsNullOrEmpty(priority))
                    query = query.Where(a => a.PriorityLevel == priority);

                if (!string.IsNullOrEmpty(assignee))
                    query = query.Where(a => a.AssignTo == assignee);

                if (fromDate.HasValue)
                    query = query.Where(a => a.DueDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(a => a.DueDate <= toDate.Value);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var items = await query
                    .OrderByDescending(a => a.DateCreated)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var dtos = items.Select(MapToDto).ToList();

                var response = new ActionCalendarsResponseDto
                {
                    Success = true,
                    Message = "Action calendars retrieved successfully",
                    Data = dtos,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving action calendars");
                return StatusCode(500, new ActionCalendarsResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving action calendars",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/ActionCalendar/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ActionCalendarResponseDto>> GetActionCalendar(int id)
        {
            try
            {
                var item = await _context.ActionCalendars
                    .Include(a => a.AssignedUser)
                    .Include(a => a.CreatedByUser)
                    .Include(a => a.ModifiedByUser)
                    .FirstOrDefaultAsync(a => a.ActionCalendarId == id && !a.RecordIsDeletedYN);

                if (item == null)
                {
                    return NotFound(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "Action calendar not found"
                    });
                }

                var response = new ActionCalendarResponseDto
                {
                    Success = true,
                    Message = "Action calendar retrieved successfully",
                    Data = MapToDto(item)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving action calendar with ID {Id}", id);
                return StatusCode(500, new ActionCalendarResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving the action calendar",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/ActionCalendar
        [HttpPost]
        public async Task<ActionResult<ActionCalendarResponseDto>> CreateActionCalendar(CreateActionCalendarDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var actionCalendar = new ActionCalendar
                {
                    TaskTitle = createDto.TaskTitle,
                    TaskDescription = createDto.TaskDescription,
                    TaskStatus = createDto.TaskStatus,
                    PriorityLevel = createDto.PriorityLevel,
                    AssignTo = createDto.AssignTo,
                    DueDate = createDto.DueDate,
                    Email = createDto.Email,
                    IsRecurring = createDto.IsRecurring,
                    ReminderDate = createDto.ReminderDate,
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = userId,
                    RecordIsDeletedYN = false
                };

                _context.ActionCalendars.Add(actionCalendar);
                await _context.SaveChangesAsync();

                // Reload with related data
                await _context.Entry(actionCalendar)
                    .Reference(a => a.AssignedUser)
                    .LoadAsync();
                await _context.Entry(actionCalendar)
                    .Reference(a => a.CreatedByUser)
                    .LoadAsync();

                var response = new ActionCalendarResponseDto
                {
                    Success = true,
                    Message = "Action calendar created successfully",
                    Data = MapToDto(actionCalendar)
                };

                // Fire-and-forget email notification, but await to capture errors
                await _actionCalendarEmailService.SendTaskAssignmentNotificationAsync(actionCalendar);

                return CreatedAtAction(nameof(GetActionCalendar), new { id = actionCalendar.ActionCalendarId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action calendar");
                return StatusCode(500, new ActionCalendarResponseDto
                {
                    Success = false,
                    Message = "An error occurred while creating the action calendar",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/ActionCalendar/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ActionCalendarResponseDto>> UpdateActionCalendar(int id, UpdateActionCalendarDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                var existing = await _context.ActionCalendars
                    .Include(a => a.AssignedUser)
                    .Include(a => a.CreatedByUser)
                    .Include(a => a.ModifiedByUser)
                    .FirstOrDefaultAsync(a => a.ActionCalendarId == id && !a.RecordIsDeletedYN);

                if (existing == null)
                {
                    return NotFound(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "Action calendar not found"
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var previousStatus = existing.TaskStatus;

                // Update properties
                existing.TaskTitle = updateDto.TaskTitle;
                existing.TaskDescription = updateDto.TaskDescription;
                existing.TaskStatus = updateDto.TaskStatus;
                existing.PriorityLevel = updateDto.PriorityLevel;
                existing.AssignTo = updateDto.AssignTo;
                existing.DueDate = updateDto.DueDate;
                existing.CompletedDate = updateDto.CompletedDate;
                existing.Email = updateDto.Email;
                existing.IsRecurring = updateDto.IsRecurring;
                existing.ReminderDate = updateDto.ReminderDate;
                existing.DateModified = DateTime.UtcNow;
                existing.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                // Reload with related data
                await _context.Entry(existing)
                    .Reference(a => a.AssignedUser)
                    .LoadAsync();
                await _context.Entry(existing)
                    .Reference(a => a.ModifiedByUser)
                    .LoadAsync();

                var response = new ActionCalendarResponseDto
                {
                    Success = true,
                    Message = "Action calendar updated successfully",
                    Data = MapToDto(existing)
                };

                if (!string.Equals(previousStatus, existing.TaskStatus, StringComparison.OrdinalIgnoreCase))
                {
                    var syntheticStatusUpdate = new ActionCalendarStatusUpdate
                    {
                        ActionCalendarId = existing.ActionCalendarId,
                        Status = existing.TaskStatus,
                        Comment = "Task status updated by administrator",
                        UpdateDate = existing.DateModified ?? DateTime.UtcNow,
                        UpdatedBy = userId,
                        UpdatedByUser = existing.ModifiedByUser
                    };

                    await _actionCalendarEmailService.SendTaskStatusUpdatedNotificationAsync(existing, syntheticStatusUpdate);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action calendar with ID {Id}", id);
                return StatusCode(500, new ActionCalendarResponseDto
                {
                    Success = false,
                    Message = "An error occurred while updating the action calendar",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/ActionCalendar/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ActionCalendarResponseDto>> DeleteActionCalendar(int id)
        {
            try
            {
                var item = await _context.ActionCalendars.FindAsync(id);
                if (item == null || item.RecordIsDeletedYN)
                {
                    return NotFound(new ActionCalendarResponseDto
                    {
                        Success = false,
                        Message = "Action calendar not found"
                    });
                }

                item.RecordIsDeletedYN = true;
                await _context.SaveChangesAsync();

                var response = new ActionCalendarResponseDto
                {
                    Success = true,
                    Message = "Action calendar deleted successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting action calendar with ID {Id}", id);
                return StatusCode(500, new ActionCalendarResponseDto
                {
                    Success = false,
                    Message = "An error occurred while deleting the action calendar",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/ActionCalendar/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<ActionCalendarStatisticsDto>> GetStatistics()
        {
            try
            {
                var today = DateTime.Today;
                var overdue = DateTime.Today.AddDays(-1);

                var statistics = new ActionCalendarStatisticsDto
                {
                    Total = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN),
                    Completed = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.TaskStatus == "completed"),
                    InProgress = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.TaskStatus == "in-progress"),
                    Pending = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.TaskStatus == "pending"),
                    Blocked = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.TaskStatus == "blocked"),
                    HighPriority = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.PriorityLevel == "high"),
                    DueToday = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.DueDate.Date == today),
                    Overdue = await _context.ActionCalendars.CountAsync(a => !a.RecordIsDeletedYN && a.DueDate.Date < today && a.TaskStatus != "completed")
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving action calendar statistics");
                return StatusCode(500, "An error occurred while retrieving statistics");
            }
        }

        private static ActionCalendarDto MapToDto(ActionCalendar actionCalendar)
        {
            return new ActionCalendarDto
            {
                ActionCalendarId = actionCalendar.ActionCalendarId,
                TaskTitle = actionCalendar.TaskTitle,
                TaskDescription = actionCalendar.TaskDescription ?? "",
                TaskStatus = actionCalendar.TaskStatus,
                PriorityLevel = actionCalendar.PriorityLevel,
                AssignTo = actionCalendar.AssignTo,
                AssignedUserName = actionCalendar.AssignedUser?.UserName ?? "Unknown",
                DueDate = actionCalendar.DueDate,
                CompletedDate = actionCalendar.CompletedDate,
                Email = actionCalendar.Email ?? "",
                IsRecurring = actionCalendar.IsRecurring,
                ReminderDate = actionCalendar.ReminderDate,
                DateCreated = actionCalendar.DateCreated,
                CreatedBy = actionCalendar.CreatedBy,
                CreatedByUserName = actionCalendar.CreatedByUser?.UserName ?? "Unknown",
                DateModified = actionCalendar.DateModified,
                ModifiedBy = actionCalendar.ModifiedBy ?? "",
                ModifiedByUserName = actionCalendar.ModifiedByUser?.UserName ?? "Unknown"
            };
        }
    }
} 