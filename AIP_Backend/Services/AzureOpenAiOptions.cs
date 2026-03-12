#nullable enable

namespace AIPBackend.Services
{
	public sealed class AzureOpenAiOptions
	{
		public string Endpoint { get; set; } = string.Empty;
		public string ApiKey { get; set; } = string.Empty;
		public string Deployment { get; set; } = string.Empty;
		public bool Enabled { get; set; } = true;
	}
}

