#nullable enable

namespace AIPBackend.Services
{
	public sealed class IncidentImageStorageResult
	{
		public string? StoredReference { get; init; }
		public byte[]? ImageBytes { get; init; }
		public bool UploadedToBlob { get; init; }
	}
}
