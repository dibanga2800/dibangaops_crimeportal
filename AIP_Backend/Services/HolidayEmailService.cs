using AIPBackend.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace AIPBackend.Services
{
    public class HolidayEmailService : IHolidayEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HolidayEmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public HolidayEmailService(IConfiguration configuration, ILogger<HolidayEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Configure SMTP settings
            var smtpSettings = _configuration.GetSection("Smtp");
            _fromEmail = smtpSettings["FromEmail"] ?? "AIPMailer@advantage1.co.uk";
            _fromName = smtpSettings["FromName"] ?? "AIP Mailer";

            var smtpHost = smtpSettings["Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
            var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];

            _smtpClient = new SmtpClient
            {
                Host = smtpHost,
                Port = smtpPort,
                EnableSsl = enableSsl
            };

            // Only set credentials if both username and password are provided
            if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpPassword))
            {
                _smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                _logger.LogInformation("Holiday email service initialized with SMTP host: {Host}, Port: {Port}, SSL: {EnableSsl}, Username: {Username}",
                    smtpHost, smtpPort, enableSsl, smtpUsername);
            }
            else
            {
                _logger.LogWarning("Holiday email service initialized without SMTP credentials. Emails may fail to send. Host: {Host}, Port: {Port}, SSL: {EnableSsl}",
                    smtpHost, smtpPort, enableSsl);
            }
        }

        public async Task<bool> SendHolidayRequestSubmittedNotificationAsync(object holidayRequest, object employee, string managerEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(managerEmail))
                {
                    _logger.LogWarning("No manager email provided for holiday request submitted notification");
                    return false;
                }

                var subject = "New Holiday Request Submitted - Action Required";
                var body = GenerateHolidayRequestSubmittedEmailBody(holidayRequest, employee);

                return await SendEmailAsync(managerEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request submitted notification");
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestApprovedNotificationAsync(object holidayRequest, object employee, string approvedBy)
        {
            try
            {
                // Get employee email from employee object
                var employeeEmail = GetEmployeeEmail(employee);
                if (string.IsNullOrEmpty(employeeEmail))
                {
                    _logger.LogWarning("No employee email available for holiday request approved notification. Employee object: {EmployeeType}", 
                        employee?.GetType().Name ?? "null");
                    return false;
                }

                _logger.LogInformation("Sending holiday request approved notification to {EmployeeEmail} for request approved by {ApprovedBy}",
                    employeeEmail, approvedBy);

                var subject = "Holiday Request Approved";
                var body = GenerateHolidayRequestApprovedEmailBody(holidayRequest, employee, approvedBy);

                return await SendEmailAsync(employeeEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request approved notification: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                }
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestRejectedNotificationAsync(object holidayRequest, object employee, string rejectedBy, string rejectionReason)
        {
            try
            {
                // Get employee email from employee object
                var employeeEmail = GetEmployeeEmail(employee);
                if (string.IsNullOrEmpty(employeeEmail))
                {
                    _logger.LogWarning("No employee email available for holiday request rejected notification. Employee object: {EmployeeType}",
                        employee?.GetType().Name ?? "null");
                    return false;
                }

                _logger.LogInformation("Sending holiday request rejected notification to {EmployeeEmail} for request rejected by {RejectedBy}",
                    employeeEmail, rejectedBy);

                var subject = "Holiday Request Update";
                var body = GenerateHolidayRequestRejectedEmailBody(holidayRequest, employee, rejectedBy, rejectionReason);

                return await SendEmailAsync(employeeEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request rejected notification: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                }
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestPendingReminderAsync(object holidayRequest, object employee, string managerEmail, int daysPending)
        {
            try
            {
                if (string.IsNullOrEmpty(managerEmail))
                {
                    _logger.LogWarning("No manager email provided for holiday request pending reminder");
                    return false;
                }

                var subject = $"Holiday Request Pending Approval - {daysPending} Day(s)";
                var body = GenerateHolidayRequestPendingReminderEmailBody(holidayRequest, employee, daysPending);

                return await SendEmailAsync(managerEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request pending reminder");
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestCancelledNotificationAsync(object holidayRequest, object employee, string managerEmail)
        {
            try
            {
                var recipients = new List<string>();

                // Add employee email if available
                var employeeEmail = GetEmployeeEmail(employee);
                if (!string.IsNullOrEmpty(employeeEmail))
                {
                    recipients.Add(employeeEmail);
                }

                // Add manager email if provided
                if (!string.IsNullOrEmpty(managerEmail))
                {
                    recipients.Add(managerEmail);
                }

                if (!recipients.Any())
                {
                    _logger.LogWarning("No recipients available for holiday request cancelled notification");
                    return false;
                }

                var subject = "Holiday Request Cancelled";
                var body = GenerateHolidayRequestCancelledEmailBody(holidayRequest, employee);

                return await SendEmailAsync(recipients, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request cancelled notification");
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestModifiedNotificationAsync(object holidayRequest, object employee, string managerEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(managerEmail))
                {
                    _logger.LogWarning("No manager email provided for holiday request modified notification");
                    return false;
                }

                var subject = "Holiday Request Modified - Review Required";
                var body = GenerateHolidayRequestModifiedEmailBody(holidayRequest, employee);

                return await SendEmailAsync(managerEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request modified notification");
                return false;
            }
        }

        public async Task<bool> SendHolidayBalanceUpdateNotificationAsync(object employee, int remainingDays, int usedDays)
        {
            try
            {
                var employeeEmail = GetEmployeeEmail(employee);
                if (string.IsNullOrEmpty(employeeEmail))
                {
                    _logger.LogWarning("No employee email available for holiday balance update notification");
                    return false;
                }

                var subject = "Holiday Balance Update";
                var body = GenerateHolidayBalanceUpdateEmailBody(employee, remainingDays, usedDays);

                return await SendEmailAsync(employeeEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday balance update notification");
                return false;
            }
        }

        public async Task<bool> SendHolidayRequestConflictNotificationAsync(object holidayRequest, object employee, List<string> conflictingEmployees)
        {
            try
            {
                var employeeEmail = GetEmployeeEmail(employee);
                if (string.IsNullOrEmpty(employeeEmail))
                {
                    _logger.LogWarning("No employee email available for holiday request conflict notification");
                    return false;
                }

                var subject = "Holiday Request - Potential Conflict Detected";
                var body = GenerateHolidayRequestConflictEmailBody(holidayRequest, employee, conflictingEmployees);

                return await SendEmailAsync(employeeEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday request conflict notification");
                return false;
            }
        }

        #region Private Helper Methods

        private async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogWarning("Cannot send email: recipient email is empty");
                    return false;
                }

                _logger.LogInformation("Sending holiday email to {To} with subject: {Subject}", to, subject);
                _logger.LogDebug("Email configuration: From={FromEmail}, Host={Host}, Port={Port}, SSL={EnableSsl}",
                    _fromEmail, _smtpClient.Host, _smtpClient.Port, _smtpClient.EnableSsl);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(to);

                await _smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Holiday email sent successfully to {To}", to);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending holiday email to {To}: {Message} (Status: {Status})",
                    to, smtpEx.Message, smtpEx.StatusCode);
                if (smtpEx.InnerException != null)
                {
                    _logger.LogError("SMTP inner exception: {InnerMessage}", smtpEx.InnerException.Message);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday email to {To}: {Message}", to, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                }
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
        {
            try
            {
                _logger.LogInformation("Sending holiday email to {RecipientCount} recipients with subject: {Subject}", to.Count(), subject);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                foreach (var recipient in to)
                {
                    mailMessage.To.Add(recipient);
                }

                await _smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Holiday email sent successfully to {RecipientCount} recipients", to.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send holiday email to multiple recipients");
                return false;
            }
        }

        private string? GetEmployeeEmail(object employee)
        {
            try
            {
                if (employee == null)
                {
                    _logger.LogWarning("Employee object is null when trying to get email");
                    return null;
                }

                // Try to get Email property using reflection
                var emailProperty = employee.GetType().GetProperty("Email");
                if (emailProperty != null)
                {
                    var email = emailProperty.GetValue(employee)?.ToString();
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        _logger.LogWarning("Employee email property is null or empty. Employee type: {Type}", employee.GetType().Name);
                    }
                    else
                    {
                        _logger.LogDebug("Extracted employee email: {Email}", email);
                    }
                    return email;
                }

                _logger.LogWarning("Employee object does not have an Email property. Type: {Type}", employee.GetType().Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email from employee object. Type: {Type}", employee?.GetType().Name ?? "null");
                return null;
            }
        }

        #endregion

        #region Email Template Generators

        private string GenerateHolidayRequestSubmittedEmailBody(object holidayRequest, object employee)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>New Holiday Request Submitted</h2>");
            sb.AppendLine("<p>A new holiday request has been submitted and requires your approval.</p>");
            sb.AppendLine("<h3>Request Details:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Employee:</strong> {GetEmployeeName(employee)}</li>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Type:</strong> {GetHolidayType(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Reason:</strong> {GetHolidayReason(holidayRequest)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Please review and approve or reject this request at your earliest convenience.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestApprovedEmailBody(object holidayRequest, object employee, string approvedBy)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }");
            sb.AppendLine(".header { background-color: #1a365d; color: #ffffff; padding: 30px 20px; text-align: center; }");
            sb.AppendLine(".header h1 { margin: 0; font-size: 24px; font-weight: 600; }");
            sb.AppendLine(".content { padding: 30px 20px; }");
            sb.AppendLine(".greeting { font-size: 16px; margin-bottom: 20px; }");
            sb.AppendLine(".message { font-size: 16px; margin-bottom: 30px; color: #2563eb; font-weight: 500; }");
            sb.AppendLine(".details-table { width: 100%; border-collapse: collapse; margin: 20px 0; background-color: #f9fafb; }");
            sb.AppendLine(".details-table th { background-color: #1a365d; color: #ffffff; padding: 12px; text-align: left; font-weight: 600; font-size: 14px; }");
            sb.AppendLine(".details-table td { padding: 12px; border-bottom: 1px solid #e5e7eb; font-size: 14px; }");
            sb.AppendLine(".details-table tr:last-child td { border-bottom: none; }");
            sb.AppendLine(".details-table tr:nth-child(even) { background-color: #ffffff; }");
            sb.AppendLine(".note { background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; font-size: 14px; }");
            sb.AppendLine(".footer { background-color: #f9fafb; padding: 20px; text-align: center; font-size: 14px; color: #6b7280; border-top: 1px solid #e5e7eb; }");
            sb.AppendLine(".footer p { margin: 5px 0; }");
            sb.AppendLine(".signature { margin-top: 30px; font-size: 14px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1>Holiday Request Approved</h1>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"content\">");
            sb.AppendLine($"<p class=\"greeting\">Dear {GetEmployeeName(employee)},</p>");
            sb.AppendLine("<p class=\"message\">Your holiday request has been approved!</p>");
            sb.AppendLine("<table class=\"details-table\">");
            sb.AppendLine("<tr><th>Details</th><th>Information</th></tr>");
            sb.AppendLine($"<tr><td><strong>Start Date</strong></td><td>{GetHolidayStartDate(holidayRequest)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>End Date</strong></td><td>{GetHolidayEndDate(holidayRequest)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Days Approved</strong></td><td>{GetHolidayDays(holidayRequest)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Type</strong></td><td>{GetHolidayType(holidayRequest)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Approved By</strong></td><td>{approvedBy}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<div class=\"note\">");
            sb.AppendLine("<strong>Please Note:</strong> Ensure all handover tasks are completed before your leave begins.");
            sb.AppendLine("</div>");
            sb.AppendLine("<p style=\"font-size: 16px; margin-top: 30px; color: #059669;\"><strong>Enjoy your time off!</strong></p>");
            sb.AppendLine("<div class=\"signature\">");
            sb.AppendLine("<p>Best regards,<br/><strong>Crime Portal Team</strong></p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"footer\">");
            sb.AppendLine("<p>This is an automated email. Please do not reply to this message.</p>");
            sb.AppendLine("<p>&copy; " + DateTime.Now.Year + " Crime Portal. All rights reserved.</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestRejectedEmailBody(object holidayRequest, object employee, string rejectedBy, string rejectionReason)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Request Update</h2>");
            sb.AppendLine($"<p>Dear {GetEmployeeName(employee)},</p>");
            sb.AppendLine("<p>Your holiday request has been reviewed and unfortunately cannot be approved at this time.</p>");
            sb.AppendLine("<h3>Request Details:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Type:</strong> {GetHolidayType(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Reviewed By:</strong> {rejectedBy}</li>");
            sb.AppendLine($"<li><strong>Reason:</strong> {rejectionReason}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Please contact your manager if you have any questions or would like to discuss alternative arrangements.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestPendingReminderEmailBody(object holidayRequest, object employee, int daysPending)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Request Pending Approval</h2>");
            sb.AppendLine($"<p>A holiday request has been pending your approval for {daysPending} day(s).</p>");
            sb.AppendLine("<h3>Request Details:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Employee:</strong> {GetEmployeeName(employee)}</li>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Type:</strong> {GetHolidayType(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Pending:</strong> {daysPending}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Please review and take action on this request as soon as possible.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestCancelledEmailBody(object holidayRequest, object employee)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Request Cancelled</h2>");
            sb.AppendLine("<p>A holiday request has been cancelled.</p>");
            sb.AppendLine("<h3>Cancelled Request Details:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Employee:</strong> {GetEmployeeName(employee)}</li>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Type:</strong> {GetHolidayType(holidayRequest)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>This request has been removed from the approval queue.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestModifiedEmailBody(object holidayRequest, object employee)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Request Modified</h2>");
            sb.AppendLine("<p>A holiday request has been modified and requires your review.</p>");
            sb.AppendLine("<h3>Modified Request Details:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Employee:</strong> {GetEmployeeName(employee)}</li>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Type:</strong> {GetHolidayType(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Reason:</strong> {GetHolidayReason(holidayRequest)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Please review the changes and approve or reject this request.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayBalanceUpdateEmailBody(object employee, int remainingDays, int usedDays)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Balance Update</h2>");
            sb.AppendLine($"<p>Dear {GetEmployeeName(employee)},</p>");
            sb.AppendLine("<p>Your holiday balance has been updated.</p>");
            sb.AppendLine("<h3>Current Balance:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Remaining Days:</strong> {remainingDays}</li>");
            sb.AppendLine($"<li><strong>Used Days:</strong> {usedDays}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Please plan your remaining leave accordingly.</p>");
            sb.AppendLine("<p>Best regards,<br/>Crime Portal Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateHolidayRequestConflictEmailBody(object holidayRequest, object employee, List<string> conflictingEmployees)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Holiday Request - Potential Conflict</h2>");
            sb.AppendLine($"<p>Dear {GetEmployeeName(employee)},</p>");
            sb.AppendLine("<p>Your holiday request may conflict with other employees' leave during the same period.</p>");
            sb.AppendLine("<h3>Your Request:</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li><strong>Start Date:</strong> {GetHolidayStartDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>End Date:</strong> {GetHolidayEndDate(holidayRequest)}</li>");
            sb.AppendLine($"<li><strong>Days Requested:</strong> {GetHolidayDays(holidayRequest)}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<h3>Employees with Overlapping Leave:</h3>");
            sb.AppendLine("<ul>");
            foreach (var conflictingEmployee in conflictingEmployees)
            {
                sb.AppendLine($"<li>{conflictingEmployee}</li>");
            }
            sb.AppendLine("</ul>");
            sb.AppendLine("<p>Your request will still be processed, but please be aware of this potential conflict.</p>");
            sb.AppendLine("<p>Best regards,<br/>Advantage One Security Team</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        #endregion

        #region Helper Methods for Extracting Data from Objects

        private string GetEmployeeName(object employee)
        {
            try
            {
                var nameProperty = employee.GetType().GetProperty("FullName") ?? 
                                  employee.GetType().GetProperty("Name") ??
                                  employee.GetType().GetProperty("FirstName");
                return nameProperty?.GetValue(employee)?.ToString() ?? "Employee";
            }
            catch
            {
                return "Employee";
            }
        }

        private string GetHolidayStartDate(object holidayRequest)
        {
            try
            {
                var startDateProperty = holidayRequest.GetType().GetProperty("StartDate");
                var startDateValue = startDateProperty?.GetValue(holidayRequest);
                
                if (startDateValue == null) return "N/A";
                
                // Try to parse as DateTime or string
                if (DateTime.TryParse(startDateValue.ToString(), out var dateTime))
                {
                    return dateTime.ToString("dd/MM/yyyy");
                }
                
                return startDateValue.ToString() ?? "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetHolidayEndDate(object holidayRequest)
        {
            try
            {
                var endDateProperty = holidayRequest.GetType().GetProperty("EndDate");
                var endDateValue = endDateProperty?.GetValue(holidayRequest);
                
                if (endDateValue == null) return "N/A";
                
                // Try to parse as DateTime or string
                if (DateTime.TryParse(endDateValue.ToString(), out var dateTime))
                {
                    return dateTime.ToString("dd/MM/yyyy");
                }
                
                return endDateValue.ToString() ?? "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetHolidayDays(object holidayRequest)
        {
            try
            {
                // Try TotalDays first (used in HolidayRequestDto), then Days, then NumberOfDays
                var daysProperty = holidayRequest.GetType().GetProperty("TotalDays") ??
                                  holidayRequest.GetType().GetProperty("Days") ?? 
                                  holidayRequest.GetType().GetProperty("NumberOfDays");
                var days = daysProperty?.GetValue(holidayRequest);
                if (days != null && int.TryParse(days.ToString(), out var daysValue))
                {
                    return $"{daysValue} {(daysValue == 1 ? "day" : "days")}";
                }
                return days?.ToString() ?? "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetHolidayType(object holidayRequest)
        {
            try
            {
                var typeProperty = holidayRequest.GetType().GetProperty("Type") ?? 
                                  holidayRequest.GetType().GetProperty("HolidayType");
                var type = typeProperty?.GetValue(holidayRequest);
                return type?.ToString() ?? "Annual Leave";
            }
            catch
            {
                return "Annual Leave";
            }
        }

        private string GetHolidayReason(object holidayRequest)
        {
            try
            {
                var reasonProperty = holidayRequest.GetType().GetProperty("Reason") ?? 
                                    holidayRequest.GetType().GetProperty("Description");
                var reason = reasonProperty?.GetValue(holidayRequest);
                return reason?.ToString() ?? "Not specified";
            }
            catch
            {
                return "Not specified";
            }
        }

        #endregion
    }
}
