#nullable enable

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	public sealed class IncidentImageStorageService : IIncidentImageStorageService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly IncidentImageStorageOptions _options;
		private readonly ILogger<IncidentImageStorageService> _logger;

		public IncidentImageStorageService(
			BlobServiceClient blobServiceClient,
			IOptions<IncidentImageStorageOptions> options,
			ILogger<IncidentImageStorageService> logger)
		{
			_blobServiceClient = blobServiceClient;
			_options = options.Value;
			_logger = logger;
		}

		public async Task<IncidentImageStorageResult> PersistVerificationImageAsync(string? imageReference, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(imageReference))
			{
				return new IncidentImageStorageResult { StoredReference = imageReference };
			}

			var mode = ParseMode(_options.Mode);
			var decoded = TryDecodeBase64DataUrl(imageReference);
			if (decoded == null || decoded.ImageBytes.Length == 0)
			{
				// Already a URL or legacy value - keep as-is.
				return new IncidentImageStorageResult { StoredReference = imageReference };
			}

			if (mode == IncidentImageStorageMode.Database)
			{
				return new IncidentImageStorageResult
				{
					StoredReference = imageReference,
					ImageBytes = decoded.ImageBytes
				};
			}

			var uploadedUrl = await TryUploadToBlobAsync(decoded.ImageBytes, decoded.ContentType, cancellationToken);
			if (string.IsNullOrWhiteSpace(uploadedUrl))
			{
				_logger.LogWarning("Incident image blob upload failed. Falling back to database storage.");
				return new IncidentImageStorageResult
				{
					StoredReference = imageReference,
					ImageBytes = decoded.ImageBytes
				};
			}

			// both = keep DB as source of truth and also backup to blob
			if (mode == IncidentImageStorageMode.Both)
			{
				return new IncidentImageStorageResult
				{
					StoredReference = imageReference,
					ImageBytes = decoded.ImageBytes,
					UploadedToBlob = true
				};
			}

			return new IncidentImageStorageResult
			{
				StoredReference = uploadedUrl,
				ImageBytes = decoded.ImageBytes,
				UploadedToBlob = true
			};
		}

		private async Task<string?> TryUploadToBlobAsync(byte[] imageBytes, string? contentType, CancellationToken cancellationToken)
		{
			try
			{
				var containerName = string.IsNullOrWhiteSpace(_options.ContainerName) ? "images" : _options.ContainerName.Trim();
				var prefix = string.IsNullOrWhiteSpace(_options.BlobPathPrefix) ? "verification" : _options.BlobPathPrefix.Trim('/');
				var extension = GetFileExtension(contentType);
				var blobName = $"{prefix}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

				var container = _blobServiceClient.GetBlobContainerClient(containerName);
				await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

				var blob = container.GetBlobClient(blobName);
				using var stream = new MemoryStream(imageBytes);
				var headers = new BlobHttpHeaders
				{
					ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
				};
				await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);
				return blob.Uri.AbsoluteUri;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not upload verification image to blob storage.");
				return null;
			}
		}

		private static string GetFileExtension(string? contentType)
		{
			return contentType?.ToLowerInvariant() switch
			{
				"image/jpeg" => ".jpg",
				"image/png" => ".png",
				"image/webp" => ".webp",
				_ => ".bin"
			};
		}

		private static IncidentImageStorageMode ParseMode(string? mode)
		{
			return mode?.Trim().ToLowerInvariant() switch
			{
				"blob" => IncidentImageStorageMode.Blob,
				"both" => IncidentImageStorageMode.Both,
				_ => IncidentImageStorageMode.Database
			};
		}

		private static DecodedImage? TryDecodeBase64DataUrl(string? value)
		{
			if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var commaIndex = value.IndexOf(',');
			if (commaIndex <= 5)
			{
				return null;
			}

			try
			{
				var metadata = value[5..commaIndex];
				var contentType = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries)[0];
				var bytes = Convert.FromBase64String(value[(commaIndex + 1)..]);
				return new DecodedImage(bytes, contentType);
			}
			catch
			{
				return null;
			}
		}

		private sealed record DecodedImage(byte[] ImageBytes, string? ContentType);

		private enum IncidentImageStorageMode
		{
			Database,
			Blob,
			Both
		}
	}
}
