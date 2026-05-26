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

		// --- Bucket: derives level from PRECISE score, then rounds for storage. ---
		// These tests guard the bug where the previous implementation rounded the
		// score first and then bucketed, mis-classifying values just below a
		// threshold (e.g. 0.6999 -> rounded 0.70 -> wrongly "high").

		[Theory]
		[InlineData(0.6999, "medium")] // < 0.7 stays medium even though it rounds to 0.70
		[InlineData(0.7000, "high")]
		[InlineData(0.7001, "high")]
		[InlineData(0.3999, "low")]    // < 0.4 stays low even though it rounds to 0.40
		[InlineData(0.4000, "medium")]
		[InlineData(0.4001, "medium")]
		[InlineData(0.0, "low")]
		[InlineData(1.0, "high")]
		public void Bucket_LevelIsDerivedFromPreciseScoreNotRoundedScore(double rawScore, string expectedLevel)
		{
			var (level, _) = IncidentRiskLevel.Bucket(rawScore);
			Assert.Equal(expectedLevel, level);
		}

		[Theory]
		[InlineData(0.6999, 0.70)] // stored score IS rounded for display
		[InlineData(0.3999, 0.40)]
		[InlineData(0.12345, 0.12)]
		[InlineData(0.0, 0.0)]
		[InlineData(1.0, 1.0)]
		public void Bucket_StoredScoreIsRoundedToTwoDecimalPlaces(double rawScore, double expectedStored)
		{
			var (_, stored) = IncidentRiskLevel.Bucket(rawScore);
			Assert.Equal(expectedStored, stored, precision: 2);
		}

		[Theory]
		[InlineData(-0.5, "low", 0.0)]
		[InlineData(1.5, "high", 1.0)]
		[InlineData(double.NaN, "low", 0.0)]
		public void Bucket_ClampsOutOfRangeInputsBeforeBucketing(double rawScore, string expectedLevel, double expectedStored)
		{
			var (level, stored) = IncidentRiskLevel.Bucket(rawScore);
			Assert.Equal(expectedLevel, level);
			Assert.Equal(expectedStored, stored, precision: 2);
		}
	}
}
