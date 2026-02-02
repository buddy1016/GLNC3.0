using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<int> GetUsersCountAsync();
        Task<List<User>> GetUsersPagedAsync(int offset, int limit);
        Task<User?> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
    }
}


