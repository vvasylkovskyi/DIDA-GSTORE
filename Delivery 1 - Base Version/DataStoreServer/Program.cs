using Grpc.Core;
using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;

namespace DataStoreServer
{
    public class Program
    {
        public Program() {}

        static void Main(string[] args)
        {
            Console.WriteLine("Hello! I'm the server");


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

        public void Crash() 
        {
            Console.WriteLine("I am going to crash");
            Environment.Exit(1);
        }
    }
}
