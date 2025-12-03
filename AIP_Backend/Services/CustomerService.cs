#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerListResponseDto> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? region = null)
        {
            var query = _context.Customers.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.CompanyName.Contains(search) || c.CompanyNumber.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(region))
            {
                query = query.Where(c => c.Region == region);
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.CompanyName) // Add default ordering to fix EF warning
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDetailResponseDto
                {
                    CustomerId = c.CustomerId,
                    CompanyName = c.CompanyName,
                    CompanyNumber = c.CompanyNumber,
                    VatNumber = c.VatNumber,
                    Status = c.Status,
                    CustomerType = c.CustomerType,
                    Region = c.Region,
                    PageAssignments = c.PageAssignments,
                    
                    // Address fields
                    Building = c.Building,
                    Street = c.Street,
                    Village = c.Village,
                    Town = c.Town,
                    County = c.County,
                    Postcode = c.Postcode,
                    
                    // Contact fields
                    ContactTitle = c.ContactTitle,
                    ContactForename = c.ContactForename,
                    ContactSurname = c.ContactSurname,
                    ContactPosition = c.ContactPosition,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedBy = c.UpdatedBy
                })
                .ToListAsync();

            return new CustomerListResponseDto
            {
                Customers = customers,
                Total = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CustomerDetailResponseDto?> GetCustomerByIdAsync(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return null;

            return new CustomerDetailResponseDto
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                CompanyNumber = customer.CompanyNumber,
                VatNumber = customer.VatNumber,
                Status = customer.Status,
                CustomerType = customer.CustomerType,
                Region = customer.Region,
                PageAssignments = customer.PageAssignments,
                
                // Address fields
                Building = customer.Building,
                Street = customer.Street,
                Village = customer.Village,
                Town = customer.Town,
                County = customer.County,
                Postcode = customer.Postcode,
                
                // Contact fields
                ContactTitle = customer.ContactTitle,
                ContactForename = customer.ContactForename,
                ContactSurname = customer.ContactSurname,
                ContactPosition = customer.ContactPosition,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                CreatedBy = customer.CreatedBy,
                UpdatedBy = customer.UpdatedBy
            };
        }

        public async Task<CustomerDetailResponseDto> CreateCustomerAsync(CustomerCreateRequestDto request)
        {
            var customer = new Customer
            {
                CompanyName = request.CompanyName,
                CompanyNumber = request.CompanyNumber,
                VatNumber = request.VatNumber,
                Status = request.Status ?? "active",
                CustomerType = request.CustomerType,
                Region = request.Region,
                PageAssignments = request.PageAssignments,
                
                // Address fields
                Building = request.Building,
                Street = request.Street,
                Village = request.Village,
                Town = request.Town,
                County = request.County,
                Postcode = request.Postcode,
                
                // Contact fields
                ContactTitle = request.ContactTitle,
                ContactForename = request.ContactForename,
                ContactSurname = request.ContactSurname,
                ContactPosition = request.ContactPosition,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy ?? string.Empty
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return new CustomerDetailResponseDto
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                CompanyNumber = customer.CompanyNumber,
                VatNumber = customer.VatNumber,
                Status = customer.Status,
                CustomerType = customer.CustomerType,
                Region = customer.Region,
                PageAssignments = customer.PageAssignments,
                
                // Address fields
                Building = customer.Building,
                Street = customer.Street,
                Village = customer.Village,
                Town = customer.Town,
                County = customer.County,
                Postcode = customer.Postcode,
                
                // Contact fields
                ContactTitle = customer.ContactTitle,
                ContactForename = customer.ContactForename,
                ContactSurname = customer.ContactSurname,
                ContactPosition = customer.ContactPosition,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                CreatedBy = customer.CreatedBy,
                UpdatedBy = customer.UpdatedBy
            };
        }

        public async Task<CustomerDetailResponseDto> UpdateCustomerAsync(int id, CustomerUpdateRequestDto request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new ArgumentException("Customer not found");

            customer.CompanyName = request.CompanyName ?? customer.CompanyName;
            customer.CompanyNumber = request.CompanyNumber ?? customer.CompanyNumber;
            customer.VatNumber = request.VatNumber ?? customer.VatNumber;
            customer.Status = request.Status ?? customer.Status;
            customer.CustomerType = request.CustomerType ?? customer.CustomerType;
            customer.Region = request.Region ?? customer.Region;
            customer.PageAssignments = request.PageAssignments ?? customer.PageAssignments;
            
            // Address fields
            customer.Building = request.Building ?? customer.Building;
            customer.Street = request.Street ?? customer.Street;
            customer.Village = request.Village ?? customer.Village;
            customer.Town = request.Town ?? customer.Town;
            customer.County = request.County ?? customer.County;
            customer.Postcode = request.Postcode ?? customer.Postcode;
            
            // Contact fields
            customer.ContactTitle = request.ContactTitle ?? customer.ContactTitle;
            customer.ContactForename = request.ContactForename ?? customer.ContactForename;
            customer.ContactSurname = request.ContactSurname ?? customer.ContactSurname;
            customer.ContactPosition = request.ContactPosition ?? customer.ContactPosition;
            customer.ContactEmail = request.ContactEmail ?? customer.ContactEmail;
            customer.ContactPhone = request.ContactPhone ?? customer.ContactPhone;
            
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = request.UpdatedBy;

            await _context.SaveChangesAsync();

            return new CustomerDetailResponseDto
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                CompanyNumber = customer.CompanyNumber,
                VatNumber = customer.VatNumber,
                Status = customer.Status,
                CustomerType = customer.CustomerType,
                Region = customer.Region,
                PageAssignments = customer.PageAssignments,
                
                // Address fields
                Building = customer.Building,
                Street = customer.Street,
                Village = customer.Village,
                Town = customer.Town,
                County = customer.County,
                Postcode = customer.Postcode,
                
                // Contact fields
                ContactTitle = customer.ContactTitle,
                ContactForename = customer.ContactForename,
                ContactSurname = customer.ContactSurname,
                ContactPosition = customer.ContactPosition,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                CreatedBy = customer.CreatedBy,
                UpdatedBy = customer.UpdatedBy
            };
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new ArgumentException("Customer not found");

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync()
        {
            var totalCustomers = await _context.Customers.CountAsync();
            var activeCustomers = await _context.Customers.CountAsync(c => c.Status == "active");
            var inactiveCustomers = await _context.Customers.CountAsync(c => c.Status == "inactive");
            var newCustomersThisMonth = await _context.Customers
                .CountAsync(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            // Customers by region
            var customersByRegion = await _context.Customers
                .Where(c => !string.IsNullOrEmpty(c.Region))
                .GroupBy(c => c.Region)
                .Select(g => new { Region = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Region!, x => x.Count);

            // Customers by type
            var customersByType = await _context.Customers
                .Where(c => !string.IsNullOrEmpty(c.CustomerType))
                .GroupBy(c => c.CustomerType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type!, x => x.Count);

            return new CustomerStatisticsDto
            {
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomers,
                InactiveCustomers = inactiveCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                CustomersByRegion = customersByRegion,
                CustomersByType = customersByType
            };
        }

        public async Task<CustomerDetailResponseDto> UpdatePageAssignmentsAsync(int id, CustomerPageAssignmentsDto request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new ArgumentException("Customer not found");

            customer.PageAssignments = request.PageAssignments;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = request.UpdatedBy;

            await _context.SaveChangesAsync();

            return new CustomerDetailResponseDto
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                CompanyNumber = customer.CompanyNumber,
                VatNumber = customer.VatNumber,
                Status = customer.Status,
                CustomerType = customer.CustomerType,
                Region = customer.Region,
                PageAssignments = customer.PageAssignments,
                
                // Address fields
                Building = customer.Building,
                Street = customer.Street,
                Village = customer.Village,
                Town = customer.Town,
                County = customer.County,
                Postcode = customer.Postcode,
                
                // Contact fields
                ContactTitle = customer.ContactTitle,
                ContactForename = customer.ContactForename,
                ContactSurname = customer.ContactSurname,
                ContactPosition = customer.ContactPosition,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                CreatedBy = customer.CreatedBy,
                UpdatedBy = customer.UpdatedBy
            };
        }

        public async Task<CustomerListResponseDto> SearchCustomersAsync(string query)
        {
            var customers = await _context.Customers
                .Where(c => c.CompanyName.Contains(query) || c.CompanyNumber.Contains(query))
                .Select(c => new CustomerDetailResponseDto
                {
                    CustomerId = c.CustomerId,
                    CompanyName = c.CompanyName,
                    CompanyNumber = c.CompanyNumber,
                    VatNumber = c.VatNumber,
                    Status = c.Status,
                    CustomerType = c.CustomerType,
                    Region = c.Region,
                    PageAssignments = c.PageAssignments,
                    
                    // Address fields
                    Building = c.Building,
                    Street = c.Street,
                    Village = c.Village,
                    Town = c.Town,
                    County = c.County,
                    Postcode = c.Postcode,
                    
                    // Contact fields
                    ContactTitle = c.ContactTitle,
                    ContactForename = c.ContactForename,
                    ContactSurname = c.ContactSurname,
                    ContactPosition = c.ContactPosition,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedBy = c.UpdatedBy
                })
                .ToListAsync();

            return new CustomerListResponseDto
            {
                Customers = customers,
                Total = customers.Count,
                Page = 1,
                PageSize = customers.Count
            };
        }

        public async Task<CustomerListResponseDto> GetCustomersByRegionAsync(string region)
        {
            var customers = await _context.Customers
                .Where(c => c.Region == region)
                .Select(c => new CustomerDetailResponseDto
                {
                    CustomerId = c.CustomerId,
                    CompanyName = c.CompanyName,
                    CompanyNumber = c.CompanyNumber,
                    VatNumber = c.VatNumber,
                    Status = c.Status,
                    CustomerType = c.CustomerType,
                    Region = c.Region,
                    PageAssignments = c.PageAssignments,
                    
                    // Address fields
                    Building = c.Building,
                    Street = c.Street,
                    Village = c.Village,
                    Town = c.Town,
                    County = c.County,
                    Postcode = c.Postcode,
                    
                    // Contact fields
                    ContactTitle = c.ContactTitle,
                    ContactForename = c.ContactForename,
                    ContactSurname = c.ContactSurname,
                    ContactPosition = c.ContactPosition,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedBy = c.UpdatedBy
                })
                .ToListAsync();

            return new CustomerListResponseDto
            {
                Customers = customers,
                Total = customers.Count,
                Page = 1,
                PageSize = customers.Count
            };
        }

        public async Task<CustomerListResponseDto> GetCustomersByStatusAsync(string status)
        {
            var customers = await _context.Customers
                .Where(c => c.Status == status)
                .Select(c => new CustomerDetailResponseDto
                {
                    CustomerId = c.CustomerId,
                    CompanyName = c.CompanyName,
                    CompanyNumber = c.CompanyNumber,
                    VatNumber = c.VatNumber,
                    Status = c.Status,
                    CustomerType = c.CustomerType,
                    Region = c.Region,
                    PageAssignments = c.PageAssignments,
                    
                    // Address fields
                    Building = c.Building,
                    Street = c.Street,
                    Village = c.Village,
                    Town = c.Town,
                    County = c.County,
                    Postcode = c.Postcode,
                    
                    // Contact fields
                    ContactTitle = c.ContactTitle,
                    ContactForename = c.ContactForename,
                    ContactSurname = c.ContactSurname,
                    ContactPosition = c.ContactPosition,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedBy = c.CreatedBy,
                    UpdatedBy = c.UpdatedBy
                })
                .ToListAsync();

            return new CustomerListResponseDto
            {
                Customers = customers,
                Total = customers.Count,
                Page = 1,
                PageSize = customers.Count
            };
        }
    }
}
