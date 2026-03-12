using System.Collections.Concurrent;
using AIPBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILoginProtectionService" />.
    /// Uses ApplicationUser.LoginAttempts / LockoutUntil for durable per-user lockout
    /// and an in-memory sliding window for basic per-IP throttling.
    /// </summary>
    public sealed class LoginProtectionService : ILoginProtectionService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginProtectionService> _logger;

        // In-memory per-IP failure tracking (process-local, resets on restart)
        private readonly ConcurrentDictionary<string, IpFailureWindow> _ipFailures = new();

        // Policy configuration (with sensible defaults)
        private readonly int _shortLockFailures;
        private readonly TimeSpan _shortLockDuration;
        private readonly int _longLockFailures;
        private readonly TimeSpan _longLockDuration;
        private readonly int _ipWindowFailures;
        private readonly TimeSpan _ipWindowDuration;

        public LoginProtectionService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<LoginProtectionService> logger)
        {
            _userManager = userManager;
            _logger = logger;

            var section = configuration.GetSection("LoginProtection");
            _shortLockFailures = section.GetValue("MaxFailuresBeforeShortLock", 5);
            _shortLockDuration = TimeSpan.FromMinutes(section.GetValue("ShortLockMinutes", 10));
            _longLockFailures = section.GetValue("MaxFailuresBeforeLongLock", 10);
            _longLockDuration = TimeSpan.FromMinutes(section.GetValue("LongLockMinutes", 60));

            _ipWindowFailures = section.GetValue("IpMaxFailuresInWindow", 50);
            _ipWindowDuration = TimeSpan.FromMinutes(section.GetValue("IpWindowMinutes", 10));
        }

        public Task<LoginProtectionResult> CheckPreLoginAsync(ApplicationUser? user, string normalizedEmail, string ipAddress)
        {
            var now = DateTime.UtcNow;

            // User-level lockout (durable)
            if (user != null && user.LockoutUntil.HasValue && user.LockoutUntil.Value > now)
            {
                return Task.FromResult(new LoginProtectionResult
                {
                    IsBlocked = true,
                    IsUserLockedOut = true,
                    LockoutUntilUtc = user.LockoutUntil,
                    Reason = "User lockout in effect"
                });
            }

            // IP-level soft throttling (in-memory)
            if (IsIpWindowExceeded(ipAddress, now, out var windowInfo))
            {
                _logger.LogWarning(
                    "IP-level throttling in effect for {IpAddress}. FailuresInWindow={Count}, WindowStartUtc={WindowStartUtc}",
                    ipAddress,
                    windowInfo!.FailureCount,
                    windowInfo.WindowStartUtc);

                return Task.FromResult(new LoginProtectionResult
                {
                    IsBlocked = true,
                    IsIpThrottled = true,
                    Reason = "Too many login attempts from this IP"
                });
            }

            return Task.FromResult(new LoginProtectionResult
            {
                IsBlocked = false,
                IsUserLockedOut = false,
                IsIpThrottled = false
            });
        }

        public async Task RegisterLoginFailureAsync(ApplicationUser? user, string normalizedEmail, string ipAddress)
        {
            var now = DateTime.UtcNow;

            // Update per-user counters and potential lockout
            if (user != null)
            {
                user.LoginAttempts++;

                if (user.LoginAttempts >= _longLockFailures)
                {
                    user.LockoutUntil = now.Add(_longLockDuration);
                    _logger.LogWarning(
                        "User {UserId} reached long lockout threshold: Attempts={Attempts}, LockoutUntil={LockoutUntilUtc}",
                        user.Id,
                        user.LoginAttempts,
                        user.LockoutUntil);
                }
                else if (user.LoginAttempts >= _shortLockFailures &&
                         (!user.LockoutUntil.HasValue || user.LockoutUntil.Value <= now))
                {
                    user.LockoutUntil = now.Add(_shortLockDuration);
                    _logger.LogInformation(
                        "User {UserId} reached short lockout threshold: Attempts={Attempts}, LockoutUntil={LockoutUntilUtc}",
                        user.Id,
                        user.LoginAttempts,
                        user.LockoutUntil);
                }

                await _userManager.UpdateAsync(user);
            }

            // Track per-IP failures in a sliding window
            _ipFailures.AddOrUpdate(
                ipAddress,
                _ => new IpFailureWindow
                {
                    WindowStartUtc = now,
                    FailureCount = 1
                },
                (_, existing) =>
                {
                    if (now - existing.WindowStartUtc > _ipWindowDuration)
                    {
                        // Start a new window
                        existing.WindowStartUtc = now;
                        existing.FailureCount = 1;
                    }
                    else
                    {
                        existing.FailureCount++;
                    }

                    return existing;
                });
        }

        public async Task RegisterLoginSuccessAsync(ApplicationUser? user, string normalizedEmail, string ipAddress)
        {
            // Reset per-user counters on successful login
            if (user != null)
            {
                if (user.LoginAttempts > 0 || user.LockoutUntil != null)
                {
                    user.LoginAttempts = 0;
                    user.LockoutUntil = null;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Optionally, we could also clear IP failure window for this IP to be more forgiving,
            // but for now we just leave it to naturally expire.
        }

        private bool IsIpWindowExceeded(string ipAddress, DateTime now, out IpFailureWindow? window)
        {
            window = null;

            if (!_ipFailures.TryGetValue(ipAddress, out var state))
            {
                return false;
            }

            if (now - state.WindowStartUtc > _ipWindowDuration)
            {
                // Window expired; reset state on next failure
                return false;
            }

            window = state;
            return state.FailureCount >= _ipWindowFailures;
        }

        private sealed class IpFailureWindow
        {
            public DateTime WindowStartUtc { get; set; }
            public int FailureCount { get; set; }
        }
    }
}

