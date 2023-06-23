using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FastMassPing
{
    public static class PingController
    {
        public static void RunPing(string startAddress, string endAddress, int port, int timeout, int threadCount, string outputPath)
        {
            ThreadManager manager = new ThreadManager();
            manager.SetAddressSpace(IPAddress.Parse(startAddress), IPAddress.Parse(endAddress));
            manager.SetOutput(outputPath);
            manager.SetThreadConfiguration(port, timeout, threadCount);
            manager.Begin();
            manager.Wait();
        }
    }
}
