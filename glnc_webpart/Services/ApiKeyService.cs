using glnc_webpart.Data;
using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;

        public ApiKeyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateApiKeyAsync()
        {
            string apiKey = Guid.NewGuid().ToString();
            
            var apiKeyEntity = new ApiKey
            {
                ApiKeyValue = apiKey
            };

            _context.ApiKeys.Add(apiKeyEntity);
            await _context.SaveChangesAsync();

            return apiKey;
        }

        public async Task<List<ApiKey>> GetAllApiKeysAsync()
        {
            return await _context.ApiKeys.OrderByDescending(k => k.Id).ToListAsync();
        }

        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            return await _context.ApiKeys.AnyAsync(k => k.ApiKeyValue == apiKey);
        }

        public async Task<bool> DeleteApiKeyAsync(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null)
                return false;

            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

