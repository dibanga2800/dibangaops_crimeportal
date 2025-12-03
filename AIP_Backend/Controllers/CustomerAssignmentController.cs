using AIPBackend.Models;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerAssignmentController : ControllerBase
    {
        private readonly ICustomerAssignmentService _customerAssignmentService;

        public CustomerAssignmentController(ICustomerAssignmentService customerAssignmentService)
        {
            _customerAssignmentService = customerAssignmentService;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<int>>> GetUserCustomerIds(string userId)
        {
            try
            {
                var customerIds = await _customerAssignmentService.GetAssignedCustomerIdsAsync(userId);
                return Ok(customerIds);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("user/{userId}/assign")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult> AssignCustomersToUser(string userId, [FromBody] List<int> customerIds)
        {
            try
            {
                await _customerAssignmentService.AssignCustomersToUserAsync(userId, customerIds);
                return Ok(new { message = "Customers assigned successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("user/{userId}/add/{customerId}")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult> AddCustomerToUser(string userId, int customerId)
        {
            try
            {
                await _customerAssignmentService.AddCustomerToUserAsync(userId, customerId);
                return Ok(new { message = "Customer added successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("user/{userId}/remove/{customerId}")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult> RemoveCustomerFromUser(string userId, int customerId)
        {
            try
            {
                await _customerAssignmentService.RemoveCustomerFromUserAsync(userId, customerId);
                return Ok(new { message = "Customer removed successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("user/{userId}/clear")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult> ClearUserCustomerAssignments(string userId)
        {
            try
            {
                await _customerAssignmentService.ClearCustomerAssignmentsAsync(userId);
                return Ok(new { message = "Customer assignments cleared successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}/has/{customerId}")]
        public async Task<ActionResult<bool>> UserHasCustomer(string userId, int customerId)
        {
            try
            {
                var hasCustomer = await _customerAssignmentService.UserHasCustomerAsync(userId, customerId);
                return Ok(hasCustomer);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("customer/{customerId}/users")]
        [Authorize(Policy = "AdvantageOneOnly")]
        public async Task<ActionResult<List<ApplicationUser>>> GetUsersByCustomer(int customerId)
        {
            try
            {
                var users = await _customerAssignmentService.GetUsersByCustomerIdAsync(customerId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("migrate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> MigrateFromUserCustomerAssignmentsTable()
        {
            try
            {
                await _customerAssignmentService.MigrateFromUserCustomerAssignmentsTableAsync();
                return Ok(new { message = "Migration completed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
