using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AuthenticationService(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?> ValidatePasswordAsync(string password)
        {
            // Validate that password is exactly 5 digits
            if (string.IsNullOrWhiteSpace(password) || password.Length != 5 || !password.All(char.IsDigit))
            {
                return null;
            }

            // Get all users and verify password hash
            var users = await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                // Verify the password against the stored hash
                if (_passwordHasher.VerifyPassword(password, user.Password))
                {
                    return user;
                }
            }

            return null;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(password);
        }
    }
}
