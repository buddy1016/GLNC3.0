using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IGeolocationService
    {
        Task<List<DriverGeolocation>> GetAllDriverLocationsAsync();
        Task<DriverGeolocation?> GetLatestDriverLocationAsync();
        Task<DriverGeolocation> CreateDriverLocationAsync(DriverGeolocation location);
        Task<List<DriverGeolocation>> GetDriverLocationsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<DeliveryGeolocation> CreateDeliveryGeolocationAsync(DeliveryGeolocation deliveryGeolocation);
        Task<DriverGeolocation?> GetLatestDriverLocationByUserIdAsync(int userId);
        Task<List<DriverGeolocation>> GetDriverLocationHistoryByUserIdAsync(int userId);
        Task<DeliveryGeolocation?> GetLatestDeliveryLocationByUserIdAsync(int userId);
        Task<List<DeliveryGeolocation>> GetDeliveryLocationHistoryByUserIdAsync(int userId);
    }
}


