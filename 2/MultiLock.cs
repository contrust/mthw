namespace MultiLock;

public interface IMultiLock
{
    public IDisposable AcquireLock(params string[] keys);
}

public class MultiLock: IMultiLock
{
    private class MultiLockKeysHolder : IDisposable
    {
        private readonly MultiLock _multiLock;
        private readonly string[] _keys;

        public MultiLockKeysHolder(MultiLock multiLock, params string[] keys)
        {
            _multiLock = multiLock;
            _keys = keys;

            var sortedKeys = new string[keys.Length];
            Array.Copy(keys, sortedKeys, keys.Length);
            Array.Sort(sortedKeys);
            
            foreach (var key in sortedKeys)
            {
                var lockObj = multiLock.GetLockObject(key);
                Monitor.Enter(lockObj);
            }
        }
    
        public void Dispose()
        {
            foreach (var key in _keys)
            {
                var lockObj = _multiLock.GetLockObject(key);
                if (Monitor.IsEntered(lockObj))
                    Monitor.Exit(lockObj);
            }
        }
    }
    
    private readonly Dictionary<string, object> _keysLockObjects;

    public MultiLock(IEnumerable<string> allowedKeys)
    {
        _keysLockObjects = new Dictionary<string, object>();
        foreach (var key in allowedKeys)
        {
            _keysLockObjects.Add(key, new object());
        }
    }
    
    private object GetLockObject(string key)
    {
        _keysLockObjects.TryGetValue(key, out var lockObj);
        if (lockObj == null) 
            throw new KeyNotFoundException($"{key} key not found in allowed keys.");
        return lockObj;
    }

    public IDisposable AcquireLock(params string[] keys)
    {
        return new MultiLockKeysHolder(this, keys);
    }
}