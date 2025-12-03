#nullable enable

namespace AIPBackend.Services
{
    public interface IExcelImportService
    {
        Task<int> ImportProductsFromExcelAsync(string filePath, string createdBy, CancellationToken cancellationToken = default);
    }
}

