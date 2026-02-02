using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDeliveryService _deliveryService;
        private readonly ITruckService _truckService;

        public HomeController(
            ILogger<HomeController> logger,
            IDeliveryService deliveryService,
            ITruckService truckService)
        {
            _logger = logger;
            _deliveryService = deliveryService;
            _truckService = truckService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetInt32("UserRole");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

