using glnc_webpart.Data;
using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Controllers
{
    public class ApiKeyController : Controller
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ITruckService _truckService;
        private readonly IUserService _userService;
        private readonly ISupplierService _supplierService;
        private readonly IPointageService _pointageService;
        private readonly IGeolocationService _geolocationService;
        private readonly IDeliveryService _deliveryService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiKeyController> _logger;

        public ApiKeyController(
            IApiKeyService apiKeyService,
            ITruckService truckService,
            IUserService userService,
            ISupplierService supplierService,
            IPointageService pointageService,
            IGeolocationService geolocationService,
            IDeliveryService deliveryService,
            ApplicationDbContext context,
            ILogger<ApiKeyController> logger)
        {
            _apiKeyService = apiKeyService;
            _truckService = truckService;
            _userService = userService;
            _supplierService = supplierService;
            _pointageService = pointageService;
            _geolocationService = geolocationService;
            _deliveryService = deliveryService;
            _context = context;
            _logger = logger;
        }

        // GET: ApiKey
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var apiKeys = await _apiKeyService.GetAllApiKeysAsync();
            return View(apiKeys);
        }

        // POST: ApiKey/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            try
            {
                var apiKey = await _apiKeyService.GenerateApiKeyAsync();
                return Json(new { success = true, apiKey = apiKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key");
                return Json(new { success = false, message = "Failed to generate API key" });
            }
        }

        // POST: ApiKey/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _apiKeyService.DeleteApiKeyAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "API key deleted successfully" });
                }
                return Json(new { success = false, message = "API key not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key");
                return Json(new { success = false, message = "Failed to delete API key" });
            }
        }

        // Helper method to validate API key
        private async Task<IActionResult?> ValidateApiKey(string? code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Unauthorized(new { error = "API key (code) is required" });
            }

            var isValid = await _apiKeyService.ValidateApiKeyAsync(code);
            if (!isValid)
            {
                return Unauthorized(new { error = "Invalid API key" });
            }

            return null;
        }

        // GET: api/Excel/CAMIONS - Trucks list
        [HttpGet("api/Excel/CAMIONS")]
        public async Task<IActionResult> GetTrucks([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var trucks = await _truckService.GetAllTrucksAsync();
                return Json(trucks.Select(t => new
                {
                    id = t.Id,
                    license = t.License,
                    brand = t.Brand,
                    model = t.Model,
                    color = t.Color
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trucks data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/Attendances - Attendance tracking list
        [HttpGet("api/Excel/Attendances")]
        public async Task<IActionResult> GetAttendances([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var attendance = await _pointageService.GetAllPointagesAsync();
                return Json(attendance.Select(a => new
                {
                    id = a.Id,
                    time = a.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    lati = a.Lati,
                    longi = a.Longi,
                    alti = a.Alti,
                    type = a.Type,
                    user_id = a.UserId
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/Drivers - Drivers list
        [HttpGet("api/Excel/Drivers")]
        public async Task<IActionResult> GetDrivers([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var drivers = await _userService.GetAllUsersAsync();
                return Json(drivers.Select(d => new
                {
                    id = d.Id,
                    name = d.Name,
                    role = d.Role
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drivers data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/Fournisseurs - Suppliers list
        [HttpGet("api/Excel/Fournisseurs")]
        public async Task<IActionResult> GetSuppliers([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var suppliers = await _supplierService.GetAllSuppliersAsync();
                return Json(suppliers.Select(s => new
                {
                    id = s.Id,
                    supplier_name = s.SupplierName,
                    mail = s.Mail,
                    check = s.Check
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/LGEOLOCs - Delivery geolocation list
        [HttpGet("api/Excel/LGEOLOCs")]
        public async Task<IActionResult> GetDeliveryGeolocations([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var deliveryGeolocations = await _context.DeliveryGeolocations
                    .Include(d => d.Delivery)
                    .Include(d => d.User)
                    .ToListAsync();
                return Json(deliveryGeolocations.Select(dg => new
                {
                    id = dg.Id,
                    sign_lati = dg.SignLati,
                    sign_longi = dg.SignLongi,
                    sign_alti = dg.SignAlti,
                    delivery_id = dg.DeliveryId,
                    user_id = dg.UserId
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivery geolocation data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/EVENEMENTS - Driver geolocation list
        [HttpGet("api/Excel/EVENEMENTS")]
        public async Task<IActionResult> GetDriverGeolocations([FromQuery] string? code)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                var driverGeolocations = await _geolocationService.GetAllDriverLocationsAsync();
                return Json(driverGeolocations.Select(dg => new
                {
                    id = dg.Id,
                    lati = dg.Lati,
                    longi = dg.Longi,
                    alti = dg.Alti,
                    date_time = dg.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving driver geolocation data");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        // GET: api/Excel/LIVRAISONS - Deliveries filtered by date range
        [HttpGet("api/Excel/LIVRAISONS")]
        public async Task<IActionResult> GetDeliveriesByDateRange(
            [FromQuery] string? code,
            [FromQuery] string? start,
            [FromQuery] string? end)
        {
            try
            {
                var validationError = await ValidateApiKey(code);
                if (validationError != null) return validationError;

                if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
                {
                    return BadRequest(new { error = "start and end dates are required (format: yyyy-MM-dd or yyyy/MM/dd)" });
                }

                if (!TryParseDate(start, out var startDate) || !TryParseDate(end, out var endDate))
                {
                    return BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd or yyyy/MM/dd" });
                }

                // Ensure start <= end
                if (startDate > endDate)
                {
                    var temp = startDate;
                    startDate = endDate;
                    endDate = temp;
                }

                var deliveries = await _deliveryService.GetAllDeliveriesAsync();
                var filtered = deliveries
                    .Where(d => d.DateTimeAppointment.Date >= startDate.Date && d.DateTimeAppointment.Date <= endDate.Date)
                    .OrderBy(d => d.DateTimeAppointment)
                    .Select(d => new
                    {
                        id = d.Id,
                        date_time_appointment = d.DateTimeAppointment.ToString("yyyy-MM-dd HH:mm:ss"),
                        date_time_leave = d.DateTimeLeave.ToString("yyyy-MM-dd HH:mm:ss"),
                        date_time_accept = d.DateTimeAccept?.ToString("yyyy-MM-dd HH:mm:ss"),
                        date_time_arrival = d.DateTimeArrival?.ToString("yyyy-MM-dd HH:mm:ss"),
                        client = d.Client,
                        address = d.Address,
                        contacts = d.Contacts,
                        invoice = d.Invoice,
                        supplier_id = d.SupplierId,
                        user_id = d.UserId,
                        truck_id = d.TruckId,
                        weight = d.Weight,
                        comment = d.Comment,
                        return_flag = d.ReturnFlag,
                        satisfaction = d.SatisfactionClient
                    });

                return Json(filtered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deliveries data by date range");
                return StatusCode(500, new { error = "An error occurred while retrieving data" });
            }
        }

        private bool TryParseDate(string input, out DateTime date)
        {
            var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd" };
            return DateTime.TryParseExact(
                input,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out date);
        }
    }
}

