using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthenticationService authenticationService, ILogger<AccountController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Please enter a password.";
                return View();
            }

            // Validate password format (5 digits)
            if (password.Length != 5 || !password.All(char.IsDigit))
            {
                ViewBag.ErrorMessage = "Password must be exactly 5 digits.";
                return View();
            }

            var user = await _authenticationService.ValidatePasswordAsync(password);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid password. Please try again.";
                return View();
            }

            // Check if user has admin role (role = 2)
            // Only admins can log in, drivers (role = 1) are not allowed
            if (user.Role != 2)
            {
                ViewBag.ErrorMessage = "Access denied. Only administrators can access this system.";
                _logger.LogWarning("Driver user {UserId} ({UserName}) attempted to log in but was denied access", user.Id, user.Name);
                return View();
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetInt32("UserRole", user.Role);

            _logger.LogInformation("Admin user {UserId} ({UserName}) logged in successfully", user.Id, user.Name);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

