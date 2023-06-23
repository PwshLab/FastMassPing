using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastMassPing
{
    internal class ThreadManager
    {
        private readonly List<PingThread> pingThreads;
        private MonitorThread monitorThread;
        private OutputThread outputThread;

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Queue<ConnectionInformation> pingOutput;
        private readonly Queue<string> monitorOutput;

        private IPAddress startAddress;
        private long addressSpace;

        private bool addressSpaceSet;
        private bool outputSet;
        private bool threadConfigurationSet;

        public ThreadManager()
        {
            pingThreads = new List<PingThread>();
            cancellationTokenSource = new CancellationTokenSource();
            pingOutput = new Queue<ConnectionInformation>();
            monitorOutput = new Queue<string>();
            addressSpaceSet = false;
            outputSet = false;
            threadConfigurationSet = false;
        }

        public bool SetAddressSpace(IPAddress startAddress, IPAddress endAddress)
        {
            this.startAddress = startAddress;
            addressSpace = AddressCounter.GetAddressSpace(startAddress, endAddress);
            monitorThread = new MonitorThread(pingThreads, addressSpace, monitorOutput, cancellationTokenSource.Token);
            if (addressSpace > 0)
                addressSpaceSet = true;
            return addressSpaceSet;
        }

        public bool SetOutput(string outputFilePath)
        {
            outputThread = new OutputThread(pingOutput, monitorOutput, outputFilePath, cancellationTokenSource.Token);
            outputSet = true;
            return true;
        }

        public bool SetThreadConfiguration(int port, int timeout, int threadCount)
        {
            if (port < 1 || port > 65535)
                return false;
            if (timeout < 0)
                return false;
            if (threadCount < 0)
                return false;

            int mean = (int)Math.Floor((double)addressSpace / threadCount);
            int addit = (int)(addressSpace % threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                int threadAddressSpace = mean + (i < addit ? 1 : 0);
                PingThread thread = new(startAddress, threadAddressSpace, port, timeout, pingOutput, cancellationTokenSource.Token);
                pingThreads.Add(thread);
            }

            threadConfigurationSet = true;
            return true;
        }

        public bool Begin()
        {
            if (!(outputSet && addressSpaceSet && threadConfigurationSet))
                return false;

            foreach (PingThread thread in pingThreads)
                thread.Start();
            monitorThread.Start();
            outputThread.Start();

            return true;
        }

        public void Wait()
        {
            foreach (PingThread thread in pingThreads)
                thread.Join();

            cancellationTokenSource.Cancel();
            monitorThread.Join();
            outputThread.Join();
        }
    }
}
