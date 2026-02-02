using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IAuthenticationService
    {
        Task<User?> ValidatePasswordAsync(string password);
        Task<User?> GetUserByIdAsync(int userId);
        string HashPassword(string password);
    }
}
