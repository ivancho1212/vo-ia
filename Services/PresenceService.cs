using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services
{
    // PresenceService stores mapping userId -> set of connectionIds in Redis when available.
    public class PresenceService
    {
        private readonly RedisService? _redis;
        private readonly ILogger<PresenceService> _logger;

        public PresenceService(RedisService? redis, ILogger<PresenceService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private string UserKey(string userId) => $"presence:user:{userId}";

        public async Task AddConnectionAsync(string userId, string connectionId)
        {
            if (_redis == null) return;
            await _redis.Db.SetAddAsync(UserKey(userId), connectionId);
            _logger.LogDebug("Presence add {user} -> {conn}", userId, connectionId);
        }

        public async Task RemoveConnectionAsync(string userId, string connectionId)
        {
            if (_redis == null) return;
            await _redis.Db.SetRemoveAsync(UserKey(userId), connectionId);
            _logger.LogDebug("Presence remove {user} -> {conn}", userId, connectionId);
        }

        public async Task<IEnumerable<string>> GetConnectionsAsync(string userId)
        {
            if (_redis == null) return new List<string>();
            var members = await _redis.Db.SetMembersAsync(UserKey(userId));
            var result = new List<string>(members.Length);
            foreach (var m in members) result.Add(m.ToString());
            return result;
        }
    }
}
