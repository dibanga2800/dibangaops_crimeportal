
using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Services;
using AIPBackend.Repositories;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configure for IIS deployment
// When running under IIS, the ASP.NET Core Module handles port binding
// Check if running under IIS by looking for the IIS environment variables
var isRunningUnderIIS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTP_PORT")) ||
						 !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPS_PORT")) ||
						 !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_PORT")) ||
						 !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_TOKEN"));

if (isRunningUnderIIS)
{
	// When running under IIS, don't configure Kestrel to listen on specific ports
	// IIS will handle the binding through the ASP.NET Core Module
	builder.WebHost.UseIISIntegration();
	
	// Clear any default URL bindings - IIS will provide the binding
	// This prevents the "Failed to bind to address" error
	builder.WebHost.UseUrls(); // Empty means no explicit binding, IIS handles it
}
else if (builder.Environment.IsProduction())
{
	// In Production but not under IIS (shouldn't happen, but handle it)
	// Still use IIS integration in case it's being called incorrectly
	builder.WebHost.UseIISIntegration();
	builder.WebHost.UseUrls(); // Don't bind to specific ports
}
else
{
	// In development, use default Kestrel configuration
	// This allows the app to run standalone with dotnet run
}

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDbConnection"));
});

builder.Services.AddSingleton(u => new BlobServiceClient(
    builder.Configuration.GetConnectionString("StorageAccount")));
builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddHttpClient();

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero,
        // Map role claims from JWT token
        RoleClaimType = ClaimTypes.Role
    };
});

// Configure Authorization - 4-role model: admin, manager, security-officer, store
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("administrator"));
    options.AddPolicy("ManagerAndAbove", policy => policy.RequireRole("administrator", "manager"));
    options.AddPolicy("AllRoles", policy => policy.RequireRole("administrator", "manager", "security-officer", "store"));
});

// Register Repositories
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IHolidayRequestRepository, HolidayRequestRepository>();
builder.Services.AddScoped<IBankHolidayRepository, BankHolidayRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();

