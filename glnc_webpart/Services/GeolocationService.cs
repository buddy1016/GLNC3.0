using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Services
{
    public class GeolocationService : IGeolocationService
    {
        private readonly ApplicationDbContext _context;

        public GeolocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DriverGeolocation>> GetAllDriverLocationsAsync()
        {
            return await _context.DriverGeolocations
                .OrderByDescending(d => d.DateTime)
                .ToListAsync();
        }

        public async Task<DriverGeolocation?> GetLatestDriverLocationAsync()
        {
            return await _context.DriverGeolocations
                .OrderByDescending(d => d.DateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<DriverGeolocation> CreateDriverLocationAsync(DriverGeolocation location)
        {
            _context.DriverGeolocations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task<List<DriverGeolocation>> GetDriverLocationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.DriverGeolocations
                .Where(d => d.DateTime >= startDate && d.DateTime <= endDate)
                .OrderByDescending(d => d.DateTime)
                .ToListAsync();
        }

        public async Task<DeliveryGeolocation> CreateDeliveryGeolocationAsync(DeliveryGeolocation deliveryGeolocation)
        {
            _context.DeliveryGeolocations.Add(deliveryGeolocation);
            await _context.SaveChangesAsync();
            return deliveryGeolocation;
        }

        public async Task<DriverGeolocation?> GetLatestDriverLocationByUserIdAsync(int userId)
        {
            // Note: drivergeolocation table doesn't have user_id column
            // Return the latest location overall (cannot filter by user)
            return await _context.DriverGeolocations
                .OrderByDescending(d => d.DateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<List<DriverGeolocation>> GetDriverLocationHistoryByUserIdAsync(int userId)
        {
            // Note: drivergeolocation table doesn't have user_id column
            // Return all locations (cannot filter by user)
            return await _context.DriverGeolocations
                .OrderByDescending(d => d.DateTime)
                .ToListAsync();
        }

        public async Task<DeliveryGeolocation?> GetLatestDeliveryLocationByUserIdAsync(int userId)
        {
            return await _context.DeliveryGeolocations
                .Include(d => d.Delivery)
                .Include(d => d.User)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.Delivery != null ? d.Delivery.DateTimeArrival : DateTime.MinValue)
                .FirstOrDefaultAsync();
        }

        public async Task<List<DeliveryGeolocation>> GetDeliveryLocationHistoryByUserIdAsync(int userId)
        {
            return await _context.DeliveryGeolocations
                .Include(d => d.Delivery)
                .Include(d => d.User)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.Delivery != null ? d.Delivery.DateTimeArrival : DateTime.MinValue)
                .ToListAsync();
        }
    }
}


