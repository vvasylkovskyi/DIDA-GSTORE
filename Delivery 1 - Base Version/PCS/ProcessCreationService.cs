using System;
using Grpc.Core;

namespace PCS
{
    public class ProcessCreationService
    {
        private DataStoreClient.Program client;
        private DataStoreServer.Program server;
        private string pcsRole;


        public static void Main(string[] args)
        {
            new ProcessCreationService(args);
        }

        public ProcessCreationService(string[] args)
        {
            Console.WriteLine(">>> Started Running PCS");
            Console.WriteLine(">>> Please Write arguments (<port>)");
            if (args.Length == 0)
            {
                while(true)
                {
                    ReadArgsFromCommandLine();
                }
            } else
            {
                Init(args);
            }
             
        }

        private void ReadArgsFromCommandLine()
        {
            string port = Console.ReadLine();;

            Console.WriteLine(">>> Starting PCS on port : " + port);
            Init(new string[] { port });
        }

        public void Init(string[] args)
        {
            InitPCSServer(args);
        }

        public void InitPCSServer(string[] args)
        {
            string portString = args[0];
            int.TryParse(portString, out int port);
            Server server = new Server
            {
                Services = { PCSServices.BindService(new PCSImpl(this)) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine(">>> PCS Server started");
        }

        public void StartServer(string[] args)
        {
            Console.WriteLine(">>> Starting Server...");
            DataStoreServer.Program server = new DataStoreServer.Program().StartServer(args);
            this.server = server;
            pcsRole = "server";
        }

        public void StartClient(string[] args)
        {
            Console.WriteLine(">>> Starting Client...");
            DataStoreClient.Program client = new DataStoreClient.Program().StartClient(args);
            this.client = client;
            pcsRole = "client";
        }

        public void Crash()
        {
             server.Crash();
        }

        public void Freeze()
        {
            server.Freeze();
        }

        public void Unfreeze()
        {
             server.Unfreeze();   
        }

        public void GlobalStatus()
        {
            if (pcsRole == "server")
            {
                server.GetStatus();
                
            }
            else if (pcsRole == "client")
            {
                client.GetStatus();
            }
        }

        //public void ShutdownAllProcesses()
        //{
        //    Console.WriteLine(">>> Exiting all processes");
        //    foreach (Process process in processesList)
        //    {
        //        try
        //        {
        //            process.CloseMainWindow();
        //            process.Close();
        //        }
        //        catch (InvalidOperationException)
        //        {
        //            Console.WriteLine(">>> Exception, Process is already closed");
        //        }
        //    }
        //    Console.WriteLine(">>> Done.");
        //}
    }
}
