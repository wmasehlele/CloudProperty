using Microsoft.Extensions.Caching.Distributed;

namespace CloudProperty.Data
{
	public class DataCacheService
	{
		private readonly IDistributedCache _distributedCache;

		public DataCacheService(IDistributedCache distributedCache)
		{
			_distributedCache = distributedCache;
		}

		public async void SetCacheValue(string key, string value)
		{
			var expiration = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
				SlidingExpiration = TimeSpan.FromMinutes(10)
			};
			int retries = 0;
			var ex = new Exception();
			do
			{
				try
				{
					await _distributedCache.SetStringAsync(key, value, expiration);
					retries = 100;
				}
				catch (Exception exception)
				{
					retries++;
					ex = exception;
				}
			} while (retries < 100);

			if (retries >= 5)
			{
				//throw new Exception(ex.Message);
			}
		}

		public async Task<string> GetCachedValue(string key)
		{
			int retries = 0;
			var ex = new Exception();
			string cachedValue = string.Empty;
			do
			{
				try
				{
					cachedValue = await _distributedCache.GetStringAsync(key);
					retries = 100;
				}
				catch (Exception exception)
				{
					retries++;
					ex = exception;
				}
			} while (retries < 100);

			if (retries >= 5)
			{
				//throw new Exception(ex.Message);
			}

			return cachedValue;
		}
	}
}
