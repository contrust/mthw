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

            var sortedKeys = new string[keys.Length];
            Array.Copy(keys, sortedKeys, keys.Length);
            Array.Sort(sortedKeys);
            
            _keys = sortedKeys;

            foreach (var key in sortedKeys)
            {
                _multiLock._semaphores.TryGetValue(key, out var semaphore);
                if (semaphore == null)
                {
                    var newSemaphore = new SemaphoreSlim(1, 1);
                    _multiLock._semaphores.TryAdd(key, newSemaphore);
                    _multiLock._semaphores.TryGetValue(key, out semaphore);
                }
                semaphore.Wait();
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

    public Task<IDisposable> AcquireLockAsync(params string[] keys)
    {
        return Task.Factory.StartNew<IDisposable>(() => new MultiLockKeysHolder(this, keys));
    }
}