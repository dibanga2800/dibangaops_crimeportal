using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IStockService
    {
        Task<List<StockItemDto>> GetAllAsync();
        Task<StockItemDto?> GetByIdAsync(int id);
        Task<StockItemDto> CreateAsync(StockCreateRequestDto dto);
        Task<StockItemDto?> UpdateAsync(int id, StockUpdateRequestDto dto);
        Task<bool> DeleteAsync(int id);
        Task CheckAndNotifyLowStockAsync();
        Task<StockItemDto?> IssueStockAsync(int id, StockIssueRequestDto dto);
        Task<StockItemDto?> AddStockAsync(int id, StockAddRequestDto dto);
        Task<List<StockItemDto>> GetLowStockItemsAsync();
    }
}


