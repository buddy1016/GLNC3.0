using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace glnc_webpart.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ISupplierService supplierService, ILogger<SupplierController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        // GET: Supplier
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return View(suppliers);
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return PartialView("_SupplierDetailsModal", supplier);
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            // Validate required fields manually
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                return Json(new { success = false, message = "Supplier Name is required" });
            }

            if (string.IsNullOrWhiteSpace(supplier.Mail))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            if (!IsValidEmailList(supplier.Mail))
            {
                return Json(new { success = false, message = "Email list is invalid. Please provide valid addresses separated by commas." });
            }

            try
            {
                await _supplierService.CreateSupplierAsync(supplier);
                return Json(new { success = true, message = "Supplier created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Supplier/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Supplier supplier)
        {
            // Validate required fields manually
            if (supplier.Id <= 0)
            {
                return Json(new { success = false, message = "Supplier ID is required" });
            }

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                return Json(new { success = false, message = "Supplier Name is required" });
            }

            if (string.IsNullOrWhiteSpace(supplier.Mail))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            if (!IsValidEmailList(supplier.Mail))
            {
                return Json(new { success = false, message = "Email list is invalid. Please provide valid addresses separated by commas." });
            }

            try
            {
                await _supplierService.UpdateSupplierAsync(supplier);
                return Json(new { success = true, message = "Supplier updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Supplier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _supplierService.DeleteSupplierAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Supplier deleted successfully" });
                }
                return Json(new { success = false, message = "Supplier not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Supplier/Get/5
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found" });
            }
            // Return with explicit property names to ensure consistency
            return Json(new { 
                success = true, 
                data = new { 
                    id = supplier.Id, 
                    supplierName = supplier.SupplierName, 
                    mail = supplier.Mail,
                    check = supplier.Check
                } 
            });
        }

        private bool IsValidEmailList(string emailList)
        {
            if (string.IsNullOrWhiteSpace(emailList))
                return false;

            var emails = emailList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(e => e.Trim())
                                  .Where(e => !string.IsNullOrWhiteSpace(e));

            foreach (var email in emails)
            {
                try
                {
                    var _ = new MailAddress(email);
                }
                catch
                {
                    return false;
                }
            }
            return emails.Any();
        }
    }
}


