#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service for storing files locally on server and network share
	/// </summary>
	public class FileStorageService : IFileStorageService
	{
		private readonly IWebHostEnvironment _environment;
		private readonly IConfiguration _configuration;
		private readonly ILogger<FileStorageService> _logger;
		private readonly string _networkShareRootPath;
		private readonly string _localUploadsRootPath;
		private readonly Dictionary<string, string> _networkFolderOverrides;

		public FileStorageService(
			IWebHostEnvironment environment,
			IConfiguration configuration,
			ILogger<FileStorageService> logger)
		{
			_environment = environment;
			_configuration = configuration;
			_logger = logger;

			var legacyNetworkPath = _configuration["FileStorage:NetworkSharePath"];
			var inferredRootFromLegacy = !string.IsNullOrWhiteSpace(legacyNetworkPath)
				? Path.GetDirectoryName(legacyNetworkPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
				: null;

			_networkShareRootPath = _configuration["FileStorage:NetworkShareRootPath"]
				?? inferredRootFromLegacy
				?? @"\\adv1server\AIPUploads";

			_localUploadsRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
			if (!Directory.Exists(_localUploadsRootPath))
			{
				Directory.CreateDirectory(_localUploadsRootPath);
				_logger.LogInformation("Created root local uploads directory: {Path}", _localUploadsRootPath);
			}

			_networkFolderOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["officer-support"] = !string.IsNullOrWhiteSpace(legacyNetworkPath)
					? legacyNetworkPath
					: Path.Combine(_networkShareRootPath, "Officer Support"),
				["manager-support"] = _configuration["FileStorage:ManagerSupportNetworkPath"]
					?? Path.Combine(_networkShareRootPath, "Manager Support")
			};
		}

		/// <summary>
		/// Save file to both network share and local app folder
		/// </summary>
		public async Task<string> SaveFileAsync(IFormFile file, string folderName, string? subfolder = null)
		{
			if (file == null || file.Length == 0)
			{
				throw new ArgumentException("File is empty or null", nameof(file));
			}

			if (string.IsNullOrWhiteSpace(folderName))
			{
				throw new ArgumentException("Folder name is required", nameof(folderName));
			}

			var fileExtension = Path.GetExtension(file.FileName);
			var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
			var folderSegments = BuildFolderSegments(folderName, subfolder);

			var localFolderPath = Path.Combine(new[] { _localUploadsRootPath }.Concat(folderSegments).ToArray());
			if (!Directory.Exists(localFolderPath))
			{
				Directory.CreateDirectory(localFolderPath);
				_logger.LogInformation("Created local directory: {Path}", localFolderPath);
			}

			var localFilePath = Path.Combine(localFolderPath, uniqueFileName);
			using (var sourceStream = file.OpenReadStream())
			using (var localStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				await sourceStream.CopyToAsync(localStream);
			}

			_logger.LogInformation("File saved to local storage: {Path}", localFilePath);

			var networkFolderPath = ResolveNetworkFolderPath(folderName);
			if (!string.IsNullOrWhiteSpace(subfolder))
			{
				networkFolderPath = Path.Combine(networkFolderPath, subfolder);
			}

			try
			{
				if (!Directory.Exists(networkFolderPath))
				{
					Directory.CreateDirectory(networkFolderPath);
					_logger.LogInformation("Created network share directory: {Path}", networkFolderPath);
				}

				var networkFilePath = Path.Combine(networkFolderPath, uniqueFileName);
				File.Copy(localFilePath, networkFilePath, overwrite: true);
				_logger.LogInformation("File saved to network share: {Path}", networkFilePath);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not save file to network share: {Path}. File saved only to local storage.", networkFolderPath);
			}

			var relativePath = string.Join('/',
				folderSegments.Append(uniqueFileName)
					.Select(segment => segment.Replace("\\", "/")));

			return relativePath;
		}

		/// <summary>
		/// Delete file from both locations
		/// </summary>
		public Task<bool> DeleteFileAsync(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return Task.FromResult(false);
			}

			var normalizedPath = filePath.Replace("\\", "/").Trim('/');
			if (string.IsNullOrWhiteSpace(normalizedPath))
			{
				return Task.FromResult(false);
			}

			var pathSegments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (pathSegments.Length == 0)
			{
				return Task.FromResult(false);
			}

			var deleted = false;

			var localFullPath = Path.Combine(new[] { _localUploadsRootPath }.Concat(pathSegments).ToArray());
			if (File.Exists(localFullPath))
			{
				try
				{
					File.Delete(localFullPath);
					deleted = true;
					_logger.LogInformation("File deleted from local storage: {Path}", localFullPath);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error deleting file from local storage: {Path}", localFullPath);
				}
			}

			var folderSegment = pathSegments[0];
			var remainder = pathSegments.Skip(1).ToArray();
			var networkFolderPath = ResolveNetworkFolderPath(folderSegment);
			var networkFullPathSegments = new List<string> { networkFolderPath };
			networkFullPathSegments.AddRange(remainder);
			var networkFullPath = Path.Combine(networkFullPathSegments.ToArray());

			if (File.Exists(networkFullPath))
			{
				try
				{
					File.Delete(networkFullPath);
					_logger.LogInformation("File deleted from network share: {Path}", networkFullPath);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not delete file from network share: {Path}", networkFullPath);
				}
			}

			return Task.FromResult(deleted);
		}

		/// <summary>
		/// Get URL to access the file
		/// </summary>
		public string GetFileUrl(string fileName, string folderName, string? subfolder = null)
		{
			var sanitizedPath = fileName.Replace("\\", "/").TrimStart('/');
			return $"/uploads/{sanitizedPath}";
		}

		/// <summary>
		/// Check if file exists
		/// </summary>
		public Task<bool> FileExistsAsync(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return Task.FromResult(false);
			}

			var normalizedPath = filePath.Replace("\\", "/").Trim('/');
			if (string.IsNullOrWhiteSpace(normalizedPath))
			{
				return Task.FromResult(false);
			}

			var localFullPath = Path.Combine(new[] { _localUploadsRootPath }.Concat(normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)).ToArray());
			return Task.FromResult(File.Exists(localFullPath));
		}

		private List<string> BuildFolderSegments(string folderName, string? subfolder)
		{
			var segments = new List<string> { folderName.Trim().Trim('/', '\\') };
			if (!string.IsNullOrWhiteSpace(subfolder))
			{
				segments.Add(subfolder.Trim().Trim('/', '\\'));
			}
			return segments;
		}

		private string ResolveNetworkFolderPath(string folderName)
		{
			var normalizedFolder = folderName.Trim().Trim('/', '\\');

			if (_networkFolderOverrides.TryGetValue(normalizedFolder, out var overridePath))
			{
				return overridePath;
			}

			return Path.Combine(_networkShareRootPath, normalizedFolder);
		}
	}
}

