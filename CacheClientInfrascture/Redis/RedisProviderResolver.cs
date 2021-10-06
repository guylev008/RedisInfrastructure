using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheClientInfrascture.Redis
{
	public interface IRedisProviderResolver
	{
		IRedisProvider ResolveProvider(string providerName);
	}

	public class RedisProviderResolver : IRedisProviderResolver
	{
		private readonly Dictionary<string, IRedisProvider> _providers;

		public RedisProviderResolver(IEnumerable<IRedisProvider> providers)
		{
			_providers = providers.ToDictionary(x => x.ProviderName, x => x);
		}

		public IRedisProvider ResolveProvider(string providerName)
		{
			_providers.TryGetValue(providerName, out var redisProvider);
			if (redisProvider is null)
				throw new ArgumentException(nameof(redisProvider));

			return redisProvider;
		}
	}
}
