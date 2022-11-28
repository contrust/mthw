namespace LockFreeConcurrentStack;

public interface IStack<T>
{
    void Push(T item);
    bool TryPop(out T item);
    int Count { get; }
}

public class ConcurrentStack<T>: IStack<T>
{
    private class StackNode
    {
        public readonly StackNode? Previous;
        public readonly T Value;
        private readonly int _count;

        public int Count
        {
            get => _count;
            private init
            {
                if (value < 0) throw new ArgumentOutOfRangeException($"Value should not be negative.");
                _count = value;
            }
        }

        public StackNode(T value = default, StackNode? previous = null, int count = 0)
        {
            Value = value;
            Previous = previous;
            Count = count;
        }
    }
    
    private StackNode _current;

    public ConcurrentStack()
    {
        _current = new StackNode();
    }

    public void Push(T item)
    {
        while (true)
        {
            var current = _current;
            var next = new StackNode(item, current, current.Count + 1);
            
            if (Interlocked.CompareExchange(ref _current, next, next.Previous) == next.Previous)
                break;
        }
    }

    public bool TryPop(out T item)
    {
        while (true)
        {
            var current = _current;
            if (current.Previous == null)
            {
                item = default;
                return false;
            }

            if (Interlocked.CompareExchange(ref _current, current.Previous, current) == current)
            {
                item = current.Value;
                return true;
            }
        }
    }

    public int Count => _current.Count;
}
