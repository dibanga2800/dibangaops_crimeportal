using System.ComponentModel.DataAnnotations;

namespace AIPBackend.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // Present when login is fully completed
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto User { get; set; } = null!;

        // For step 1: indicates that a second factor is required instead of tokens
        public bool RequiresTwoFactor { get; set; }
        public string[] TwoFactorMethods { get; set; } = Array.Empty<string>();

        /// <summary>
        /// True when the verification code email was accepted by the mail provider.
        /// </summary>
        public bool TwoFactorEmailSent { get; set; }

        /// <summary>
        /// User-facing hint when the code email could not be delivered.
        /// </summary>
        public string? TwoFactorDeliveryMessage { get; set; }

        /// <summary>
        /// CSRF token for cross-origin SPAs (cookie is host-only on the API domain).
        /// </summary>
        public string? CsrfToken { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        /// <summary>Optional when refresh token is sent via HttpOnly cookie.</summary>
        public string? RefreshToken { get; set; }
    }

    public class RefreshTokenResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponseDto? User { get; set; }
        public string? CsrfToken { get; set; }
    }

    public class LogoutRequestDto
    {
        /// <summary>Optional when refresh token is sent via HttpOnly cookie.</summary>
        public string? RefreshToken { get; set; }
    }

    public class LogoutResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateProfileRequestDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(1500000)]
        public string? ProfilePicture { get; set; }

        public bool? ClearProfilePicture { get; set; }

        public bool? TwoFactorEnabled { get; set; }

        public bool? EmailNotificationsEnabled { get; set; }

        public bool? LoginAlertsEnabled { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class CompleteTwoFactorLoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(12)]
        public string Code { get; set; } = string.Empty;
    }

    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PaginatedResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
