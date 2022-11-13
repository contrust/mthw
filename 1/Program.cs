using System.Diagnostics;

namespace MultiThreading;

public static class Program
{
    public static void Main(string[] args)
    {
        Process.GetCurrentProcess().ProcessorAffinity = 
            (IntPtr)(1 << (Environment.ProcessorCount - 1));
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        const int measurementsCount = 100;
        var timeSlicesInMilliseconds = new long[measurementsCount];

        for (var j = 0; j < measurementsCount; ++j)
        {
            var timer = new Stopwatch();
            var thread1 = new Thread
            (() =>
                {
                    timer.Start();
                    while (timer.IsRunning) { }
                }
            );
            var thread2 = new Thread
            (() =>
                {
                    timer.Stop();
                }
            );

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            timeSlicesInMilliseconds[j] = timer.ElapsedMilliseconds;
        }
        
        Console.WriteLine("Average thread time slice in " +
                          $"{measurementsCount} measurements: " +
                          $"{timeSlicesInMilliseconds.Average()} ms");
        
        Console.WriteLine("Median thread time slice in " +
                          $"{measurementsCount} measurements: " +
                          $"{timeSlicesInMilliseconds[measurementsCount / 2]} ms");
        
        Console.WriteLine("Min thread time slice in " +
                          $"{measurementsCount} measurements: " +
                          $"{timeSlicesInMilliseconds.Min()} ms");
        
        Console.WriteLine("Max thread time slice in " +
                          $"{measurementsCount} measurements: " +
                          $"{timeSlicesInMilliseconds.Max()} ms");
    }
}
