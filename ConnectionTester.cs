﻿using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FastMassPing
{
    class ConnectionTester : IDisposable
    {
        readonly TcpClient tcpClient;
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

    struct ConnectionInformation
    {
        public IPAddress address;
        public int port;
        public bool reachable;
        public int ping;

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