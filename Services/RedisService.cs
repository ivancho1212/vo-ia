using System;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services
{
    // Minimal wrapper around StackExchange.Redis to centralize connection handling.
    public class RedisService : IDisposable
    {
        private readonly ILogger<RedisService> _logger;
        private readonly ConnectionMultiplexer _redis;

        public IDatabase Db => _redis.GetDatabase();

        public RedisService(string configuration, ILogger<RedisService> logger)
        {
            _logger = logger;
            _logger.LogInformation("Connecting to Redis: {cfg}", configuration);
            _redis = ConnectionMultiplexer.Connect(configuration);
        }

        public ISubscriber GetSubscriber() => _redis.GetSubscriber();

        public void Dispose()
        {
            try
            {
                _redis?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing Redis connection");
            }
        }
    }
}
