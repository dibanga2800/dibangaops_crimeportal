using AIPBackend.Models;

namespace AIPBackend.Services
{
    public interface IActionCalendarEmailService
    {
        Task SendTaskAssignmentNotificationAsync(ActionCalendar actionCalendar);
        Task SendTaskStatusUpdatedNotificationAsync(ActionCalendar actionCalendar, ActionCalendarStatusUpdate statusUpdate);
    }
}

