using System.Threading.Tasks;
namespace Voia.Api.Services
{
    public interface IMessageQueue
    {
        Task EnqueueAsync(MessageJob job);
    }
}
