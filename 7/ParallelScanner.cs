using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TPL;

public class ParallelScanner : IPScanner
{
    public Task Scan(IPAddress[] ipAddresses, int[] ports)
    {
        const int portConnectionTimeout = 1000;
        var ipScans = new Task[ipAddresses.Length];
        for (var i = 0; i < ipAddresses.Length; ++i)
        {
            var ii = i;
            ipScans[i] = Task.Run(async () =>
            {
                Console.WriteLine($"Pinging {ipAddresses[ii]}");
                var ping = await new Ping().SendPingAsync(ipAddresses[ii]);
                Console.WriteLine($"Pinged {ipAddresses[ii]}: {ping.Status}");
                if (ping.Status != IPStatus.Success) return;;
                var portConnections = new Task[ports.Length];
                for (var j = 0; j < ports.Length; ++j)
                {
                    var jj = j;
                    portConnections[j] = Task.Run(async () =>
                    {
                        using var tcpClient = new TcpClient();
                        Console.WriteLine($"Checking {ipAddresses[ii]}:{ports[jj]}");
                        var portStatus = await tcpClient
                            .ConnectAsync(ipAddresses[ii], ports[jj], portConnectionTimeout);
                        Console.WriteLine(
                            $"Checked {ipAddresses[ii]}:{ports[jj]} - {portStatus}");
                    });
                }

                await Task.WhenAll(portConnections);
            });
        }
        return Task.WhenAll(ipScans);
    }
}