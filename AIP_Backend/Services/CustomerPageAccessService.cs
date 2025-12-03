using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
    public interface ICustomerPageAccessService
    {
        Task<CustomerPageAccessResponseDto> GetCustomerPageAccessAsync(int customerId);
        Task<CustomerPageAccessResponseDto> UpdateCustomerPageAccessAsync(UpdateCustomerPageAccessRequestDto request, string currentUserId);
    }

    public class CustomerPageAccessService : ICustomerPageAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerPageAccessService> _logger;

        public CustomerPageAccessService(ApplicationDbContext context, ILogger<CustomerPageAccessService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomerPageAccessResponseDto> GetCustomerPageAccessAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer with ID {customerId} not found.");
            }

            var availablePages = await GetAssignableCustomerPagesAsync();
            
            _logger.LogInformation("Found {PageCount} assignable customer pages for customer {CustomerId}", 
                availablePages.Count, customerId);

            var assignedPageIds = await _context.CustomerPageAccesses
                .Where(cpa => cpa.CustomerId == customerId)
                .Select(cpa => cpa.PageAccess.PageId)
                .ToArrayAsync();

            _logger.LogInformation("Customer {CustomerId} has {AssignedCount} pages assigned", 
                customerId, assignedPageIds.Length);

            return new CustomerPageAccessResponseDto
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                AvailablePages = availablePages,
                AssignedPageIds = assignedPageIds
            };
        }

        public async Task<CustomerPageAccessResponseDto> UpdateCustomerPageAccessAsync(UpdateCustomerPageAccessRequestDto request, string currentUserId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer with ID {request.CustomerId} not found.");
            }

            var availablePages = await _context.PageAccesses
                .Where(p => p.IsActive && 
                    ((p.Category != null && p.Category.ToLower() == "customer") || 
                     (p.Path != null && p.Path.ToLower().StartsWith("/customer"))))
                .ToDictionaryAsync(p => p.PageId, p => p);

            var validPageIds = request.PageIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(pageId => availablePages.ContainsKey(pageId))
                .ToList();

            var existingAssignments = await _context.CustomerPageAccesses
                .Where(cpa => cpa.CustomerId == request.CustomerId)
                .ToListAsync();

            _context.CustomerPageAccesses.RemoveRange(existingAssignments);

            foreach (var pageId in validPageIds)
            {
                var page = availablePages[pageId];
                _context.CustomerPageAccesses.Add(new CustomerPageAccess
                {
                    CustomerId = request.CustomerId,
                    PageAccessId = page.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                });
            }

            await _context.SaveChangesAsync();

            return await GetCustomerPageAccessAsync(request.CustomerId);
        }

        private async Task<List<PageAccessDto>> GetAssignableCustomerPagesAsync()
        {
            // First, get all active pages to debug
            var allActivePages = await _context.PageAccesses
                .Where(p => p.IsActive)
                .Select(p => new { p.PageId, p.Category, p.Path })
                .ToListAsync();
            
            _logger.LogInformation("Total active pages in database: {Count}", allActivePages.Count);
            _logger.LogInformation("Sample pages: {Sample}", 
                string.Join(", ", allActivePages.Take(10).Select(p => $"{p.PageId}(Cat:{p.Category}, Path:{p.Path})")));

            var customerPages = await _context.PageAccesses
                .Where(p => p.IsActive && 
                    ((p.Category != null && p.Category.ToLower() == "customer") || 
                     (p.Path != null && p.Path.ToLower().StartsWith("/customer"))))
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
            
            _logger.LogInformation("Filtered customer pages: {Count}. Page IDs: {PageIds}", 
                customerPages.Count, 
                string.Join(", ", customerPages.Select(p => p.PageId)));

            return customerPages;
        }
    }
}

