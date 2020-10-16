using Grpc.Core;
using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;

namespace DataStoreServer
{
    class Program
    {
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
    }
}