// Register Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ILookupTableRepository, LookupTableRepository>();
builder.Services.AddScoped<ILookupTableService, LookupTableService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<IPageAccessService, PageAccessService>();
builder.Services.AddScoped<ICustomerPageAccessService, CustomerPageAccessService>();
builder.Services.AddScoped<IDailyOccurrenceBookService, DailyOccurrenceBookService>();
builder.Services.AddScoped<IHolidayRequestService, HolidayRequestService>();
builder.Services.AddScoped<IBankHolidayService, BankHolidayService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHolidayEmailService, HolidayEmailService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockEmailService, StockEmailService>();
builder.Services.AddScoped<ICustomerAssignmentService, CustomerAssignmentService>();
builder.Services.AddScoped<IUserSoftDeleteService, UserSoftDeleteService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IDailyActivityReportService, DailyActivityReportService>();
builder.Services.AddScoped<IActionCalendarEmailService, ActionCalendarEmailService>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
// AI classification: Azure OpenAI with rule-based fallback
builder.Services.Configure<AzureOpenAiOptions>(
	builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.AddHttpClient<IAzureOpenAiClient, AzureOpenAiClient>();
builder.Services.AddScoped<RuleBasedIncidentClassifier>();
builder.Services.AddScoped<IIncidentClassifier, AzureOpenAiIncidentClassifier>();
builder.Services.AddScoped<IIncidentAnalyticsService, IncidentAnalyticsService>();
builder.Services.AddScoped<IEvidenceService, EvidenceService>();
builder.Services.AddScoped<IAlertEscalationService, AlertEscalationService>();
builder.Services.AddScoped<ILoginProtectionService, LoginProtectionService>();
builder.Services.AddScoped<IOffenderRecognitionService, OffenderRecognitionService>();
builder.Services.AddScoped<IIncidentPatternService, IncidentPatternService>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.Configure<StockNotificationSettings>(
	builder.Configuration.GetSection("StockNotifications"));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // In development, allow all localhost origins
                policy.SetIsOriginAllowed(origin => 
                    {
                        if (string.IsNullOrEmpty(origin)) return false;
                        try
                        {
                            var uri = new Uri(origin);
                            return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1";
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
            else
            {
				// In production, use specific origins
				var allowedOrigins = new List<string>
				{
					"https://coop-aip-ui.vercel.app",  // Production domain
					"https://coop-aip-ui-*.vercel.app"  // Preview deployments
				};
				
				// Add custom domain if you have one
				var customDomain = builder.Configuration["FrontendUrl"];
				if (!string.IsNullOrEmpty(customDomain))
				{
					allowedOrigins.Add(customDomain);
				}
				
				policy.SetIsOriginAllowed(origin =>
					{
						if (string.IsNullOrEmpty(origin)) return false;
						
						// Exact match
						if (allowedOrigins.Any(allowed => allowed == origin))
							return true;
						
						// Allow any Vercel domain (for testing - can be narrowed down later)
						// This covers all Vercel deployments: production, preview, and branch deployments
						if (origin.StartsWith("https://") && origin.EndsWith(".vercel.app"))
						{
							return true;
						}
						
						return false;
					})
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DibangOps API",
        Version = "v1",
        Description = "DibangOps\u2122 \u2014 AI-Driven Enterprise Security Intelligence Platform API"
    });
    
    // Add JWT Bearer token authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Handle nullable reference types and schema generation
    options.UseAllOfToExtendReferenceSchemas();
    options.UseOneOfForPolymorphism();
    options.UseAllOfForInheritance();
    
    // Add custom operation filter for file uploads
    options.OperationFilter<AIPBackend.Filters.FileUploadOperationFilter>();
});

var app = builder.Build();

// Ensure page access is initialized on startup (database-first approach)
// Run initialization in background after app starts
_ = Task.Run(async () =>
{
    // Wait for app to be fully ready
    await Task.Delay(3000);
    
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("=== STARTING PAGE ACCESS INITIALIZATION ===");

            // Migrate User_Roles lookup table to 3-tier model (runs on every startup, idempotent)
            var dataSeedingService = services.GetRequiredService<IDataSeedingService>();
            await dataSeedingService.MigrateUserRolesAsync();
            
            var pageAccessService = services.GetRequiredService<IPageAccessService>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            
            // Try to get an admin user, or use null (FK allows nulls now)
            var adminUser = await userManager.FindByEmailAsync("admin@advantageone.com");
            var userId = adminUser?.Id ?? null;
            
            logger.LogInformation("Admin user found: {Found}, UserId: {UserId}", adminUser != null, userId ?? "null");
            
            // Initialize pages - this is idempotent and safe to call multiple times
            logger.LogInformation("Calling InitializeDefaultPageAccessAsync...");
            var result = await pageAccessService.InitializeDefaultPageAccessAsync(userId ?? "System");
            
            logger.LogInformation("=== PAGE ACCESS INITIALIZATION COMPLETED: {Result} ===", result);
            
            // Verify pages were created
            var context = services.GetRequiredService<ApplicationDbContext>();
            var pageCount = await context.PageAccesses.CountAsync();
            logger.LogInformation("Total pages in database after initialization: {Count}", pageCount);
            
            if (pageCount == 0)
            {
                logger.LogWarning("WARNING: No pages found after initialization! This may indicate an error.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "=== ERROR DURING PAGE ACCESS INITIALIZATION ===");
            logger.LogError("Exception Type: {Type}", ex.GetType().Name);
            logger.LogError("Exception Message: {Message}", ex.Message);
            logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                logger.LogError("Inner Exception: {InnerMessage}", ex.InnerException.Message);
            }
        }
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Don't force HTTPS redirect in development to avoid CORS issues
}
else
{
    // Enable Swagger in Production for API documentation
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

// Use CORS
app.UseCors("AllowSpecificOrigin");

// Enable static file serving for uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
	Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(uploadsPath),
	RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
