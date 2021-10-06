using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CacheClientInfrascture.Redis.Exceptions;
using StackExchange.Redis;

namespace CacheClientInfrascture.Redis
{
	public class RedisProvider : IRedisProvider
	{
		public string ProviderName { get; internal set; }

		private readonly IDatabase _database;

		public RedisProvider(IRedisConnectionFactory redisConnectionFactory, string providerName)
		{
			ProviderName = providerName;
			_database = redisConnectionFactory.GetFactory();
		}
		public async Task<T> GetAsync<T>(string key)
		{
			if (key == null) throw ExceptionFactory.Create(eRedisExceptionType.AcquireKeyIsNull);
			var redisValue = await _database.StringGetAsync(key);
			if (redisValue.HasValue == true)
			{
				T obj = JsonSerializer.Deserialize<T>(redisValue);
				return obj;
			}
			return default(T);
		}
		public async Task<bool> AcquireLockAsync(string key, object value, int minutes)
		{
			if (key == null) throw ExceptionFactory.Create(eRedisExceptionType.AcquireKeyIsNull);
			if (value == null) throw ExceptionFactory.Create(eRedisExceptionType.ReleaseLockValueIsNull);

			var json = JsonSerializer.Serialize(value);
			return await _database.LockTakeAsync(key, json, TimeSpan.FromMinutes(minutes));
		}
		public async Task<bool> ReleaseLockAsync(string key, object value)
		{
			if (key == null) throw ExceptionFactory.Create(eRedisExceptionType.ReleaseKeyIsNull);
			if (value == null) throw ExceptionFactory.Create(eRedisExceptionType.ReleaseLockValueIsNull);

			var json = JsonSerializer.Serialize(value);
			return await _database.LockReleaseAsync(key, json);
		}
		public void Add(string key, object value)
		{
			Add(key, value, 0);
		}

		public void Add(string key, object value, int minutes)
		{
			var json = JsonSerializer.Serialize(value);

			if (minutes != 0)
			{
				_database.StringSet(key, json, expiry: TimeSpan.FromMinutes(minutes));
			}
			else
			{
				_database.StringSet(key, json);
			}
		}

		public async Task<bool> AddAsync(string key, object value, int minutes)
		{
			var json = JsonSerializer.Serialize(value);

			if (minutes != 0)
			{
				return await _database.StringSetAsync(key, json, expiry: TimeSpan.FromMinutes(minutes));
			}
			else
			{
				return await _database.StringSetAsync(key, json);
			}
		}

		public T Get<T>(string key, Func<T> callBack, bool force = false)
		{
			return Get(key, 0, callBack, force);
		}

		public T Get<T>(string key, int minutes, Func<T> callBack, bool force = false)
		{
			if (key == null)
				return default(T);

			var value = _database.StringGet(key);

			if ((value.HasValue == false || force) && callBack != null)
			{
				var data = callBack();

				if (data != null)
				{
					Add(key, data, minutes);
					return data;
				}
			}

			if (value.HasValue)
				return JsonSerializer.Deserialize<T>(value);

			return default(T);
		}

		public async Task<T> GetAsync<T>(string key, bool force, Func<Task<T>> callBack)
		{
			return await GetAsync(key, force, 0, callBack);
		}

		public async Task<T> GetAsync<T>(string key, bool force, int minutes, Func<Task<T>> callBack)
		{
			if (key == null)
				return default(T);

			var value = await _database.StringGetAsync(key);

			if ((value.HasValue == false || force) && callBack != null)
			{
				var data = await callBack();

				if (data != null)
				{
					await AddAsync(key, data, minutes);
					return data;
				}
			}

			if (value.HasValue)
				return JsonSerializer.Deserialize<T>(value);

			return default(T);
		}



		public void Remove(string key)
		{
			_database.KeyDelete(key);
		}

		public async Task RemoveAsync(string key)
		{
			await _database.KeyDeleteAsync(key);
		}

		public async Task<int> BatchAsync(IList<KeyValuePair<string, object>> data, int batchSize = 100)
		{
			int total = 0;
			var batch = new List<KeyValuePair<RedisKey, RedisValue>>(batchSize);
			foreach (var pair in data)
			{
				batch.Add(new KeyValuePair<RedisKey, RedisValue>(pair.Key, JsonSerializer.Serialize(pair.Value)));
				if (batch.Count == batchSize)
				{
					await _database.StringSetAsync(batch.ToArray());
					total += batch.Count;
					batch.Clear();
				}
			}
			if (batch.Count != 0)
			{
				await _database.StringSetAsync(batch.ToArray());
				total += batch.Count;
			}

			return total;
		}

		public async Task<List<T>> GetBatchAsync<T>(IList<string> keys, int batchSize = 100)
		{
			var result = new List<T>();
			var batch = new List<RedisKey>(batchSize);
			foreach (var key in keys)
			{
				batch.Add(key);
				if (batch.Count == batchSize)
				{
					await GetResult(result, batch);
					batch.Clear();
				}
			}

			if (batch.Count != 0)
			{
				await GetResult(result, batch);
			}

			return result;

			async Task GetResult<T>(List<T> result, List<RedisKey> batch)
			{
				var values = await _database.StringGetAsync(batch.ToArray());
				var res = values.Select(x =>
				{
					if (x.HasValue)
						return JsonSerializer.Deserialize<T>(x);
					else
						return default(T);
				});
				result.AddRange(res);
			}
		}

		public async Task RemoveBatchAsync(IList<string> keys, int batchSize = 100)
		{
			var batch = new List<RedisKey>(batchSize);
			foreach (var key in keys)
			{
				batch.Add(key);
				if (batch.Count == batchSize)
				{
					await _database.KeyDeleteAsync(batch.ToArray());
					batch.Clear();
				}
			}

			if (batch.Count != 0)
			{
				await _database.KeyDeleteAsync(batch.ToArray());
			}
		}

		public async Task<bool> LockWrapper(string keyToLock, string lockToken, int ttlInMinutes = 60, int timeToSleepInMilliseconds = 100)
		{
			var isLocked = false;
			while (isLocked == false)
			{
				isLocked = await AcquireLockAsync(keyToLock, lockToken, ttlInMinutes);
				if (isLocked == false)
				{
					Thread.Sleep(timeToSleepInMilliseconds);
				}
			}

			return isLocked;
		}
	}
}

