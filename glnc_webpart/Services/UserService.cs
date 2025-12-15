using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace glnc_webpart.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Hash the password before saving
            user.Password = _passwordHasher.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
                throw new ArgumentException("User not found");

            existingUser.Name = user.Name;
            existingUser.Role = user.Role;

            // Only update password if it's provided and different
            if (!string.IsNullOrEmpty(user.Password) && user.Password != existingUser.Password)
            {
                // Check if it's already hashed (SHA256 produces 64-character hex strings)
                // If it's 5 digits, it's a plain password that needs hashing
                if (user.Password.Length == 5 && user.Password.All(char.IsDigit))
                {
                    existingUser.Password = _passwordHasher.HashPassword(user.Password);
                }
                // If it's already 64 characters, assume it's already hashed
                else if (user.Password.Length == 64)
                {
                    existingUser.Password = user.Password;
                }
                else
                {
                    // Hash any other format
                    existingUser.Password = _passwordHasher.HashPassword(user.Password);
                }
            }

            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


