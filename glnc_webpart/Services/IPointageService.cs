using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IPointageService
    {
        Task<List<AttendanceTracking>> GetAllPointagesAsync();
        Task<List<AttendanceTracking>> GetPointagesByUserIdAsync(int userId);
        Task<List<AttendanceTracking>> GetPointagesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<AttendanceTracking> CreateAttendanceTrackingAsync(AttendanceTracking attendanceTracking);
    }
}


