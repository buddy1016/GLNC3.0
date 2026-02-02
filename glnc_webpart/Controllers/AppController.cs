using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json.Serialization;
using static glnc_webpart.Services.TimezoneHelper;

namespace glnc_webpart.Controllers
{
    [ApiController]
    [Route("api/app")]
    public class AppController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IPointageService _pointageService;
        private readonly IDeliveryService _deliveryService;
        private readonly IGeolocationService _geolocationService;
        private readonly IUserService _userService;
        private readonly ITruckService _truckService;
        private readonly ISupplierService _supplierService;
        private readonly IInvoiceOcrService _invoiceOcrService;
        private readonly ILogger<AppController> _logger;

        public AppController(
            IAuthenticationService authenticationService, 
            IPointageService pointageService,
            IDeliveryService deliveryService,
            IGeolocationService geolocationService,
            IUserService userService,
            ITruckService truckService,
            ISupplierService supplierService,
            IInvoiceOcrService invoiceOcrService,
            ILogger<AppController> logger)
        {
            _authenticationService = authenticationService;
            _pointageService = pointageService;
            _deliveryService = deliveryService;
            _geolocationService = geolocationService;
            _userService = userService;
            _truckService = truckService;
            _supplierService = supplierService;
            _invoiceOcrService = invoiceOcrService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Code is required."
                    });
                }

                // Validate code format (5 digits)
                if (request.Code.Length != 5 || !request.Code.All(char.IsDigit))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Code must be exactly 5 digits."
                    });
                }

                // Validate code and get user
                var user = await _authenticationService.ValidatePasswordAsync(request.Code);

                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt with code: {Code}", request.Code);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid code. Please try again."
                    });
                }

                // Allow both drivers (role = 1) and administrators (role = 2) to log in via API
                if (user.Role != 1 && user.Role != 2)
                {
                    _logger.LogWarning("User {UserId} ({UserName}) with role {Role} attempted to log in via API - role not allowed", 
                        user.Id, user.Name, user.Role);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Access denied. Invalid user role."
                    });
                }

                var roleLabel = user.Role == 2 ? "Admin" : "Driver";
                _logger.LogInformation("{Role} user {UserId} ({UserName}) logged in via API", 
                    roleLabel, user.Id, user.Name);

                // Return success response with user information
                // Response format matches Android app expectations
                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Id = user.Id, // Root level id for Android compatibility
                    UserId = user.Id, // Alternative field name
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later."
                });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 15)
        {
            try
            {
                // Validation
                if (offset < 0) offset = 0;
                if (limit <= 0) limit = 15;
                if (limit > 50) limit = 50;

                var totalCount = await _userService.GetUsersCountAsync();
                var users = await _userService.GetUsersPagedAsync(offset, limit);
                var userList = users.Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    role = u.Role
                }).ToList();

                return Ok(new
                {
                    success = true,
                    users = userList,
                    total = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching users. Please try again later."
                });
            }
        }

        /// <summary>
        /// Update user by id
        /// </summary>
        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Valid user id is required." });
                }

                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request data is required." });
                }

                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "Name is required." });
                }

                if (request.Name.Length > 30)
                {
                    return BadRequest(new { success = false, message = "Name must not exceed 30 characters." });
                }

                if (request.Role < 1 || request.Role > 2)
                {
                    return BadRequest(new { success = false, message = "Role must be 1 (Chauffeur) or 2 (Administrateur)." });
                }

                // If password provided, validate it
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    if (request.Password.Length != 5 || !request.Password.All(char.IsDigit))
                    {
                        return BadRequest(new { success = false, message = "Password must be exactly 5 digits." });
                    }
                }

                var userToUpdate = new User
                {
                    Id = existingUser.Id,
                    Name = request.Name.Trim(),
                    Role = request.Role,
                    Password = string.IsNullOrWhiteSpace(request.Password) ? existingUser.Password : request.Password
                };
                await _userService.UpdateUserAsync(userToUpdate);

                _logger.LogInformation("User {UserId} ({UserName}) updated via API", id, userToUpdate.Name);

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully.",
                    user = new { id = userToUpdate.Id, name = userToUpdate.Name, role = userToUpdate.Role }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating user. Please try again later."
                });
            }
        }

        /// <summary>
        /// Delete (remove) user by id
        /// </summary>
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Valid user id is required." });
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                var result = await _userService.DeleteUserAsync(id);

                if (result)
                {
                    _logger.LogInformation("User {UserId} ({UserName}) deleted via API", id, user.Name);
                    return Ok(new { success = true, message = "User deleted successfully." });
                }

                return BadRequest(new { success = false, message = "Failed to delete user." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting user. Please try again later."
                });
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request data is required." });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "Name is required." });
                }

                if (request.Name.Length > 30)
                {
                    return BadRequest(new { success = false, message = "Name must not exceed 30 characters." });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { success = false, message = "Password is required." });
                }

                if (request.Password.Length != 5 || !request.Password.All(char.IsDigit))
                {
                    return BadRequest(new { success = false, message = "Password must be exactly 5 digits." });
                }

                if (request.Role < 1 || request.Role > 2)
                {
                    return BadRequest(new { success = false, message = "Role must be 1 (Chauffeur) or 2 (Administrateur)." });
                }

                var user = new User
                {
                    Name = request.Name.Trim(),
                    Password = request.Password,
                    Role = request.Role
                };

                await _userService.CreateUserAsync(user);

                _logger.LogInformation("User {UserId} ({UserName}) created via API", user.Id, user.Name);

                return Ok(new
                {
                    success = true,
                    message = "User created successfully.",
                    user = new { id = user.Id, name = user.Name, role = user.Role }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("deliveries")]
        public async Task<IActionResult> GetDeliveries(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 15)
        {
            try
            {
                // Validation
                if (offset < 0) offset = 0;
                if (limit <= 0) limit = 15;
                if (limit > 50) limit = 50;

                var totalCount = await _deliveryService.GetDeliveriesCountAsync();
                var deliveries = await _deliveryService.GetDeliveriesPagedAsync(offset, limit);
                var deliveryList = deliveries.Select(d => new
                {
                    id = d.Id,
                    client = d.Client ?? string.Empty,
                    camion = d.Truck?.License ?? "N/A",
                    statut = GetDeliveryStatut(d)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    deliveries = deliveryList,
                    total = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching deliveries");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching deliveries. Please try again later."
                });
            }
        }

        private static string GetDeliveryStatut(Delivery d)
        {
            if (d.ReturnFlag) return "Ret.";
            if (d.DateTimeArrival.HasValue) return "Term.";
            if (d.DateTimeAccept.HasValue) return "Acc.";
            return "Att.";
        }

        /// <summary>
        /// Get deliveries for calendar display by date range
        /// </summary>
        [HttpGet("deliveries/calendar")]
        public async Task<IActionResult> GetDeliveriesForCalendar(
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            try
            {
                // Validate date parameters
                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    return BadRequest(new { success = false, message = "startDate and endDate are required." });
                }

                if (!DateTime.TryParse(startDate, out DateTime start))
                {
                    return BadRequest(new { success = false, message = "Invalid startDate format. Use yyyy-MM-dd." });
                }

                if (!DateTime.TryParse(endDate, out DateTime end))
                {
                    return BadRequest(new { success = false, message = "Invalid endDate format. Use yyyy-MM-dd." });
                }

                // Set end date to end of day
                end = end.Date.AddDays(1).AddSeconds(-1);

                var deliveries = await _deliveryService.GetDeliveriesByDateRangeAsync(start, end);

                var deliveryList = deliveries.Select(d => new
                {
                    id = d.Id,
                    client = d.Client ?? string.Empty,
                    address = d.Address ?? string.Empty,
                    dateTimeAppointment = d.DateTimeAppointment.ToString("yyyy-MM-dd HH:mm:ss"),
                    dateTimeLeave = d.DateTimeLeave.ToString("yyyy-MM-dd HH:mm:ss"),
                    driverName = d.User?.Name ?? string.Empty,
                    truckLicense = d.Truck?.License ?? string.Empty,
                    statut = GetDeliveryStatut(d)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    deliveries = deliveryList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching deliveries for calendar");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching deliveries. Please try again later."
                });
            }
        }

        /// <summary>
        /// Get full delivery details by id (for view/edit modal)
        /// </summary>
        [HttpGet("deliveries/{id:int}")]
        public async Task<IActionResult> GetDeliveryById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Valid delivery id is required." });
                }

                var delivery = await _deliveryService.GetDeliveryByIdAsync(id);
                if (delivery == null)
                {
                    return NotFound(new { success = false, message = "Delivery not found." });
                }

                return Ok(new
                {
                    success = true,
                    delivery = new
                    {
                        id = delivery.Id,
                        client = delivery.Client ?? string.Empty,
                        address = delivery.Address ?? string.Empty,
                        contacts = delivery.Contacts ?? string.Empty,
                        invoice = delivery.Invoice ?? string.Empty,
                        weight = delivery.Weight,
                        supplierId = delivery.SupplierId,
                        supplierName = delivery.Supplier?.SupplierName ?? string.Empty,
                        dateTimeAppointment = delivery.DateTimeAppointment.ToString("yyyy-MM-dd HH:mm:ss"),
                        dateTimeLeave = delivery.DateTimeLeave.ToString("yyyy-MM-dd HH:mm:ss"),
                        driverName = delivery.User?.Name ?? string.Empty,
                        userId = delivery.UserId,
                        truckLicense = delivery.Truck?.License ?? string.Empty,
                        truckId = delivery.TruckId,
                        statut = GetDeliveryStatut(delivery)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching delivery {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching delivery. Please try again later."
                });
            }
        }

        /// <summary>
        /// Update delivery by id
        /// </summary>
        [HttpPut("deliveries/{id:int}")]
        public async Task<IActionResult> UpdateDelivery(int id, [FromBody] CreateDeliveryRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Valid delivery id is required." });
                }

                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request data is required." });
                }

                var existingDelivery = await _deliveryService.GetDeliveryByIdAsync(id);
                if (existingDelivery == null)
                {
                    return NotFound(new { success = false, message = "Delivery not found." });
                }

                // Validate required fields
                if (request.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Driver (user_id) is required." });
                }

                if (request.TruckId <= 0)
                {
                    return BadRequest(new { success = false, message = "Truck (truck_id) is required." });
                }

                if (request.DateTimeAppointment == default)
                {
                    return BadRequest(new { success = false, message = "Appointment date/time is required." });
                }

                if (request.DateTimeLeave == default)
                {
                    return BadRequest(new { success = false, message = "Leave date/time is required." });
                }

                if (request.DateTimeLeave <= request.DateTimeAppointment)
                {
                    return BadRequest(new { success = false, message = "Leave date/time must be after appointment date/time." });
                }

                if (request.SupplierId <= 0)
                {
                    return BadRequest(new { success = false, message = "Supplier (supplier_id) is required." });
                }

                if (string.IsNullOrWhiteSpace(request.Client))
                {
                    return BadRequest(new { success = false, message = "Client name is required." });
                }

                if (request.Client.Length > 250)
                {
                    return BadRequest(new { success = false, message = "Client name must not exceed 250 characters." });
                }

                if (request.Weight <= 0)
                {
                    return BadRequest(new { success = false, message = "Weight must be greater than 0." });
                }

                // Update only editable planning fields (preserve completion/signature data)
                existingDelivery.UserId = request.UserId;
                existingDelivery.TruckId = request.TruckId;
                existingDelivery.DateTimeAppointment = request.DateTimeAppointment;
                existingDelivery.DateTimeLeave = request.DateTimeLeave;
                existingDelivery.SupplierId = request.SupplierId;
                existingDelivery.Client = request.Client.Trim();
                existingDelivery.Address = (request.Address ?? string.Empty).Trim();
                existingDelivery.Contacts = (request.Contacts ?? string.Empty).Trim();
                existingDelivery.Invoice = (request.Invoice ?? string.Empty).Trim();
                existingDelivery.Weight = request.Weight;

                await _deliveryService.UpdateDeliveryAsync(existingDelivery);

                _logger.LogInformation("Delivery {DeliveryId} updated via API for client {Client}", id, existingDelivery.Client);

                return Ok(new
                {
                    success = true,
                    message = "Delivery updated successfully.",
                    delivery = new { id = existingDelivery.Id, client = existingDelivery.Client }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating delivery {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating delivery. Please try again later."
                });
            }
        }

        /// <summary>
        /// Delete (remove) delivery by id
        /// </summary>
        [HttpDelete("deliveries/{id:int}")]
        public async Task<IActionResult> DeleteDelivery(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "Valid delivery id is required." });
                }

                var delivery = await _deliveryService.GetDeliveryByIdAsync(id);
                if (delivery == null)
                {
                    return NotFound(new { success = false, message = "Delivery not found." });
                }

                var result = await _deliveryService.DeleteDeliveryAsync(id);

                if (result)
                {
                    _logger.LogInformation("Delivery {DeliveryId} deleted via API", id);
                    return Ok(new { success = true, message = "Delivery deleted successfully." });
                }

                return BadRequest(new { success = false, message = "Failed to delete delivery." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting delivery {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting delivery. Please try again later."
                });
            }
        }

        [HttpGet("trucks")]
        public async Task<IActionResult> GetTrucks()
        {
            try
            {
                var trucks = await _truckService.GetAllTrucksAsync();
                var truckList = trucks.Select(t => new
                {
                    id = t.Id,
                    license = t.License,
                    brand = t.Brand,
                    model = t.Model
                }).ToList();

                return Ok(new
                {
                    success = true,
                    trucks = truckList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching trucks");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching trucks. Please try again later."
                });
            }
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                var supplierList = suppliers.Select(s => new
                {
                    id = s.Id,
                    name = s.SupplierName
                }).ToList();

                return Ok(new
                {
                    success = true,
                    suppliers = supplierList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching suppliers");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching suppliers. Please try again later."
                });
            }
        }

        /// <summary>
        /// OCR extraction from invoice image - extracts client name and weight
        /// </summary>
        [HttpPost("invoice/ocr")]
        public async Task<IActionResult> ExtractInvoiceOcr(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Image file is required. Send as multipart/form-data with key 'file'."
                    });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB max
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Image size must not exceed 10MB."
                    });
                }

                var contentType = file.ContentType ?? "";
                if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "File must be an image (jpeg, png, etc.)."
                    });
                }

                await using var stream = file.OpenReadStream();
                var result = await _invoiceOcrService.ExtractFromImageAsync(stream, contentType);

                if (!result.Success)
                {
                    return Ok(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "OCR extraction failed.",
                        clientName = (string?)null,
                        weight = (double?)null
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Extraction completed.",
                    clientName = result.ClientName,
                    weight = result.Weight
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice OCR extraction");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during OCR. Please try again later.",
                    clientName = (string?)null,
                    weight = (double?)null
                });
            }
        }

        [HttpPost("deliveries/create")]
        public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request data is required." });
                }

                // Validate required fields
                if (request.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Driver (user_id) is required." });
                }

                if (request.TruckId <= 0)
                {
                    return BadRequest(new { success = false, message = "Truck (truck_id) is required." });
                }

                if (request.DateTimeAppointment == default)
                {
                    return BadRequest(new { success = false, message = "Appointment date/time is required." });
                }

                if (request.DateTimeLeave == default)
                {
                    return BadRequest(new { success = false, message = "Leave date/time is required." });
                }

                if (request.DateTimeLeave <= request.DateTimeAppointment)
                {
                    return BadRequest(new { success = false, message = "Leave date/time must be after appointment date/time." });
                }

                if (request.SupplierId <= 0)
                {
                    return BadRequest(new { success = false, message = "Supplier (supplier_id) is required." });
                }

                if (string.IsNullOrWhiteSpace(request.Client))
                {
                    return BadRequest(new { success = false, message = "Client name is required." });
                }

                if (request.Client.Length > 250)
                {
                    return BadRequest(new { success = false, message = "Client name must not exceed 250 characters." });
                }

                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return BadRequest(new { success = false, message = "Address is required." });
                }

                if (request.Address.Length > 250)
                {
                    return BadRequest(new { success = false, message = "Address must not exceed 250 characters." });
                }

                if (string.IsNullOrWhiteSpace(request.Contacts))
                {
                    return BadRequest(new { success = false, message = "Contacts is required." });
                }

                if (request.Contacts.Length > 250)
                {
                    return BadRequest(new { success = false, message = "Contacts must not exceed 250 characters." });
                }

                if (string.IsNullOrWhiteSpace(request.Invoice))
                {
                    return BadRequest(new { success = false, message = "Invoice is required." });
                }

                if (request.Invoice.Length > 250)
                {
                    return BadRequest(new { success = false, message = "Invoice must not exceed 250 characters." });
                }

                if (request.Weight <= 0)
                {
                    return BadRequest(new { success = false, message = "Weight must be greater than 0." });
                }

                // Create delivery (times stored as-is, no timezone conversion)
                var delivery = new Delivery
                {
                    UserId = request.UserId,
                    TruckId = request.TruckId,
                    DateTimeAppointment = request.DateTimeAppointment,
                    DateTimeLeave = request.DateTimeLeave,
                    SupplierId = request.SupplierId,
                    Client = request.Client.Trim(),
                    Address = request.Address.Trim(),
                    Contacts = request.Contacts.Trim(),
                    Invoice = request.Invoice.Trim(),
                    Weight = request.Weight,
                    ReturnFlag = false,
                    DateTimeAccept = null,
                    DateTimeArrival = null,
                    Description = null,
                    Comment = string.Empty,
                    SignClient = null,
                    SatisfactionClient = null,
                    InvoiceImage = null
                };

                await _deliveryService.CreateDeliveryAsync(delivery);

                _logger.LogInformation("Delivery {DeliveryId} created via API for client {Client}", delivery.Id, delivery.Client);

                return Ok(new
                {
                    success = true,
                    message = "Delivery created successfully.",
                    delivery = new
                    {
                        id = delivery.Id,
                        client = delivery.Client
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating delivery");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating delivery. Please try again later."
                });
            }
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations(
            [FromQuery] int? userId = null,
            [FromQuery] string locationType = "driver")
        {
            try
            {
                // For delivery locations, userId is required (delivery_geolocation has user_id)
                if (locationType.Equals("delivery", StringComparison.OrdinalIgnoreCase) && (!userId.HasValue || userId <= 0))
                {
                    return BadRequest(new { success = false, message = "User ID is required for delivery location type." });
                }

                if (locationType.Equals("delivery", StringComparison.OrdinalIgnoreCase))
                {
                    var deliveryLocation = await _geolocationService.GetLatestDeliveryLocationByUserIdAsync(userId!.Value);
                    if (deliveryLocation == null)
                    {
                        return Ok(new { success = true, location = (object?)null, message = "No delivery location found for this driver." });
                    }

                    return Ok(new
                    {
                        success = true,
                        location = new
                        {
                            id = deliveryLocation.Id,
                            latitude = deliveryLocation.SignLati,
                            longitude = deliveryLocation.SignLongi,
                            altitude = deliveryLocation.SignAlti,
                            date_time = deliveryLocation.Delivery?.DateTimeArrival?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty
                        }
                    });
                }
                else
                {
                    // Driver locations - returns latest only (drivergeolocation has no user_id)
                    var driverLocation = await _geolocationService.GetLatestDriverLocationAsync();
                    if (driverLocation == null)
                    {
                        return Ok(new { success = true, location = (object?)null, message = "No driver location found." });
                    }

                    return Ok(new
                    {
                        success = true,
                        location = new
                        {
                            id = driverLocation.Id,
                            latitude = driverLocation.Lati,
                            longitude = driverLocation.Longi,
                            altitude = driverLocation.Alti,
                            date_time = driverLocation.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching location");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching location. Please try again later."
                });
            }
        }

        [HttpPost("excel/pointer")]
        public async Task<IActionResult> SaveAttendanceLocation([FromBody] AttendanceLocationRequest request)
        {
            try
            {
                // Check ModelState for validation errors
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning("Model validation failed: {Errors}", errors);
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = $"Validation failed: {errors}"
                    });
                }

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("Attendance location request is null");
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = "Request data is required."
                    });
                }

                // Log received data for debugging
                _logger.LogDebug("Received attendance location: Time={Time}, Lati={Lati}, Longi={Longi}, Alti={Alti}, Type={Type}, UserId={UserId}",
                    request.Time, request.Lati, request.Longi, request.Alti, request.Type, request.UserId);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Time))
                {
                    _logger.LogWarning("Time field is missing or empty");
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = "Time is required."
                    });
                }

                if (request.UserId <= 0)
                {
                    _logger.LogWarning("Invalid UserId: {UserId}", request.UserId);
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = "Valid user_id is required."
                    });
                }

                // Parse time string from Android format: "yyyy-MM-dd HH:mm:ss"
                DateTime time;
                try
                {
                    time = DateTime.ParseExact(request.Time, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = "Invalid time format. Expected format: yyyy-MM-dd HH:mm:ss"
                    });
                }

                // Verify user exists
                var user = await _authenticationService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    return BadRequest(new AttendanceLocationResponse
                    {
                        Success = false,
                        Message = "User not found."
                    });
                }

                // Create attendance tracking record
                var attendanceTracking = new AttendanceTracking
                {
                    Time = time,
                    Lati = request.Lati,
                    Longi = request.Longi,
                    Alti = request.Alti,
                    Type = request.Type,
                    UserId = request.UserId
                };

                // Save to database
                await _pointageService.CreateAttendanceTrackingAsync(attendanceTracking);

                _logger.LogInformation("Attendance location saved for user {UserId}, type {Type}, at {Time}", 
                    request.UserId, request.Type, time);

                return Ok(new AttendanceLocationResponse
                {
                    Success = true,
                    Message = "Location saved successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving attendance location");
                return StatusCode(500, new AttendanceLocationResponse
                {
                    Success = false,
                    Message = "An error occurred while saving location. Please try again later."
                });
            }
        }

        [HttpPost("delivery")]
        public async Task<IActionResult> GetDeliveries([FromBody] DeliveryRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.UserId <= 0)
                {
                    _logger.LogWarning("Invalid delivery request: UserId={UserId}", request?.UserId ?? 0);
                    return BadRequest(new { error = "Valid user_id is required." });
                }

                // Verify user exists
                var user = await _authenticationService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for delivery request: UserId={UserId}", request.UserId);
                    return BadRequest(new { error = "User not found." });
                }

                // Get deliveries for the user
                var allDeliveries = await _deliveryService.GetDeliveriesByUserIdAsync(request.UserId);

                // Filter deliveries for the next 3 days starting from today (in New Caledonia timezone)
                var today = TimezoneHelper.GetNewCaledoniaTime().Date;
                var threeDaysFromNow = today.AddDays(3).AddHours(23).AddMinutes(59).AddSeconds(59); // End of day 3

                var deliveries = allDeliveries
                    .Where(d => d.DateTimeLeave >= today && d.DateTimeLeave <= threeDaysFromNow)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} deliveries for user {UserId} (next 3 days)", deliveries.Count, request.UserId);

                // Format deliveries for Android app - only required fields
                var deliveryList = deliveries.Select(d => new
                {
                    id = d.Id,
                    date_time_leave = d.DateTimeLeave.ToString("yyyy-MM-dd HH:mm:ss"),
                    date_time_arrival = d.DateTimeArrival?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                    return_flag = d.ReturnFlag ? 1 : 0,
                    client = d.Client ?? string.Empty,
                    Address = d.Address ?? string.Empty,
                    Contact = d.Contacts ?? string.Empty,
                    Detail = d.Description ?? string.Empty
                }).ToList();

                return Ok(deliveryList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching deliveries");
                return StatusCode(500, new { error = "An error occurred while fetching deliveries. Please try again later." });
            }
        }

        [HttpPost("delivery_cancel")]
        public async Task<IActionResult> CancelDelivery([FromBody] DeliveryCancelRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.Id <= 0)
                {
                    _logger.LogWarning("Invalid delivery cancel request: Id={Id}", request?.Id ?? 0);
                    return BadRequest(new { success = false, message = "Valid id is required." });
                }

                // Check if delivery exists
                var delivery = await _deliveryService.GetDeliveryByIdAsync(request.Id);
                if (delivery == null)
                {
                    _logger.LogWarning("Delivery not found for cancellation: Id={Id}", request.Id);
                    return BadRequest(new { success = false, message = "Delivery not found." });
                }

                // Check if already cancelled
                if (delivery.ReturnFlag)
                {
                    _logger.LogInformation("Delivery {Id} is already cancelled", request.Id);
                    return Ok(new { success = true, message = "Delivery is already cancelled." });
                }

                // Cancel the delivery (set return_flag to 1) and store cancellation comment
                var result = await _deliveryService.CancelDeliveryAsync(request.Id, request.Comment);

                if (result)
                {
                    _logger.LogInformation("Delivery {Id} cancelled successfully with comment: {Comment}", request.Id, request.Comment ?? "No comment");
                    return Ok(new { success = true, message = "Delivery cancelled successfully." });
                }
                else
                {
                    _logger.LogWarning("Failed to cancel delivery: Id={Id}", request.Id);
                    return BadRequest(new { success = false, message = "Failed to cancel delivery." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling delivery");
                return StatusCode(500, new { success = false, message = "An error occurred while cancelling delivery. Please try again later." });
            }
        }

        [HttpPost("sign_delivery")]
        public async Task<IActionResult> SignDelivery([FromBody] SignDeliveryRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.DeliveryId <= 0)
                {
                    _logger.LogWarning("Invalid sign delivery request: DeliveryId={DeliveryId}", request?.DeliveryId ?? 0);
                    return BadRequest(new { success = false, message = "Valid delivery_id is required." });
                }

                // Check if delivery exists
                var delivery = await _deliveryService.GetDeliveryByIdAsync(request.DeliveryId);
                if (delivery == null)
                {
                    _logger.LogWarning("Delivery not found for signing: DeliveryId={DeliveryId}", request.DeliveryId);
                    return BadRequest(new { success = false, message = "Delivery not found." });
                }

                // Validate satisfaction value (1, 2, or 3)
                if (request.Satisfaction < 1 || request.Satisfaction > 3)
                {
                    _logger.LogWarning("Invalid satisfaction value: {Satisfaction}", request.Satisfaction);
                    return BadRequest(new { success = false, message = "Satisfaction must be 1, 2, or 3." });
                }

                // Validate weight
                if (request.Weight <= 0)
                {
                    _logger.LogWarning("Invalid weight: {Weight}", request.Weight);
                    return BadRequest(new { success = false, message = "Weight must be greater than 0." });
                }

                // Complete the delivery
                var result = await _deliveryService.CompleteDeliveryAsync(
                    request.DeliveryId,
                    request.Signature ?? string.Empty,
                    request.InvoicePhoto ?? string.Empty,
                    request.Comment ?? string.Empty,
                    request.Weight,
                    request.Satisfaction
                );

                if (result)
                {
                    _logger.LogInformation("Delivery {DeliveryId} completed successfully with signature", request.DeliveryId);
                    return Ok(new { success = true, message = "Delivery validated successfully!" });
                }
                else
                {
                    _logger.LogWarning("Failed to complete delivery: DeliveryId={DeliveryId}", request.DeliveryId);
                    return BadRequest(new { success = false, message = "Failed to complete delivery." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while signing delivery");
                return StatusCode(500, new { success = false, message = "An error occurred while completing delivery. Please try again later." });
            }
        }

        [HttpPost("sign_coordinate")]
        public async Task<IActionResult> SaveSignCoordinate([FromBody] SignCoordinateRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.DeliveryId <= 0 || request.UserId <= 0)
                {
                    _logger.LogWarning("Invalid sign coordinate request: DeliveryId={DeliveryId}, UserId={UserId}", 
                        request?.DeliveryId ?? 0, request?.UserId ?? 0);
                    return BadRequest(new { success = false, message = "Valid delivery_id and user_id are required." });
                }

                // Check if delivery exists
                var delivery = await _deliveryService.GetDeliveryByIdAsync(request.DeliveryId);
                if (delivery == null)
                {
                    _logger.LogWarning("Delivery not found for coordinate save: DeliveryId={DeliveryId}", request.DeliveryId);
                    return BadRequest(new { success = false, message = "Delivery not found." });
                }

                // Verify user exists
                var user = await _authenticationService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for coordinate save: UserId={UserId}", request.UserId);
                    return BadRequest(new { success = false, message = "User not found." });
                }

                // Create delivery geolocation record
                var deliveryGeolocation = new DeliveryGeolocation
                {
                    DeliveryId = request.DeliveryId,
                    UserId = request.UserId,
                    SignLati = request.Latitude,
                    SignLongi = request.Longitude,
                    SignAlti = request.Altitude
                };

                // Save to database
                await _geolocationService.CreateDeliveryGeolocationAsync(deliveryGeolocation);

                _logger.LogInformation("Delivery coordinate saved for delivery {DeliveryId}, user {UserId}", 
                    request.DeliveryId, request.UserId);

                return Ok(new { success = true, message = "Coordinate saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving delivery coordinate");
                return StatusCode(500, new { success = false, message = "An error occurred while saving coordinate. Please try again later." });
            }
        }

        [HttpPost("current_location")]
        public async Task<IActionResult> SaveCurrentLocation([FromBody] CurrentLocationRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.UserId <= 0)
                {
                    _logger.LogWarning("Invalid current location request: UserId={UserId}", request?.UserId ?? 0);
                    return BadRequest(new { success = false, message = "Valid user_id is required." });
                }

                // Verify user exists
                var user = await _authenticationService.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for current location save: UserId={UserId}", request.UserId);
                    return BadRequest(new { success = false, message = "User not found." });
                }

                // Create driver geolocation record with current timestamp
                // Note: drivergeolocation table doesn't have user_id column, so we save location only
                var driverLocation = new DriverGeolocation
                {
                    Lati = request.Latitude,
                    Longi = request.Longitude,
                    Alti = request.Altitude,
                    DateTime = TimezoneHelper.GetNewCaledoniaTime()
                };

                // Save to database
                await _geolocationService.CreateDriverLocationAsync(driverLocation);

                _logger.LogDebug("Driver current location saved for user {UserId} at {DateTime}", 
                    request.UserId, driverLocation.DateTime);

                return Ok(new { success = true, message = "Location saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving current location");
                return StatusCode(500, new { success = false, message = "An error occurred while saving location. Please try again later." });
            }
        }
    }

    // Request DTO
    public class LoginRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    // Create User Request DTO
    public class CreateUserRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public int Role { get; set; } = 1;
    }

    // Update User Request DTO (password optional - if empty, unchanged)
    public class UpdateUserRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("role")]
        public int Role { get; set; } = 1;
    }

    // Create Delivery Request DTO
    public class CreateDeliveryRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("truck_id")]
        public int TruckId { get; set; }

        [JsonPropertyName("date_time_appointment")]
        public DateTime DateTimeAppointment { get; set; }

        [JsonPropertyName("date_time_leave")]
        public DateTime DateTimeLeave { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("client")]
        public string Client { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("contacts")]
        public string Contacts { get; set; } = string.Empty;

        [JsonPropertyName("invoice")]
        public string Invoice { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }

    // Response DTO
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? Id { get; set; } // Root level id for Android compatibility
        public int? UserId { get; set; } // Alternative field name
        public UserInfo? User { get; set; }
    }

    // User Info DTO (without sensitive data)
    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Role { get; set; }
    }

    // Attendance Location Request DTO
    public class AttendanceLocationRequest
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty; // Format: "yyyy-MM-dd HH:mm:ss"
        
        [JsonPropertyName("lati")]
        public double Lati { get; set; }
        
        [JsonPropertyName("longi")]
        public double Longi { get; set; }
        
        [JsonPropertyName("alti")]
        public double Alti { get; set; }
        
        [JsonPropertyName("type")]
        public byte Type { get; set; }
        
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
    }

    // Attendance Location Response DTO
    public class AttendanceLocationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Delivery Request DTO
    public class DeliveryRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
    }

    // Delivery Cancel Request DTO
    public class DeliveryCancelRequest
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }

    // Sign Delivery Request DTO
    public class SignDeliveryRequest
    {
        [JsonPropertyName("delivery_id")]
        public int DeliveryId { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("invoice_photo")]
        public string? InvoicePhoto { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("satisfaction")]
        public int Satisfaction { get; set; }
    }

    // Sign Coordinate Request DTO
    public class SignCoordinateRequest
    {
        [JsonPropertyName("delivery_id")]
        public int DeliveryId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }
    }

    // Current Location Request DTO
    public class CurrentLocationRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }
    }
}

