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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var enableSwaggerInProduction = builder.Configuration.GetValue<bool>("Security:EnableSwaggerInProduction");
var enableSwaggerUiInProduction = builder.Configuration.GetValue<bool>("Security:EnableSwaggerUiInProduction");
var allowWildcardVercelOrigins = builder.Configuration.GetValue<bool>("Security:AllowWildcardVercelOrigins");
var enableRateLimiting = builder.Configuration.GetValue<bool?>("Security:EnableRateLimiting") ?? builder.Environment.IsProduction();
var globalRateLimitPermitLimit = builder.Configuration.GetValue<int?>("Security:GlobalRateLimitPermitLimit") ?? 120;
var globalRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("Security:GlobalRateLimitWindowSeconds") ?? 60;
var enableUploadsStaticFilesInProduction = builder.Configuration.GetValue<bool>("Security:EnableUploadsStaticFilesInProduction");
var requireAuthForUploads = builder.Configuration.GetValue<bool?>("Security:RequireAuthForUploads") ?? !builder.Environment.IsDevelopment();
var runMigrationsOnStartup = builder.Configuration.GetValue<bool?>("Security:RunMigrationsOnStartup") ?? !builder.Environment.IsEnvironment("Testing");
var runPageAccessInitializationOnStartup = builder.Configuration.GetValue<bool?>("Security:RunPageAccessInitializationOnStartup") ?? !builder.Environment.IsEnvironment("Testing");
var maxMultipartBodyLengthBytes =
	builder.Configuration.GetValue<long?>("Security:MaxMultipartBodyLengthBytes") ?? 10 * 1024 * 1024;

AddDevelopmentLocalAppSettings(builder);

static void AddDevelopmentLocalAppSettings(WebApplicationBuilder webBuilder)
{
	if (!webBuilder.Environment.IsDevelopment())
	{
		return;
	}

	const string localFileName = "appsettings.Local.json";

	// Gitignored local secrets; overrides appsettings.json and appsettings.Development.json.
	webBuilder.Configuration.AddJsonFile(localFileName, optional: true, reloadOnChange: true);
}

static bool IsMissingOrPlaceholderStorageConnectionString(string? value)
{
	return string.IsNullOrWhiteSpace(value) ||
		value.Contains("YOUR_STORAGE_ACCOUNT_KEY", StringComparison.OrdinalIgnoreCase);
}

static bool RequiresBlobStorage(string? mode)
{
	return mode?.Trim().ToLowerInvariant() is "blob" or "both";
}

static List<string> GetJwtSigningKeys(IConfiguration configuration)
{
	var primaryKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
	var keys = new List<string> { primaryKey };

	var previousKeySection = configuration.GetSection("Jwt:PreviousKeys");
	if (previousKeySection.Exists())
	{
		foreach (var child in previousKeySection.GetChildren())
		{
			if (!string.IsNullOrWhiteSpace(child.Value))
			{
				keys.Add(child.Value.Trim());
			}
		}
	}

	var previousKeyCsv = configuration["Jwt:PreviousKeysCsv"];
	if (!string.IsNullOrWhiteSpace(previousKeyCsv))
	{
		foreach (var item in previousKeyCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			keys.Add(item);
		}
	}

	return keys
		.Where(key => !string.IsNullOrWhiteSpace(key))
		.Distinct(StringComparer.Ordinal)
		.ToList();
}

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
    var defaultConnection =
        builder.Configuration.GetConnectionString("DefaultConnection") ??
        builder.Configuration.GetConnectionString("DefaultDbConnection");

    if (string.IsNullOrWhiteSpace(defaultConnection))
    {
        throw new InvalidOperationException("Missing SQL connection string. Configure ConnectionStrings:DefaultConnection (or legacy DefaultDbConnection).");
    }

    options.UseSqlServer(defaultConnection);
});

builder.Services.Configure<IncidentImageStorageOptions>(builder.Configuration.GetSection("IncidentImageStorage"));

var incidentImageStorageOptions =
	builder.Configuration.GetSection("IncidentImageStorage").Get<IncidentImageStorageOptions>()
	?? new IncidentImageStorageOptions();

var rawStorageConnectionString = builder.Configuration.GetConnectionString("StorageAccount");
string effectiveStorageConnectionString;

