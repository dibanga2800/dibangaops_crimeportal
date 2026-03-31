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

            var blockingReasons = new List<string>();

            var linkedUsers = await _context.ApplicationUsers.CountAsync(
                u => u.CustomerId == id && !u.RecordIsDeletedYN
            );
            if (linkedUsers > 0)
            {
                blockingReasons.Add($"{linkedUsers} linked user account{(linkedUsers == 1 ? string.Empty : "s")}");
            }

            var linkedIncidents = await _context.Incidents.CountAsync(
                i => i.CustomerId == id && !i.RecordIsDeletedYN
            );
            if (linkedIncidents > 0)
            {
                blockingReasons.Add($"{linkedIncidents} incident record{(linkedIncidents == 1 ? string.Empty : "s")}");
            }

            var customerAssignments = await _context.UserCustomerAssignments
                .Where(assignment => assignment.CustomerId == id)
                .ToListAsync();
            if (customerAssignments.Count > 0)
            {
                _context.UserCustomerAssignments.RemoveRange(customerAssignments);
            }

            var customerPageAccesses = await _context.CustomerPageAccesses
                .Where(access => access.CustomerId == id)
                .ToListAsync();
            if (customerPageAccesses.Count > 0)
            {
                _context.CustomerPageAccesses.RemoveRange(customerPageAccesses);
            }

            var customerSites = await _context.Sites.CountAsync(
                site => site.fkCustomerID == id && !site.RecordIsDeletedYN
            );
            if (customerSites > 0)
            {
                blockingReasons.Add($"{customerSites} site record{(customerSites == 1 ? string.Empty : "s")}");
            }

            var customerRegions = await _context.Regions.CountAsync(
                region => region.fkCustomerID == id && !region.RecordIsDeletedYN
            );
            if (customerRegions > 0)
            {
                blockingReasons.Add($"{customerRegions} region record{(customerRegions == 1 ? string.Empty : "s")}");
            }

            // These modules are no longer used in the current app flow, so clean their rows up
            // during customer deletion instead of letting old foreign keys block admin cleanup.
            var legacyDailyActivityReports = await _context.DailyActivityReports
                .Where(report => report.CustomerId == id)
                .ToListAsync();
            if (legacyDailyActivityReports.Count > 0)
            {
                _context.DailyActivityReports.RemoveRange(legacyDailyActivityReports);
            }

            var legacyOccurrenceBooks = await _context.DailyOccurrenceBooks
                .Where(book => book.CustomerId == id)
                .ToListAsync();
            if (legacyOccurrenceBooks.Count > 0)
            {
                _context.DailyOccurrenceBooks.RemoveRange(legacyOccurrenceBooks);
            }

            var legacyRiskScores = await _context.StoreRiskScores
                .Where(score => score.CustomerId == id)
                .ToListAsync();
            if (legacyRiskScores.Count > 0)
            {
                _context.StoreRiskScores.RemoveRange(legacyRiskScores);
            }

            var customerAlertRules = await _context.AlertRules
                .Where(rule => rule.CustomerId == id)
                .ToListAsync();
            if (customerAlertRules.Count > 0)
            {
                _context.AlertRules.RemoveRange(customerAlertRules);
            }

            if (blockingReasons.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot delete customer '{customer.CompanyName}' because it still has {string.Join(" and ", blockingReasons)}. " +
                    "Remove or reassign those records first.");
            }

            // Inactive rows can still keep SQL foreign keys alive even though the UI
            // no longer shows them. Clean those up so an admin can remove a retired customer.
            var softDeletedUsers = await _context.ApplicationUsers
                .Where(u => u.CustomerId == id && u.RecordIsDeletedYN)
                .ToListAsync();
            foreach (var user in softDeletedUsers)
            {
                user.CustomerId = null;
            }

            var softDeletedIncidents = await _context.Incidents
                .Where(i => i.CustomerId == id && i.RecordIsDeletedYN)
                .ToListAsync();
            if (softDeletedIncidents.Count > 0)
            {
                var softDeletedIncidentIds = softDeletedIncidents
                    .Select(i => i.IncidentId)
                    .ToList();

                var evidenceItems = await _context.EvidenceItems
                    .Where(item => softDeletedIncidentIds.Contains(item.IncidentId))
                    .ToListAsync();
                if (evidenceItems.Count > 0)
                {
                    var evidenceItemIds = evidenceItems
                        .Select(item => item.EvidenceItemId)
                        .ToList();

                    var custodyEvents = await _context.EvidenceCustodyEvents
                        .Where(evt => evidenceItemIds.Contains(evt.EvidenceItemId))
                        .ToListAsync();
                    if (custodyEvents.Count > 0)
                    {
                        _context.EvidenceCustodyEvents.RemoveRange(custodyEvents);
                    }

                    _context.EvidenceItems.RemoveRange(evidenceItems);
                }

                var alertInstances = await _context.AlertInstances
                    .Where(instance => instance.IncidentId.HasValue && softDeletedIncidentIds.Contains(instance.IncidentId.Value))
                    .ToListAsync();
                if (alertInstances.Count > 0)
                {
                    _context.AlertInstances.RemoveRange(alertInstances);
                }

                _context.Incidents.RemoveRange(softDeletedIncidents);
            }

            var softDeletedSites = await _context.Sites
                .Where(site => site.fkCustomerID == id && site.RecordIsDeletedYN)
                .ToListAsync();
            if (softDeletedSites.Count > 0)
            {
                _context.Sites.RemoveRange(softDeletedSites);
            }

            var softDeletedRegions = await _context.Regions
                .Where(region => region.fkCustomerID == id && region.RecordIsDeletedYN)
                .ToListAsync();
            if (softDeletedRegions.Count > 0)
            {
                _context.Regions.RemoveRange(softDeletedRegions);
            }

            // These legacy survey tables still exist in older databases even though
            // the entities are no longer part of the active EF model.
            await _context.Database.ExecuteSqlRawAsync(
                @"
                IF OBJECT_ID(N'[dbo].[SiteVisits]', N'U') IS NOT NULL
                BEGIN
                    DELETE FROM [dbo].[SiteVisits]
                    WHERE [CustomerId] = {0};
                END

                IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveys]', N'U') IS NOT NULL
                BEGIN
                    IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveyFollowUpActions]', N'U') IS NOT NULL
                    BEGIN
                        DELETE followUps
                        FROM [dbo].[CustomerSatisfactionSurveyFollowUpActions] AS followUps
                        INNER JOIN [dbo].[CustomerSatisfactionSurveys] AS surveys
                            ON followUps.[SurveyId] = CONVERT(nvarchar(50), surveys.[Id])
                        WHERE surveys.[CustomerId] = {0};
                    END

                    IF OBJECT_ID(N'[dbo].[CustomerSatisfactionSurveyDatesToBeCompleted]', N'U') IS NOT NULL
                    BEGIN
                        DELETE dueDates
                        FROM [dbo].[CustomerSatisfactionSurveyDatesToBeCompleted] AS dueDates
                        INNER JOIN [dbo].[CustomerSatisfactionSurveys] AS surveys
                            ON dueDates.[SurveyId] = CONVERT(nvarchar(50), surveys.[Id])
                        WHERE surveys.[CustomerId] = {0};
                    END

                    DELETE FROM [dbo].[CustomerSatisfactionSurveys]
                    WHERE [CustomerId] = {0};
                END
                ",
                id
            );

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
