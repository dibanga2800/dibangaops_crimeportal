#nullable enable

namespace AIPBackend.Models.DTOs
{
	public sealed class BarcodeCsvImportRowErrorDto
	{
		public int LineNumber { get; set; }
		public string Message { get; set; } = string.Empty;
	}

	public sealed class BarcodeCsvImportResultDto
	{
		public string FileName { get; set; } = string.Empty;
		public int TotalDataRows { get; set; }
		public int ValidRows { get; set; }
		public int InvalidRows { get; set; }
		public int CreatedCount { get; set; }
		public int UpdatedCount { get; set; }
		/// <summary>Rows with a barcode that appeared earlier in the same file; the last row wins.</summary>
		public int DeduplicatedInFileCount { get; set; }
		public bool RetailPriceColumnPresent { get; set; }
		public bool IgnoredCostPriceColumnDetected { get; set; }
		/// <summary>Legacy alias for IgnoredCostPriceColumnDetected.</summary>
		public bool IgnoredPriceColumnsDetected
		{
			get => IgnoredCostPriceColumnDetected;
			set => IgnoredCostPriceColumnDetected = value;
		}
		public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
		/// <summary>Number of row errors included in <see cref="RowErrors"/> (capped at 200).</summary>
		public int RowErrorsReturned { get; set; }
		public bool ImportCompleted { get; set; } = true;
		public int? FailedAtChunk { get; set; }
		public string? ErrorMessage { get; set; }
		public List<string> IgnoredExtraHeaders { get; set; } = new();
		public List<BarcodeCsvImportRowErrorDto> RowErrors { get; set; } = new();
	}
}
