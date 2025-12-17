using glnc_webpart.Models;
using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json.Serialization;

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
        private readonly ILogger<AppController> _logger;

        public AppController(
            IAuthenticationService authenticationService, 
            IPointageService pointageService,
            IDeliveryService deliveryService,
            IGeolocationService geolocationService,
            ILogger<AppController> logger)
        {
            _authenticationService = authenticationService;
            _pointageService = pointageService;
            _deliveryService = deliveryService;
            _geolocationService = geolocationService;
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

                // Check if user is a driver (role = 1)
                // Only drivers can log in via Android app
                if (user.Role != 1)
                {
                    _logger.LogWarning("Non-driver user {UserId} ({UserName}) with role {Role} attempted to log in via Android app", 
                        user.Id, user.Name, user.Role);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Access denied. Only drivers can access the mobile application."
                    });
                }

                _logger.LogInformation("Driver user {UserId} ({UserName}) logged in via Android app", 
                    user.Id, user.Name);

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

                // Filter deliveries for the next 3 days starting from today
                var today = DateTime.Today;
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
                    DateTime = DateTime.Now
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

