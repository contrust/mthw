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
                const int portConnectionTimeout = 1000;
                var ii = i;
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine($"Pinging {ipAddresses[ii]}");
                    var ping = new Ping().SendPingAsync(ipAddresses[ii]);
                    ping.ContinueWith(task =>
                        {
                            task.Wait();
                            Console.WriteLine($"Pinged {ipAddresses[ii]}: {task.Result.Status}");
                            if (task.Result.Status != IPStatus.Success) return;
                            for (var j = 0; j < ports.Length; ++j)
                            {
                                var jj = j;
                                Task.Factory.StartNew(() =>
                                {
                                    Console.WriteLine($"Checking {ipAddresses[ii]}:{ports[jj]}");
                                    var tcpConnection = new TcpClient()
                                        .ConnectAsync(ipAddresses[ii], ports[jj], portConnectionTimeout);
                                    tcpConnection.ContinueWith(t =>
                                        {
                                            t.Wait();
                                            Console.WriteLine(
                                                $"Checked {ipAddresses[ii]}:{ports[jj]} - {t.Result}");
                                        });
                                }, TaskCreationOptions.AttachedToParent);
                            }
                        },
                        TaskContinuationOptions.AttachedToParent);
                }, TaskCreationOptions.AttachedToParent);
            }
        });
    }
}