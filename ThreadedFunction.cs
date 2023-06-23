using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastMassPing
{
    internal interface IThreadedFunction : IDisposable
    {
        void Start();
        void Join();
        bool IsRunning();
    }

    internal abstract class ThreadedFunction<T> : IThreadedFunction
    {
        private readonly Queue<T> output;
        private readonly CancellationToken token;
        private Thread thread;
        protected bool isActive;

        protected ThreadedFunction(Queue<T> output, CancellationToken token)
        {
            this.output = output;
            this.token = token;
        }

        protected void WriteOutput(T data)
        {
            lock (output)
            {
                output.Enqueue(data);
            }
        }

        protected bool IsCancelled()
        {
            return token.IsCancellationRequested;
        }

        public abstract void Dispose();

        public void Start()
        {
            thread = new(Run)
            {
                IsBackground = true
            };
            thread.Start();
            isActive = true;
        }

        protected abstract void Run();

        public void Join()
        {
            thread?.Join();
        }

        public bool IsRunning()
        {
            return thread.IsAlive && isActive;
        }
    }

    internal class PingThread : ThreadedFunction<ConnectionInformation>
    {
        private readonly AddressCounter counter;
        private readonly ConnectionTester tester;
        private int processedCount;

        public PingThread(IPAddress startAddress, int addressCount, int port, int timeout, Queue<ConnectionInformation> output, CancellationToken token) 
            : base(output, token)
        {
            IPAddress endAddress = AddressCounter.IncrementAddress(startAddress, addressCount);
            counter = new(startAddress, endAddress);
            tester = new(port, timeout);
            processedCount = 0;
        }

        public override void Dispose()
        {
            tester.Dispose();
        }

        protected override void Run()
        {
            foreach (IPAddress address in counter)
            {
                if (IsCancelled())
                    break;

                ConnectionInformation info = tester.Test(address);
                if (info.reachable)
                    WriteOutput(info);

                processedCount++;
            }
            isActive = false;
        }

        public int GetProcessed()
        {
            return processedCount;
        }
    }

    internal class MonitorThread : ThreadedFunction<string>
    {
        private readonly List<PingThread> pingThreads;
        private readonly long addressSpace;
        private readonly DateTime beginTime;

        public MonitorThread(List<PingThread> pingThreads, long addressSpace, Queue<string> output, CancellationToken token)
            : base(output, token)
        {
            this.pingThreads = pingThreads;
            this.addressSpace = addressSpace;
            beginTime = DateTime.Now;
        }

        public override void Dispose()
        {
        }

        protected override void Run()
        {
            while (!IsCancelled())
            {
                Thread.Sleep(500);
                long processed = 0;
                pingThreads.ForEach(x => processed += x.GetProcessed());
                processed = Math.Max(1, processed);
                TimeSpan elapsedTime = DateTime.Now.Subtract(beginTime);
                TimeSpan eta = elapsedTime.Multiply((double)addressSpace / processed).Subtract(elapsedTime);
                string information = String.Format(
                        "\rPinged {0} out of {1} addresses  ( {2} Completed )  T: {3}   ETA: {4}   {5}",
                        processed, 
                        addressSpace, 
                        ((decimal)processed / addressSpace).ToString("P"), 
                        elapsedTime.ToString(@"dd\.hh\:mm\:ss"),
                        eta.ToString(@"dd\.hh\:mm\:ss"), 
                        beginTime.Add(elapsedTime).Add(eta).ToString("G")
                    );
                WriteOutput(information);
            }
            isActive = false;
        }
    }

    internal class OutputThread : ThreadedFunction<string>
    {
        private readonly Queue<ConnectionInformation> pingOutput;
        private readonly Queue<string> monitorOutput;
        private readonly StreamWriter outputWriter;

        public OutputThread(Queue<ConnectionInformation> pingOutput, Queue<string> monitorOutput, string outputFile, CancellationToken token)
            : base (null, token)
        {
            this.pingOutput = pingOutput;
            this.monitorOutput = monitorOutput;
            
            try
            {
                outputWriter = new StreamWriter(outputFile, true)
                {
                    AutoFlush = true
                };
            }
            catch
            {
                outputWriter = null;
            }
        }

        public override void Dispose()
        {
            outputWriter.Dispose();
        }

        protected override void Run()
        {
            if (outputWriter != null)
            {
                while (!IsCancelled())
                {
                    ReadLocked(pingOutput, outputWriter.WriteLine);
                    ReadLocked(monitorOutput, (x) => Console.Write(x[..Math.Min(Console.WindowWidth, x.Length)]));
                }
            }
            else
            {
                while (!IsCancelled())
                {
                    ReadLocked(pingOutput, Console.WriteLine);
                    ReadLocked(monitorOutput, (x) => Console.Title = x);
                }
            }
            isActive = false;
        }

        private static void ReadLocked(Queue<ConnectionInformation> queue, Action<string> action)
        {
            if (queue != null && queue.Count > 0)
            {
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        action.Invoke(queue.Dequeue().ToString());
                    }
                }
            } 
        }

        private static void ReadLocked(Queue<string> queue, Action<string> action)
        {
            if (queue != null && queue.Count > 0)
            {
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        action.Invoke(queue.Dequeue().ToString());
                    }
                }
            }
        }
    }
}
