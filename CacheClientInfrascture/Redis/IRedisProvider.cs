using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CacheClientInfrascture.Redis
{
	public interface IRedisProvider
	{
		public string ProviderName { get; }
		Task<T> GetAsync<T>(string key);
		Task<bool> AcquireLockAsync(string key, object value, int minutes);
		Task<bool> ReleaseLockAsync(string key, object value);
		void Add(string key, object value);
		void Add(string key, object value, int minutes);
		Task<bool> AddAsync(string key, object value, int minutes);
		T Get<T>(string key, Func<T> callBack, bool force = false);
		T Get<T>(string key, int minutes, Func<T> callBack, bool force = false);
		Task<T> GetAsync<T>(string key, bool force, Func<Task<T>> callBack);
		Task<T> GetAsync<T>(string key, bool force, int minutes, Func<Task<T>> callBack);
		void Remove(string key);
		Task RemoveAsync(string key);
		Task<int> BatchAsync(IList<KeyValuePair<string, object>> data, int batchSize = 100);
		Task<List<T>> GetBatchAsync<T>(IList<string> keys, int batchSize = 100);
		Task RemoveBatchAsync(IList<string> keys, int batchSize = 100);
		Task<bool> LockWrapper(string keyToLock, string lockToken, int ttlInMinutes = 60, int timeToSleepInMilliseconds = 100);
	}
}
