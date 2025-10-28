using System;
using System.Collections.Concurrent;

namespace Voia.Api.Services.Chat
{
    public class PresignedUploadMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = "application/octet-stream";
        public int ConversationId { get; set; }
        public int? UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public interface IPresignedUploadService
    {
        string CreateToken(PresignedUploadMetadata meta, TimeSpan ttl);
        bool TryConsumeToken(string token, out PresignedUploadMetadata? meta);
    }

    public class PresignedUploadService : IPresignedUploadService
    {
        private readonly ConcurrentDictionary<string, PresignedUploadMetadata> _store = new();

        public string CreateToken(PresignedUploadMetadata meta, TimeSpan ttl)
        {
            var token = Guid.NewGuid().ToString("N");
            meta.ExpiresAt = DateTime.UtcNow.Add(ttl);
            _store[token] = meta;
            return token;
        }

        public bool TryConsumeToken(string token, out PresignedUploadMetadata? meta)
        {
            meta = null;
            if (string.IsNullOrWhiteSpace(token)) return false;
            if (!_store.TryRemove(token, out var stored)) return false;
            if (stored.ExpiresAt < DateTime.UtcNow) return false;
            meta = stored;
            return true;
        }
    }
}
