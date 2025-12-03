using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace AIPBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Configure SMTP settings
            var smtpSettings = _configuration.GetSection("Smtp");
            _fromEmail = smtpSettings["FromEmail"] ?? "AIPMailer@advantage1.co.uk";
            _fromName = smtpSettings["FromName"] ?? "AIP Mailer";

            _smtpClient = new SmtpClient
            {
                Host = smtpSettings["Host"] ?? "smtp.gmail.com",
                Port = int.Parse(smtpSettings["Port"] ?? "25"),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "false"),
                Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"]
                ),
                Timeout = 30000, // 30 seconds timeout
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            _logger.LogInformation("Email service initialized with SMTP host: {Host}", _smtpClient.Host);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? fromName = null)
        {
            try
            {
                _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, fromName ?? _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(to);

                await _smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? fromName = null)
        {
            try
            {
                _logger.LogInformation("Sending email to {RecipientCount} recipients with subject: {Subject}", to.Count(), subject);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, fromName ?? _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                foreach (var recipient in to)
                {
                    mailMessage.To.Add(recipient);
                }

                await _smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {RecipientCount} recipients", to.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients");
                return false;
            }
        }

        // Employee-related email methods removed (Employee model deleted)

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var subject = "Password Reset Request - Advantage One Security";
                var body = GeneratePasswordResetEmailBody(resetLink);

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }

        // SendWelcomeEmailAsync removed (Employee model deleted)

        public async Task<bool> SendSystemMaintenanceNotificationAsync(IEnumerable<string> recipients, string maintenanceDetails)
        {
            try
            {
                var subject = "System Maintenance Notification - Advantage One Security";
                var body = GenerateSystemMaintenanceEmailBody(maintenanceDetails);

                return await SendEmailAsync(recipients, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system maintenance notification");
                return false;
            }
        }

        public async Task<bool> SendSecurityIncidentNotificationAsync(IEnumerable<string> recipients, string incidentDetails)
        {
            try
            {
                var subject = "Security Incident Alert - Advantage One Security";
                var body = GenerateSecurityIncidentEmailBody(incidentDetails);

                return await SendEmailAsync(recipients, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security incident notification");
                return false;
            }
        }

        #region Email Template Generators

        // Employee email template generators removed (Employee model deleted)

        private string GeneratePasswordResetEmailBody(string resetLink)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Password Reset Request</h2>");
            sb.AppendLine("<p>You have requested a password reset for your Advantage One Security account.</p>");
            sb.AppendLine("<p>Click the link below to reset your password:</p>");
            sb.AppendLine($"<p><a href=\"{resetLink}\" style=\"background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;\">Reset Password</a></p>");
            sb.AppendLine("<p>If you did not request this password reset, please ignore this email.</p>");
            sb.AppendLine("<p>This link will expire in 24 hours for security reasons.</p>");
            sb.AppendLine("<p>Best regards,<br/>Advantage One Security Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        // GenerateWelcomeEmailBody removed (Employee model deleted)

        private string GenerateSystemMaintenanceEmailBody(string maintenanceDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>System Maintenance Notification</h2>");
            sb.AppendLine("<p>Advantage One Security will be performing scheduled system maintenance.</p>");
            sb.AppendLine("<h3>Maintenance Details:</h3>");
            sb.AppendLine($"<p>{maintenanceDetails}</p>");
            sb.AppendLine("<p>We apologize for any inconvenience this may cause.</p>");
            sb.AppendLine("<p>Best regards,<br/>Advantage One Security Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateSecurityIncidentEmailBody(string incidentDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Security Incident Alert</h2>");
            sb.AppendLine("<p>A security incident has been reported in the Advantage One Security system.</p>");
            sb.AppendLine("<h3>Incident Details:</h3>");
            sb.AppendLine($"<p>{incidentDetails}</p>");
            sb.AppendLine("<p>Please review the incident and take appropriate action as required.</p>");
            sb.AppendLine("<p>Best regards,<br/>Advantage One Security Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        #endregion
    }
}
