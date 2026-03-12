using AIPBackend.Models;

namespace AIPBackend.Services
{
    /// <summary>
    /// Centralised service for protecting authentication endpoints against brute-force attacks.
    /// Tracks per-user failures using ApplicationUser fields and applies basic per-IP throttling
    /// using in-memory counters.
    /// </summary>
    public interface ILoginProtectionService
    {
        /// <summary>
        /// Check whether a login attempt should be blocked before verifying credentials.
        /// This inspects existing lockout state on the user and per-IP throttling.
        /// </summary>
        /// <param name="user">Resolved user, if any.</param>
        /// <param name="normalizedEmail">Normalized email/username used for login.</param>
        /// <param name="ipAddress">Client IP address (best-effort).</param>
        /// <returns>Result describing whether the attempt is allowed and any lockout metadata.</returns>
        Task<LoginProtectionResult> CheckPreLoginAsync(ApplicationUser? user, string normalizedEmail, string ipAddress);

        /// <summary>
        /// Register a failed login attempt. This updates per-user counters and may set lockout windows.
        /// It also feeds per-IP throttling state.
        /// </summary>
        Task RegisterLoginFailureAsync(ApplicationUser? user, string normalizedEmail, string ipAddress);

        /// <summary>
        /// Register a successful login. This resets per-user counters and clears transient IP state.
        /// </summary>
        Task RegisterLoginSuccessAsync(ApplicationUser? user, string normalizedEmail, string ipAddress);
    }

    public sealed class LoginProtectionResult
    {
        public bool IsBlocked { get; init; }
        public bool IsUserLockedOut { get; init; }
        public bool IsIpThrottled { get; init; }
        public DateTime? LockoutUntilUtc { get; init; }
        public string? Reason { get; init; }
    }
}

