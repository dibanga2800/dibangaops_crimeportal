#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
	public class EvidenceService : IEvidenceService
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<EvidenceService> _logger;

		public EvidenceService(ApplicationDbContext context, ILogger<EvidenceService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<EvidenceItemDto> RegisterEvidenceAsync(int incidentId, RegisterEvidenceDto dto, string userId)
		{
			var incident = await _context.Incidents.FindAsync(incidentId);
			if (incident == null)
				throw new KeyNotFoundException($"Incident {incidentId} not found");

			var evidence = new EvidenceItem
			{
				IncidentId = incidentId,
				Barcode = dto.Barcode,
				EvidenceType = dto.EvidenceType,
				Description = dto.Description,
				StorageLocation = dto.StorageLocation,
				Status = "registered",
				RegisteredAt = DateTime.UtcNow,
				RegisteredBy = userId
			};

			_context.EvidenceItems.Add(evidence);

			var custodyEvent = new EvidenceCustodyEvent
			{
				EvidenceItem = evidence,
				EventType = "registered",
				Notes = $"Evidence registered for incident #{incidentId}",
				EventTimestamp = DateTime.UtcNow,
				PerformedBy = userId
			};
			_context.EvidenceCustodyEvents.Add(custodyEvent);

			incident.EvidenceAttached = true;

			await _context.SaveChangesAsync();

			_logger.LogInformation("Evidence {EvidenceId} registered for incident {IncidentId} by {UserId}",
				evidence.EvidenceItemId, incidentId, userId);

			return await GetByIdAsync(evidence.EvidenceItemId);
		}

		public async Task<EvidenceItemDto> GetByIdAsync(int evidenceItemId)
		{
			var evidence = await _context.EvidenceItems
				.Include(e => e.CustodyEvents.OrderBy(c => c.EventTimestamp))
				.FirstOrDefaultAsync(e => e.EvidenceItemId == evidenceItemId);

			if (evidence == null)
				throw new KeyNotFoundException($"Evidence item {evidenceItemId} not found");

			return MapToDto(evidence);
		}

		public async Task<EvidenceListResponseDto> GetByIncidentAsync(int incidentId)
		{
			var items = await _context.EvidenceItems
				.Include(e => e.CustodyEvents.OrderBy(c => c.EventTimestamp))
				.Where(e => e.IncidentId == incidentId)
				.OrderByDescending(e => e.RegisteredAt)
				.ToListAsync();

			return new EvidenceListResponseDto
			{
				Success = true,
				Data = items.Select(MapToDto).ToList(),
				TotalCount = items.Count
			};
		}

		public async Task<BarcodeScanResultDto> ScanBarcodeAsync(BarcodeScanDto dto)
		{
			var evidence = await _context.EvidenceItems
				.Include(e => e.Incident)
				.Include(e => e.CustodyEvents)
				.FirstOrDefaultAsync(e => e.Barcode == dto.Barcode);

			if (evidence == null)
			{
				return new BarcodeScanResultDto
				{
					Found = false,
					Message = $"No evidence found for barcode {dto.Barcode}"
				};
			}

			if (!string.IsNullOrWhiteSpace(dto.ScanLocation))
			{
				_context.EvidenceCustodyEvents.Add(new EvidenceCustodyEvent
				{
					EvidenceItemId = evidence.EvidenceItemId,
					EventType = "scanned",
					Location = dto.ScanLocation,
					EventTimestamp = DateTime.UtcNow,
					PerformedBy = "system"
				});
				await _context.SaveChangesAsync();
			}

			return new BarcodeScanResultDto
			{
				Found = true,
				EvidenceItem = MapToDto(evidence),
				IncidentId = evidence.IncidentId,
				IncidentType = evidence.Incident?.IncidentType,
				SiteName = evidence.Incident?.StoreName,
				Message = "Evidence item located"
			};
		}

		public async Task<EvidenceCustodyEventDto> RecordCustodyEventAsync(
			int evidenceItemId, RecordCustodyEventDto dto, string userId)
		{
			var evidence = await _context.EvidenceItems.FindAsync(evidenceItemId);
			if (evidence == null)
				throw new KeyNotFoundException($"Evidence item {evidenceItemId} not found");

			var custodyEvent = new EvidenceCustodyEvent
			{
				EvidenceItemId = evidenceItemId,
				EventType = dto.EventType,
				Notes = dto.Notes,
				Location = dto.Location,
				EventTimestamp = DateTime.UtcNow,
				PerformedBy = userId
			};

			_context.EvidenceCustodyEvents.Add(custodyEvent);

			var statusMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["transferred"] = "in-transit",
				["received"] = "in-storage",
				["released"] = "released",
				["disposed"] = "disposed",
				["returned"] = "returned"
			};

			if (statusMap.TryGetValue(dto.EventType, out var newStatus))
			{
				evidence.Status = newStatus;
			}

			await _context.SaveChangesAsync();

			_logger.LogInformation("Custody event '{EventType}' recorded for evidence {EvidenceId} by {UserId}",
				dto.EventType, evidenceItemId, userId);

			return new EvidenceCustodyEventDto
			{
				CustodyEventId = custodyEvent.CustodyEventId,
				EventType = custodyEvent.EventType,
				Notes = custodyEvent.Notes,
				Location = custodyEvent.Location,
				EventTimestamp = custodyEvent.EventTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				PerformedBy = custodyEvent.PerformedBy,
				PerformedByName = custodyEvent.PerformedByName
			};
		}

		private static EvidenceItemDto MapToDto(EvidenceItem evidence)
		{
			return new EvidenceItemDto
			{
				EvidenceItemId = evidence.EvidenceItemId,
				IncidentId = evidence.IncidentId,
				Barcode = evidence.Barcode,
				EvidenceType = evidence.EvidenceType,
				Description = evidence.Description,
				StorageLocation = evidence.StorageLocation,
				Status = evidence.Status,
				RegisteredAt = evidence.RegisteredAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				RegisteredBy = evidence.RegisteredBy,
				CustodyEvents = evidence.CustodyEvents?.Select(c => new EvidenceCustodyEventDto
				{
					CustodyEventId = c.CustodyEventId,
					EventType = c.EventType,
					Notes = c.Notes,
					Location = c.Location,
					EventTimestamp = c.EventTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					PerformedBy = c.PerformedBy,
					PerformedByName = c.PerformedByName
				}).ToList() ?? new()
			};
		}
	}
}
