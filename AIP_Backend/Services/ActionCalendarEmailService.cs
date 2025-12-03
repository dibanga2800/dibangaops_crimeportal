using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIPBackend.Models;
using Microsoft.Extensions.Logging;

namespace AIPBackend.Services
{
    public class ActionCalendarEmailService : IActionCalendarEmailService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ActionCalendarEmailService> _logger;

        public ActionCalendarEmailService(IEmailService emailService, ILogger<ActionCalendarEmailService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendTaskAssignmentNotificationAsync(ActionCalendar actionCalendar)
        {
            var recipients = GetNotificationRecipients(actionCalendar);
            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping task assignment notification for ActionCalendarId {TaskId} because no recipients were resolved.", actionCalendar.ActionCalendarId);
                return;
            }

            var subject = $"[Action Calendar] New Task Assigned - {actionCalendar.TaskTitle}";
            var body = BuildAssignmentEmailBody(actionCalendar);

            await _emailService.SendEmailAsync(recipients, subject, body, true, "Action Calendar Notifications");
        }

        public async Task SendTaskStatusUpdatedNotificationAsync(ActionCalendar actionCalendar, ActionCalendarStatusUpdate statusUpdate)
        {
            var recipients = GetNotificationRecipients(actionCalendar);
            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping task status notification for ActionCalendarId {TaskId} because no recipients were resolved.", actionCalendar.ActionCalendarId);
                return;
            }

            var subject = $"[Action Calendar] Task Updated - {actionCalendar.TaskTitle}";
            var body = BuildStatusEmailBody(actionCalendar, statusUpdate);

            await _emailService.SendEmailAsync(recipients, subject, body, true, "Action Calendar Notifications");
        }

        private static IEnumerable<string> GetNotificationRecipients(ActionCalendar actionCalendar)
        {
            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(actionCalendar.AssignedUser?.Email))
            {
                recipients.Add(actionCalendar.AssignedUser.Email);
            }

            if (!string.IsNullOrWhiteSpace(actionCalendar.CreatedByUser?.Email))
            {
                recipients.Add(actionCalendar.CreatedByUser.Email);
            }

            if (!string.IsNullOrWhiteSpace(actionCalendar.Email))
            {
                recipients.Add(actionCalendar.Email);
            }

            return recipients;
        }

        private static string BuildAssignmentEmailBody(ActionCalendar actionCalendar)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<html><body>");
            builder.AppendLine("<h2>New Action Calendar Task Assigned</h2>");
            builder.AppendLine("<p>A new task has been created and assigned.</p>");
            builder.AppendLine("<ul>");
            builder.AppendLine($"<li><strong>Title:</strong> {actionCalendar.TaskTitle}</li>");
            builder.AppendLine($"<li><strong>Description:</strong> {actionCalendar.TaskDescription}</li>");
            builder.AppendLine($"<li><strong>Priority:</strong> {actionCalendar.PriorityLevel}</li>");
            builder.AppendLine($"<li><strong>Due Date:</strong> {actionCalendar.DueDate:dd MMM yyyy}</li>");
            builder.AppendLine($"<li><strong>Status:</strong> {actionCalendar.TaskStatus}</li>");
            builder.AppendLine($"<li><strong>Assigned To:</strong> {actionCalendar.AssignedUser?.FullName ?? "Unassigned"}</li>");
            builder.AppendLine($"<li><strong>Created By:</strong> {actionCalendar.CreatedByUser?.FullName ?? "System"}</li>");
            builder.AppendLine("</ul>");
            builder.AppendLine("<p>Please log into the Action Calendar to review and take the necessary actions.</p>");
            builder.AppendLine("</body></html>");
            return builder.ToString();
        }

        private static string BuildStatusEmailBody(ActionCalendar actionCalendar, ActionCalendarStatusUpdate statusUpdate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<html><body>");
            builder.AppendLine("<h2>Action Calendar Task Updated</h2>");
            builder.AppendLine("<p>A task has been updated.</p>");
            builder.AppendLine("<ul>");
            builder.AppendLine($"<li><strong>Title:</strong> {actionCalendar.TaskTitle}</li>");
            builder.AppendLine($"<li><strong>New Status:</strong> {statusUpdate.Status}</li>");
            if (!string.IsNullOrWhiteSpace(statusUpdate.Comment))
            {
                builder.AppendLine($"<li><strong>Comment:</strong> {statusUpdate.Comment}</li>");
            }
            builder.AppendLine($"<li><strong>Updated By:</strong> {statusUpdate.UpdatedByUser?.FullName ?? actionCalendar.AssignedUser?.FullName ?? "Unknown"}</li>");
            builder.AppendLine($"<li><strong>Updated On:</strong> {statusUpdate.UpdateDate:dd MMM yyyy HH:mm}</li>");
            builder.AppendLine("</ul>");
            builder.AppendLine("<p>Please log into the Action Calendar to review the latest progress.</p>");
            builder.AppendLine("</body></html>");
            return builder.ToString();
        }
    }
}

