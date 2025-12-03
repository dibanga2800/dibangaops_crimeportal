using AIPBackend.Models;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserSoftDeleteController : ControllerBase
    {
        private readonly IUserSoftDeleteService _softDeleteService;

        public UserSoftDeleteController(IUserSoftDeleteService softDeleteService)
        {
            _softDeleteService = softDeleteService;
        }

        [HttpPost("soft-delete/{userId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> SoftDeleteUser(string userId)
        {
            try
            {
                var result = await _softDeleteService.SoftDeleteUserAsync(userId);
                if (result)
                {
                    return Ok(new { message = "User soft deleted successfully" });
                }
                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("restore/{userId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> RestoreUser(string userId)
        {
            try
            {
                var result = await _softDeleteService.RestoreUserAsync(userId);
                if (result)
                {
                    return Ok(new { message = "User restored successfully" });
                }
                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("hard-delete/{userId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> HardDeleteUser(string userId)
        {
            try
            {
                var result = await _softDeleteService.HardDeleteUserAsync(userId);
                if (result)
                {
                    return Ok(new { message = "User permanently deleted" });
                }
                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("active")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult<List<ApplicationUser>>> GetActiveUsers()
        {
            try
            {
                var users = await _softDeleteService.GetActiveUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("deleted")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<ApplicationUser>>> GetDeletedUsers()
        {
            try
            {
                var users = await _softDeleteService.GetDeletedUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("is-deleted/{userId}")]
        public async Task<ActionResult<bool>> IsUserDeleted(string userId)
        {
            try
            {
                var isDeleted = await _softDeleteService.IsUserDeletedAsync(userId);
                return Ok(isDeleted);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
