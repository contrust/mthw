using System.Collections.Concurrent;

namespace CustomThreadPool;

public class CustomThreadPool: IThreadPool
{
    private readonly Queue<Action> _mainQueue;
    private readonly ConcurrentDictionary<Thread, WorkStealingQueue<Action>> _workersQueues;
    private readonly HashSet<Thread> _threadPoolThreads;
    private long _processedTasksCount;

    public CustomThreadPool()
    {
        var threadCount = Environment.ProcessorCount;
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
                var sw = new SpinWait();
                while (true)
                {
                    Action task = null;
                    if (workerQueue.IsEmpty || !workerQueue.LocalPop(ref task))
                    {
                        if (!_mainQueue.Any())
                        {
                            foreach (var queue in _workersQueues.Values)
                            {
                                if (!queue.IsEmpty && queue.TrySteal(ref task))
                                    break;
                            }
                        }
                        if (task == null)
                        {
                            lock (_mainQueue)
                            {
                                if (!_mainQueue.Any())
                                {
                                    Monitor.Wait(_mainQueue);
                                }
                                _mainQueue.TryDequeue(out task);
                            }
                        }
                    }
                    if (task == null)
                    {
                        sw.SpinOnce();
                        continue;
                    }
                    task.Invoke();
                    Interlocked.Increment(ref _processedTasksCount);
                    sw.Reset();
                }
            });
            _threadPoolThreads.Add(thread);
            thread.Start();
        }
    }

    public void EnqueueAction(Action action)
    {
        _workersQueues.TryGetValue(Thread.CurrentThread, out var workStealingQueue);
        if (workStealingQueue != null) workStealingQueue.LocalPush(action);
        else EnqueueActionToMainQueue(action);
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
