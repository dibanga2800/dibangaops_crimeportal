#nullable enable

namespace AIPBackend.Services
{
    public interface IStockEmailService
    {
        Task SendLowStockNotificationAsync(string itemName, int currentQuantity, int minimumStock, string category);
        Task SendOutOfStockNotificationAsync(string itemName, string category);
        Task SendStockAddedNotificationAsync(string itemName, int quantityAdded, string issuedBy);
        Task SendStockIssuedNotificationAsync(string itemName, int quantityIssued, string issuedBy);
    }
}
