using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastMassPing
{
    internal class ConnectionTester : IDisposable
    {
        TcpClient tcpClient;
        readonly Stopwatch stopwatch;
        readonly int port;
        readonly int timeout;

        public ConnectionTester(int port, int timeout)
        {
            tcpClient = new TcpClient();
            stopwatch = new Stopwatch();
            this.port = port;
            this.timeout = timeout;
        }

        public ConnectionInformation Test(IPAddress address)
        {
            if (tcpClient.Connected)
            {
                tcpClient.Close();
                tcpClient = new TcpClient();
            }
            try
            {
                using CancellationTokenSource cts = new(timeout);
                Task connectTask = tcpClient.ConnectAsync(address, port, cts.Token).AsTask();
                stopwatch.Restart();
                connectTask.Wait();
                return new ConnectionInformation(address, port, tcpClient.Connected, (int)stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                return new ConnectionInformation(address, port, false, -1);
            }
        }

        public void Dispose()
        {
            tcpClient.Close();
        }
    }

    internal readonly struct ConnectionInformation
    {
        public readonly IPAddress address;
        public readonly int port;
        public readonly bool reachable;
        public readonly int ping;

        public ConnectionInformation(IPAddress address, int port, bool reachable, int ping)
        {
            this.address = address;
            this.port = port;
            this.reachable = reachable;
            if (reachable)
                this.ping = ping;
            else
                this.ping = -1;
        }

        public override readonly string ToString()
        {
            return address.ToString() + ":" + port.ToString() + " -> " + ping + "ms";
        }
    }
}
