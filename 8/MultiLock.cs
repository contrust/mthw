using System.Collections.Concurrent;

namespace MultiLock;

public interface IAsyncMultiLock
{
    Task<IDisposable> AcquireLockAsync(params string[] keys);
}

public class MultiLock: IAsyncMultiLock
{
    private class MultiLockKeysHolder : IDisposable
    {
        private readonly MultiLock _multiLock;
        private readonly string[] _keys;

        public MultiLockKeysHolder(MultiLock multiLock, params string[] keys)
        {
            _multiLock = multiLock;
            _keys = keys;
        }

        public async Task Acquire()
        {
            Array.Sort(_keys);
            foreach (var key in _keys)
            {
                _multiLock._semaphores.TryGetValue(key, out var semaphore);
                if (semaphore == null)
                {
                    var newSemaphore = new SemaphoreSlim(1, 1);
                    _multiLock._semaphores.TryAdd(key, newSemaphore);
                    _multiLock._semaphores.TryGetValue(key, out semaphore);
                }
                await semaphore.WaitAsync();
            }
        }
    
        public void Dispose()
        {
            foreach (var key in _keys)
            {
                _multiLock._semaphores.TryGetValue(key, out var semaphore);
                semaphore.Release();
            }
        }
    }
    
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores;

    public MultiLock()
    {
        _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task<IDisposable> AcquireLockAsync(params string[] keys)
    {
        var multiLockKeysHolder = new MultiLockKeysHolder(this, keys);
        await multiLockKeysHolder.Acquire();
        return multiLockKeysHolder;
    }
}
