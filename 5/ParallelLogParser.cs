using System.Collections.Concurrent;

namespace LogParsing.LogParsers;

public class ParallelLogParser: ILogParser
{
    private readonly FileInfo file;
    private readonly Func<string, string?> tryGetIdFromLine;

    public ParallelLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
    {
        this.file = file;
        this.tryGetIdFromLine = tryGetIdFromLine;
    }
    public string[] GetRequestedIdsFromLogFile()
    {
        var lines = File.ReadLines(file.FullName);
        var ids = new ConcurrentBag<string>();
        Parallel.ForEach(lines, line =>
        {
            var id = tryGetIdFromLine(line);
            if (id != null)
            {
                ids.Add(id);
            }
        });
        return ids.ToArray();
    }
}