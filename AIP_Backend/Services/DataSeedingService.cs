using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
    public class DataSeedingService : IDataSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<DataSeedingService> _logger;
        private readonly IPageAccessService _pageAccessService;

        public DataSeedingService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IPageAccessService pageAccessService,
            ILogger<DataSeedingService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _pageAccessService = pageAccessService;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");

                await SeedRolesAsync();
                await SeedPermissionsAsync();
                await SeedCustomersAsync();
                await SeedAdminUserAsync();
                await SeedTestUsersAsync();
                await SeedSampleUsersForTestingAsync(); // Add the new sample users
                await SeedTestEmployeesAsync();
                await SeedLookupTablesAsync();
                await UpdatePositionsAsync();
                await SeedSitesAsync();
                await SeedPageAccessAsync();

                _logger.LogInformation("Data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data seeding");
                throw;
            }
        }

        public async Task SeedRolesAsync()
        {
            _logger.LogInformation("Seeding roles...");

            var roles = new[]
            {
                new { Name = "administrator", Description = "System administrator with full access" },
                new { Name = "advantageoneofficer", Description = "Advantage One officer role" },
                new { Name = "advantageonehoofficer", Description = "Advantage One head office officer role" },
                new { Name = "customersitemanager", Description = "Customer site manager role" },
                new { Name = "customerhomanager", Description = "Customer head office manager role" }
            };

            foreach (var roleInfo in roles)
            {
                var existingRole = await _roleManager.FindByNameAsync(roleInfo.Name);
                if (existingRole == null)
                {
                    var role = new ApplicationRole
                    {
                        Name = roleInfo.Name,
                        Description = roleInfo.Description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create role: {RoleName}. Errors: {Errors}", 
                            roleInfo.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Role already exists: {RoleName}", roleInfo.Name);
                }
            }
        }

        public async Task SeedPermissionsAsync()
        {
            _logger.LogInformation("Seeding permissions...");

            var permissions = new[]
            {
                // User Management
                new { Name = "CreateUser", Description = "Create new users", Resource = "User", Action = "Create" },
                new { Name = "UpdateUser", Description = "Update user information", Resource = "User", Action = "Update" },
                new { Name = "DeleteUser", Description = "Delete users", Resource = "User", Action = "Delete" },
                new { Name = "ViewUser", Description = "View user information", Resource = "User", Action = "Read" },
                new { Name = "ManageUserRoles", Description = "Manage user roles", Resource = "User", Action = "ManageRoles" },

                // Employee Management
                new { Name = "CreateEmployee", Description = "Create new employees", Resource = "Employee", Action = "Create" },
                new { Name = "UpdateEmployee", Description = "Update employee information", Resource = "Employee", Action = "Update" },
                new { Name = "DeleteEmployee", Description = "Delete employees", Resource = "Employee", Action = "Delete" },
                new { Name = "ViewEmployee", Description = "View employee information", Resource = "Employee", Action = "Read" },

                // Customer Management
                new { Name = "CreateCustomer", Description = "Create new customers", Resource = "Customer", Action = "Create" },
                new { Name = "UpdateCustomer", Description = "Update customer information", Resource = "Customer", Action = "Update" },
                new { Name = "DeleteCustomer", Description = "Delete customers", Resource = "Customer", Action = "Delete" },
                new { Name = "ViewCustomer", Description = "View customer information", Resource = "Customer", Action = "Read" },

                // System Administration
                new { Name = "SystemAdmin", Description = "Full system administration", Resource = "System", Action = "Admin" },
                new { Name = "ViewReports", Description = "View system reports", Resource = "Reports", Action = "Read" },
                new { Name = "ManageSettings", Description = "Manage system settings", Resource = "Settings", Action = "Manage" }
            };

            foreach (var permInfo in permissions)
            {
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permInfo.Name);

                if (existingPermission == null)
                {
                    var permission = new Permission
                    {
                        Name = permInfo.Name,
                        Description = permInfo.Description,
                        Resource = permInfo.Resource,
                        Action = permInfo.Action,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Permissions.Add(permission);
                    _logger.LogInformation("Created permission: {PermissionName}", permInfo.Name);
                }
                else
                {
                    _logger.LogInformation("Permission already exists: {PermissionName}", permInfo.Name);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task SeedCustomersAsync()
        {
            _logger.LogInformation("Seeding customers...");

            var customers = new[]
            {
                new { CompanyName = "Central England COOP", CompanyNumber = "21001", VatNumber = "GB210001001" },
                new { CompanyName = "Heart of England", CompanyNumber = "22001", VatNumber = "GB220001001" },
                new { CompanyName = "Midcounties COOP", CompanyNumber = "23001", VatNumber = "GB230001001" },
                new { CompanyName = "Eastbrook Worcester", CompanyNumber = "24001", VatNumber = "GB240001001" },
                new { CompanyName = "Eastbrook Tewksbury", CompanyNumber = "25001", VatNumber = "GB250001001" }
            };

            foreach (var customerInfo in customers)
            {
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CompanyName == customerInfo.CompanyName);

                if (existingCustomer == null)
                {
                    var customer = new Customer
                    {
                        CompanyName = customerInfo.CompanyName,
                        CompanyNumber = customerInfo.CompanyNumber,
                        VatNumber = customerInfo.VatNumber,
                        Status = "active",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    _context.Customers.Add(customer);
                    _logger.LogInformation("Created customer: {CustomerName}", customerInfo.CompanyName);
                }
                else
                {
                    _logger.LogInformation("Customer already exists: {CustomerName}", customerInfo.CompanyName);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task SeedAdminUserAsync()
        {
            _logger.LogInformation("Seeding admin user...");

            var adminEmail = "admin@advantageone.com";
            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "administrator",
                    PageAccessRole = "administrator",
                    JobTitle = "System Administrator",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System" // Use a string identifier instead of null
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin123!@#");
                if (result.Succeeded)
                {
                    // Now update the CreatedBy and UpdatedBy to reference the user's own ID
                    adminUser.CreatedBy = adminUser.Id;
                    adminUser.UpdatedBy = adminUser.Id;
                    await _userManager.UpdateAsync(adminUser);

                    // Add to Administrator role
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, "administrator");
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Created admin user and assigned Administrator role");
                    }
                    else
                    {
                        _logger.LogWarning("Created admin user but failed to assign role. Errors: {Errors}", 
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }

                    // Assign to all customers (since it's an admin)
                    var customers = await _context.Customers.ToListAsync();
                    var assignments = customers.Select(c => new UserCustomerAssignment
                    {
                        UserId = adminUser.Id,
                        CustomerId = c.CustomerId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminUser.Id // Use the user's ID instead of "System"
                    }).ToList();

                    _context.UserCustomerAssignments.AddRange(assignments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Assigned admin user to all customers");
                }
                else
                {
                    _logger.LogWarning("Failed to create admin user. Errors: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists");
            }

            // Update CreatedBy for all entities that were created with null CreatedBy
            var existingAdminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdminUser != null)
            {
                try
                {
                    // Update roles - handle null values safely
                    var roles = await _context.Roles.Where(r => r.CreatedBy == null || r.CreatedBy == "System").ToListAsync();
                    foreach (var role in roles)
                    {
                        role.CreatedBy = existingAdminUser.Id;
                    }

                    // Update customers - handle null values safely
                    var customers = await _context.Customers.Where(c => c.CreatedBy == null || c.CreatedBy == "System").ToListAsync();
                    foreach (var customer in customers)
                    {
                        customer.CreatedBy = existingAdminUser.Id;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated CreatedBy fields for seeded entities");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to update CreatedBy fields: {Error}", ex.Message);
                }
            }
        }

        public async Task SeedTestUsersAsync()
        {
            _logger.LogInformation("Seeding test users...");

            var testUsers = new[]
            {
                new {
                    UserName = "advantage.officer",
                    Email = "officer@advantageone.com",
                    FirstName = "John",
                    LastName = "Officer",
                    Role = "advantageoneofficer",
                    PageAccessRole = "advantageoneofficer",
                    JobTitle = "Security Officer",
                    CompanyName = "Central England COOP" // Used to find customers for assignment
                },
                new {
                    UserName = "customer.manager",
                    Email = "manager@customer.com",
                    FirstName = "Sarah",
                    LastName = "Manager",
                    Role = "customersitemanager",
                    PageAccessRole = "customersitemanager",
                    JobTitle = "Site Manager",
                    CompanyName = "Heart of England" // Used to find customer for CustomerId
                }
            };

            foreach (var userInfo in testUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(userInfo.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userInfo.UserName,
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        Role = userInfo.Role,
                        PageAccessRole = userInfo.PageAccessRole,
                        JobTitle = userInfo.JobTitle,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System" // Set to System initially to avoid self-reference
                    };

                    var result = await _userManager.CreateAsync(user, "Test123!@#");
                    if (result.Succeeded)
                    {
                        // Now update the CreatedBy to reference the user's own ID
                        user.CreatedBy = user.Id;
                        user.UpdatedBy = user.Id;
                        await _userManager.UpdateAsync(user);

                        var roleResult = await _userManager.AddToRoleAsync(user, userInfo.Role);
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Created test user {UserName} and assigned {Role} role", 
                                userInfo.UserName, userInfo.Role);

                            // For customer users, set CustomerId
                            if (userInfo.Role == "customersitemanager" || userInfo.Role == "customerhomanager")
                            {
                                var customer = await _context.Customers
                                    .FirstOrDefaultAsync(c => c.CompanyName == userInfo.CompanyName);
                                if (customer != null)
                                {
                                    user.CustomerId = customer.CustomerId;
                                    await _userManager.UpdateAsync(user);
                                    _logger.LogInformation("Set CustomerId {CustomerId} for customer user {UserName}", customer.CustomerId, userInfo.UserName);
                                }
                            }
                            // For AdvantageOne users, assign to specific customers
                            else if (userInfo.Role.StartsWith("AdvantageOne"))
                            {
                                var customers = await _context.Customers
                                    .Where(c => c.CompanyName == userInfo.CompanyName)
                                    .ToListAsync();

                                var assignments = customers.Select(c => new UserCustomerAssignment
                                {
                                    UserId = user.Id,
                                    CustomerId = c.CustomerId,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = user.Id // Use the user's ID instead of "System"
                                }).ToList();

                                _context.UserCustomerAssignments.AddRange(assignments);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Created test user but failed to assign role. Errors: {Errors}",
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create test user. Errors: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Test user {UserName} already exists", userInfo.UserName);
                }
            }
        }

        public async Task SeedSampleUsersForTestingAsync()
        {
            _logger.LogInformation("Seeding sample users for testing...");

            var sampleUsers = new[]
            {
                new {
                    UserName = "admin.test",
                    Email = "admin.test@advantageone.com",
                    FirstName = "Michael",
                    LastName = "Admin",
                    Role = "administrator",
                    PageAccessRole = "administrator",
                    JobTitle = "System Administrator",
                    CompanyName = "Central England COOP", // Used to find customers for assignment
                    Signature = "M.Admin",
                    SignatureCode = "MA001"
                },
                new {
                    UserName = "ho.officer",
                    Email = "ho.officer@advantageone.com",
                    FirstName = "Emily",
                    LastName = "Wilson",
                    Role = "advantageonehoofficer",
                    PageAccessRole = "advantageonehoofficer",
                    JobTitle = "Head Office Officer",
                    CompanyName = "Central England COOP", // Used to find customers for assignment
                    Signature = "E.Wilson",
                    SignatureCode = "EW001"
                },
                new {
                    UserName = "field.officer",
                    Email = "field.officer@advantageone.com",
                    FirstName = "David",
                    LastName = "Brown",
                    Role = "advantageoneofficer",
                    PageAccessRole = "advantageoneofficer",
                    JobTitle = "Field Officer",
                    CompanyName = "Central England COOP", // Used to find customers for assignment
                    Signature = "D.Brown",
                    SignatureCode = "DB001"
                },
                new {
                    UserName = "site.manager",
                    Email = "site.manager@heartofengland.com",
                    FirstName = "Lisa",
                    LastName = "Garcia",
                    Role = "customersitemanager",
                    PageAccessRole = "customersitemanager",
                    JobTitle = "Site Manager",
                    CompanyName = "Heart of England", // Used to find customer for CustomerId
                    Signature = "L.Garcia",
                    SignatureCode = "LG001"
                },
                new {
                    UserName = "ho.manager",
                    Email = "ho.manager@midcounties.com",
                    FirstName = "Robert",
                    LastName = "Taylor",
                    Role = "customerhomanager",
                    PageAccessRole = "customerhomanager",
                    JobTitle = "Head Office Manager",
                    CompanyName = "Midcounties COOP", // Used to find customer for CustomerId
                    Signature = "R.Taylor",
                    SignatureCode = "RT001"
                }
            };

            // Get admin user for CreatedBy reference first
            var adminUser = await _userManager.FindByEmailAsync("admin@advantageone.com");
            
            foreach (var userInfo in sampleUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(userInfo.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userInfo.UserName,
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        Role = userInfo.Role,
                        PageAccessRole = userInfo.PageAccessRole,
                        JobTitle = userInfo.JobTitle,
                        Signature = userInfo.Signature,
                        SignatureCode = userInfo.SignatureCode,
                        IsActive = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = adminUser?.Id ?? "System"
                    };

                    var result = await _userManager.CreateAsync(user, "Sample123!@#");
                    if (result.Succeeded)
                    {
                        // Update UpdatedBy field
                        user.UpdatedBy = adminUser?.Id ?? user.Id;
                        await _userManager.UpdateAsync(user);

                        var roleResult = await _userManager.AddToRoleAsync(user, userInfo.Role);
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Created sample user {UserName} with role {Role}", 
                                userInfo.UserName, userInfo.Role);

                            // For customer users, set CustomerId
                            if (userInfo.Role == "customersitemanager" || userInfo.Role == "customerhomanager")
                            {
                                var customer = await _context.Customers
                                    .FirstOrDefaultAsync(c => c.CompanyName == userInfo.CompanyName);
                                if (customer != null)
                                {
                                    user.CustomerId = customer.CustomerId;
                                    await _userManager.UpdateAsync(user);
                                    _logger.LogInformation("Set CustomerId {CustomerId} for customer user {UserName}", customer.CustomerId, userInfo.UserName);
                                }
                            }
                            // For AdvantageOne users, assign to customers
                            else if (userInfo.Role.StartsWith("advantageone") || userInfo.Role == "administrator")
                            {
                                var customers = await _context.Customers.ToListAsync();
                                var assignments = customers.Select(c => new UserCustomerAssignment
                                {
                                    UserId = user.Id,
                                    CustomerId = c.CustomerId,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = adminUser?.Id ?? user.Id
                                }).ToList();

                                _context.UserCustomerAssignments.AddRange(assignments);
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation("Assigned user {UserName} to {CustomerCount} customers", 
                                    userInfo.UserName, customers.Count);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Created sample user but failed to assign role. Errors: {Errors}",
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create sample user {UserName}. Errors: {Errors}",
                            userInfo.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Sample user {UserName} already exists", userInfo.UserName);
                }
            }
        }

        public async Task SeedTestEmployeesAsync()
        {
            _logger.LogInformation("Seeding test employees...");

            var testEmployees = new[]
            {
                new {
                    Title = "Mr",
                    FirstName = "John",
                    Surname = "Officer",
                    Position = "Security Officer",
                    EmployeeStatus = "Active",
                    EmploymentType = "Full Time",
                    Department = "Security",
                    Region = "Central",
                    Email = "officer@advantageone.com",
                    ContactNumber = "07700900001"
                },
                new {
                    Title = "Mrs",
                    FirstName = "Sarah",
                    Surname = "Manager",
                    Position = "Site Manager",
                    EmployeeStatus = "Active",
                    EmploymentType = "Full Time",
                    Department = "Management",
                    Region = "North",
                    Email = "manager@customer.com",
                    ContactNumber = "07700900002"
                }
            };

            // Note: Employee seeding removed (Employee model deleted)
            // Test employees are no longer created
            foreach (var empInfo in testEmployees)
            {
                _logger.LogInformation("Skipping test employee creation (Employee model deleted): {FullName}", $"{empInfo.FirstName} {empInfo.Surname}");
            }

            await _context.SaveChangesAsync();
        }



        public async Task SeedLookupTablesAsync()
        {
            try
            {
                _logger.LogInformation("Starting lookup table seeding...");

                // Check if data already exists
                var existingCount = await _context.LookupTables.CountAsync();
                if (existingCount > 0)
                {
                    _logger.LogInformation("Lookup tables already seeded. Skipping...");
                    return;
                }

                var lookupTables = new List<LookupTable>();

                // UK Counties
                var ukCounties = new[]
                {
                    "Bedfordshire", "Berkshire", "Bristol", "Buckinghamshire", "Cambridgeshire",
                    "Cheshire", "Cornwall", "Cumbria", "Derbyshire", "Devon", "Dorset", "Durham",
                    "East Sussex", "Essex", "Gloucestershire", "Greater London", "Greater Manchester",
                    "Hampshire", "Herefordshire", "Hertfordshire", "Isle of Wight", "Kent", "Lancashire",
                    "Leicestershire", "Lincolnshire", "Merseyside", "Norfolk", "Northamptonshire",
                    "Northumberland", "Nottinghamshire", "Oxfordshire", "Rutland", "Shropshire",
                    "Somerset", "South Yorkshire", "Staffordshire", "Suffolk", "Surrey", "Tyne and Wear",
                    "Warwickshire", "West Midlands", "West Sussex", "West Yorkshire", "Wiltshire", "Worcestershire"
                };

                for (int i = 0; i < ukCounties.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "UK_Counties",
                        Value = ukCounties[i],
                        Description = $"UK County: {ukCounties[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // UK Regions
                var ukRegions = new[]
                {
                    "East Midlands", "East of England", "London", "North East", "North West",
                    "South East", "South West", "West Midlands", "Yorkshire and the Humber"
                };

                for (int i = 0; i < ukRegions.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "UK_Regions",
                        Value = ukRegions[i],
                        Description = $"UK Region: {ukRegions[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Trainers
                var trainers = new[]
                {
                    "James Haigh", "Scott Bowhil", "Adam Pilcher", "Said Said", "Gil Sheffield", "David Ibanga"
                };

                for (int i = 0; i < trainers.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "Trainers",
                        Value = trainers[i],
                        Description = $"Trainer: {trainers[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // SIA Licence Types
                var siaLicenceTypes = new[]
                {
                    "Door Supervisor", "Security Guard", "Close Protection",
                    "Public Space Surveillance (CCTV)", "Vehicle Immobiliser", "Key Holding"
                };

                for (int i = 0; i < siaLicenceTypes.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "SIA_Licence_Types",
                        Value = siaLicenceTypes[i],
                        Description = $"SIA Licence Type: {siaLicenceTypes[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Driving Licence Types
                var drivingLicenceTypes = new[]
                {
                    "Full UK Driving Licence", "Provisional UK Driving Licence",
                    "International Driving Permit", "No Driving Licence"
                };

                for (int i = 0; i < drivingLicenceTypes.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "Driving_Licence_Types",
                        Value = drivingLicenceTypes[i],
                        Description = $"Driving Licence Type: {drivingLicenceTypes[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Right to Work Conditions
                var rightToWorkConditions = new[]
                {
                    "British Citizen", "EU Citizen", "Settled Status", "Pre-settled Status",
                    "Work Visa", "Student Visa", "Other"
                };

                for (int i = 0; i < rightToWorkConditions.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "Right_To_Work_Conditions",
                        Value = rightToWorkConditions[i],
                        Description = $"Right to Work Condition: {rightToWorkConditions[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Working Time Directive
                var workingTimeDirectives = new[]
                {
                    "Opted Out", "Opted In", "Not Applicable"
                };

                for (int i = 0; i < workingTimeDirectives.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "Working_Time_Directive",
                        Value = workingTimeDirectives[i],
                        Description = $"Working Time Directive: {workingTimeDirectives[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // User Roles
                var userRoles = new[]
                {
                    "AdvantageOneAdmin", "AdvantageOneManager", "AdvantageOneOfficer",
                    "CustomerHOManager", "CustomerSiteManager"
                };

                for (int i = 0; i < userRoles.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "User_Roles",
                        Value = userRoles[i],
                        Description = $"User Role: {userRoles[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Employee Positions
                var employeePositions = new[]
                {
                    "Security Officer", "Security Supervisor", "Security Manager",
                    "Training Coordinator", "Senior Security Officer", "HR Specialist",
                    "Uniform Officers", "Store Detectives", "Supervisor", "Area Manager", 
                    "HR", "Business Dev Manager", "IT Manager", "Director", 
                    "Operations Director", "Operations Manager", "Account Manager"
                };

                for (int i = 0; i < employeePositions.Length; i++)
                {
                    lookupTables.Add(new LookupTable
                    {
                        Category = "Positions",
                        Value = employeePositions[i],
                        Description = $"Employee Position: {employeePositions[i]}",
                        Code = "",
                        SortOrder = i,
                        IsActive = true,
                        CreatedBy = "System"
                    });
                }

                // Add all lookup tables to the context
                await _context.LookupTables.AddRangeAsync(lookupTables);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} lookup table records", lookupTables.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding lookup tables");
                throw;
            }
        }

        public async Task SeedPageAccessAsync()
        {
            try
            {
                _logger.LogInformation("Seeding page access settings...");

                // Get the admin user to use as the creator
                var adminUser = await _userManager.FindByEmailAsync("admin@advantageone.com");
                if (adminUser == null)
                {
                    _logger.LogWarning("Admin user not found, skipping page access seeding");
                    return;
                }

                // Initialize default page access settings
                var result = await _pageAccessService.InitializeDefaultPageAccessAsync(adminUser.Id);

                if (result)
                {
                    _logger.LogInformation("Page access settings seeded successfully");
                }
                else
                {
                    _logger.LogInformation("Page access settings already exist, skipping initialization");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding page access settings");
                // Don't throw here as page access is not critical for basic functionality
            }
        }

        public async Task UpdatePositionsAsync()
        {
            try
            {
                _logger.LogInformation("Updating positions data...");

                // First, update the category name from "Employee_Positions" to "Positions"
                var oldPositions = await _context.LookupTables
                    .Where(lt => lt.Category == "Employee_Positions")
                    .ToListAsync();

                foreach (var position in oldPositions)
                {
                    position.Category = "Positions";
                }

                // Add new positions if they don't exist
                var newPositions = new[]
                {
                    "Uniform Officers", "Store Detectives", "Supervisor", "Area Manager", 
                    "HR", "Business Dev Manager", "IT Manager", "Director", 
                    "Operations Director", "Operations Manager", "Account Manager"
                };

                var existingPositions = await _context.LookupTables
                    .Where(lt => lt.Category == "Positions")
                    .Select(lt => lt.Value)
                    .ToListAsync();

                var positionsToAdd = newPositions
                    .Where(np => !existingPositions.Contains(np))
                    .ToList();

                if (positionsToAdd.Any())
                {
                    var maxSortOrder = await _context.LookupTables
                        .Where(lt => lt.Category == "Positions")
                        .MaxAsync(lt => lt.SortOrder);

                    var newLookupTables = positionsToAdd.Select((position, index) => new LookupTable
                    {
                        Category = "Positions",
                        Value = position,
                        Description = $"Employee Position: {position}",
                        Code = "",
                        SortOrder = maxSortOrder + index + 1,
                        IsActive = true,
                        CreatedBy = "System"
                    }).ToList();

                    await _context.LookupTables.AddRangeAsync(newLookupTables);
                    _logger.LogInformation("Added {Count} new positions", newLookupTables.Count);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Positions data updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating positions data");
                throw;
            }
        }

        public async Task SeedSitesAsync()
        {
            try
            {
                _logger.LogInformation("Seeding sites...");

                // Get existing site IDs to avoid duplicates
                var existingSiteIds = await _context.Sites
                    .Select(s => s.SiteID)
                    .ToListAsync();

                var sites = new[]
                {
                    new {
                        SiteID = 9,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Repton Road",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Repton Road",
                        VillageOrSuburb = "Willington",
                        Town = "Rolleston",
                        County = "Derbyshire",
                        Postcode = "DE65 6BX",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "677",
                        DateCreated = new DateTime(2018, 4, 30, 3, 5, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 10,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Rolleston",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Stone Road",
                        VillageOrSuburb = "",
                        Town = "Stafford",
                        County = "Staffordshire",
                        Postcode = "DE13 9DN",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2019, 3, 11, 14, 31, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 11,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Stone Road",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Main Street",
                        VillageOrSuburb = "",
                        Town = "Derby",
                        County = "Derbyshire",
                        Postcode = "ST16 1LA",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2020, 10, 14, 16, 11, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 12,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Stretton",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Horninglow Road North",
                        VillageOrSuburb = "Stretton",
                        Town = "Burton on Trent",
                        County = "Staffordshire",
                        Postcode = "DE13 0EA",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2021, 3, 17, 17, 56, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 13,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Castle Park",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "",
                        Town = "Burton on Trent",
                        County = "Staffordshire",
                        Postcode = "DE13 0SX",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 14,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Queensway",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "Queensway",
                        Town = "Rugeley",
                        County = "Staffordshire",
                        Postcode = "WS15 1NN",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 15,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Dyas Road",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "196 Dyas Road",
                        VillageOrSuburb = "",
                        Town = "Great Barr",
                        County = "West Midlands",
                        Postcode = "B44 8SX",
                        TelephoneNumber = "0121 3608633",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "2460",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 16,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Burntwood",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Cannock Road",
                        VillageOrSuburb = "Chasetown",
                        Town = "Lichfield",
                        County = "Staffordshire",
                        Postcode = "WS7 0BB",
                        TelephoneNumber = "01543 683109",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 17,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Stapenhill",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Woods Lane",
                        VillageOrSuburb = "",
                        Town = "Burton on Trent",
                        County = "Staffordshire",
                        Postcode = "DE15 9EJ",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 18,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Highfields Road",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Crowberry Lane",
                        VillageOrSuburb = "",
                        Town = "Lichfield",
                        County = "Staffordshire",
                        Postcode = "WS7 4QU",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 19,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Baswich Lane",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Church Hill Street",
                        VillageOrSuburb = "",
                        Town = "Stafford",
                        County = "Staffordshire",
                        Postcode = "ST17 0AP",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 20,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "First Avenue",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "Birmingham Road",
                        VillageOrSuburb = "",
                        Town = "Stafford",
                        County = "Staffordshire",
                        Postcode = "ST16 1QE",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 21,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Weston Road",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "",
                        Town = "Stafford",
                        County = "Staffordshire",
                        Postcode = "ST16 3RL",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 22,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Barton Under Need",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "",
                        Town = "Burton on Trent",
                        County = "Staffordshire",
                        Postcode = "DE12 8AF",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 23,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Winshill",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "Winshill",
                        Town = "Burton on Trent",
                        County = "Staffordshire",
                        Postcode = "DE15 0HR",
                        TelephoneNumber = "",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    },
                    new {
                        SiteID = 24,
                        fkCustomerID = 1,
                        fkRegionID = 1,
                        CoreSiteYN = true,
                        LocationName = "Fosseway Gate",
                        LocationType = "Site",
                        BuildingName = "",
                        NumberandStreet = "",
                        VillageOrSuburb = "",
                        Town = "Lichfield",
                        County = "Staffordshire",
                        Postcode = "WS14 9BJ",
                        TelephoneNumber = "01283 564340",
                        ContractStartDate = new DateTime(2018, 4, 30),
                        ContractEndDate = new DateTime(2018, 4, 30),
                        Details = "",
                        SiteSurveyComplete = new DateTime(2018, 4, 30),
                        AssignmentInstructionsIssued = new DateTime(2018, 4, 30),
                        RiskAssessmentIssued = new DateTime(2018, 4, 30),
                        SINNumber = "2874",
                        DateCreated = new DateTime(2023, 2, 2, 15, 57, 0),
                        CreatedBy = "system"
                    }
                };

                // Filter out sites that already exist
                var sitesToAdd = sites.Where(s => !existingSiteIds.Contains(s.SiteID)).ToList();

                if (!sitesToAdd.Any())
                {
                    _logger.LogInformation("All sites already exist, skipping seeding");
                    return;
                }

                // Use raw SQL to enable IDENTITY_INSERT and insert the sites
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                try
                {
                    foreach (var site in sitesToAdd)
                    {
                        var insertSql = $@"
                            SET IDENTITY_INSERT Sites ON;
                            INSERT INTO Sites (
                                SiteID, fkCustomerID, fkRegionID, CoreSiteYN, LocationName, SINNumber, 
                                LocationType, BuildingName, NumberandStreet, VillageOrSuburb, Town, 
                                County, Postcode, TelephoneNumber, ContractStartDate, ContractEndDate, 
                                Details, SiteSurveyComplete, AssignmentInstructionsIssued, RiskAssessmentIssued,
                                RecordIsDeletedYN, DateCreated, CreatedBy, DateModified, ModifiedBy
                            ) VALUES (
                                {site.SiteID}, {site.fkCustomerID}, {site.fkRegionID}, {(site.CoreSiteYN ? 1 : 0)}, 
                                '{site.LocationName}', {(string.IsNullOrEmpty(site.SINNumber) ? "NULL" : $"'{site.SINNumber}'")}, 
                                {(string.IsNullOrEmpty(site.LocationType) ? "NULL" : $"'{site.LocationType}'")}, 
                                {(string.IsNullOrEmpty(site.BuildingName) ? "NULL" : $"'{site.BuildingName}'")}, 
                                {(string.IsNullOrEmpty(site.NumberandStreet) ? "NULL" : $"'{site.NumberandStreet}'")}, 
                                {(string.IsNullOrEmpty(site.VillageOrSuburb) ? "NULL" : $"'{site.VillageOrSuburb}'")}, 
                                {(string.IsNullOrEmpty(site.Town) ? "NULL" : $"'{site.Town}'")}, 
                                {(string.IsNullOrEmpty(site.County) ? "NULL" : $"'{site.County}'")}, 
                                {(string.IsNullOrEmpty(site.Postcode) ? "NULL" : $"'{site.Postcode}'")}, 
                                {(string.IsNullOrEmpty(site.TelephoneNumber) ? "NULL" : $"'{site.TelephoneNumber}'")}, 
                                '{site.ContractStartDate:yyyy-MM-dd HH:mm:ss}', 
                                '{site.ContractEndDate:yyyy-MM-dd HH:mm:ss}', 
                                {(string.IsNullOrEmpty(site.Details) ? "NULL" : $"'{site.Details}'")}, 
                                '{site.SiteSurveyComplete:yyyy-MM-dd HH:mm:ss}', 
                                '{site.AssignmentInstructionsIssued:yyyy-MM-dd HH:mm:ss}', 
                                '{site.RiskAssessmentIssued:yyyy-MM-dd HH:mm:ss}',
                                0, '{site.DateCreated:yyyy-MM-dd HH:mm:ss}', '{site.CreatedBy}', NULL, NULL
                            );
                            SET IDENTITY_INSERT Sites OFF;";

                        using var command = connection.CreateCommand();
                        command.CommandText = insertSql;
                        await command.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("Successfully seeded {Count} new sites", sitesToAdd.Count);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sites");
                throw;
            }
        }
    }
}
