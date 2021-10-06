using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace CacheClientInfrascture.Redis
{
	public interface IRedisConnectionFactory
	{
		IDatabase GetFactory();
	}
}
