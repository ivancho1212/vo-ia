using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voia.Api.Services
{
    public class InMemoryMessageQueue : IMessageQueue
    {
        private readonly ConcurrentQueue<MessageJob> _queue = new();
        private readonly ILogger<InMemoryMessageQueue> _logger;

        public InMemoryMessageQueue(ILogger<InMemoryMessageQueue> logger)
        {
            _logger = logger;
        }

        public Task EnqueueAsync(MessageJob job)
        {
            _queue.Enqueue(job);
            _logger.LogDebug("Enqueued message job for conversation {conv} message {msg}", job.ConversationId, job.MessageId);
            return Task.CompletedTask;
        }

        public bool TryDequeue(out MessageJob? job)
        {
            return _queue.TryDequeue(out job);
        }
    }
}
