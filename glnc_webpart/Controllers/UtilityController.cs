using glnc_webpart.Services;
using Microsoft.AspNetCore.Mvc;

namespace glnc_webpart.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilityController : ControllerBase
    {
        private readonly IPasswordHasher _passwordHasher;

        public UtilityController(IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        [HttpGet("hash-password")]
        public IActionResult HashPassword([FromQuery] string password = "12345")
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { error = "Password cannot be empty" });
            }

            try
            {
                var hashedPassword = _passwordHasher.HashPassword(password);
                return Ok(new
                {
                    originalPassword = password,
                    hashedPassword = hashedPassword,
                    message = "Password hashed successfully using BCrypt"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("verify-password")]
        public IActionResult VerifyPassword([FromBody] VerifyPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.HashedPassword))
            {
                return BadRequest(new { error = "Password and hashed password are required" });
            }

            var isValid = _passwordHasher.VerifyPassword(request.Password, request.HashedPassword);
            return Ok(new
            {
                isValid = isValid,
                message = isValid ? "Password matches" : "Password does not match"
            });
        }
    }

    public class VerifyPasswordRequest
    {
        public string Password { get; set; } = string.Empty;
        public string HashedPassword { get; set; } = string.Empty;
    }
}

