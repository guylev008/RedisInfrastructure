using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace CacheClientInfrascture.Redis
{
	public class RedisConnectionFactory : IRedisConnectionFactory
	{
		private readonly RedisSettings _redisSettings;
		private IDatabase _database;

		public RedisConnectionFactory(RedisSettings redisSettings)
		{
			_redisSettings = redisSettings;
		}

		public IDatabase GetFactory()
		{
			if (_database != null)
				return _database;

			if (_redisSettings == null)
				throw new ArgumentNullException(nameof(RedisSettings));
			if (string.IsNullOrEmpty(_redisSettings.ConnectionString))
				throw new KeyNotFoundException("Connection String was not found");
			if (_redisSettings.DbNumber < 0 && _redisSettings.DbNumber > 15)
				throw new ArgumentException("DbNumber in incorrect");

			var options = ConfigurationOptions.Parse(_redisSettings.ConnectionString);
			options.AbortOnConnectFail = false;
			var connection = ConnectionMultiplexer.Connect(options);
			_database = connection.GetDatabase(_redisSettings.DbNumber);
			return _database;
		}
	}
}
