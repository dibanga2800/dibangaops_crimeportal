#nullable enable

namespace AIPBackend.Services
{
	/// <summary>
	/// Service interface for local file storage operations
	/// </summary>
	public interface IFileStorageService
	{
		Task<string> SaveFileAsync(IFormFile file, string folderName, string? subfolder = null);
		Task<bool> DeleteFileAsync(string filePath);
		string GetFileUrl(string fileName, string folderName, string? subfolder = null);
		Task<bool> FileExistsAsync(string filePath);
	}
}

