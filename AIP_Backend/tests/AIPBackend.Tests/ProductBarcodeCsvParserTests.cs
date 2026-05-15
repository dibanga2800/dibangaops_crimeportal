using System.Text;
using AIPBackend.Services;

namespace AIPBackend.Tests;

public class ProductBarcodeCsvParserTests
{
	private const string StandardHeaders = "barcode,Department,VMECode,ProductName,RetailPrice";

	[Fact]
	public void Parse_AcceptsHeadersCaseInsensitive_AndParsesRows()
	{
		var csv = "Barcode,Department,vmecode,PRODUCTNAME,RetailPrice\n5012345678900,PROVISIONS,Code,Name,2.80\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.True(result.Success);
		Assert.Single(result.Rows);
		Assert.Null(result.Rows[0].ValidationError);
		Assert.Equal("5012345678900", ProductBarcodeCsvParser.NormalizeField(result.Rows[0].Barcode));
		Assert.Equal("PROVISIONS", ProductBarcodeCsvParser.NormalizeField(result.Rows[0].Department));
		Assert.Equal(2.80m, result.Rows[0].RetailPrice);
	}

	[Fact]
	public void Parse_RejectsMissingRequiredHeader()
	{
		var csv = "barcode,ProductName,RetailPrice\n1,A,1\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.False(result.Success);
		Assert.Contains("VMECode", result.FatalError ?? string.Empty);
	}

	[Fact]
	public void Parse_RejectsMissingRetailPriceHeader()
	{
		var csv = "barcode,Department,VMECode,ProductName\n1,D,V,N\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.False(result.Success);
		Assert.Contains("RetailPrice", result.FatalError ?? string.Empty);
	}

	[Fact]
	public void Parse_ValidatesDepartmentLength()
	{
		var dept = new string('X', 101);
		var csv = $"{StandardHeaders}\n5012345678900,{dept},V1,Name,1\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.True(result.Success);
		Assert.Contains("Department exceeds", result.Rows[0].ValidationError ?? string.Empty);
	}

	[Fact]
	public void Parse_AllowsEmptyDepartmentCell_ForPreserveOnUpdate()
	{
		var csv = $"{StandardHeaders}\n5012345678900,,V1,Name,2.49\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.True(result.Success);
		Assert.Null(result.Rows[0].ValidationError);
		Assert.Null(ProductBarcodeCsvParser.NormalizeField(result.Rows[0].Department));
	}

	[Fact]
	public void Parse_IgnoresCostPrice_ButMapsRetailPrice()
	{
		var csv = "barcode,Department,VMECode,ProductName,CostPrice,RetailPrice\n1,BAKERY,C,N,9,10\n";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
		var result = ProductBarcodeCsvParser.Parse(stream);
		Assert.True(result.Success);
		Assert.True(result.RetailPriceColumnPresent);
		Assert.True(result.IgnoredCostPriceColumnDetected);
		Assert.Equal(10m, result.Rows[0].RetailPrice);
	}

	[Fact]
	public void Parse_AllowsBlankHeaderCells_TrailingCommaOrGap()
	{
		var trailing = $"{StandardHeaders},\n5012345678900,PROVISIONS,V1,Name,2.50\n";
		using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(trailing)))
		{
			var result = ProductBarcodeCsvParser.Parse(stream);
			Assert.True(result.Success);
			Assert.Single(result.Rows);
			Assert.Null(result.Rows[0].ValidationError);
		}

		var gap = "barcode,Department,,VMECode,ProductName,RetailPrice\n5012345678900,PROVISIONS,,V1,Name,2.50\n";
		using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(gap));
		var result2 = ProductBarcodeCsvParser.Parse(stream2);
		Assert.True(result2.Success);
		Assert.Single(result2.Rows);
		Assert.Null(result2.Rows[0].ValidationError);
	}

	[Theory]
	[InlineData("=1+1", "'=1+1")]
	[InlineData("+cmd", "'+cmd")]
	public void SanitizeFormulaInjectionPrefix_PrefixesDangerousCells(string input, string expected)
	{
		Assert.Equal(expected, ProductBarcodeCsvParser.SanitizeFormulaInjectionPrefix(input));
	}

	[Fact]
	public void ParseCsvLine_HandlesQuotedCommas()
	{
		var line = "\"a,b\",c,d";
		var cells = ProductBarcodeCsvParser.ParseCsvLine(line);
		Assert.Equal(new[] { "a,b", "c", "d" }, cells);
	}
}
