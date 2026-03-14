#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIPBackend.Models
{
	/// <summary>
	/// Stores a single face embedding vector for an offender-related image.
	/// This allows the computer-vision layer to perform similarity search
	/// without re-processing raw images on every request.
	/// </summary>
	public class FaceEmbedding
	{
		[Key]
		public int FaceEmbeddingId { get; set; }

		/// <summary>
		/// Optional link back to a known offender identity where one exists.
		/// </summary>
		public string? OffenderId { get; set; }

		/// <summary>
		/// Optional link back to the originating incident.
		/// </summary>
		public int? IncidentId { get; set; }

		/// <summary>
		/// Logical file name or blob name of the source image in storage.
		/// </summary>
		[Required]
		[MaxLength(200)]
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// Model identifier used to generate the embedding (e.g. facenet-v1).
		/// </summary>
		[MaxLength(100)]
		public string ModelId { get; set; } = "unknown-model";

		/// <summary>
		/// Serialised embedding vector (e.g. JSON array or base64-encoded float buffer).
		/// Using nvarchar(max) keeps the store simple; a specialised vector store
		/// can be introduced later without changing consumers of this entity.
		/// </summary>
		[Column(TypeName = "nvarchar(max)")]
		public string Embedding { get; set; } = string.Empty;

		/// <summary>
		/// Optional metadata about how similar this embedding is to the best match
		/// at the time of creation (useful for auditing).
		/// </summary>
		public double? BestMatchSimilarity { get; set; }

		/// <summary>
		/// Azure Face API Person ID when using Person Group for offender recognition.
		/// Maps to the enrolled person in the Face API Person Group.
		/// </summary>
		[MaxLength(36)]
		public string? AzurePersonId { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[MaxLength(450)]
		public string? CreatedBy { get; set; }
	}
}

