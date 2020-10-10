using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace processCreationService
{
    public class Program : IProcessCreationService
    {
        List<Process> processesList = new List<Process>();
        static void Main(string[] args)
        {
            new Program().Init(args);
        }

        private void Init(string[] args)
        {

        }

        public void StartServer(string args)
        {
            Console.WriteLine(">>> Starting Server: " + args);
            processesList.Add(Process.Start("../server/bin/Debug/netcoreapp3.1/server.dll", args));
        }

        public void StartClient(string args)
        {
            Console.WriteLine(">>> Starting Client: " + args);
            processesList.Add(Process.Start("../client/bin/Debug/netcoreapp3.1/client.dll", args));
        }

        public void ShutdownAllProcesses()
        {
            Console.WriteLine(">>> Exiting all processes");
            foreach (Process process in processesList)
            {
                try
                {
                    process.CloseMainWindow();
                    process.Close();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine(">>> Exception, Process is already closed");
                }
            }
            Console.WriteLine(">>> Done.");
        }
    }
}
