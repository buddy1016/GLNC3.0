using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IDeliveryService
    {
        Task<List<Delivery>> GetAllDeliveriesAsync();
        Task<Delivery?> GetDeliveryByIdAsync(int id);
        Task<List<Delivery>> GetDeliveriesByUserIdAsync(int userId);
        Task<Delivery> CreateDeliveryAsync(Delivery delivery);
        Task<Delivery> UpdateDeliveryAsync(Delivery delivery);
        Task<bool> CompleteDeliveryAsync(int deliveryId, string signature, string invoiceImage, string comment, double weight, int satisfaction);
        Task<bool> CancelDeliveryAsync(int deliveryId, string? comment = null);
        Task<bool> DeleteDeliveryAsync(int id);
    }
}


