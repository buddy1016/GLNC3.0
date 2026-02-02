using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace glnc_webpart.Controllers
{
    public class PlanningController : Controller
    {
        private readonly IDeliveryService _deliveryService;
        private readonly ITruckService _truckService;
        private readonly ISupplierService _supplierService;
        private readonly IUserService _userService;
        private readonly ILogger<PlanningController> _logger;

        public PlanningController(
            IDeliveryService deliveryService,
            ITruckService truckService,
            ISupplierService supplierService,
            IUserService userService,
            ILogger<PlanningController> logger)
        {
            _deliveryService = deliveryService;
            _truckService = truckService;
            _supplierService = supplierService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Planning
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Trucks = await _truckService.GetAllTrucksAsync();
            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Users = await _userService.GetAllUsersAsync();
            ViewBag.Deliveries = await _deliveryService.GetAllDeliveriesAsync();
            return View();
        }

        // GET: Planning/GetDeliveries - API endpoint for calendar
        [HttpGet]
        public async Task<IActionResult> GetDeliveries(DateTime? start, DateTime? end, int? driverId, int? truckId, bool? inTransitOnly)
        {
            var deliveries = await _deliveryService.GetAllDeliveriesAsync();
            
            // Filter by date range if provided
            if (start.HasValue && end.HasValue)
            {
                deliveries = deliveries.Where(d => 
                    d.DateTimeAppointment.Date >= start.Value.Date && 
                    d.DateTimeAppointment.Date <= end.Value.Date).ToList();
            }
            
            // Filter by driver if provided
            if (driverId.HasValue)
            {
                deliveries = deliveries.Where(d => d.UserId == driverId.Value).ToList();
            }
            
            // Filter by truck if provided
            if (truckId.HasValue)
            {
                deliveries = deliveries.Where(d => d.TruckId == truckId.Value).ToList();
            }
            
            // Filter for in-transit deliveries only (accepted but not arrived)
            if (inTransitOnly == true)
            {
                deliveries = deliveries.Where(d => 
                    d.DateTimeAccept.HasValue && 
                    !d.DateTimeArrival.HasValue).ToList();
            }
            
            var events = deliveries.Select(d => 
            {
                // Get truck color, ensure it starts with # if it's a hex code
                string truckColor = "#dc3545"; // Default red color
                if (!string.IsNullOrWhiteSpace(d.Truck?.Color))
                {
                    truckColor = d.Truck.Color.Trim();
                    // If it's a hex code without #, add it
                    if (truckColor.Length == 6 && System.Text.RegularExpressions.Regex.IsMatch(truckColor, "^[0-9A-Fa-f]{6}$"))
                    {
                        truckColor = "#" + truckColor;
                    }
                    // If it's already a valid hex with #, use it as is
                    else if (!truckColor.StartsWith("#") || truckColor.Length != 7)
                    {
                        // Invalid format, use default
                        truckColor = "#dc3545";
                    }
                }
                
                // Calculate text color based on background color brightness
                string textColor = GetContrastTextColor(truckColor);
                
                // Determine status icon based on delivery completion
                string statusIcon = "";
                if (d.DateTimeArrival.HasValue)
                {
                    // Delivery is complete
                    statusIcon = "/images/marker.livraison.r.32x32.png";
                }
                else if (d.DateTimeAccept.HasValue)
                {
                    // Delivery is in transit
                    statusIcon = "/images/marker.livraison.p.32x32.png";
                }
                
                return new
                {
                    id = d.Id,
                    title = d.Client ?? d.Supplier?.SupplierName ?? "Delivery",
                    start = d.DateTimeAppointment.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = d.DateTimeLeave.ToString("yyyy-MM-ddTHH:mm:ss"),
                    color = truckColor,
                    textColor = textColor,
                    statusIcon = statusIcon, // Also add at root level for easier access
                    extendedProps = new
                    {
                        statusIcon = statusIcon,
                        driverId = d.UserId,
                        driverName = d.User?.Name ?? "Unknown",
                        truckId = d.TruckId,
                        truckLicense = d.Truck?.License ?? "Unknown",
                        supplierName = d.Supplier?.SupplierName ?? "Unknown",
                        address = d.Address
                    }
                };
            }).ToList();
            
            return Json(events);
        }

        // Helper method to determine text color based on background color
        private string GetContrastTextColor(string hexColor)
        {
            try
            {
                // Remove # if present
                string hex = hexColor.Trim().Replace("#", "");
                
                // Parse RGB values
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                
                // Calculate relative luminance (perceived brightness)
                // Using the formula: 0.299*R + 0.587*G + 0.114*B
                double luminance = (0.299 * r + 0.587 * g + 0.114 * b);
                
                // If luminance is greater than 128, the color is light, use dark text
                // Otherwise, use light text
                return luminance > 128 ? "#000000" : "#ffffff";
            }
            catch
            {
                // If color parsing fails, default to white text
                return "#ffffff";
            }
        }

        // POST: Planning/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Delivery delivery)
        {
            // Validate required fields from modal
            if (delivery.UserId == 0)
            {
                return Json(new { success = false, message = "Driver is required" });
            }

            if (delivery.TruckId == 0)
            {
                return Json(new { success = false, message = "Truck is required" });
            }

            if (delivery.DateTimeAppointment == default(DateTime))
            {
                return Json(new { success = false, message = "Appointment Date & Time is required" });
            }

            if (delivery.DateTimeLeave == default(DateTime))
            {
                return Json(new { success = false, message = "Leave Date & Time is required" });
            }

            if (delivery.DateTimeLeave <= delivery.DateTimeAppointment)
            {
                return Json(new { success = false, message = "Leave Date & Time must be after Appointment Date & Time" });
            }

            if (delivery.SupplierId == 0)
            {
                return Json(new { success = false, message = "Supplier is required" });
            }

            if (string.IsNullOrWhiteSpace(delivery.Client))
            {
                return Json(new { success = false, message = "Client is required" });
            }

            if (delivery.Client.Length > 250)
            {
                return Json(new { success = false, message = "Client name must not exceed 250 characters" });
            }

            if (string.IsNullOrWhiteSpace(delivery.Address))
            {
                return Json(new { success = false, message = "Address is required" });
            }

            if (delivery.Address.Length > 250)
            {
                return Json(new { success = false, message = "Address must not exceed 250 characters" });
            }

            if (string.IsNullOrWhiteSpace(delivery.Contacts))
            {
                return Json(new { success = false, message = "Contacts is required" });
            }

            if (delivery.Contacts.Length > 250)
            {
                return Json(new { success = false, message = "Contacts must not exceed 250 characters" });
            }

            if (string.IsNullOrWhiteSpace(delivery.Invoice))
            {
                return Json(new { success = false, message = "Invoice is required" });
            }

            if (delivery.Invoice.Length > 250)
            {
                return Json(new { success = false, message = "Invoice must not exceed 250 characters" });
            }

            if (delivery.Weight <= 0)
            {
                return Json(new { success = false, message = "Weight must be greater than 0" });
            }

            try
            {
                // Create a new delivery object with only the fields from the modal
                var newDelivery = new Delivery
                {
                    UserId = delivery.UserId,
                    TruckId = delivery.TruckId,
                    DateTimeAppointment = delivery.DateTimeAppointment,
                    DateTimeLeave = delivery.DateTimeLeave,
                    SupplierId = delivery.SupplierId,
                    Client = delivery.Client.Trim(),
                    Address = delivery.Address.Trim(),
                    Contacts = delivery.Contacts.Trim(),
                    Invoice = delivery.Invoice.Trim(),
                    Weight = delivery.Weight,
                    ReturnFlag = delivery.ReturnFlag, // Set from form (default false)
                    // Fields not in modal - set to null/empty/default
                    DateTimeAccept = null, // Will be set when driver accepts
                    DateTimeArrival = null, // Will be set when driver arrives
                    Description = null, // Not in modal
                    Comment = string.Empty, // Not in modal, but required by DB - set to empty
                    SignClient = null, // Will be set when client signs
                    SatisfactionClient = null, // Will be set after delivery
                    InvoiceImage = null // Will be uploaded later
                };

                await _deliveryService.CreateDeliveryAsync(newDelivery);
                return Json(new { success = true, message = "Livraison réussie", id = newDelivery.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la livraison");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}


