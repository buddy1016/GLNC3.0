using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Services
{
    public class PointageService : IPointageService
    {
        private readonly ApplicationDbContext _context;

        public PointageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttendanceTracking>> GetAllPointagesAsync()
        {
            return await _context.AttendanceTrackings
                .Include(p => p.User)
                .OrderByDescending(p => p.Time)
                .ToListAsync();
        }

        public async Task<List<AttendanceTracking>> GetPointagesByUserIdAsync(int userId)
        {
            return await _context.AttendanceTrackings
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Time)
                .ToListAsync();
        }

        public async Task<List<AttendanceTracking>> GetPointagesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.AttendanceTrackings
                .Include(p => p.User)
                .Where(p => p.Time >= startDate && p.Time <= endDate)
                .OrderByDescending(p => p.Time)
                .ToListAsync();
        }

        public async Task<AttendanceTracking> CreateAttendanceTrackingAsync(AttendanceTracking attendanceTracking)
        {
            _context.AttendanceTrackings.Add(attendanceTracking);
            await _context.SaveChangesAsync();
            return attendanceTracking;
        }
    }
}


