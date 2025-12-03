using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ActionCalendarStatusUpdateController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending",
            "in-progress",
            "completed",
            "blocked"
        };

        private readonly ApplicationDbContext _context;
        private readonly IActionCalendarEmailService _actionCalendarEmailService;
        private readonly ILogger<ActionCalendarStatusUpdateController> _logger;

        public ActionCalendarStatusUpdateController(
            ApplicationDbContext context,
            IActionCalendarEmailService actionCalendarEmailService,
            ILogger<ActionCalendarStatusUpdateController> logger)
        {
            _context = context;
            _actionCalendarEmailService = actionCalendarEmailService;
            _logger = logger;
        }

        [HttpGet("/api/ActionCalendar/{actionCalendarId}/status-updates")]
        public async Task<ActionResult<ActionCalendarStatusUpdatesResponseDto>> GetStatusUpdatesForActionCalendar(int actionCalendarId)
        {
            try
            {
                var updates = await _context.ActionCalendarStatusUpdates
                    .Where(su => su.ActionCalendarId == actionCalendarId)
                    .Include(su => su.UpdatedByUser)
                    .OrderByDescending(su => su.UpdateDate)
                    .ToListAsync();

                var response = new ActionCalendarStatusUpdatesResponseDto
                {
                    Success = true,
                    Message = "Status updates retrieved successfully",
                    Data = updates.Select(MapToDto).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status updates for ActionCalendarId {ActionCalendarId}", actionCalendarId);
                return StatusCode(500, new ActionCalendarStatusUpdatesResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving the status updates",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("/api/ActionCalendar/{actionCalendarId}/status-updates")]
        public async Task<ActionResult<ActionCalendarStatusUpdateResponseDto>> CreateStatusUpdate(int actionCalendarId, CreateActionCalendarStatusUpdateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ActionCalendarStatusUpdateResponseDto
                    {
                        Success = false,
                        Message = "Invalid status update payload",
                        Errors = errors
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ActionCalendarStatusUpdateResponseDto
                    {
                        Success = false,
                        Message = "User context is missing"
                    });
                }

                var actionCalendar = await _context.ActionCalendars
                    .Include(a => a.AssignedUser)
                    .Include(a => a.CreatedByUser)
                    .FirstOrDefaultAsync(a => a.ActionCalendarId == actionCalendarId && !a.RecordIsDeletedYN);

                if (actionCalendar == null)
                {
                    return NotFound(new ActionCalendarStatusUpdateResponseDto
                    {
                        Success = false,
                        Message = "Action calendar not found"
                    });
                }

                var normalizedStatus = request.Status.Trim().ToLowerInvariant();
                if (!AllowedStatuses.Contains(normalizedStatus))
                {
                    return BadRequest(new ActionCalendarStatusUpdateResponseDto
                    {
                        Success = false,
                        Message = $"Unsupported status '{request.Status}'. Allowed statuses: {string.Join(", ", AllowedStatuses)}"
                    });
                }

                var isAdministrator = User.IsInRole("administrator");
                var isAssignee = string.Equals(actionCalendar.AssignTo, userId, StringComparison.OrdinalIgnoreCase);
                var isCreator = string.Equals(actionCalendar.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);

                if (!isAdministrator && !isAssignee && !isCreator)
                {
                    return Forbid();
                }

                var statusUpdate = new ActionCalendarStatusUpdate
                {
                    ActionCalendarId = actionCalendarId,
                    Status = normalizedStatus,
                    Comment = request.Comment,
                    UpdateDate = DateTime.UtcNow,
                    UpdatedBy = userId
                };

                _context.ActionCalendarStatusUpdates.Add(statusUpdate);

                actionCalendar.TaskStatus = normalizedStatus;
                actionCalendar.CompletedDate = normalizedStatus == "completed" ? DateTime.UtcNow : null;
                actionCalendar.DateModified = DateTime.UtcNow;
                actionCalendar.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                await _context.Entry(statusUpdate)
                    .Reference(su => su.UpdatedByUser)
                    .LoadAsync();

                var dto = MapToDto(statusUpdate);

                await _actionCalendarEmailService.SendTaskStatusUpdatedNotificationAsync(actionCalendar, statusUpdate);

                return Ok(new ActionCalendarStatusUpdateResponseDto
                {
                    Success = true,
                    Message = "Status update recorded successfully",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating status update for ActionCalendarId {ActionCalendarId}", actionCalendarId);
                return StatusCode(500, new ActionCalendarStatusUpdateResponseDto
                {
                    Success = false,
                    Message = "An error occurred while recording the status update",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private static ActionCalendarStatusUpdateDto MapToDto(ActionCalendarStatusUpdate statusUpdate)
        {
            return new ActionCalendarStatusUpdateDto
            {
                ActionCalendarStatusUpdateId = statusUpdate.ActionCalendarStatusUpdateId,
                ActionCalendarId = statusUpdate.ActionCalendarId,
                Status = statusUpdate.Status,
                Comment = statusUpdate.Comment,
                UpdateDate = statusUpdate.UpdateDate,
                UpdatedBy = statusUpdate.UpdatedBy,
                UpdatedByUserName = statusUpdate.UpdatedByUser?.FullName ?? "Unknown"
            };
        }
    }
}