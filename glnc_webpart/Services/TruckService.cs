using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Services
{
    public class TruckService : ITruckService
    {
        private readonly ApplicationDbContext _context;

        public TruckService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Truck>> GetAllTrucksAsync()
        {
            return await _context.Trucks.OrderBy(t => t.Id).ToListAsync();
        }

        public async Task<Truck?> GetTruckByIdAsync(int id)
        {
            return await _context.Trucks.FindAsync(id);
        }

        public async Task<Truck> CreateTruckAsync(Truck truck)
        {
            // Check if license already exists
            var existingTruck = await _context.Trucks
                .FirstOrDefaultAsync(t => t.License == truck.License);
            
            if (existingTruck != null)
            {
                throw new ArgumentException($"A truck with license '{truck.License}' already exists.");
            }

            _context.Trucks.Add(truck);
            await _context.SaveChangesAsync();
            return truck;
        }

        public async Task<Truck> UpdateTruckAsync(Truck truck)
        {
            var existingTruck = await _context.Trucks.FindAsync(truck.Id);
            if (existingTruck == null)
                throw new ArgumentException("Truck not found");

            // Check if license already exists for a different truck
            var truckWithSameLicense = await _context.Trucks
                .FirstOrDefaultAsync(t => t.License == truck.License && t.Id != truck.Id);
            
            if (truckWithSameLicense != null)
            {
                throw new ArgumentException($"A truck with license '{truck.License}' already exists.");
            }

            existingTruck.License = truck.License;
            existingTruck.Brand = truck.Brand;
            existingTruck.Model = truck.Model;
            existingTruck.Color = truck.Color;

            await _context.SaveChangesAsync();
            return existingTruck;
        }

        public async Task<bool> DeleteTruckAsync(int id)
        {
            var truck = await _context.Trucks.FindAsync(id);
            if (truck == null)
                return false;

            _context.Trucks.Remove(truck);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


