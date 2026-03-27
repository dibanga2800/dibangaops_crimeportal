#nullable enable

namespace AIPBackend.Services
{
	public sealed class IncidentImageStorageOptions
	{
		/// <summary>
		/// Supported values: database, blob, both.
		/// </summary>
		public string Mode { get; set; } = "database";
		public string ContainerName { get; set; } = "images";
		public string BlobPathPrefix { get; set; } = "verification";
	}
}
