using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;
using static glnc_webpart.Services.TimezoneHelper;

namespace glnc_webpart.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(ApplicationDbContext context, IEmailService emailService, ILogger<DeliveryService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<Delivery>> GetAllDeliveriesAsync()
        {
            return await _context.Deliveries
                .Include(d => d.Supplier)
                .Include(d => d.User)
                .Include(d => d.Truck)
                .OrderByDescending(d => d.DateTimeAppointment)
                .ToListAsync();
        }

        public async Task<Delivery?> GetDeliveryByIdAsync(int id)
        {
            return await _context.Deliveries
                .Include(d => d.Supplier)
                .Include(d => d.User)
                .Include(d => d.Truck)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Delivery>> GetDeliveriesByUserIdAsync(int userId)
        {
            return await _context.Deliveries
                .Include(d => d.Supplier)
                .Include(d => d.User)
                .Include(d => d.Truck)
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.DateTimeAppointment)
                .ToListAsync();
        }

        public async Task<Delivery> CreateDeliveryAsync(Delivery delivery)
        {
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();
            return delivery;
        }

        public async Task<Delivery> UpdateDeliveryAsync(Delivery delivery)
        {
            var existingDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            if (existingDelivery == null)
                throw new ArgumentException("Delivery not found");

            existingDelivery.DateTimeAppointment = delivery.DateTimeAppointment;
            existingDelivery.DateTimeLeave = delivery.DateTimeLeave;
            existingDelivery.DateTimeAccept = delivery.DateTimeAccept;
            existingDelivery.DateTimeArrival = delivery.DateTimeArrival;
            existingDelivery.Description = delivery.Description;
            existingDelivery.SignClient = delivery.SignClient;
            existingDelivery.SatisfactionClient = delivery.SatisfactionClient;
            existingDelivery.ReturnFlag = delivery.ReturnFlag;
            existingDelivery.TruckId = delivery.TruckId;
            existingDelivery.Client = delivery.Client;
            existingDelivery.Address = delivery.Address;
            existingDelivery.Contacts = delivery.Contacts;
            existingDelivery.Invoice = delivery.Invoice;
            existingDelivery.SupplierId = delivery.SupplierId;
            existingDelivery.Weight = delivery.Weight;
            existingDelivery.Comment = delivery.Comment;
            existingDelivery.InvoiceImage = delivery.InvoiceImage;
            existingDelivery.UserId = delivery.UserId;

            await _context.SaveChangesAsync();
            return existingDelivery;
        }

        public async Task<bool> CompleteDeliveryAsync(int deliveryId, string signature, string invoiceImage, string comment, double weight, int satisfaction)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Supplier)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);
            
            if (delivery == null)
                return false;

            var currentTime = TimezoneHelper.GetNewCaledoniaTime();
            delivery.DateTimeAccept = currentTime;
            delivery.DateTimeArrival = currentTime;
            delivery.SignClient = signature;
            delivery.InvoiceImage = invoiceImage;
            delivery.Comment = comment;
            delivery.Weight = weight;
            delivery.SatisfactionClient = satisfaction;

            await _context.SaveChangesAsync();

            // Send email notification to supplier if enabled
            try
            {
                bool check = delivery.Supplier?.Check ?? false;
                
                if (check)
                {
                    string message = "Message non défini";
                    string toMail = delivery.Supplier?.Mail ?? "gregooly@gmail.com";
                    
                    // Get driver name from User, fallback to email if not available
                    string driverName = delivery.User?.Name ?? "no name";

                    // Split email addresses by comma and send to each
                    var emailAddresses = toMail.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();

                    if (emailAddresses.Count > 0)
                    {
                        string subject = $"{message}#{delivery.Client}#{delivery.DateTimeArrival:yyyy-MM-dd HH:mm:ss}#{delivery.Invoice}";
                        string weightText = delivery.Weight > 0 ? delivery.Weight.ToString() : "Non spécifié";
                        string body = $"La marchandise a été livrée ce jour.\nPoids de: {weightText}\nlivreur: {driverName}";

                        // Send email to each address
                        foreach (var emailAddress in emailAddresses)
                        {
                            try
                            {
                                await _emailService.SendDeliveryNotificationAsync(emailAddress, subject, body);
                                _logger.LogInformation("Delivery notification email sent to {Email} for delivery {DeliveryId}", emailAddress, deliveryId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send email to {Email} for delivery {DeliveryId}", emailAddress, deliveryId);
                                // Continue sending to other addresses even if one fails
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification for delivery {DeliveryId}", deliveryId);
                // Don't fail the delivery completion if email fails
            }

            return true;
        }

        public async Task<bool> CancelDeliveryAsync(int deliveryId, string? comment = null)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
                return false;

            delivery.ReturnFlag = true;
            
            // Store cancellation comment in Description field
            if (!string.IsNullOrWhiteSpace(comment))
            {
                delivery.Description = comment;
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDeliveryAsync(int id)
        {
            var delivery = await _context.Deliveries.FindAsync(id);
            if (delivery == null)
                return false;

            _context.Deliveries.Remove(delivery);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


