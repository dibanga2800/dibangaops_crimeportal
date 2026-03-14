#nullable enable

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// Response from InsightFace /detect endpoint.
	/// </summary>
	public sealed class InsightFaceDetectResponse
	{
		public bool FaceDetected { get; set; }
		public InsightFaceRectangle? FaceRectangle { get; set; }
	}

	/// <summary>
	/// Bounding box from InsightFace (left, top, width, height).
	/// </summary>
	public sealed class InsightFaceRectangle
	{
		public int Left { get; set; }
		public int Top { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
	}

	/// <summary>
	/// Response from InsightFace /embed endpoint.
	/// </summary>
	public sealed class InsightFaceEmbedResponse
	{
		public bool FaceDetected { get; set; }
		public float[]? Embedding { get; set; }
		public InsightFaceRectangle? FaceRectangle { get; set; }
	}

	/// <summary>
	/// Client for InsightFace REST API (detect and embed).
	/// </summary>
	public interface IInsightFaceClient
	{
		Task<InsightFaceDetectResponse?> DetectAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
		Task<InsightFaceEmbedResponse?> GetEmbeddingAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// HTTP client for InsightFace service. POSTs raw image bytes to /detect and /embed.
	/// </summary>
	public sealed class InsightFaceClient : IInsightFaceClient
	{
		private readonly HttpClient _httpClient;
		private readonly InsightFaceOptions _options;
		private readonly ILogger<InsightFaceClient> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true
		};

		public InsightFaceClient(
			HttpClient httpClient,
			IOptions<InsightFaceOptions> options,
			ILogger<InsightFaceClient> logger)
		{
			_httpClient = httpClient;
			_options = options.Value;
			_logger = logger;

			var baseUrl = _options.BaseUrl?.TrimEnd('/') ?? "http://localhost:8000";
			_httpClient.BaseAddress = new Uri(baseUrl + "/");
			_httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
		}

		public async Task<InsightFaceDetectResponse?> DetectAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled)
				return null;

			try
			{
				using var content = new ByteArrayContent(imageBytes);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
				using var response = await _httpClient.PostAsync("detect", content, cancellationToken);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync(cancellationToken);
					_logger.LogWarning("InsightFace detect failed: {StatusCode} {Body}", response.StatusCode, body);
					return null;
				}
				var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
				return JsonSerializer.Deserialize<InsightFaceDetectResponse>(responseBody, JsonOptions);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "InsightFace detect request failed");
				return null;
			}
		}

		public async Task<InsightFaceEmbedResponse?> GetEmbeddingAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled)
				return null;

			try
			{
				using var content = new ByteArrayContent(imageBytes);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
				using var response = await _httpClient.PostAsync("embed", content, cancellationToken);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync(cancellationToken);
					_logger.LogWarning("InsightFace embed failed: {StatusCode} {Body}", response.StatusCode, body);
					return null;
				}
				var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
				return JsonSerializer.Deserialize<InsightFaceEmbedResponse>(responseBody, JsonOptions);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "InsightFace embed request failed");
				return null;
			}
		}
	}
}
