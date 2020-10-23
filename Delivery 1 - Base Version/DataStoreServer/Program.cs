using Grpc.Core;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;

namespace DataStoreServer
{
    public class Program
    {
        private bool _isFrozen = false;
        private string serverId = "";
        public Program() {}

        static void Main(string[] args)
        {
            Console.WriteLine("Hello! I'm the server " + args[0]);

            Data database = new Data();
            Server server = new Server
            {
                Services = { DataStoreService.BindService(new DataStoreServiceImpl(database)) },
                Ports = { new ServerPort("localhost", 9080, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("I'm ready to work");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }

        public void getStatus() 
        {
            Console.WriteLine("Printing status...");
            Console.WriteLine("I am server");
            Console.WriteLine("My id: " + serverId);   
        }

        public void Crash() 
        {
            Console.WriteLine("I am going to crash");
            Environment.Exit(1);
        }

        public void Freez() 
        {
            Console.WriteLine("I am going to freeze");
            _isFrozen = true;
        }

        public void Unfreez()
        {
            Console.WriteLine("I am going to unfreeze");
            _isFrozen = false;
        }

        public void setServerId(string serverId) 
        {
            this.serverId = serverId;
        }
    }
}
