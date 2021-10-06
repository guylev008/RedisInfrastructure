using System;
using System.Collections.Generic;
using System.Text;

namespace CacheClientInfrascture.Redis
{
	public class RedisSettings
	{
		public int DbNumber { get; set; } = -1;
		public string ConnectionString { get; set; }
	}
}
