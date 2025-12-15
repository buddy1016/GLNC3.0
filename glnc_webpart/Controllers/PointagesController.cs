using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    public class PointagesController : Controller
    {
        private readonly IPointageService _pointageService;
        private readonly IUserService _userService;
        private readonly ILogger<PointagesController> _logger;

        public PointagesController(IPointageService pointageService, IUserService userService, ILogger<PointagesController> logger)
        {
            _pointageService = pointageService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Pointages
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var pointages = await _pointageService.GetAllPointagesAsync();
            ViewBag.Users = await _userService.GetAllUsersAsync();
            return View(pointages);
        }

        // GET: Pointages/GetByUser
        [HttpGet]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var pointages = await _pointageService.GetPointagesByUserIdAsync(userId);
            return Json(new { success = true, data = pointages });
        }

        // GET: Pointages/GetByDateRange
        [HttpGet]
        public async Task<IActionResult> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var pointages = await _pointageService.GetPointagesByDateRangeAsync(startDate, endDate);
            return Json(new { success = true, data = pointages });
        }
    }
}