if (IsMissingOrPlaceholderStorageConnectionString(rawStorageConnectionString))
{
	if (RequiresBlobStorage(incidentImageStorageOptions.Mode) && !builder.Environment.IsDevelopment())
	{
		throw new InvalidOperationException(
			"Blob storage is enabled but ConnectionStrings:StorageAccount is missing or still uses a placeholder value.");
	}

	effectiveStorageConnectionString = "UseDevelopmentStorage=true";
}
else
{
	effectiveStorageConnectionString = rawStorageConnectionString!;
}

builder.Services.AddSingleton(u => new BlobServiceClient(effectiveStorageConnectionString));
builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IIncidentImageStorageService, IncidentImageStorageService>();
builder.Services.AddHttpClient();
builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = maxMultipartBodyLengthBytes;
});

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
var jwtSigningKeys = GetJwtSigningKeys(builder.Configuration);
var securityKeys = jwtSigningKeys
	.Select(signingKey => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)))
	.Cast<SecurityKey>()
	.ToList();
if (securityKeys.Count == 0)
{
	throw new InvalidOperationException("No JWT signing keys were configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
	options.Events = new JwtBearerEvents
	{
		OnMessageReceived = context =>
		{
			if (!string.IsNullOrEmpty(context.Token))
			{
				return Task.CompletedTask;
			}

			var cookieService = context.HttpContext.RequestServices.GetRequiredService<IAuthCookieService>();
			context.Token = cookieService.GetAccessTokenFromRequest(context.Request);
			return Task.CompletedTask;
		}
	};
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = securityKeys[0],
        IssuerSigningKeys = securityKeys,
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

if (enableRateLimiting)
{
	builder.Services.AddRateLimiter(options =>
	{
		options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
		options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
		{
			var key =
				httpContext.User.Identity?.IsAuthenticated == true
					? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
					  httpContext.Connection.RemoteIpAddress?.ToString() ??
					  "authenticated-unknown"
					: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

			return RateLimitPartition.GetFixedWindowLimiter(
				partitionKey: key,
				factory: _ => new FixedWindowRateLimiterOptions
				{
					PermitLimit = globalRateLimitPermitLimit,
					Window = TimeSpan.FromSeconds(globalRateLimitWindowSeconds),
					QueueLimit = 0,
					QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
					AutoReplenishment = true
				});
		});
	});
}

// Register Repositories
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();

// Register Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ILookupTableRepository, LookupTableRepository>();
builder.Services.AddScoped<ILookupTableService, LookupTableService>();
builder.Services.Configure<AIPBackend.Options.AuthCookieOptions>(
	builder.Configuration.GetSection(AIPBackend.Options.AuthCookieOptions.SectionName));
builder.Services.AddSingleton<IAuthCookieService, AuthCookieService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<IPageAccessService, PageAccessService>();
builder.Services.AddScoped<ICustomerPageAccessService, CustomerPageAccessService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICustomerAssignmentService, CustomerAssignmentService>();
builder.Services.AddScoped<IUserSoftDeleteService, UserSoftDeleteService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IProductBarcodeCsvImportService, ProductBarcodeCsvImportService>();
builder.Services.AddScoped<IDailyActivityReportService, DailyActivityReportService>();
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
builder.Services.Configure<AzureFaceOptions>(
	builder.Configuration.GetSection("AzureFace"));
builder.Services.AddHttpClient<IAzureFaceClient, AzureFaceClient>();
builder.Services.Configure<InsightFaceOptions>(builder.Configuration.GetSection("InsightFace"));
builder.Services.AddHttpClient<IInsightFaceClient, InsightFaceClient>();
var useInsightFace = builder.Configuration.GetValue<bool>("InsightFace:Enabled");
if (useInsightFace)
	builder.Services.AddScoped<IOffenderRecognitionService, InsightFaceOffenderRecognitionService>();
else
	builder.Services.AddScoped<IOffenderRecognitionService, OffenderRecognitionService>();
builder.Services.AddScoped<IIncidentPatternService, IncidentPatternService>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
// Stock notifications removed with legacy stock module; configuration section reserved for future use.

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
					"https://www.dibangops.com",
					"https://dibangops.com"
				};
				
				// Add custom domain(s): comma-separated for apex + www (e.g. https://www.example.com,https://example.com)
				var customDomains = builder.Configuration["FrontendUrl"];
				if (!string.IsNullOrEmpty(customDomains))
				{
					foreach (var part in customDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						if (!string.IsNullOrEmpty(part))
							allowedOrigins.Add(part);
					}
				}

				allowedOrigins = allowedOrigins
					.Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
						string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				
				policy.SetIsOriginAllowed(origin =>
					{
						if (string.IsNullOrEmpty(origin)) return false;
						
						// Exact match
						if (allowedOrigins.Any(allowed => allowed == origin))
							return true;
						
						// Optional fallback for preview URLs. Keep disabled in production unless explicitly needed.
						if (allowWildcardVercelOrigins &&
							origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
							origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
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
builder.Services.AddProblemDetails();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DibangOps Crime Portal API",
        Version = "v1",
        Description = "DibangOps Crime Portal\u2122 \u2014 AI-Driven Enterprise Security Intelligence Platform API"
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

    // Stable schema IDs (avoids collisions from duplicate short type names across namespaces).
    options.CustomSchemaIds(type => type.FullName!.Replace('+', '.'));

    // Required for IFormFile / IFormFile? in [FromForm] DTOs (e.g. contact form) so swagger.json generates.
    options.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });

    // Avoid aggressive polymorphism/inheritance expansion — it frequently throws during
    // swagger.json generation for real-world DTO graphs (500 on GET /swagger/v1/swagger.json).
    options.UseAllOfToExtendReferenceSchemas();

    // Add custom operation filter for file uploads
    options.OperationFilter<AIPBackend.Filters.FileUploadOperationFilter>();
});

