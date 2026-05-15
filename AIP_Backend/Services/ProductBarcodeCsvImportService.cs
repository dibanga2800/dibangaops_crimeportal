#nullable enable

using AIPBackend.Data;
using AIPBackend.Models;
using AIPBackend.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AIPBackend.Services
{
	public interface IProductBarcodeCsvImportService
	{
		Task<BarcodeCsvImportResultDto> ImportAsync(
			Stream fileStream,
			string fileName,
			string actorUserId,
			CancellationToken cancellationToken = default);
	}

	public sealed class ProductBarcodeCsvImportService : IProductBarcodeCsvImportService
	{
		private const int MaxRowErrorsReturned = 200;
		/// <summary>Rows per DB round-trip (one lookup query + one SaveChanges per chunk).</summary>
		private const int UpsertChunkSize = 500;

		private readonly ApplicationDbContext _context;
		private readonly ILogger<ProductBarcodeCsvImportService> _logger;

		public ProductBarcodeCsvImportService(
			ApplicationDbContext context,
			ILogger<ProductBarcodeCsvImportService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<BarcodeCsvImportResultDto> ImportAsync(
			Stream fileStream,
			string fileName,
			string actorUserId,
			CancellationToken cancellationToken = default)
		{
			var parse = ProductBarcodeCsvParser.Parse(fileStream);
			if (!parse.Success || parse.FatalError != null)
			{
				throw new InvalidOperationException(parse.FatalError ?? "CSV could not be parsed.");
			}

			var rowErrors = new List<BarcodeCsvImportRowErrorDto>();
			var lastRowByBarcode = new Dictionary<string, BarcodeCsvParsedRow>(StringComparer.OrdinalIgnoreCase);
			var deduplicatedInFileCount = 0;

			foreach (var row in parse.Rows)
			{
				if (row.ValidationError != null)
				{
					rowErrors.Add(new BarcodeCsvImportRowErrorDto { LineNumber = row.LineNumber, Message = row.ValidationError });
					continue;
				}

				var barcode = ProductBarcodeCsvParser.NormalizeField(row.Barcode)!;
				if (lastRowByBarcode.ContainsKey(barcode))
				{
					deduplicatedInFileCount++;
				}

				lastRowByBarcode[barcode] = row;
			}

			var toProcess = lastRowByBarcode.Values.ToList();
			var utcNow = DateTime.UtcNow;

			var created = 0;
			var updated = 0;
			var importCompleted = true;
			int? failedAtChunk = null;
			string? importErrorMessage = null;
			var chunkIndex = 0;

			foreach (var chunk in toProcess.Chunk(UpsertChunkSize))
			{
				chunkIndex++;
				var chunkRows = chunk.ToList();
				var chunkResult = await TrySaveChunkAsync(chunkRows, utcNow, actorUserId, cancellationToken);
				if (chunkResult.Saved)
				{
					created += chunkResult.Created;
					updated += chunkResult.Updated;
					continue;
				}

				// Retry once after detach (concurrent import / unique race on same EAN)
				DetachTrackedProducts();
				chunkResult = await TrySaveChunkAsync(chunkRows, utcNow, actorUserId, cancellationToken);
				if (chunkResult.Saved)
				{
					created += chunkResult.Created;
					updated += chunkResult.Updated;
					continue;
				}

				if (!chunkResult.Saved)
				{
					importCompleted = false;
					failedAtChunk = chunkIndex;
					importErrorMessage =
						$"Import stopped at chunk {chunkIndex} due to a database conflict (duplicate barcode). " +
						$"Earlier chunks were saved ({created} created, {updated} updated so far).";
					break;
				}
			}

			var returnedRowErrors = rowErrors.Take(MaxRowErrorsReturned).ToList();
			var result = new BarcodeCsvImportResultDto
			{
				FileName = fileName,
				TotalDataRows = parse.Rows.Count,
				ValidRows = toProcess.Count,
				InvalidRows = rowErrors.Count,
				DeduplicatedInFileCount = deduplicatedInFileCount,
				CreatedCount = created,
				UpdatedCount = updated,
				RetailPriceColumnPresent = parse.RetailPriceColumnPresent,
				IgnoredCostPriceColumnDetected = parse.IgnoredCostPriceColumnDetected,
				IgnoredExtraHeaders = parse.IgnoredExtraHeaders.ToList(),
				ImportCompleted = importCompleted,
				FailedAtChunk = failedAtChunk,
				ErrorMessage = importErrorMessage,
				RowErrorsReturned = returnedRowErrors.Count,
				RowErrors = returnedRowErrors
			};

			_logger.LogInformation(
				"Barcode CSV import completed. Actor={Actor}, File={File}, TotalRows={Total}, Valid={Valid}, Invalid={Invalid}, DedupedInFile={Deduped}, Created={Created}, Updated={Updated}, RetailPriceCol={RetailPriceCol}, IgnoredCostPrice={IgnoredCostPrice}, IgnoredExtraHeaders={Extras}",
				actorUserId,
				fileName,
				result.TotalDataRows,
				result.ValidRows,
				result.InvalidRows,
				result.DeduplicatedInFileCount,
				result.CreatedCount,
				result.UpdatedCount,
				parse.RetailPriceColumnPresent,
				parse.IgnoredCostPriceColumnDetected,
				string.Join(",", parse.IgnoredExtraHeaders));

			return result;
		}

		private sealed record ChunkSaveResult(bool Saved, int Created, int Updated);

		private async Task<ChunkSaveResult> TrySaveChunkAsync(
			IReadOnlyList<BarcodeCsvParsedRow> chunkRows,
			DateTime utcNow,
			string actorUserId,
			CancellationToken cancellationToken)
		{
			var chunkCreated = 0;
			var chunkUpdated = 0;
			var eans = chunkRows
				.Select(r => ProductBarcodeCsvParser.NormalizeField(r.Barcode)!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var existingByEan = await _context.Products
				.Where(p => eans.Contains(p.EAN))
				.ToDictionaryAsync(p => p.EAN, StringComparer.OrdinalIgnoreCase, cancellationToken);

			foreach (var row in chunkRows)
			{
				var ean = ProductBarcodeCsvParser.NormalizeField(row.Barcode)!;
				var productName = ProductBarcodeCsvParser.NormalizeField(row.ProductName)!;
				var department = ProductBarcodeCsvParser.NormalizeField(row.Department);
				var description = ProductBarcodeCsvParser.NormalizeField(row.VmeCode);

				if (existingByEan.TryGetValue(ean, out var existing))
				{
					existing.ProductName = productName;
					if (description != null)
					{
						existing.Description = description;
					}
					if (department != null)
					{
						existing.Department = department;
					}
					if (row.RetailPrice.HasValue)
					{
						existing.Price = row.RetailPrice;
					}
					existing.UpdatedAt = utcNow;
					existing.UpdatedBy = actorUserId;
					existing.IsActive = true;
					chunkUpdated++;
				}
				else
				{
					var product = new Product
					{
						EAN = ean,
						ProductName = productName,
						Department = department,
						Description = description,
						Price = row.RetailPrice,
						CreatedAt = utcNow,
						CreatedBy = actorUserId,
						IsActive = true
					};
					_context.Products.Add(product);
					existingByEan[ean] = product;
					chunkCreated++;
				}
			}

			try
			{
				await _context.SaveChangesAsync(cancellationToken);
				return new ChunkSaveResult(true, chunkCreated, chunkUpdated);
			}
			catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
			{
				_logger.LogWarning(ex, "Barcode CSV import chunk hit unique constraint on EAN");
				DetachTrackedProducts();
				return new ChunkSaveResult(false, 0, 0);
			}
		}

		private void DetachTrackedProducts()
		{
			foreach (var entry in _context.ChangeTracker.Entries<Product>().ToList())
			{
				entry.State = EntityState.Detached;
			}
		}

		private static bool IsUniqueConstraintViolation(DbUpdateException ex)
		{
			for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
			{
				if (inner is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
				{
					return true;
				}
			}

			return false;
		}
	}
}
