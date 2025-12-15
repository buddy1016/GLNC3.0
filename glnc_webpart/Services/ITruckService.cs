using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface ITruckService
    {
        Task<List<Truck>> GetAllTrucksAsync();
        Task<Truck?> GetTruckByIdAsync(int id);
        Task<Truck> CreateTruckAsync(Truck truck);
        Task<Truck> UpdateTruckAsync(Truck truck);
        Task<bool> DeleteTruckAsync(int id);
    }
}