var app = builder.Build();
app.Logger.LogInformation("JWT validation key count configured: {ValidationKeyCount}", securityKeys.Count);

// Apply pending EF migrations (Azure SQL and local DBs are often empty until this runs).
if (runMigrationsOnStartup)
{
	using (var scope = app.Services.CreateScope())
	{
		var migrateLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		try
		{
			if (db.Database.IsRelational())
			{
				migrateLogger.LogInformation("Applying database migrations if needed...");
				db.Database.Migrate();
				migrateLogger.LogInformation("Database migrations complete.");
			}
			else
			{
				migrateLogger.LogInformation("Skipping database migrations because provider is non-relational.");
			}
		}
		catch (Exception ex)
		{
			migrateLogger.LogError(ex, "Database migration failed.");
			throw;
		}
	}
}

// Ensure page access is initialized on startup (database-first approach)
// Run initialization in background after app starts
if (runPageAccessInitializationOnStartup)
{
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
				using (var migrateScope = app.Services.CreateScope())
				{
					var dataSeedingService = migrateScope.ServiceProvider.GetRequiredService<IDataSeedingService>();
					await dataSeedingService.MigrateUserRolesAsync();
				}

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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Don't force HTTPS redirect in development to avoid CORS issues
}
else
{
	app.UseExceptionHandler(errorApp =>
	{
		errorApp.Run(async context =>
		{
			var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
			var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
			if (exceptionHandler?.Error != null)
			{
				logger.LogError(exceptionHandler.Error, "Unhandled exception for request {Method} {Path}",
					context.Request.Method,
					context.Request.Path.Value);
			}

			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.Response.ContentType = "application/problem+json";
			var problem = new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "An unexpected error occurred.",
				Type = "https://httpstatuses.com/500"
			};
			await context.Response.WriteAsJsonAsync(problem);
		});
	});

    if (enableSwaggerInProduction)
    {
		app.UseSwagger();
		if (enableSwaggerUiInProduction)
		{
			app.UseSwaggerUI();
		}
    }

	app.UseHsts();
    app.UseHttpsRedirection();
}

// Use CORS
app.UseCors("AllowSpecificOrigin");

if (enableRateLimiting)
{
	app.UseRateLimiter();
}

app.Use(async (context, next) =>
{
	context.Response.Headers["X-Content-Type-Options"] = "nosniff";
	context.Response.Headers["X-Frame-Options"] = "DENY";
	context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
	context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
	await next();
});

app.UseMiddleware<AIPBackend.Middleware.ForbiddenAccessExceptionMiddleware>();
app.UseMiddleware<AIPBackend.Middleware.CsrfValidationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

var enableUploadsStaticFiles = app.Environment.IsDevelopment() || enableUploadsStaticFilesInProduction;
if (enableUploadsStaticFiles)
{
	var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
	if (!Directory.Exists(uploadsPath))
	{
		Directory.CreateDirectory(uploadsPath);
	}

	app.UseWhen(
		context => context.Request.Path.StartsWithSegments("/uploads"),
		uploadsApp =>
		{
			if (requireAuthForUploads)
			{
				uploadsApp.Use(async (context, next) =>
				{
					if (context.User.Identity?.IsAuthenticated != true)
					{
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						return;
					}

					await next();
				});
			}

			uploadsApp.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(uploadsPath),
				RequestPath = "/uploads"
			});
		});
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program { }
