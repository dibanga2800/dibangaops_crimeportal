#nullable enable

using System.Globalization;
using System.Text;
using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
	public sealed class BarcodeCsvParsedRow
	{
		public int LineNumber { get; init; }
		public string? Barcode { get; init; }
		public string? Department { get; init; }
		public string? VmeCode { get; init; }
		public string? ProductName { get; init; }
		public string? RetailPriceRaw { get; init; }
		public decimal? RetailPrice { get; init; }
		public string? ValidationError { get; init; }
	}

	public sealed class BarcodeCsvParseOutcome
	{
		public bool Success { get; init; }
		public string? FatalError { get; init; }
		public IReadOnlyList<string> IgnoredExtraHeaders { get; init; } = Array.Empty<string>();
		public bool RetailPriceColumnPresent { get; init; }
		public bool IgnoredCostPriceColumnDetected { get; init; }
		public IReadOnlyList<BarcodeCsvParsedRow> Rows { get; init; } = Array.Empty<BarcodeCsvParsedRow>();
	}

	/// <summary>
	/// CSV contract: required headers barcode, Department, VMECode, ProductName, RetailPrice (case-insensitive).
	/// CostPrice is ignored. Empty Department, VMECode, or RetailPrice cells preserve existing values on update.
	/// </summary>
	public static class ProductBarcodeCsvParser
	{
		private const int MaxRows = 10_000;

		public static BarcodeCsvParseOutcome Parse(Stream stream, int maxRows = MaxRows)
		{
			if (stream == null)
			{
				return new BarcodeCsvParseOutcome { Success = false, FatalError = "No import stream was provided." };
			}

			using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

			var headerLine = reader.ReadLine();
			if (headerLine == null)
			{
				return new BarcodeCsvParseOutcome { Success = false, FatalError = "CSV is empty." };
			}

			headerLine = TrimBom(headerLine);
			var headerFields = ParseCsvLine(headerLine);
			if (!TryMapHeaders(
				headerFields,
				out var barcodeIdx,
				out var departmentIdx,
				out var vmeIdx,
				out var nameIdx,
				out var retailPriceIdx,
				out var fatal,
				out var ignoredExtras,
				out var retailPriceColumnPresent,
				out var ignoredCostPrice))
			{
				return new BarcodeCsvParseOutcome { Success = false, FatalError = fatal };
			}

			var rows = new List<BarcodeCsvParsedRow>();
			var lineNumber = 1;
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				lineNumber++;
				if (rows.Count >= maxRows)
				{
					return new BarcodeCsvParseOutcome
					{
						Success = false,
						FatalError = $"CSV exceeds maximum of {maxRows} data rows."
					};
				}

				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				var cells = ParseCsvLine(line);
				var barcode = GetCell(cells, barcodeIdx);
				var department = GetCell(cells, departmentIdx);
				var vme = GetCell(cells, vmeIdx);
				var name = GetCell(cells, nameIdx);
				var retailPriceRaw = GetCell(cells, retailPriceIdx);

				var validation = ValidateRow(barcode, department, vme, name, retailPriceRaw, out var retailPrice);
				rows.Add(new BarcodeCsvParsedRow
				{
					LineNumber = lineNumber,
					Barcode = barcode,
					Department = department,
					VmeCode = vme,
					ProductName = name,
					RetailPriceRaw = retailPriceRaw,
					RetailPrice = retailPrice,
					ValidationError = validation
				});
			}

			return new BarcodeCsvParseOutcome
			{
				Success = true,
				IgnoredExtraHeaders = ignoredExtras,
				RetailPriceColumnPresent = retailPriceColumnPresent,
				IgnoredCostPriceColumnDetected = ignoredCostPrice,
				Rows = rows
			};
		}

		internal static bool TryMapHeaders(
			IReadOnlyList<string> headerFields,
			out int barcodeIdx,
			out int departmentIdx,
			out int vmeIdx,
			out int nameIdx,
			out int retailPriceIdx,
			out string? fatalError,
			out IReadOnlyList<string> ignoredExtras,
			out bool retailPriceColumnPresent,
			out bool ignoredCostPriceColumnDetected)
		{
			barcodeIdx = departmentIdx = vmeIdx = nameIdx = retailPriceIdx = -1;
			fatalError = null;
			ignoredExtras = Array.Empty<string>();
			retailPriceColumnPresent = false;
			ignoredCostPriceColumnDetected = false;

			if (headerFields.Count == 0)
			{
				fatalError = "CSV header row is empty.";
				return false;
			}

			var extras = new List<string>();
			for (var i = 0; i < headerFields.Count; i++)
			{
				var raw = headerFields[i]?.Trim() ?? string.Empty;
				if (string.IsNullOrEmpty(raw))
				{
					continue;
				}

				if (string.Equals(raw, "barcode", StringComparison.OrdinalIgnoreCase))
				{
					if (barcodeIdx >= 0)
					{
						fatalError = "Duplicate 'barcode' column in header.";
						return false;
					}
					barcodeIdx = i;
				}
				else if (string.Equals(raw, "Department", StringComparison.OrdinalIgnoreCase))
				{
					if (departmentIdx >= 0)
					{
						fatalError = "Duplicate 'Department' column in header.";
						return false;
					}
					departmentIdx = i;
				}
				else if (string.Equals(raw, "VMECode", StringComparison.OrdinalIgnoreCase))
				{
					if (vmeIdx >= 0)
					{
						fatalError = "Duplicate 'VMECode' column in header.";
						return false;
					}
					vmeIdx = i;
				}
				else if (string.Equals(raw, "ProductName", StringComparison.OrdinalIgnoreCase))
				{
					if (nameIdx >= 0)
					{
						fatalError = "Duplicate 'ProductName' column in header.";
						return false;
					}
					nameIdx = i;
				}
				else if (string.Equals(raw, "RetailPrice", StringComparison.OrdinalIgnoreCase))
				{
					if (retailPriceIdx >= 0)
					{
						fatalError = "Duplicate 'RetailPrice' column in header.";
						return false;
					}
					retailPriceIdx = i;
					retailPriceColumnPresent = true;
				}
				else if (string.Equals(raw, "CostPrice", StringComparison.OrdinalIgnoreCase))
				{
					ignoredCostPriceColumnDetected = true;
				}
				else
				{
					extras.Add(raw);
				}
			}

			if (barcodeIdx < 0 || departmentIdx < 0 || vmeIdx < 0 || nameIdx < 0 || retailPriceIdx < 0)
			{
				fatalError = "CSV must include headers: barcode, Department, VMECode, ProductName, RetailPrice.";
				return false;
			}

			ignoredExtras = extras;
			return true;
		}

		internal static string? ValidateRow(
			string? barcode,
			string? department,
			string? vmeCode,
			string? productName,
			string? retailPriceRaw,
			out decimal? retailPrice)
		{
			retailPrice = null;
			var b = NormalizeField(barcode);
			var d = NormalizeField(department);
			var v = NormalizeField(vmeCode);
			var n = NormalizeField(productName);

			if (string.IsNullOrWhiteSpace(b))
			{
				return "barcode is required.";
			}
			if (b.Length > 50)
			{
				return "barcode exceeds 50 characters.";
			}
			if (d != null && d.Length > 100)
			{
				return "Department exceeds 100 characters.";
			}
			if (string.IsNullOrWhiteSpace(n))
			{
				return "ProductName is required.";
			}
			if (n.Length > 500)
			{
				return "ProductName exceeds 500 characters.";
			}
			if (v != null && v.Length > 500)
			{
				return "VMECode exceeds 500 characters.";
			}

			if (!string.IsNullOrWhiteSpace(retailPriceRaw))
			{
				if (!TryParseRetailPrice(retailPriceRaw, out var parsed))
				{
					return "RetailPrice is not a valid decimal value.";
				}
				if (parsed < 0)
				{
					return "RetailPrice cannot be negative.";
				}
				retailPrice = parsed;
			}

			return null;
		}

		internal static bool TryParseRetailPrice(string raw, out decimal value)
		{
			var trimmed = raw.Trim().Replace("£", string.Empty).Replace("$", string.Empty).Trim();
			return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
		}

		public static string? NormalizeField(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			var trimmed = value.Trim();
			trimmed = SanitizeFormulaInjectionPrefix(trimmed);
			return string.IsNullOrEmpty(trimmed) ? null : trimmed;
		}

		public static string SanitizeFormulaInjectionPrefix(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			var s = value.TrimStart('\t', '\u0009');
			if (s.Length == 0)
			{
				return s;
			}

			var c0 = s[0];
			if (c0 is '=' or '+' or '-' or '@')
			{
				return "'" + s;
			}

			return s;
		}

		public static List<string> ParseCsvLine(string line)
		{
			var fields = new List<string>();
			var sb = new StringBuilder();
			var inQuotes = false;

			for (var i = 0; i < line.Length; i++)
			{
				var c = line[i];
				if (inQuotes)
				{
					if (c == '"')
					{
						if (i + 1 < line.Length && line[i + 1] == '"')
						{
							sb.Append('"');
							i++;
						}
						else
						{
							inQuotes = false;
						}
					}
					else
					{
						sb.Append(c);
					}
				}
				else
				{
					if (c == '"')
					{
						inQuotes = true;
					}
					else if (c == ',')
					{
						fields.Add(sb.ToString());
						sb.Clear();
					}
					else
					{
						sb.Append(c);
					}
				}
			}

			fields.Add(sb.ToString());
			return fields;
		}

		private static string TrimBom(string line)
		{
			if (line.Length > 0 && line[0] == '\ufeff')
			{
				return line.TrimStart('\ufeff');
			}
			return line;
		}

		private static string? GetCell(IReadOnlyList<string> cells, int index)
		{
			if (index < 0 || index >= cells.Count)
			{
				return null;
			}
			return cells[index];
		}
	}
}
