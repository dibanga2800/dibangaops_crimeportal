using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML)</param>
        /// <param name="isHtml">Whether the body is HTML (default: true)</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? fromName = null);

        /// <summary>
        /// Sends an email to multiple recipients asynchronously
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML)</param>
        /// <param name="isHtml">Whether the body is HTML (default: true)</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? fromName = null);

        // Employee-related email methods removed (Employee model deleted)

        /// <summary>
        /// Sends password reset email
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="resetLink">Password reset link</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);

        // SendWelcomeEmailAsync removed (Employee model deleted)

        /// <summary>
        /// Sends system maintenance notification
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <param name="maintenanceDetails">Maintenance details</param>
        /// <returns>True if notifications were sent successfully</returns>
        Task<bool> SendSystemMaintenanceNotificationAsync(IEnumerable<string> recipients, string maintenanceDetails);

        /// <summary>
        /// Sends security incident notification
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <param name="incidentDetails">Incident details</param>
        /// <returns>True if notifications were sent successfully</returns>
        Task<bool> SendSecurityIncidentNotificationAsync(IEnumerable<string> recipients, string incidentDetails);
    }
}
