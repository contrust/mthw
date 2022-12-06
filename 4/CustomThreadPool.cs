using System.Collections.Concurrent;

namespace CustomThreadPool;

public class CustomThreadPool: IThreadPool
{
    private readonly Queue<Action> _mainQueue;
    private readonly Thread _mainThread;
    private readonly ConcurrentDictionary<Thread, WorkStealingQueue<Action>> _workersQueues;
    private readonly HashSet<Thread> _threadPoolThreads;
    private long _processedTasksCount;

    public CustomThreadPool()
    {
        var threadCount = Environment.ProcessorCount;
        _mainThread = Thread.CurrentThread;
        _mainQueue = new Queue<Action>();
        _threadPoolThreads = new HashSet<Thread>();
        _workersQueues = new ConcurrentDictionary<Thread, WorkStealingQueue<Action>>();
        _processedTasksCount = 0;
        for (var i = 0; i < threadCount; ++i)
        {
            var thread = new Thread(() =>
            {
                var workerQueue = new WorkStealingQueue<Action>();
                _workersQueues[Thread.CurrentThread] = workerQueue;
                while (true)
                {
                    Action task = null;
                    if (workerQueue.IsEmpty || !workerQueue.LocalPop(ref task))
                    {
                        lock (_mainQueue)
                        {
                            if (!_mainQueue.Any())
                            {
                                foreach (var queue in _workersQueues.Values)
                                {
                                    if (!queue.IsEmpty && queue.TrySteal(ref task))
                                        break;
                                }
                                if (task == null) Monitor.Wait(_mainQueue);
                            }
                            if (task == null) _mainQueue.TryDequeue(out task);
                        }
                    }

                    if (task == null) continue;
                    task.Invoke();
                    Interlocked.Increment(ref _processedTasksCount);
                }
            });
            _threadPoolThreads.Add(thread);
            thread.Start();
        }
    }

    public void EnqueueAction(Action action)
    {
        if (Thread.CurrentThread == _mainThread)
        {
            EnqueueActionToMainQueue(action);
        }
        else
        {
            _workersQueues.TryGetValue(Thread.CurrentThread, out var workStealingQueue);
            if (workStealingQueue != null) workStealingQueue.LocalPush(action);
            else EnqueueActionToMainQueue(action);
        }
    }

    private void EnqueueActionToMainQueue(Action action)
    {
        lock (_mainQueue)
        {
            _mainQueue.Enqueue(action);
            Monitor.Pulse(_mainQueue);
        }
    }

    public long GetTasksProcessedCount() => _processedTasksCount;
}