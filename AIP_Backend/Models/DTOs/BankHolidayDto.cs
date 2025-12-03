using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
	public class BankHolidayDto
	{
		public string Id { get; set; } = string.Empty;
		public int OfficerId { get; set; }
		public string OfficerName { get; set; } = string.Empty;
		public string OfficerNumber { get; set; } = string.Empty;
		public string HolidayDate { get; set; } = string.Empty;
		public string DateOfRequest { get; set; } = string.Empty;
		public int? AuthorisedByEmployeeId { get; set; }
		public string? AuthorisedByName { get; set; }
		public string? DateAuthorised { get; set; }
		public string Status { get; set; } = "pending";
		public string? Reason { get; set; }
		public bool Archived { get; set; }
		public string CreatedAt { get; set; } = string.Empty;
		public string? UpdatedAt { get; set; }
	}

	public class CreateBankHolidayDto
	{
		[Required]
		public int OfficerId { get; set; }

		[Required]
		public string HolidayDate { get; set; } = string.Empty;

		public string? Reason { get; set; }
	}

	public class UpdateBankHolidayDto
	{
		public int? OfficerId { get; set; }
		public string? HolidayDate { get; set; }
		public int? AuthorisedByEmployeeId { get; set; }
		public string? DateAuthorised { get; set; }

		[StringLength(20)]
		public string? Status { get; set; }

		public string? Reason { get; set; }
		public bool? Archived { get; set; }
	}

	public class BankHolidayListResponseDto
	{
		public List<BankHolidayDto> Data { get; set; } = new();
		public int Total { get; set; }
		public int Page { get; set; }
		public int Limit { get; set; }
		public int TotalPages { get; set; }
	}
}

