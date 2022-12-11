using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;

namespace LogParsing.LogParsers;

public class ThreadLogParser: ILogParser
{
    private readonly FileInfo file;
    private readonly Func<string, string?> tryGetIdFromLine;

    public ThreadLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
    {
        this.file = file;
        this.tryGetIdFromLine = tryGetIdFromLine;
    }
    public string[] GetRequestedIdsFromLogFile()
    {
        var lines = File.ReadLines(file.FullName);
        var ids = new ConcurrentBag<String>();
        var threadCount = Environment.ProcessorCount;
        var queues = new ConcurrentQueue<String>[threadCount];
        var threads = new Thread[threadCount];
        var isLoopRunning = true;
        for (var i = 0; i < threadCount; ++i)
        {
            var queue = new ConcurrentQueue<String>();
            queues[i] = queue;
            threads[i] = new Thread(() =>
            {
                var sw = new SpinWait();
                while (isLoopRunning)
                {
                    queue.TryDequeue(out var line);
                    if (line == null)
                    { 
                        sw.SpinOnce();
                    }
                    else
                    {
                        var id = tryGetIdFromLine(line);
                        if (id != null)
                        {
                            ids.Add(id);
                        }
                    }
                }
            });
            threads[i].Start();
        }

        var index = 0;
        foreach (var line in lines)
        {
            queues[index].Enqueue(line);
            if (++index == threadCount)
            {
                index = 0;
            }
        }
        
        isLoopRunning = false;

        for (var i = 0; i < threadCount; ++i)
        {
            threads[i].Join();
        }

        return ids.ToArray();
    }
}