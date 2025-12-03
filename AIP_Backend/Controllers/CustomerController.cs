#nullable enable

using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customers with optional filtering and pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<CustomerListResponseDto>>> GetCustomers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? region = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get customers request by user {CurrentUserId}", currentUserId);

                var result = await _customerService.GetCustomersAsync(page, pageSize, search, status, region);

                return Ok(new ApiResponseDto<CustomerListResponseDto>
                {
                    Success = true,
                    Message = "Customers retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return StatusCode(500, new ApiResponseDto<CustomerListResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers. Please try again."
                });
            }
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<CustomerDetailResponseDto>>> GetCustomer(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get customer request for ID {CustomerId} by user {CurrentUserId}", id, currentUserId);

                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return NotFound(new ApiResponseDto<CustomerDetailResponseDto>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                return Ok(new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = true,
                    Message = "Customer retrieved successfully",
                    Data = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                return StatusCode(500, new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the customer. Please try again."
                });
            }
        }

        /// <summary>
        /// Create new customer
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<CustomerDetailResponseDto>>> CreateCustomer([FromBody] CustomerCreateRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Create customer request by user {CurrentUserId}", currentUserId);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<CustomerDetailResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                request.CreatedBy = currentUserId;
                var result = await _customerService.CreateCustomerAsync(request);

                return CreatedAtAction(nameof(GetCustomer), new { id = result.CustomerId }, new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = true,
                    Message = "Customer created successfully",
                    Data = result
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Customer creation validation failed: {Message}", ex.Message);
                return BadRequest(new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the customer. Please try again."
                });
            }
        }

        /// <summary>
        /// Update customer
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<CustomerDetailResponseDto>>> UpdateCustomer(int id, [FromBody] CustomerUpdateRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update customer request for ID {CustomerId} by user {CurrentUserId}", id, currentUserId);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponseDto<CustomerDetailResponseDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = errors
                    });
                }

                request.UpdatedBy = currentUserId;
                var result = await _customerService.UpdateCustomerAsync(id, request);

                return Ok(new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = true,
                    Message = "Customer updated successfully",
                    Data = result
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Customer update validation failed: {Message}", ex.Message);
                return BadRequest(new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                return StatusCode(500, new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the customer. Please try again."
                });
            }
        }

        /// <summary>
        /// Delete customer
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<object>>> DeleteCustomer(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Delete customer request for ID {CustomerId} by user {CurrentUserId}", id, currentUserId);

                await _customerService.DeleteCustomerAsync(id);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Customer deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the customer. Please try again."
                });
            }
        }

        /// <summary>
        /// Get customer statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponseDto<CustomerStatisticsDto>>> GetCustomerStatistics()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get customer statistics request by user {CurrentUserId}", currentUserId);

                var statistics = await _customerService.GetCustomerStatisticsAsync();

                return Ok(new ApiResponseDto<CustomerStatisticsDto>
                {
                    Success = true,
                    Message = "Customer statistics retrieved successfully",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer statistics");
                return StatusCode(500, new ApiResponseDto<CustomerStatisticsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer statistics. Please try again."
                });
            }
        }

        /// <summary>
        /// Update customer page assignments
        /// </summary>
        [HttpPut("{id}/page-assignments")]
        [Authorize(Roles = "administrator")]
        public async Task<ActionResult<ApiResponseDto<CustomerDetailResponseDto>>> UpdatePageAssignments(int id, [FromBody] CustomerPageAssignmentsDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Update page assignments request for customer {CustomerId} by user {CurrentUserId}", id, currentUserId);

                request.UpdatedBy = currentUserId;
                var result = await _customerService.UpdatePageAssignmentsAsync(id, request);

                return Ok(new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = true,
                    Message = "Page assignments updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page assignments for customer {CustomerId}", id);
                return StatusCode(500, new ApiResponseDto<CustomerDetailResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating page assignments. Please try again."
                });
            }
        }

        /// <summary>
        /// Search customers
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponseDto<List<CustomerDetailResponseDto>>>> SearchCustomers([FromQuery] string q)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Search customers request with query '{Query}' by user {CurrentUserId}", q, currentUserId);

                var results = await _customerService.SearchCustomersAsync(q);

                return Ok(new ApiResponseDto<CustomerListResponseDto>
                {
                    Success = true,
                    Message = "Customer search completed successfully",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers");
                return StatusCode(500, new ApiResponseDto<List<CustomerDetailResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while searching customers. Please try again."
                });
            }
        }

        /// <summary>
        /// Get customers by region
        /// </summary>
        [HttpGet("by-region/{region}")]
        public async Task<ActionResult<ApiResponseDto<List<CustomerDetailResponseDto>>>> GetCustomersByRegion(string region)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get customers by region request for '{Region}' by user {CurrentUserId}", region, currentUserId);

                var results = await _customerService.GetCustomersByRegionAsync(region);

                return Ok(new ApiResponseDto<CustomerListResponseDto>
                {
                    Success = true,
                    Message = "Customers retrieved successfully",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers by region {Region}", region);
                return StatusCode(500, new ApiResponseDto<List<CustomerDetailResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers by region. Please try again."
                });
            }
        }

        /// <summary>
        /// Get customers by status
        /// </summary>
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<ApiResponseDto<List<CustomerDetailResponseDto>>>> GetCustomersByStatus(string status)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Get customers by status request for '{Status}' by user {CurrentUserId}", status, currentUserId);

                var results = await _customerService.GetCustomersByStatusAsync(status);

                return Ok(new ApiResponseDto<CustomerListResponseDto>
                {
                    Success = true,
                    Message = "Customers retrieved successfully",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers by status {Status}", status);
                return StatusCode(500, new ApiResponseDto<List<CustomerDetailResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customers by status. Please try again."
                });
            }
        }
    }
}
