using System;
using System.IO;
using System.Linq;

namespace LogParsing.LogParsers
{
    public class PLinqLogParser: ILogParser
    {
        private readonly FileInfo file;
        private readonly Func<string, string?> tryGetIdFromLine;

        public PLinqLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this.file = file;
            this.tryGetIdFromLine = tryGetIdFromLine;
        }
        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(file.FullName);
            return lines
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(tryGetIdFromLine)
                .Where(id => id != null)
                .ToArray();
        }
    }
}