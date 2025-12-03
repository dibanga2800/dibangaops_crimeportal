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
    [Authorize(Roles = "administrator")] // Only administrators can import products
    public class ProductImportController : ControllerBase
    {
        private readonly IExcelImportService _excelImportService;
        private readonly ILogger<ProductImportController> _logger;

        public ProductImportController(
            IExcelImportService excelImportService, 
            ILogger<ProductImportController> logger)
        {
            _excelImportService = excelImportService;
            _logger = logger;
        }

        /// <summary>
        /// Import products from Excel file
        /// </summary>
        [HttpPost("excel")]
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
                    Message = "An error occurred while importing products",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Import products from local Excel file path (for development/testing)
        /// </summary>
        [HttpPost("excel/path")]
        [AllowAnonymous] // Temporary for development - remove in production
        public async Task<ActionResult<ApiResponseDto<ImportResultDto>>> ImportFromExcelPath(
            [FromBody] ExcelImportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
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
                    Message = $"Excel file not found: {request.FilePath}",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                return StatusCode(500, new ApiResponseDto<ImportResultDto>
                {
                    Success = false,
                    Message = "An error occurred while importing products",
                    Errors = new List<string> { ex.Message }
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

