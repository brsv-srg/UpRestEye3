using System.Collections.Concurrent;
using System.Threading.Tasks;


namespace UpRestEye3.Services.BusinessLogic
{
    public interface IProcessingLockService
    {
        Task<bool> TryLockAsync(int resourceId);
        Task ReleaseLockAsync(int resourceId);
    }

    public class InMemoryProcessingLockService : IProcessingLockService
    {
        private readonly ConcurrentDictionary<int, bool> _locks = new();

        public Task<bool> TryLockAsync(int resourceId)
        {
            return Task.FromResult(_locks.TryAdd(resourceId, true));
        }

        public Task ReleaseLockAsync(int resourceId)
        {
            _locks.TryRemove(resourceId, out _);
            return Task.CompletedTask;
        }
    }
}
