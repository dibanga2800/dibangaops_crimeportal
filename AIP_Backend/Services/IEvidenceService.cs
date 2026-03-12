#nullable enable

using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	/// <summary>
	/// Service for managing barcode-based evidence chain-of-custody tracking.
	/// </summary>
	public interface IEvidenceService
	{
		Task<EvidenceItemDto> RegisterEvidenceAsync(int incidentId, RegisterEvidenceDto dto, string userId);
		Task<EvidenceItemDto> GetByIdAsync(int evidenceItemId);
		Task<EvidenceListResponseDto> GetByIncidentAsync(int incidentId);
		Task<BarcodeScanResultDto> ScanBarcodeAsync(BarcodeScanDto dto);
		Task<EvidenceCustodyEventDto> RecordCustodyEventAsync(int evidenceItemId, RecordCustodyEventDto dto, string userId);
	}
}
