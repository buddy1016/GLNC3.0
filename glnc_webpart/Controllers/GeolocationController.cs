using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using static glnc_webpart.Services.TimezoneHelper;

namespace glnc_webpart.Controllers
{
    public class GeolocationController : Controller
    {
        private readonly IGeolocationService _geolocationService;
        private readonly IUserService _userService;
        private readonly ILogger<GeolocationController> _logger;

        public GeolocationController(
            IGeolocationService geolocationService, 
            IUserService userService,
            ILogger<GeolocationController> logger)
        {
            _geolocationService = geolocationService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Geolocation
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var locations = await _geolocationService.GetAllDriverLocationsAsync();
            var latestLocation = await _geolocationService.GetLatestDriverLocationAsync();
            var users = await _userService.GetAllUsersAsync();
            
            ViewBag.LatestLocation = latestLocation;
            ViewBag.Users = users;
            return View(locations);
        }

        // POST: Geolocation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] DriverGeolocation location)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    location.DateTime = TimezoneHelper.GetNewCaledoniaTime();
                    await _geolocationService.CreateDriverLocationAsync(location);
                    return Json(new { success = true, message = "Location saved successfully" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid model state" });
        }

        // GET: Geolocation/GetLatest
        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var location = await _geolocationService.GetLatestDriverLocationAsync();
            if (location == null)
            {
                return Json(new { success = false, message = "No location found" });
            }
            return Json(new { success = true, data = location });
        }

        // GET: Geolocation/GetLastLocation
        [HttpGet]
        public async Task<IActionResult> GetLastLocation(int userId, bool isDeliveryLocation = false)
        {
            try
            {
                if (isDeliveryLocation)
                {
                    var deliveryLocation = await _geolocationService.GetLatestDeliveryLocationByUserIdAsync(userId);
                    if (deliveryLocation == null)
                    {
                        return Json(new { success = false, message = "No delivery location found for this driver" });
                    }
                    return Json(new { 
                        success = true, 
                        data = new {
                            lati = deliveryLocation.SignLati,
                            longi = deliveryLocation.SignLongi,
                            alti = deliveryLocation.SignAlti,
                            dateTime = deliveryLocation.Delivery?.DateTimeArrival?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
                        }
                    });
                }
                else
                {
                    var driverLocation = await _geolocationService.GetLatestDriverLocationByUserIdAsync(userId);
                    if (driverLocation == null)
                    {
                        return Json(new { success = false, message = "No driver location found" });
                    }
                    return Json(new { 
                        success = true, 
                        data = new {
                            lati = driverLocation.Lati,
                            longi = driverLocation.Longi,
                            alti = driverLocation.Alti,
                            dateTime = driverLocation.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last location for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while retrieving location" });
            }
        }

        // GET: Geolocation/GetLocationHistory
        [HttpGet]
        public async Task<IActionResult> GetLocationHistory(int userId, bool isDeliveryLocation = false)
        {
            try
            {
                if (isDeliveryLocation)
                {
                    var deliveryLocations = await _geolocationService.GetDeliveryLocationHistoryByUserIdAsync(userId);
                    var history = deliveryLocations.Select(d => new
                    {
                        id = d.Id,
                        lati = d.SignLati,
                        longi = d.SignLongi,
                        alti = d.SignAlti,
                        dateTime = d.Delivery?.DateTimeArrival?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                        deliveryId = d.DeliveryId
                    }).ToList();
                    return Json(new { success = true, data = history });
                }
                else
                {
                    var driverLocations = await _geolocationService.GetDriverLocationHistoryByUserIdAsync(userId);
                    var history = driverLocations.Select(d => new
                    {
                        id = d.Id,
                        lati = d.Lati,
                        longi = d.Longi,
                        alti = d.Alti,
                        dateTime = d.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                    return Json(new { success = true, data = history });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location history for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while retrieving location history" });
            }
        }
    }
}


