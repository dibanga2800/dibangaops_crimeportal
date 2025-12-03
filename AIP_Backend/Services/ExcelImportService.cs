#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace AIPBackend.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(ApplicationDbContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
            
            // Set EPPlus license context (free for non-commercial use)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<int> ImportProductsFromExcelAsync(string filePath, string createdBy, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            }

            var importedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0]; // Get first worksheet
                
                if (worksheet == null)
                {
                    throw new InvalidOperationException("No worksheets found in Excel file");
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                _logger.LogInformation("Found {RowCount} rows in Excel file", rowCount);

                // Find header row (assume first row contains headers)
                var headerRow = 1;
                var eanColumn = FindColumn(worksheet, headerRow, new[] { 
                    "EAN", "ean", "Barcode", "barcode", "EAN13", "ean13", "EAN-13", "ean-13",
                    "BARCODE", "BAR CODE", "Bar Code", "bar code", "Code", "code",
                    "GTIN", "gtin", "UPC", "upc", "Product Code", "product code",
                    "EANNUMBER", "EANNumber", "EAN Number", "ean number", "EAN_NUMBER", "ean_number"
                });
                // First, find L8NAME column specifically (it's the primary product name source)
                var l8NameColumn = FindColumn(worksheet, headerRow, new[] { 
                    "L8NAME", "L8Name", "l8name", "L8 NAME", "L8 Name", "l8 name",
                    "L8Name", "L8NAME", "L8_Name", "l8_name"
                });
                
                // Find SECTION column (will be stored in Section field in database)
                var sectionColumn = FindColumn(worksheet, headerRow, new[] { 
                    "SECTION", "Section", "section", "Section Name", "section name"
                });
                
                // Then find product name column - prioritize standard names first, then use L8NAME
                var productNameColumn = FindColumn(worksheet, headerRow, new[] { 
                    "ProductName", "productname", "Product Name", "product name", "Name", "name",
                    "Product", "product", "Item Name", "item name", "Item", "item",
                    "Description", "description", "Product Description", "product description"
                });
                
                // If no standard product name found, use L8NAME (which we already found above)
                if (productNameColumn == -1 && l8NameColumn != -1)
                {
                    productNameColumn = l8NameColumn;
                }
                var categoryColumn = FindColumn(worksheet, headerRow, new[] { 
                    "CATEGORY", "Category", "category", "Cat", "cat",
                    "Product Category", "product category"
                });
                var descriptionColumn = FindColumn(worksheet, headerRow, new[] { 
                    "Description", "description", "Desc", "desc", "DESCRIPTION",
                    "Product Description", "product description", "Details", "details"
                });
                var priceColumn = FindColumn(worksheet, headerRow, new[] { 
                    "Price", "price", "Cost", "cost", "PRICE", "COST",
                    "Unit Price", "unit price", "Unit Cost", "unit cost"
                });

                // Get all column headers for better error messages
                var availableColumns = new List<string>();
                if (worksheet.Dimension != null)
                {
                    for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerValue = worksheet.Cells[headerRow, col].Value?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            availableColumns.Add(headerValue);
                        }
                    }
                }

                if (eanColumn == -1)
                {
                    var availableColumnsStr = availableColumns.Any() 
                        ? $" Available columns: {string.Join(", ", availableColumns)}" 
                        : " No columns found in the first row.";
                    throw new InvalidOperationException(
                        $"EAN/Barcode column not found in Excel file.{availableColumnsStr} " +
                        "Please ensure your Excel file has a column named: EAN, Barcode, EAN13, Code, GTIN, UPC, or Product Code.");
                }

                // If still no product name column found, try other L columns as last resort
                if (productNameColumn == -1)
                {
                    // Try other L columns as fallback (but L8NAME should have been found already)
                    productNameColumn = FindColumn(worksheet, headerRow, new[] { 
                        "L6NAME", "L6Name", "l6name", "L6 NAME", "L6 Name", "l6 name",
                        "L5NAME", "L5Name", "l5name", "L5 NAME", "L5 Name", "l5 name",
                        "L4NAME", "L4Name", "l4name", "L4 NAME", "L4 Name", "l4 name",
                        "L3NAME", "L3Name", "l3name", "L3 NAME", "L3 Name", "l3 name"
                    });
                    
                    if (productNameColumn == -1)
                    {
                        var availableColumnsStr = availableColumns.Any() 
                            ? $" Available columns: {string.Join(", ", availableColumns)}" 
                            : " No columns found in the first row.";
                        throw new InvalidOperationException(
                            $"Product Name column not found in Excel file.{availableColumnsStr} " +
                            "Please ensure your Excel file has a column named: ProductName, Product Name, Name, Product, Item Name, Item, or L8NAME.");
                    }
                }
                
                // Ensure L8NAME column is found - if it wasn't found, use productNameColumn (which should be L8NAME)
                if (l8NameColumn == -1)
                {
                    l8NameColumn = productNameColumn; // This should be L8NAME if no standard ProductName column exists
                }
                
                // Note: Section field in database will store SECTION value from Excel

                var productsToAdd = new List<Product>();
                var existingEANsList = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.EAN)
                    .ToListAsync(cancellationToken);
                var existingEANs = existingEANsList.ToHashSet();

                // Start from row 2 (skip header row)
                for (var row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var eanValue = worksheet.Cells[row, eanColumn].Value?.ToString()?.Trim();
                        var productNameValue = worksheet.Cells[row, productNameColumn].Value?.ToString()?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(eanValue) || string.IsNullOrWhiteSpace(productNameValue))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Skip if EAN already exists
                        if (existingEANs.Contains(eanValue))
                        {
                            skippedCount++;
                            _logger.LogDebug("Skipping duplicate EAN: {EAN}", eanValue);
                            continue;
                        }

                        // Get SECTION value (will be stored in Section field in database)
                        var sectionValue = sectionColumn != -1 ? worksheet.Cells[row, sectionColumn].Value?.ToString()?.Trim() : null;
                        var categoryValue = categoryColumn != -1 ? worksheet.Cells[row, categoryColumn].Value?.ToString()?.Trim() : null;
                        var descriptionValue = descriptionColumn != -1 ? worksheet.Cells[row, descriptionColumn].Value?.ToString()?.Trim() : null;
                        
                        // Try to parse price
                        decimal? priceValue = null;
                        if (priceColumn != -1)
                        {
                            var priceString = worksheet.Cells[row, priceColumn].Value?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(priceString) && decimal.TryParse(priceString, out var parsedPrice))
                            {
                                priceValue = parsedPrice;
                            }
                        }

                        // Use CATEGORY column from Excel (no fallback)
                        var finalCategory = categoryValue;

                        // L8NAME from Excel is used as ProductName
                        // SECTION from Excel is stored in Section field in database
                        var product = new Product
                        {
                            EAN = eanValue,
                            ProductName = productNameValue, // This is L8NAME from Excel
                            Section = sectionValue, // Store SECTION from Excel in Section field
                            Category = finalCategory,
                            Description = descriptionValue,
                            Price = priceValue,
                            CreatedBy = createdBy,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        productsToAdd.Add(product);
                        existingEANs.Add(eanValue); // Track to avoid duplicates in batch

                        // Batch insert every 1000 records
                        if (productsToAdd.Count >= 1000)
                        {
                            _context.Products.AddRange(productsToAdd);
                            await _context.SaveChangesAsync(cancellationToken);
                            importedCount += productsToAdd.Count;
                            _logger.LogInformation("Imported {Count} products (total: {Total})", productsToAdd.Count, importedCount);
                            productsToAdd.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Error importing row {Row}: {Error}", row, ex.Message);
                    }
                }

                // Insert remaining products
                if (productsToAdd.Any())
                {
                    _context.Products.AddRange(productsToAdd);
                    await _context.SaveChangesAsync(cancellationToken);
                    importedCount += productsToAdd.Count;
                }

                _logger.LogInformation("Import completed. Imported: {Imported}, Skipped: {Skipped}, Errors: {Errors}", 
                    importedCount, skippedCount, errorCount);

                return importedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel file: {FilePath}", filePath);
                throw;
            }
        }

        private static int FindColumn(ExcelWorksheet worksheet, int headerRow, string[] possibleColumnNames)
        {
            if (worksheet.Dimension == null) return -1;

            for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var headerValue = worksheet.Cells[headerRow, col].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(headerValue)) continue;

                foreach (var possibleName in possibleColumnNames)
                {
                    if (headerValue.Equals(possibleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return col;
                    }
                }
            }

            return -1;
        }
    }
}

