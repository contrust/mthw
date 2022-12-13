using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TPL;

public class ParallelScanner : IPScanner
{
    public Task Scan(IPAddress[] ipAddresses, int[] ports)
    {
        return Task.Factory.StartNew(() =>
        {
            for (var i = 0; i < ipAddresses.Length; ++i)
            {
                var ii = i;
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine($"Pinging {ipAddresses[ii]}");
                    var ping = new Ping().SendPingAsync(ipAddresses[ii]);
                    ping.ContinueWith(task =>
                            Console.WriteLine($"Pinged {ipAddresses[ii]}: {task.Result.Status}"),
                        TaskContinuationOptions.AttachedToParent);
                    ping.ContinueWith(task =>
                        {
                            if (task.Result.Status != IPStatus.Success) return;
                            for (var j = 0; j < ports.Length; ++j)
                            {
                                var jj = j;
                                Task.Factory.StartNew(() =>
                                {
                                    using var tcpClient = new TcpClient();
                                    Console.WriteLine($"Checking {ipAddresses[ii]}:{ports[jj]}");
                                    var tcpConnection = tcpClient
                                        .ConnectAsync(ipAddresses[ii], ports[jj], 3000);
                                    tcpConnection.Wait();
                                    tcpConnection.ContinueWith(t =>
                                        Console.WriteLine(
                                            $"Checked {ipAddresses[ii]}:{ports[jj]} - {t.Result}"));
                                }, TaskCreationOptions.AttachedToParent);
                            }
                        },
                        TaskContinuationOptions.AttachedToParent);
                }, TaskCreationOptions.AttachedToParent);
            }
        });
    }
}