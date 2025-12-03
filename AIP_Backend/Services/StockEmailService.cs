#nullable enable

using AIPBackend.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	public class StockEmailService : IStockEmailService
	{
		private readonly IEmailService _emailService;
		private readonly ILogger<StockEmailService> _logger;
		private readonly string _operationsEmail;
		private readonly string _fromName;
		private const string DEFAULT_OPERATIONS_EMAIL = "ops@advantage1.co.uk";
		private const string DEFAULT_FROM_NAME = "Advantage One Security - Stock Management";

		public StockEmailService(
			IEmailService emailService,
			ILogger<StockEmailService> logger,
			IOptions<StockNotificationSettings> stockNotificationOptions)
		{
			_emailService = emailService;
			_logger = logger;
			var settings = stockNotificationOptions.Value ?? new StockNotificationSettings();

			_operationsEmail = string.IsNullOrWhiteSpace(settings.OperationsEmail)
				? DEFAULT_OPERATIONS_EMAIL
				: settings.OperationsEmail;
			_fromName = string.IsNullOrWhiteSpace(settings.FromName)
				? DEFAULT_FROM_NAME
				: settings.FromName;

			if (_operationsEmail == DEFAULT_OPERATIONS_EMAIL && string.IsNullOrWhiteSpace(settings.OperationsEmail))
			{
				_logger.LogWarning("StockNotifications:OperationsEmail not configured. Defaulting to {OperationsEmail}", DEFAULT_OPERATIONS_EMAIL);
			}

			if (_fromName == DEFAULT_FROM_NAME && string.IsNullOrWhiteSpace(settings.FromName))
			{
				_logger.LogWarning("StockNotifications:FromName not configured. Defaulting to {FromName}", DEFAULT_FROM_NAME);
			}
		}

		public async Task SendLowStockNotificationAsync(string itemName, int currentQuantity, int minimumStock, string category)
		{
			try
			{
				var subject = $"⚠️ LOW STOCK ALERT: {itemName}";
				var body = GenerateLowStockEmailBody(itemName, currentQuantity, minimumStock, category);
				
				await _emailService.SendEmailAsync(_operationsEmail, subject, body, fromName: _fromName);
                
				_logger.LogInformation("Low stock notification sent for item: {ItemName} (Current: {CurrentQuantity}, Minimum: {MinimumStock})",
					itemName, currentQuantity, minimumStock);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send low stock notification for item: {ItemName}", itemName);
			}
		}

		public async Task SendOutOfStockNotificationAsync(string itemName, string category)
		{
			try
			{
				var subject = $"🚨 OUT OF STOCK: {itemName}";
				var body = GenerateOutOfStockEmailBody(itemName, category);
				
				await _emailService.SendEmailAsync(_operationsEmail, subject, body, fromName: _fromName);
                
				_logger.LogInformation("Out of stock notification sent for item: {ItemName}", itemName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send out of stock notification for item: {ItemName}", itemName);
			}
		}

		public async Task SendStockAddedNotificationAsync(string itemName, int quantityAdded, string issuedBy)
		{
			try
			{
				var subject = $"📦 STOCK ADDED: {itemName}";
				var body = GenerateStockAddedEmailBody(itemName, quantityAdded, issuedBy);
				
				await _emailService.SendEmailAsync(_operationsEmail, subject, body, fromName: _fromName);
                
				_logger.LogInformation("Stock added notification sent for item: {ItemName} (Quantity: {QuantityAdded}, By: {IssuedBy})",
					itemName, quantityAdded, issuedBy);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send stock added notification for item: {ItemName}", itemName);
			}
		}

		public async Task SendStockIssuedNotificationAsync(string itemName, int quantityIssued, string issuedBy)
		{
			try
			{
				var subject = $"📤 STOCK ISSUED: {itemName}";
				var body = GenerateStockIssuedEmailBody(itemName, quantityIssued, issuedBy);
				
				await _emailService.SendEmailAsync(_operationsEmail, subject, body, fromName: _fromName);
                
				_logger.LogInformation("Stock issued notification sent for item: {ItemName} (Quantity: {QuantityIssued}, By: {IssuedBy})",
					itemName, quantityIssued, issuedBy);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send stock issued notification for item: {ItemName}", itemName);
			}
		}

        private string GenerateLowStockEmailBody(string itemName, int currentQuantity, int minimumStock, string category)
        {
            return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .alert {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; }}
        .header {{ background-color: #f8d7da; color: #721c24; padding: 10px; border-radius: 5px; margin-bottom: 15px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
        .urgent {{ color: #dc3545; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>⚠️ LOW STOCK ALERT</h2>
    </div>
    
    <div class='alert'>
        <p><strong>Item:</strong> {itemName}</p>
        <p><strong>Category:</strong> {category}</p>
        <p><strong>Current Stock:</strong> <span class='urgent'>{currentQuantity}</span></p>
        <p><strong>Minimum Stock Level:</strong> {minimumStock}</p>
        <p><strong>Status:</strong> <span class='urgent'>LOW STOCK</span></p>
    </div>
    
    <div class='details'>
        <p>This item has fallen below the minimum stock level and requires immediate attention.</p>
        <p><strong>Action Required:</strong> Please reorder this item to maintain adequate stock levels.</p>
        <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <p>This is an automated notification from the AIP Stock Management System.</p>
</body>
</html>";
        }

        private string GenerateOutOfStockEmailBody(string itemName, string category)
        {
            return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .alert {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; }}
        .header {{ background-color: #dc3545; color: white; padding: 10px; border-radius: 5px; margin-bottom: 15px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
        .urgent {{ color: #dc3545; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>🚨 OUT OF STOCK</h2>
    </div>
    
    <div class='alert'>
        <p><strong>Item:</strong> {itemName}</p>
        <p><strong>Category:</strong> {category}</p>
        <p><strong>Current Stock:</strong> <span class='urgent'>0</span></p>
        <p><strong>Status:</strong> <span class='urgent'>OUT OF STOCK</span></p>
    </div>
    
    <div class='details'>
        <p>This item is completely out of stock and requires URGENT attention.</p>
        <p><strong>Action Required:</strong> Please reorder this item immediately to prevent service disruption.</p>
        <p><strong>Priority:</strong> <span class='urgent'>HIGH</span></p>
        <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <p>This is an automated notification from the AIP Stock Management System.</p>
</body>
</html>";
        }

        private string GenerateStockAddedEmailBody(string itemName, int quantityAdded, string issuedBy)
        {
            return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .alert {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; }}
        .header {{ background-color: #28a745; color: white; padding: 10px; border-radius: 5px; margin-bottom: 15px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>📦 STOCK ADDED</h2>
    </div>
    
    <div class='alert'>
        <p><strong>Item:</strong> {itemName}</p>
        <p><strong>Quantity Added:</strong> {quantityAdded}</p>
        <p><strong>Added By:</strong> {issuedBy}</p>
        <p><strong>Status:</strong> Stock replenished</p>
    </div>
    
    <div class='details'>
        <p>Stock has been successfully added to the inventory.</p>
        <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <p>This is an automated notification from the AIP Stock Management System.</p>
</body>
</html>";
        }

        private string GenerateStockIssuedEmailBody(string itemName, int quantityIssued, string issuedBy)
        {
            return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .alert {{ background-color: #cce5ff; border: 1px solid #b3d9ff; padding: 15px; border-radius: 5px; }}
        .header {{ background-color: #007bff; color: white; padding: 10px; border-radius: 5px; margin-bottom: 15px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>📤 STOCK ISSUED</h2>
    </div>
    
    <div class='alert'>
        <p><strong>Item:</strong> {itemName}</p>
        <p><strong>Quantity Issued:</strong> {quantityIssued}</p>
        <p><strong>Issued By:</strong> {issuedBy}</p>
        <p><strong>Status:</strong> Stock issued</p>
    </div>
    
    <div class='details'>
        <p>Stock has been issued from the inventory.</p>
        <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
    
    <p>This is an automated notification from the AIP Stock Management System.</p>
</body>
</html>";
        }
    }
}
