using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly IDeliveryService _deliveryService;
        private readonly ITruckService _truckService;
        private readonly ISupplierService _supplierService;
        private readonly IUserService _userService;
        private readonly ILogger<DeliveryController> _logger;

        public DeliveryController(
            IDeliveryService deliveryService,
            ITruckService truckService,
            ISupplierService supplierService,
            IUserService userService,
            ILogger<DeliveryController> logger)
        {
            _deliveryService = deliveryService;
            _truckService = truckService;
            _supplierService = supplierService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Delivery
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var deliveries = await _deliveryService.GetAllDeliveriesAsync();
            ViewBag.Trucks = await _truckService.GetAllTrucksAsync();
            ViewBag.Suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Users = await _userService.GetAllUsersAsync();
            return View(deliveries);
        }

        // GET: Delivery/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var delivery = await _deliveryService.GetDeliveryByIdAsync(id);
            if (delivery == null)
            {
                return NotFound();
            }
            return PartialView("_DeliveryDetailsModal", delivery);
        }

        // POST: Delivery/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] Delivery delivery)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _deliveryService.UpdateDeliveryAsync(delivery);
                    return Json(new { success = true, message = "Delivery updated successfully" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid model state" });
        }

        // POST: Delivery/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _deliveryService.DeleteDeliveryAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Delivery deleted successfully" });
                }
                return Json(new { success = false, message = "Delivery not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Delivery/Get/5
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var delivery = await _deliveryService.GetDeliveryByIdAsync(id);
            if (delivery == null)
            {
                return Json(new { success = false, message = "Delivery not found" });
            }
            return Json(new { success = true, data = delivery });
        }
    }
}


