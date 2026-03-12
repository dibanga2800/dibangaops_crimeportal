using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace AIPBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient? _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _emailProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _graphTenantId;
        private readonly string? _graphClientId;
        private readonly string? _graphClientSecret;
        private readonly string? _graphFromUser;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // Configure SMTP settings
            var smtpSettings = _configuration.GetSection("Smtp");
            _fromEmail = smtpSettings["FromEmail"] ?? "AIPMailer@advantage1.co.uk";
            _fromName = smtpSettings["FromName"] ?? "AIP Mailer";
            _emailProvider = _configuration["EmailProvider"] ?? "Smtp";

            var port = int.Parse(smtpSettings["Port"] ?? "25");
            var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "false");
            
            // For port 587, TLS is typically required (EnableSsl = true)
            // For port 465, SSL is required (EnableSsl = true)
            if (port == 587 || port == 465)
            {
                enableSsl = true;
            }

            var smtpHost = smtpSettings["Host"] ?? "smtp.gmail.com";
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];

            var graphSettings = _configuration.GetSection("GraphEmail");
            _graphTenantId = graphSettings["TenantId"];
            _graphClientId = graphSettings["ClientId"];
            _graphClientSecret = graphSettings["ClientSecret"];
            _graphFromUser = graphSettings["FromUser"] ?? _fromEmail;

            if (string.Equals(_emailProvider, "Graph", StringComparison.OrdinalIgnoreCase))
            {
                _smtpClient = null;
                _logger.LogInformation("Email service initialized with Microsoft Graph provider. FromUser: {FromUser}", _graphFromUser ?? "NOT SET");
            }
            else
            {
                // Handle SSL certificate validation for mail servers with self-signed or mismatched certificates
                // This is necessary for mail.advantage1.co.uk which may have certificate name mismatches
                if (enableSsl)
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                        delegate (object s, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
                        {
                            // Accept certificates for the configured mail server
                            // In production, you may want to add more specific validation
                            return true;
                        };
                }

                _smtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = port,
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(
                        smtpUsername,
                        smtpPassword
                    ),
                    Timeout = 30000, // 30 seconds timeout
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                _logger.LogInformation("Email service initialized - Host: {Host}, Port: {Port}, SSL: {EnableSsl}, Username: {Username}",
                    smtpHost, port, enableSsl, smtpUsername ?? "NOT SET");
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? fromName = null)
        {
            try
            {
                if (string.Equals(_emailProvider, "Graph", StringComparison.OrdinalIgnoreCase))
                {
                    return await SendGraphEmailAsync(new[] { to }, subject, body, isHtml, fromName);
                }

                if (_smtpClient == null)
                {
                    _logger.LogError("SMTP client is not configured.");
                    return false;
                }

                _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
                _logger.LogInformation("SMTP Config - Host: {Host}, Port: {Port}, SSL: {Ssl}, From: {From}",
                    _smtpClient.Host, _smtpClient.Port, _smtpClient.EnableSsl, _fromEmail);

                using (var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, fromName ?? _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                })
                {
                    mailMessage.To.Add(to);

                    await _smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation("Email sent successfully to {To}", to);
                    return true;
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP Error sending email to {To}. StatusCode: {StatusCode}, Message: {Message}", 
                    to, smtpEx.StatusCode, smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}. Error: {ErrorType}, Message: {Message}", 
                    to, ex.GetType().Name, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? fromName = null)
        {
            try
            {
                if (string.Equals(_emailProvider, "Graph", StringComparison.OrdinalIgnoreCase))
                {
                    return await SendGraphEmailAsync(to, subject, body, isHtml, fromName);
                }

                if (_smtpClient == null)
                {
                    _logger.LogError("SMTP client is not configured.");
                    return false;
                }

                _logger.LogInformation("Sending email to {RecipientCount} recipients with subject: {Subject}", to.Count(), subject);

                using (var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, fromName ?? _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                })
                {
                    foreach (var recipient in to)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await _smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation("Email sent successfully to {RecipientCount} recipients", to.Count());
                    return true;
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP Error sending email to multiple recipients. StatusCode: {StatusCode}, Message: {Message}", 
                    smtpEx.StatusCode, smtpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients. Error: {ErrorType}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                return false;
            }
        }

        // Employee-related email methods removed (Employee model deleted)

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var subject = "Password Reset Request - Crime Portal";
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
                var subject = "System Maintenance Notification - Crime Portal";
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
                var subject = "Security Incident Alert - Crime Portal";
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
            sb.AppendLine("<p>You have requested a password reset for your Crime Portal account.</p>");
            sb.AppendLine("<p>Click the link below to reset your password:</p>");
            sb.AppendLine($"<p><a href=\"{resetLink}\" style=\"background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;\">Reset Password</a></p>");
            sb.AppendLine("<p>If you did not request this password reset, please ignore this email.</p>");
            sb.AppendLine("<p>This link will expire in 24 hours for security reasons.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        // GenerateWelcomeEmailBody removed (Employee model deleted)

        private string GenerateSystemMaintenanceEmailBody(string maintenanceDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>System Maintenance Notification</h2>");
            sb.AppendLine("<p>Crime Portal will be performing scheduled system maintenance.</p>");
            sb.AppendLine("<h3>Maintenance Details:</h3>");
            sb.AppendLine($"<p>{maintenanceDetails}</p>");
            sb.AppendLine("<p>We apologize for any inconvenience this may cause.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateSecurityIncidentEmailBody(string incidentDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Security Incident Alert</h2>");
            sb.AppendLine("<p>A security incident has been reported in the Crime Portal system.</p>");
            sb.AppendLine("<h3>Incident Details:</h3>");
            sb.AppendLine($"<p>{incidentDetails}</p>");
            sb.AppendLine("<p>Please review the incident and take appropriate action as required.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        #endregion

        private bool HasGraphConfig()
        {
            return !string.IsNullOrWhiteSpace(_graphTenantId)
                && !string.IsNullOrWhiteSpace(_graphClientId)
                && !string.IsNullOrWhiteSpace(_graphClientSecret)
                && !string.IsNullOrWhiteSpace(_graphFromUser);
        }

        private async Task<bool> SendGraphEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml, string? fromName)
        {
            if (!HasGraphConfig())
            {
                _logger.LogError("Microsoft Graph email settings are incomplete.");
                return false;
            }

            var accessToken = await GetGraphAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Unable to acquire Microsoft Graph access token.");
                return false;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var recipients = to
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .Select(address => new
                {
                    emailAddress = new { address }
                })
                .ToList();

            var payload = new
            {
                message = new
                {
                    subject,
                    body = new
                    {
                        contentType = isHtml ? "HTML" : "Text",
                        content = body
                    },
                    toRecipients = recipients,
                    from = new
                    {
                        emailAddress = new
                        {
                            address = _graphFromUser,
                            name = fromName ?? _fromName
                        }
                    }
                },
                saveToSentItems = "true"
            };

            var json = JsonSerializer.Serialize(payload);
            var requestUrl = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(_graphFromUser!)}/sendMail";
            var response = await httpClient.PostAsync(requestUrl, new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Graph email sent successfully to {RecipientCount} recipients", recipients.Count);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Graph email failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
            return false;
        }

        private async Task<string?> GetGraphAccessTokenAsync()
        {
            if (!HasGraphConfig())
            {
                return null;
            }

            var httpClient = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"https://login.microsoftonline.com/{_graphTenantId}/oauth2/v2.0/token";

            var form = new Dictionary<string, string>
            {
                ["client_id"] = _graphClientId!,
                ["client_secret"] = _graphClientSecret!,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials"
            };

            var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Graph token request failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                return tokenElement.GetString();
            }

            _logger.LogError("Graph token response missing access_token.");
            return null;
        }
    }
}
