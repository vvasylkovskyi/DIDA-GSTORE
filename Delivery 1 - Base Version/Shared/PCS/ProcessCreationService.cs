﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Shared.PCS
{
    public class ProcessCreationService : IProcessCreationService
    {
        List<Process> processesList = new List<Process>();
        
        static void Main(string[] args)
        {
            new ProcessCreationService().Init(args);
        }

        private void Init(string[] args)
        {

        }

        public void StartServer(string args)
        {
            Console.WriteLine(">>> Starting Server: " + args);
            string directory = Directory.GetCurrentDirectory();
            string path = System.IO.Path.Combine(directory, "../DataStoreServer/bin/Debug/netcoreapp3.1/DataStoreServer");
            processesList.Add(Process.Start(path, args));
        } 

        public void StartClient(string args)
        {
            Console.WriteLine(">>> Starting Client: " + args);
            string directory = Directory.GetCurrentDirectory();
            string path = System.IO.Path.Combine(directory, "../client/bin/Release/netcoreapp3.1/client");
            processesList.Add(Process.Start(path, args));
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
