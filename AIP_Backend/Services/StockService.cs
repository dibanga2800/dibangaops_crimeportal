using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _db;
        private readonly IStockEmailService _stockEmailService;
        private readonly ILogger<StockService> _logger;

        public StockService(ApplicationDbContext db, IStockEmailService stockEmailService, ILogger<StockService> logger)
        {
            _db = db;
            _stockEmailService = stockEmailService;
            _logger = logger;
        }

        public async Task<List<StockItemDto>> GetAllAsync()
        {
            return await _db.StockItems
                .OrderByDescending(s => s.DateCreated)
                .Select(s => new StockItemDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Quantity = s.Quantity,
                    MinimumStock = s.MinimumStock,
                    Category = s.Category,
                    Status = s.Status,
                    Description = s.Description,
                    NumberAdded = s.NumberAdded,
                    Date = s.Date,
                    NumberIssued = s.NumberIssued,
                    IssuedBy = s.IssuedBy,
                    DateCreated = s.DateCreated,
                    DateModified = s.DateModified
                })
                .ToListAsync();
        }

        public async Task<StockItemDto?> GetByIdAsync(int id)
        {
            return await _db.StockItems
                .Where(s => s.Id == id)
                .Select(s => new StockItemDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Quantity = s.Quantity,
                    MinimumStock = s.MinimumStock,
                    Category = s.Category,
                    Status = s.Status,
                    Description = s.Description,
                    NumberAdded = s.NumberAdded,
                    Date = s.Date,
                    NumberIssued = s.NumberIssued,
                    IssuedBy = s.IssuedBy,
                    DateCreated = s.DateCreated,
                    DateModified = s.DateModified
                })
                .FirstOrDefaultAsync();
        }

        public async Task<StockItemDto> CreateAsync(StockCreateRequestDto dto)
        {
            var status = dto.Quantity == 0 ? "Out of Stock" : dto.Quantity <= dto.MinimumStock ? "Low Stock" : "In Stock";
            var entity = new StockItem
            {
                Name = dto.Name,
                Quantity = dto.Quantity,
                MinimumStock = dto.MinimumStock,
                Category = dto.Category,
                Status = status,
                Description = dto.Description,
                NumberAdded = dto.NumberAdded,
                Date = dto.Date,
                NumberIssued = dto.NumberIssued,
                IssuedBy = dto.IssuedBy,
                DateCreated = DateTime.UtcNow
            };

            _db.StockItems.Add(entity);
            await _db.SaveChangesAsync();

            // Send email notifications based on stock status
            await SendStockNotificationsAsync(entity);

            return new StockItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Quantity = entity.Quantity,
                MinimumStock = entity.MinimumStock,
                Category = entity.Category,
                Status = entity.Status,
                Description = entity.Description,
                NumberAdded = entity.NumberAdded,
                Date = entity.Date,
                NumberIssued = entity.NumberIssued,
                IssuedBy = entity.IssuedBy,
                DateCreated = entity.DateCreated,
                DateModified = entity.DateModified
            };
        }

        public async Task<StockItemDto?> UpdateAsync(int id, StockUpdateRequestDto dto)
        {
            var entity = await _db.StockItems.FindAsync(id);
            if (entity == null) return null;

            // Store old values for comparison
            var oldQuantity = entity.Quantity;
            var oldStatus = entity.Status;
            var oldNumberAdded = entity.NumberAdded;
            var oldNumberIssued = entity.NumberIssued;

            entity.Name = dto.Name ?? entity.Name;
            entity.Quantity = dto.Quantity ?? entity.Quantity;
            entity.MinimumStock = dto.MinimumStock ?? entity.MinimumStock;
            entity.Category = dto.Category ?? entity.Category;
            entity.Description = dto.Description ?? entity.Description;
            entity.NumberAdded = dto.NumberAdded ?? entity.NumberAdded;
            entity.Date = dto.Date ?? entity.Date;
            entity.NumberIssued = dto.NumberIssued ?? entity.NumberIssued;
            entity.IssuedBy = dto.IssuedBy ?? entity.IssuedBy;

            // recompute status
            entity.Status = entity.Quantity == 0 ? "Out of Stock" : entity.Quantity <= entity.MinimumStock ? "Low Stock" : "In Stock";
            entity.DateModified = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Send email notifications based on changes
            await SendStockNotificationsAsync(entity, oldQuantity, oldStatus, oldNumberAdded, oldNumberIssued);

            return new StockItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Quantity = entity.Quantity,
                MinimumStock = entity.MinimumStock,
                Category = entity.Category,
                Status = entity.Status,
                Description = entity.Description,
                NumberAdded = entity.NumberAdded,
                Date = entity.Date,
                NumberIssued = entity.NumberIssued,
                IssuedBy = entity.IssuedBy,
                DateCreated = entity.DateCreated,
                DateModified = entity.DateModified
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.StockItems.FindAsync(id);
            if (entity == null) return false;
            _db.StockItems.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<StockItemDto?> IssueStockAsync(int id, StockIssueRequestDto dto)
        {
            var entity = await _db.StockItems.FindAsync(id);
            if (entity == null) return null;

            // Check if we have enough stock to issue
            if (entity.Quantity < dto.QuantityToIssue)
            {
                _logger.LogWarning("Insufficient stock to issue. Item: {ItemName}, Available: {Available}, Requested: {Requested}", 
                    entity.Name, entity.Quantity, dto.QuantityToIssue);
                return null;
            }

            // Store old values for comparison
            var oldQuantity = entity.Quantity;
            var oldStatus = entity.Status;
            var oldNumberIssued = entity.NumberIssued;

            // Update stock quantities
            entity.Quantity -= dto.QuantityToIssue;
            entity.NumberIssued += dto.QuantityToIssue;
            entity.IssuedBy = dto.IssuedBy;
            entity.Date = DateTime.UtcNow;

            // Update status based on new quantity
            entity.Status = entity.Quantity == 0 ? "Out of Stock" : entity.Quantity <= entity.MinimumStock ? "Low Stock" : "In Stock";
            entity.DateModified = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Send notifications based on stock level changes
            await SendStockIssuanceNotificationsAsync(entity, oldQuantity, oldStatus, oldNumberIssued, dto.QuantityToIssue, dto.IssuedBy);

            return new StockItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Quantity = entity.Quantity,
                MinimumStock = entity.MinimumStock,
                Category = entity.Category,
                Status = entity.Status,
                Description = entity.Description,
                NumberAdded = entity.NumberAdded,
                Date = entity.Date,
                NumberIssued = entity.NumberIssued,
                IssuedBy = entity.IssuedBy,
                DateCreated = entity.DateCreated,
                DateModified = entity.DateModified
            };
        }

        public async Task<StockItemDto?> AddStockAsync(int id, StockAddRequestDto dto)
        {
            var entity = await _db.StockItems.FindAsync(id);
            if (entity == null) return null;

            // Store old values for comparison
            var oldQuantity = entity.Quantity;
            var oldStatus = entity.Status;
            var oldNumberAdded = entity.NumberAdded;

            // Update stock quantities
            entity.Quantity += dto.QuantityToAdd;
            entity.NumberAdded += dto.QuantityToAdd;
            entity.IssuedBy = dto.AddedBy; // Using IssuedBy field for the person who added stock
            entity.Date = DateTime.UtcNow;

            // Update status based on new quantity
            entity.Status = entity.Quantity == 0 ? "Out of Stock" : entity.Quantity <= entity.MinimumStock ? "Low Stock" : "In Stock";
            entity.DateModified = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Send notifications for stock added
            await SendStockAddedNotificationsAsync(entity, oldQuantity, oldStatus, oldNumberAdded, dto.QuantityToAdd, dto.AddedBy);

            return new StockItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Quantity = entity.Quantity,
                MinimumStock = entity.MinimumStock,
                Category = entity.Category,
                Status = entity.Status,
                Description = entity.Description,
                NumberAdded = entity.NumberAdded,
                Date = entity.Date,
                NumberIssued = entity.NumberIssued,
                IssuedBy = entity.IssuedBy,
                DateCreated = entity.DateCreated,
                DateModified = entity.DateModified
            };
        }

        public async Task CheckAndNotifyLowStockAsync()
        {
            try
            {
                var lowStockItems = await _db.StockItems
                    .Where(s => s.Quantity <= s.MinimumStock)
                    .ToListAsync();

                foreach (var item in lowStockItems)
                {
                    if (item.Quantity == 0)
                    {
                        await _stockEmailService.SendOutOfStockNotificationAsync(item.Name, item.Category);
                    }
                    else
                    {
                        await _stockEmailService.SendLowStockNotificationAsync(item.Name, item.Quantity, item.MinimumStock, item.Category);
                    }
                }

                _logger.LogInformation("Low stock check completed. Found {Count} items with low stock levels.", lowStockItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check and notify low stock items");
            }
        }

        public async Task<List<StockItemDto>> GetLowStockItemsAsync()
        {
            return await _db.StockItems
                .Where(s => s.Quantity <= s.MinimumStock)
                .OrderBy(s => s.Quantity)
                .ThenBy(s => s.Name)
                .Select(s => new StockItemDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Quantity = s.Quantity,
                    MinimumStock = s.MinimumStock,
                    Category = s.Category,
                    Status = s.Status,
                    Description = s.Description,
                    NumberAdded = s.NumberAdded,
                    Date = s.Date,
                    NumberIssued = s.NumberIssued,
                    IssuedBy = s.IssuedBy,
                    DateCreated = s.DateCreated,
                    DateModified = s.DateModified
                })
                .ToListAsync();
        }

        private async Task SendStockNotificationsAsync(StockItem entity, int? oldQuantity = null, string? oldStatus = null, int? oldNumberAdded = null, int? oldNumberIssued = null)
        {
            try
            {
                // Check for stock level changes
                if (oldQuantity.HasValue && oldQuantity.Value > entity.MinimumStock && entity.Quantity <= entity.MinimumStock)
                {
                    // Stock has fallen below minimum level
                    if (entity.Quantity == 0)
                    {
                        await _stockEmailService.SendOutOfStockNotificationAsync(entity.Name, entity.Category);
                    }
                    else
                    {
                        await _stockEmailService.SendLowStockNotificationAsync(entity.Name, entity.Quantity, entity.MinimumStock, entity.Category);
                    }
                }

                // Check for stock added
                if (oldNumberAdded.HasValue && entity.NumberAdded > oldNumberAdded.Value)
                {
                    var quantityAdded = entity.NumberAdded - oldNumberAdded.Value;
                    await _stockEmailService.SendStockAddedNotificationAsync(entity.Name, quantityAdded, entity.IssuedBy);
                }

                // Check for stock issued
                if (oldNumberIssued.HasValue && entity.NumberIssued > oldNumberIssued.Value)
                {
                    var quantityIssued = entity.NumberIssued - oldNumberIssued.Value;
                    await _stockEmailService.SendStockIssuedNotificationAsync(entity.Name, quantityIssued, entity.IssuedBy);
                }

                // For new items, check if they start with low stock
                if (!oldQuantity.HasValue)
                {
                    if (entity.Quantity == 0)
                    {
                        await _stockEmailService.SendOutOfStockNotificationAsync(entity.Name, entity.Category);
                    }
                    else if (entity.Quantity <= entity.MinimumStock)
                    {
                        await _stockEmailService.SendLowStockNotificationAsync(entity.Name, entity.Quantity, entity.MinimumStock, entity.Category);
                    }

                    // Send notifications for initial stock added/issued
                    if (entity.NumberAdded > 0)
                    {
                        await _stockEmailService.SendStockAddedNotificationAsync(entity.Name, entity.NumberAdded, entity.IssuedBy);
                    }

                    if (entity.NumberIssued > 0)
                    {
                        await _stockEmailService.SendStockIssuedNotificationAsync(entity.Name, entity.NumberIssued, entity.IssuedBy);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send stock notifications for item: {ItemName}", entity.Name);
            }
        }

        private async Task SendStockIssuanceNotificationsAsync(StockItem entity, int oldQuantity, string oldStatus, int oldNumberIssued, int quantityIssued, string issuedBy)
        {
            try
            {
                _logger.LogInformation("Starting stock issuance notifications for item: {ItemName} (Old Qty: {OldQuantity}, New Qty: {NewQuantity}, Min: {MinimumStock})", 
                    entity.Name, oldQuantity, entity.Quantity, entity.MinimumStock);

                // Always send stock issued notification
                await _stockEmailService.SendStockIssuedNotificationAsync(entity.Name, quantityIssued, issuedBy);
                _logger.LogInformation("Stock issued notification sent for item: {ItemName}", entity.Name);

                // Check if stock level has dropped below minimum threshold
                if (oldQuantity > entity.MinimumStock && entity.Quantity <= entity.MinimumStock)
                {
                    _logger.LogWarning("Stock level dropped below minimum for item: {ItemName} (Old: {OldQuantity}, New: {NewQuantity}, Min: {MinimumStock})", 
                        entity.Name, oldQuantity, entity.Quantity, entity.MinimumStock);

                    if (entity.Quantity == 0)
                    {
                        await _stockEmailService.SendOutOfStockNotificationAsync(entity.Name, entity.Category);
                        _logger.LogWarning("Out of stock notification sent for item: {ItemName}", entity.Name);
                    }
                    else
                    {
                        await _stockEmailService.SendLowStockNotificationAsync(entity.Name, entity.Quantity, entity.MinimumStock, entity.Category);
                        _logger.LogWarning("Low stock notification sent for item: {ItemName} (Qty: {Quantity}, Min: {MinimumStock})", 
                            entity.Name, entity.Quantity, entity.MinimumStock);
                    }
                }

                _logger.LogInformation("Stock issuance notifications completed for item: {ItemName} (Issued: {QuantityIssued}, New Quantity: {NewQuantity})", 
                    entity.Name, quantityIssued, entity.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send stock issuance notifications for item: {ItemName}", entity.Name);
            }
        }

        private async Task SendStockAddedNotificationsAsync(StockItem entity, int oldQuantity, string oldStatus, int oldNumberAdded, int quantityAdded, string addedBy)
        {
            try
            {
                // Always send stock added notification
                await _stockEmailService.SendStockAddedNotificationAsync(entity.Name, quantityAdded, addedBy);

                // Check if stock level has risen above minimum threshold (no longer low stock)
                if (oldQuantity <= entity.MinimumStock && entity.Quantity > entity.MinimumStock)
                {
                    _logger.LogInformation("Stock level restored above minimum for item: {ItemName} (New Quantity: {NewQuantity}, Minimum: {MinimumStock})", 
                        entity.Name, entity.Quantity, entity.MinimumStock);
                }

                _logger.LogInformation("Stock addition notifications sent for item: {ItemName} (Added: {QuantityAdded}, New Quantity: {NewQuantity})", 
                    entity.Name, quantityAdded, entity.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send stock addition notifications for item: {ItemName}", entity.Name);
            }
        }
    }
}


