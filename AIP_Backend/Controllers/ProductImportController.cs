#nullable enable

using AIPBackend.Models.DTOs;
using AIPBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Per-action roles: Excel = administrators only; barcode CSV = administrators + managers
    public class ProductImportController : ControllerBase
    {
        private const long MaxImportFileSizeBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
        };
        private const long MaxBarcodeCsvFileSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedBarcodeCsvContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "text/csv",
            "application/csv",
            "text/plain",
            "application/octet-stream",
            "application/vnd.ms-excel"
        };

        private readonly IExcelImportService _excelImportService;
        private readonly IProductBarcodeCsvImportService _barcodeCsvImportService;
        private readonly ILogger<ProductImportController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ProductImportController(
            IExcelImportService excelImportService,
            IProductBarcodeCsvImportService barcodeCsvImportService,
            ILogger<ProductImportController> logger,
            IWebHostEnvironment environment)
        {
            _excelImportService = excelImportService;
            _barcodeCsvImportService = barcodeCsvImportService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Import products from Excel file
        /// </summary>
        [HttpPost("excel")]
        [Authorize(Roles = "administrator")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)] // Exclude from Swagger to avoid IFormFile generation issues
        public async Task<ActionResult<ApiResponseDto<ImportResultDto>>> ImportFromExcel(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _logger.LogInformation("Product import request by user {CurrentUserId}", currentUserId);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponseDto<ImportResultDto>
                    {
                        Success = false,
                        Message = "No file uploaded"
                    });
                }
                if (file.Length > MaxImportFileSizeBytes)
                {
                    return BadRequest(new ApiResponseDto<ImportResultDto>
                    {
                        Success = false,
                        Message = "File must be 10MB or less"
                    });
                }

                // Validate file type
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ApiResponseDto<ImportResultDto>
                    {
                        Success = false,
                        Message = "Invalid file type. Only Excel files (.xlsx, .xls) are allowed"
                    });
                }
                if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType.Trim()))
                {
                    return BadRequest(new ApiResponseDto<ImportResultDto>
                    {
                        Success = false,
                        Message = "Invalid content type for Excel import file"
                    });
                }

                // Save uploaded file temporarily
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);
                try
                {
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    // Import products from Excel
                    var importedCount = await _excelImportService.ImportProductsFromExcelAsync(
                        tempFilePath, 
                        currentUserId, 
                        cancellationToken);

                    return Ok(new ApiResponseDto<ImportResultDto>
                    {
                        Success = true,
                        Message = $"Successfully imported {importedCount} products from Excel file",
                        Data = new ImportResultDto
                        {
                            ImportedCount = importedCount,
                            FileName = file.FileName
                        }
                    });
                }
                finally
                {
                    // Clean up temporary file
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                return StatusCode(500, new ApiResponseDto<ImportResultDto>
                {
                    Success = false,
                    Message = "An error occurred while importing products"
                });
            }
        }

        /// <summary>
        /// Import or update products from barcode CSV (Barcode, Department, VMECode, ProductName, RetailPrice).
        /// </summary>
        [HttpPost("barcode-csv")]
        [Authorize(Roles = "administrator,manager")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxBarcodeCsvFileSizeBytes)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponseDto<BarcodeCsvImportResultDto>>> ImportBarcodeCsv(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _logger.LogInformation("Barcode CSV import request by user {CurrentUserId}", currentUserId);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = "No file uploaded"
                    });
                }

                if (file.Length > MaxBarcodeCsvFileSizeBytes)
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = "File must be 5MB or less"
                    });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".csv")
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = "Invalid file type. Only .csv files are allowed"
                    });
                }

                if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                    !AllowedBarcodeCsvContentTypes.Contains(file.ContentType.Trim()))
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = "Invalid content type for CSV import file"
                    });
                }

                await using var readStream = file.OpenReadStream();
                BarcodeCsvImportResultDto result;
                try
                {
                    result = await _barcodeCsvImportService.ImportAsync(
                        readStream,
                        file.FileName,
                        currentUserId,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new ApiResponseDto<BarcodeCsvImportResultDto>
                    {
                        Success = false,
                        Message = ex.Message
                    });
                }

                return Ok(new ApiResponseDto<BarcodeCsvImportResultDto>
                {
                    Success = result.ImportCompleted,
                    Message = BuildBarcodeImportSummaryMessage(result),
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing barcode CSV");
                return StatusCode(500, new ApiResponseDto<BarcodeCsvImportResultDto>
                {
                    Success = false,
                    Message = "An error occurred while importing the barcode CSV"
                });
            }
        }

        private static string BuildBarcodeImportSummaryMessage(BarcodeCsvImportResultDto result)
        {
            if (!result.ImportCompleted)
            {
                return result.ErrorMessage
                    ?? $"Import stopped partway: {result.CreatedCount} created, {result.UpdatedCount} updated before failure.";
            }

            return $"Import finished: {result.CreatedCount} created, {result.UpdatedCount} updated, " +
                   $"{result.InvalidRows} invalid/skipped rows, {result.ValidRows} valid rows processed.";
        }

        /// <summary>
        /// Import products from local Excel file path (for development/testing)
        /// </summary>
        [HttpPost("excel/path")]
        public async Task<ActionResult<ApiResponseDto<ImportResultDto>>> ImportFromExcelPath(
            [FromBody] ExcelImportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_environment.IsDevelopment())
                {
                    _logger.LogWarning("Blocked use of development-only import endpoint in non-development environment");
                    return NotFound();
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _logger.LogInformation("Product import from path request by user {CurrentUserId}", currentUserId);

                if (string.IsNullOrWhiteSpace(request.FilePath))
                {
                    return BadRequest(new ApiResponseDto<ImportResultDto>
                    {
                        Success = false,
                        Message = "File path is required"
                    });
                }

                // Import products from Excel
                var importedCount = await _excelImportService.ImportProductsFromExcelAsync(
                    request.FilePath, 
                    currentUserId, 
                    cancellationToken);

                return Ok(new ApiResponseDto<ImportResultDto>
                {
                    Success = true,
                    Message = $"Successfully imported {importedCount} products from Excel file",
                    Data = new ImportResultDto
                    {
                        ImportedCount = importedCount,
                        FileName = Path.GetFileName(request.FilePath)
                    }
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Excel file not found: {FilePath}", request.FilePath);
                return NotFound(new ApiResponseDto<ImportResultDto>
                {
                    Success = false,
                    Message = "Excel file not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                return StatusCode(500, new ApiResponseDto<ImportResultDto>
                {
                    Success = false,
                    Message = "An error occurred while importing products"
                });
            }
        }
    }

    public class ImportResultDto
    {
        public int ImportedCount { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class ExcelImportRequestDto
    {
        public string FilePath { get; set; } = string.Empty;
    }
}

