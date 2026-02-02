using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    public class TrucksController : Controller
    {
        private readonly ITruckService _truckService;
        private readonly ILogger<TrucksController> _logger;

        public TrucksController(ITruckService truckService, ILogger<TrucksController> logger)
        {
            _truckService = truckService;
            _logger = logger;
        }

        // GET: Trucks
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trucks = await _truckService.GetAllTrucksAsync();
            return View(trucks);
        }

        // GET: Trucks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var truck = await _truckService.GetTruckByIdAsync(id);
            if (truck == null)
            {
                return NotFound();
            }
            return PartialView("_TruckDetailsModal", truck);
        }

        // POST: Trucks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Truck truck)
        {
            // Validate required fields manually
            if (string.IsNullOrWhiteSpace(truck.License))
            {
                return Json(new { success = false, message = "License is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Brand))
            {
                return Json(new { success = false, message = "Brand is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Model))
            {
                return Json(new { success = false, message = "Model is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Color))
            {
                return Json(new { success = false, message = "Color is required" });
            }

            try
            {
                await _truckService.CreateTruckAsync(truck);
                return Json(new { success = true, message = "Truck created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Trucks/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Truck truck)
        {
            // Validate required fields manually
            if (truck.Id <= 0)
            {
                return Json(new { success = false, message = "Truck ID is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.License))
            {
                return Json(new { success = false, message = "License is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Brand))
            {
                return Json(new { success = false, message = "Brand is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Model))
            {
                return Json(new { success = false, message = "Model is required" });
            }

            if (string.IsNullOrWhiteSpace(truck.Color))
            {
                return Json(new { success = false, message = "Color is required" });
            }

            try
            {
                await _truckService.UpdateTruckAsync(truck);
                return Json(new { success = true, message = "Truck updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Trucks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _truckService.DeleteTruckAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Truck deleted successfully" });
                }
                return Json(new { success = false, message = "Truck not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Trucks/Get/5
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var truck = await _truckService.GetTruckByIdAsync(id);
            if (truck == null)
            {
                return Json(new { success = false, message = "Truck not found" });
            }
            return Json(new { success = true, data = truck });
        }
    }
}


