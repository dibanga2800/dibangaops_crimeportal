#nullable enable

using AIPBackend.Services;
using Xunit;

namespace AIPBackend.Tests
{
	public class IncidentRiskLevelTests
	{
		[Theory]
		[InlineData(0.0, "low")]
		[InlineData(0.1, "low")]
		[InlineData(0.399, "low")]
		[InlineData(0.4, "medium")]
		[InlineData(0.5, "medium")]
		[InlineData(0.699, "medium")]
		[InlineData(0.7, "high")]
		[InlineData(0.85, "high")]
		[InlineData(1.0, "high")]
		public void FromScore_MapsToCorrectBucket(double score, string expected)
		{
			Assert.Equal(expected, IncidentRiskLevel.FromScore(score));
		}

		[Theory]
		[InlineData(-0.5, "low")]
		[InlineData(1.5, "high")]
		[InlineData(double.NaN, "low")]
		public void FromScore_ClampsAndBucketsOutOfRange(double score, string expected)
		{
			Assert.Equal(expected, IncidentRiskLevel.FromScore(score));
		}

		[Theory]
		[InlineData(-1.0, 0.0)]
		[InlineData(2.0, 1.0)]
		[InlineData(double.NaN, 0.0)]
		[InlineData(0.5, 0.5)]
		public void ClampScore_KeepsScoreInRange(double input, double expected)
		{
			Assert.Equal(expected, IncidentRiskLevel.ClampScore(input), 2);
		}
	}
}
