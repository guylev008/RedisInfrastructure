using CacheClientInfrascture.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestCacheProvider
{
	public class RedisProviderTest
	{
		public class TestValue
		{
			public int Id { get; set; }
			public string Value { get; set; }
		}
		[Fact]
		public async void TestCRUDOperations()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");
			try
			{
				var testNullKey = new Guid().ToString();
				var testNullValue = await redisProvider.GetAsync<TestValue>(testNullKey);
				Assert.Null(testNullValue);
				//TO DO: implement tests
			}
			catch (Exception e)
			{
				Console.WriteLine($"[!] GetAsync Failed {e.Message}");
			}


		}
		[Fact]
		public async void TestLock()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");
			try
			{
				var testKey = await redisProvider.GetAsync<TestValue>("someKey");
			}
			catch (Exception e)
			{
				Console.WriteLine($"[!] GetAsync Failed {e.Message}");
			}
			var testKeyLock = await redisProvider.AcquireLockAsync("lock:someKey", "myLockValue", 10);
			Console.WriteLine(testKeyLock);

		}

		[Fact]
		public async void TestMultiThreadsLock()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");

			var testLockKey = "lock:TestMultiThreadsLock";
			var testLockValue = "lock:value";
			var testLockTTL = 10;

			List<Task> tasks = new List<Task>();
			var acquiredCounter = 0;
			await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
			await redisProvider.ReleaseLockAsync(testLockKey, testLockValue);
			for (var i = 0; i < 10; i++)
			{
				var task = Task.Run(async () =>
				{
					var isAcquired = await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
					
					if (isAcquired)
					{
						acquiredCounter++;
					}

				});
				tasks.Add(task);
			}
			await Task.WhenAll(tasks);

			Assert.Equal(1, acquiredCounter);
		}


		[Fact]
		public async void TestMultiThreadsLockWithRelease()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");

			var threadsCount = 5;
			var testLockKey = "lock:TestMultiThreadsLockWithRelease";
			var testLockValue = "lock:value";
			var testLockTTL = 100;

			List<Task> tasks = new List<Task>();
			var acquiredCounter = 0;
			await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
			await redisProvider.ReleaseLockAsync(testLockKey, testLockValue);
			DateTime t0 = DateTime.Now;
			for (var i = 0; i < threadsCount; i++)
			{
				var task = Task.Run(async () =>
				{

					var isAcquired = false;
					while (!isAcquired)
					{
						isAcquired = await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
					}
					acquiredCounter++;
					var releaseLockTask = await redisProvider.ReleaseLockAsync(testLockKey, testLockValue);

				});
				tasks.Add(task);
			}
			await Task.WhenAll(tasks);
			DateTime t1 = DateTime.Now;
			Assert.Equal(threadsCount, acquiredCounter);
		}
		[Fact]
		public async void TestWrongTestLockValue()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");

			var testLockKey = "lock:TestWrongTestLockValue";
			var testLockValue = "lock:value";
			var testWrongLockValue = "lock:WrongValue";
			var testLockTTL = 100;

			//reset
			await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
			await redisProvider.ReleaseLockAsync(testLockKey, testLockValue);

			var lockResult = await redisProvider.AcquireLockAsync(testLockKey, testLockValue, testLockTTL);
			var releaseResultWrongValue = await redisProvider.ReleaseLockAsync(testLockKey, testWrongLockValue);
			var releaseResultCorrectValue = await redisProvider.ReleaseLockAsync(testLockKey, testLockValue);

			Assert.True(lockResult);
			Assert.False(releaseResultWrongValue);
			Assert.True(releaseResultCorrectValue);

		}
		[Fact]
		public async void TestGetBatch()
		{
			var connection = new RedisConnectionFactory(new RedisSettings { DbNumber = 0, ConnectionString = "127.0.0.1:6379" });
			var redisProvider = new RedisProvider(connection, "testProvider");

			var batch = new List<KeyValuePair<string, object>>();


			for (int i = 0; i < 100000; i++)
			{
				batch.Add(new KeyValuePair<string, object>($"test{i}", new TestValue { Id = i, Value = $"testvalue{i}" }));
			}

			await redisProvider.BatchAsync(batch);


			var testNullValue = await redisProvider.GetBatchAsync<TestValue>(batch.Select(x=>x.Key).ToList());
			Assert.NotNull(testNullValue);
		}
	}
}
