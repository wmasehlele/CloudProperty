using Microsoft.Extensions.Caching.Distributed;

namespace CloudProperty.Data
{
    public class DataCacheService
    {
        private readonly IDistributedCache distributedCache;

        public DataCacheService (IDistributedCache distributedCache)
        {           
            this.distributedCache = distributedCache;
        }

        public async void SetCacheValue(string key, string value)
        {
            var expiration = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
            await distributedCache.SetStringAsync(key, value, expiration);
        }

        public async Task<string> GetCachedValue(string key)
        {
            return await distributedCache.GetStringAsync(key);
        }
    }
}
