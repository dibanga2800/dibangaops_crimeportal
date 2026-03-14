#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// Azure Face API client for face detection, Person Group management, and identification.
	/// Uses REST API via HttpClient. Gracefully degrades when Face API is disabled or returns 401/403.
	/// </summary>
	public sealed class AzureFaceClient : IAzureFaceClient
	{
		private readonly HttpClient _httpClient;
		private readonly AzureFaceOptions _options;
		private readonly ILogger<AzureFaceClient> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true
		};

		public AzureFaceClient(
			HttpClient httpClient,
			IOptions<AzureFaceOptions> options,
			ILogger<AzureFaceClient> logger)
		{
			_httpClient = httpClient;
			_options = options.Value;
			_logger = logger;

			if (!string.IsNullOrWhiteSpace(_options.Endpoint))
			{
				_httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
			}

			if (!string.IsNullOrWhiteSpace(_options.ApiKey))
			{
				_httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
			}
		}

		public async Task<AzureFaceDetectResult?> DetectFacesAsync(
			byte[] imageBytes,
			CancellationToken cancellationToken = default,
			bool preferPermissiveModel = false)
		{
			if (!_options.Enabled)
				return null;

			if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
			{
				_logger.LogDebug("Azure Face API not configured; skipping face detection");
				return null;
			}

			// detection_01 is often more permissive for live camera captures (guided capture UX)
			var primaryModel = preferPermissiveModel ? "detection_01" : "detection_03";
			var fallbackModel = preferPermissiveModel ? "detection_03" : "detection_01";
			var url = $"face/v1.0/detect?returnFaceId=true&recognitionModel=recognition_04&detectionModel={primaryModel}";
			using var content = new ByteArrayContent(imageBytes);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

			using var response = await _httpClient.PostAsync(url, content, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				var statusCode = (int)response.StatusCode;
				if (statusCode is 401 or 403)
				{
					_logger.LogWarning(
						"Azure Face API authentication failed (HTTP {StatusCode}). Check ApiKey and subscription. Response: {Body}",
						response.StatusCode, body);
				}
				else
				{
					_logger.LogWarning("Azure Face API detect failed: {StatusCode} {Body}", response.StatusCode, body);
				}
				return null;
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			var faces = JsonSerializer.Deserialize<List<AzureFaceDetectItem>>(responseBody);
			var result = new AzureFaceDetectResult { Faces = faces ?? new List<AzureFaceDetectItem>() };

			// Fallback: try alternate model if no face found
			if (result.Faces.Count == 0 && imageBytes.Length >= 1024)
			{
				var fallbackUrl = $"face/v1.0/detect?returnFaceId=true&recognitionModel=recognition_04&detectionModel={fallbackModel}";
				using var fallbackContent = new ByteArrayContent(imageBytes);
				fallbackContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
				using var fallbackResponse = await _httpClient.PostAsync(fallbackUrl, fallbackContent, cancellationToken);
				if (fallbackResponse.IsSuccessStatusCode)
				{
					var fallbackBody = await fallbackResponse.Content.ReadAsStringAsync(cancellationToken);
					var fallbackFaces = JsonSerializer.Deserialize<List<AzureFaceDetectItem>>(fallbackBody);
					if (fallbackFaces != null && fallbackFaces.Count > 0)
					{
						_logger.LogDebug("Azure Face API: {Primary} found no face; {Fallback} found {Count}", primaryModel, fallbackModel, fallbackFaces.Count);
						result.Faces = fallbackFaces;
					}
				}
			}

			return result;
		}

		public async Task<string?> CreatePersonAndAddFaceAsync(
			string name,
			byte[] faceImageBytes,
			string userData,
			CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return null;

			var personGroupId = _options.PersonGroupId;
			var personId = await CreatePersonInternalAsync(personGroupId, name, userData, cancellationToken);
			if (personId == null)
				return null;

			var added = await AddPersonFaceInternalAsync(personGroupId, personId, faceImageBytes, cancellationToken);
			if (added == null)
				return null;

			return personId;
		}

		private async Task<string?> CreatePersonInternalAsync(
			string personGroupId,
			string name,
			string userData,
			CancellationToken cancellationToken)
		{
			var url = $"face/v1.0/persongroups/{personGroupId}/persons";
			var payload = new { name, userData };
			var json = JsonSerializer.Serialize(payload);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.PostAsync(url, content, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API CreatePerson failed: {StatusCode} {Body}", response.StatusCode, body);
				return null;
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			using var doc = JsonDocument.Parse(responseBody);
			return doc.RootElement.TryGetProperty("personId", out var pid) ? pid.GetString() : null;
		}

		private async Task<string?> AddPersonFaceInternalAsync(
			string personGroupId,
			string personId,
			byte[] imageBytes,
			CancellationToken cancellationToken)
		{
			var url = $"face/v1.0/persongroups/{personGroupId}/persons/{personId}/persistedfaces";
			using var content = new ByteArrayContent(imageBytes);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

			using var response = await _httpClient.PostAsync(url, content, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API AddPersonFace failed: {StatusCode} {Body}", response.StatusCode, body);
				return null;
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			using var doc = JsonDocument.Parse(responseBody);
			return doc.RootElement.TryGetProperty("persistedFaceId", out var fid) ? fid.GetString() : null;
		}

		public async Task<bool> TrainPersonGroupAsync(CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return false;

			var url = $"face/v1.0/persongroups/{_options.PersonGroupId}/train";
			using var response = await _httpClient.PostAsync(url, null, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API Train failed: {StatusCode} {Body}", response.StatusCode, body);
				return false;
			}
			return true;
		}

		public async Task<bool> WaitForTrainingCompletionAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return false;

			var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(60));
			var url = $"face/v1.0/persongroups/{_options.PersonGroupId}/training";

			while (DateTime.UtcNow < deadline)
			{
				using var response = await _httpClient.GetAsync(url, cancellationToken);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync(cancellationToken);
					_logger.LogWarning("Azure Face API GetTrainingStatus failed: {StatusCode} {Body}", response.StatusCode, body);
					return false;
				}

				var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
				using var doc = JsonDocument.Parse(responseBody);
				var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

				switch (status?.ToLowerInvariant())
				{
					case "succeeded":
						_logger.LogDebug("Azure Face API Person Group training succeeded");
						return true;
					case "failed":
						var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "unknown";
						_logger.LogWarning("Azure Face API Person Group training failed: {Message}", msg);
						return false;
					case "notstarted":
					case "running":
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
						continue;
					default:
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
						continue;
				}
			}

			_logger.LogWarning("Azure Face API Person Group training timed out");
			return false;
		}

		public async Task<bool> EnsurePersonGroupExistsAsync(CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return false;

			var personGroupId = _options.PersonGroupId;
			var url = $"face/v1.0/persongroups/{personGroupId}";
			using var getResponse = await _httpClient.GetAsync(url, cancellationToken);

			if (getResponse.IsSuccessStatusCode)
				return true;

			if (getResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				var payload = new { name = personGroupId, userData = "Crime Portal offender recognition", recognitionModel = "recognition_04" };
				var json = JsonSerializer.Serialize(payload);
				using var content = new StringContent(json, Encoding.UTF8, "application/json");
				using var putResponse = await _httpClient.PutAsync(url, content, cancellationToken);
				if (putResponse.IsSuccessStatusCode)
				{
					_logger.LogInformation("Created Azure Face API Person Group: {PersonGroupId}", personGroupId);
					return true;
				}
				var body = await putResponse.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API Create Person Group failed: {StatusCode} {Body}", putResponse.StatusCode, body);
			}

			return false;
		}

		public async Task<IReadOnlyList<AzureFaceIdentifyCandidate>> IdentifyAsync(
			IReadOnlyList<string> faceIds,
			int maxNumOfCandidatesReturned = 10,
			CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || faceIds.Count == 0 || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return Array.Empty<AzureFaceIdentifyCandidate>();

			var url = "face/v1.0/identify";
			var payload = new
			{
				personGroupId = _options.PersonGroupId,
				faceIds,
				maxNumOfCandidatesReturned,
				confidenceThreshold = _options.ConfidenceThreshold
			};
			var json = JsonSerializer.Serialize(payload);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.PostAsync(url, content, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API Identify failed: {StatusCode} {Body}", response.StatusCode, body);
				return Array.Empty<AzureFaceIdentifyCandidate>();
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			var results = JsonSerializer.Deserialize<List<AzureFaceIdentifyResult>>(responseBody);
			var candidates = new List<AzureFaceIdentifyCandidate>();
			if (results != null)
			{
				foreach (var r in results)
				{
					if (r.Candidates != null)
					{
						foreach (var c in r.Candidates)
						{
							candidates.Add(c);
						}
					}
				}
			}
			return candidates;
		}

		public async Task<AzurePerson?> GetPersonAsync(string personId, CancellationToken cancellationToken = default)
		{
			if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonGroupId))
				return null;

			var url = $"face/v1.0/persongroups/{_options.PersonGroupId}/persons/{personId}";
			using var response = await _httpClient.GetAsync(url, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				_logger.LogWarning("Azure Face API GetPerson failed: {StatusCode} {Body}", response.StatusCode, body);
				return null;
			}

			var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			return JsonSerializer.Deserialize<AzurePerson>(responseBody);
		}
	}

	/// <summary>
	/// Internal DTO for Identify API JSON deserialization.
	/// </summary>
	internal class AzureFaceIdentifyResult
	{
		public string? FaceId { get; set; }
		public List<AzureFaceIdentifyCandidate>? Candidates { get; set; }
	}
}
