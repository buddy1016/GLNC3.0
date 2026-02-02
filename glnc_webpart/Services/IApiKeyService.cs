using glnc_webpart.Models;

namespace glnc_webpart.Services
{
    public interface IApiKeyService
    {
        Task<string> GenerateApiKeyAsync();
        Task<List<ApiKey>> GetAllApiKeysAsync();
        Task<bool> ValidateApiKeyAsync(string apiKey);
        Task<bool> DeleteApiKeyAsync(int id);
    }
}

