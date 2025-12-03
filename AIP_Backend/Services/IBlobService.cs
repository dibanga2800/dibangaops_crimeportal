namespace AIPBackend.Services
{
    public interface IBlobService
    {
        Task<string> UploadBlobAsync(IFormFile file, string containerName);
        Task<string> GetBlobUrlAsync(string blobName, string containerName);
        Task DeleteBlobAsync(string blobName, string containerName);
        
    }
}
