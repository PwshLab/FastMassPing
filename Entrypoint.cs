using System;
using System.Net;
using System.IO;

namespace FastMassPing
{
    class Entrypoint
    {
        static void SendHelp(bool freeLine)
        {
            if (freeLine)
                Console.WriteLine();
            Console.WriteLine("FastMassPing.exe            The fastest ping in the west");
            Console.WriteLine();
            Console.WriteLine("Avaliable Parameters");
            Console.WriteLine("--help           Displays this help");
            Console.WriteLine("--start-ip       First IP adress                      Mandatory");
            Console.WriteLine("--end-ip         Last IP address                      Mandatory");
            Console.WriteLine("--port           Port to check                        Mandatory");
            Console.WriteLine("--timeout        Timeout in milliseconds              Default: 1000    Minimum: 0");
            Console.WriteLine("--threads        Number of threads to use             Default: 16      Minimum: 2");
            Console.WriteLine("--output         File to write found addresses to     Default: None");
            Console.WriteLine();
            Console.WriteLine("Use of a proxy or vpn is recommended.");
            Console.WriteLine("If no output file is specified found IPs will be displayed in the console.");
            Console.WriteLine();
            Console.WriteLine("Warning: Use of 128 threads or more can lead to serious internet performance");
            Console.WriteLine("          issues not just for the machine running this program but for the");
            Console.WriteLine("          entire network sharing the same router. Please set appropriate");
            Console.WriteLine("          values that you know your network can handle.");
            return;
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                SendHelp(false);
                return;
            }

            IPAddress startAddress = null;
            IPAddress endAddress = null;
            Int32 port = 0;
            Int32 timeout = 1000;
            Int32 threadCount = 16;
            string outputFile = "";
            Boolean[] varCheck = { false, false, false, false, false, false };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                        SendHelp(false);
                        return;
                        break;

                    case "--start-ip":
                        try
                        {
                            startAddress = IPAddress.Parse(args[i + 1]);
                            varCheck[0] = true;
                        }
                        catch
                        {
                            Console.WriteLine("Start IP address invalid. Aborting.");
                            return;
                        }
                        break;

                    case "--end-ip":
                        try
                        {
                            endAddress = IPAddress.Parse(args[i + 1]);
                            varCheck[1] = true;
                        }
                        catch
                        {
                            Console.WriteLine("End IP address invalid. Aborting.");
                            return;
                        }
                        break;

                    case "--port":
                        try
                        {
                            port = Convert.ToInt32(args[i + 1]);
                            varCheck[2] = true;
                        }
                        catch
                        {
                            Console.WriteLine("Port invalid. Aborting.");
                            return;
                        }
                        break;

                    case "--timeout":
                        try
                        {
                            timeout = Convert.ToInt32(args[i + 1]);
                            varCheck[3] = true;
                        }
                        catch
                        {
                            Console.WriteLine("Timeout invalid. Setting to Default.");
                            timeout = 1000;
                        }
                        break;

                    case "--threads":
                        try
                        {
                            threadCount = Convert.ToInt32(args[i + 1]);
                            varCheck[4] = true;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Number of Threads invalid. Setting to Default.");
                            threadCount = 16;
                        }
                        break;

                    case "--output":
                        outputFile = args[i + 1];
                        varCheck[5] = true;
                        break;

                    default:
                        break;
                }
            }

            if (!varCheck[0])
            {
                Console.WriteLine("Start IP address unspecified. Aborting.");
                SendHelp(true);
                return;
            }
            if (!varCheck[1])
            {
                Console.WriteLine("End IP address unspecified. Aborting.");
                SendHelp(true);
                return;
            }
            if (!varCheck[2])
            {
                Console.WriteLine("Port unspecified. Aborting.");
                SendHelp(true);
                return;
            }
            else
            {
                if (port < 0)
                {
                    Console.WriteLine("Port cannot be smaller than 0. Aborting.");
                    return;
                }
                if (port > 65535)
                {
                    Console.WriteLine("Port cannot be larger than 65535. Aborting.");
                    return;
                }
            }
            if (varCheck[3])
            {
                if (timeout < 0)
                    Console.WriteLine("Warning: Timeout cannot be smaller than 0. Setting to Minimum.");
                timeout = Math.Max(timeout, 0);
            }
            if (varCheck[4])
            {
                if (threadCount < 2)
                    Console.WriteLine("Warning: No fewer than 2 Threads allowed. Setting to Minimum.");
                threadCount = Math.Max(threadCount, 2);

            }
            if (varCheck[5])
            {
                try
                {
                    using (StreamWriter outFile = new StreamWriter(outputFile, append: true)) { };
                }
                catch
                {
                    Console.WriteLine("Output file could not be opened. Aborting.");
                    return;
                }
            }

            Console.WriteLine("Searching for addresses with open Port {0} with {1} Threads", port, threadCount);
            Console.WriteLine("Pinging from {0} to {1}\n", startAddress.ToString(), endAddress.ToString());

            ThreadManager manager = new();

            if (!manager.SetAddressSpace(startAddress, endAddress))
            {
                Console.WriteLine("End address cannot be smaller or equal to Start address. Aborting.");
                return;
            }

            manager.SetOutput(outputFile);
            manager.SetThreadConfiguration(port, timeout, threadCount);
            manager.Begin();
            manager.Wait();

            return;
        }
    }
}
