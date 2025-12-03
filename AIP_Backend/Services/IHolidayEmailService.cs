using AIPBackend.Models.DTOs;

namespace AIPBackend.Services
{
    public interface IHolidayEmailService
    {
        /// <summary>
        /// Sends holiday request submitted notification to manager
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="managerEmail">Manager email address</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestSubmittedNotificationAsync(object holidayRequest, object employee, string managerEmail);

        /// <summary>
        /// Sends holiday request approved notification to employee
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="approvedBy">Name of person who approved</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestApprovedNotificationAsync(object holidayRequest, object employee, string approvedBy);

        /// <summary>
        /// Sends holiday request rejected notification to employee
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="rejectedBy">Name of person who rejected</param>
        /// <param name="rejectionReason">Reason for rejection</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestRejectedNotificationAsync(object holidayRequest, object employee, string rejectedBy, string rejectionReason);

        /// <summary>
        /// Sends holiday request pending approval reminder to manager
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="managerEmail">Manager email address</param>
        /// <param name="daysPending">Number of days the request has been pending</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestPendingReminderAsync(object holidayRequest, object employee, string managerEmail, int daysPending);

        /// <summary>
        /// Sends holiday request cancellation notification
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="managerEmail">Manager email address</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestCancelledNotificationAsync(object holidayRequest, object employee, string managerEmail);

        /// <summary>
        /// Sends holiday request modification notification
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="managerEmail">Manager email address</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestModifiedNotificationAsync(object holidayRequest, object employee, string managerEmail);

        /// <summary>
        /// Sends holiday balance update notification to employee
        /// </summary>
        /// <param name="employee">Employee details</param>
        /// <param name="remainingDays">Remaining holiday days</param>
        /// <param name="usedDays">Used holiday days</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayBalanceUpdateNotificationAsync(object employee, int remainingDays, int usedDays);

        /// <summary>
        /// Sends holiday request conflict notification
        /// </summary>
        /// <param name="holidayRequest">Holiday request details</param>
        /// <param name="employee">Employee details</param>
        /// <param name="conflictingEmployees">List of employees with conflicting requests</param>
        /// <returns>True if notification was sent successfully</returns>
        Task<bool> SendHolidayRequestConflictNotificationAsync(object holidayRequest, object employee, List<string> conflictingEmployees);
    }
}
