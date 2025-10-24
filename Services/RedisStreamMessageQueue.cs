using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Voia.Api.Services
{
    // Implements a simple Redis Streams producer using XADD. Consumer groups are used by workers.
    public class RedisStreamMessageQueue : IMessageQueue, IDisposable
    {
        private readonly RedisService _redis;
        private readonly ILogger<RedisStreamMessageQueue> _logger;
        private readonly IDatabase _db;
        private readonly string _streamKey;

        public RedisStreamMessageQueue(RedisService redis, ILogger<RedisStreamMessageQueue> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _db = _redis.Db;
            _streamKey = "voia:message_jobs";
            _logger.LogInformation("RedisStreamMessageQueue initialized for stream {stream}", _streamKey);
        }

        public async Task EnqueueAsync(MessageJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            try
            {
                var payload = JsonSerializer.Serialize(job);
                // XADD stream * payload
                var entryId = await _db.StreamAddAsync(_streamKey, new NameValueEntry[] { new NameValueEntry("payload", payload) });
                _logger.LogDebug("Added job to Redis stream {stream} id={id} conv={conv} msg={msg}", _streamKey, entryId, job.ConversationId, job.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue job to Redis stream");
                throw;
            }
        }

        public void Dispose()
        {
            // RedisService manages ConnectionMultiplexer lifecycle; nothing to dispose here
        }
    }
}
