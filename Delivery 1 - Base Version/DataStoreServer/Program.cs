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

            if (args.Length < 4) {
                Console.WriteLine("this program needs 4 arguments <server_id>, <URL>, <Min_delay>  and <Max_delay>");
                return;
            }
            int server_id = int.Parse(args[0]);
            String url = args[1];
            int min_delay = int.Parse(args[2]);
            int max_delay = int.Parse(args[3]);
            String host_name = url.Split(":")[0];
            int port = int.Parse(url.Split(":")[1]);

            Server server = new Server
            {
                Services = { DataStoreService.BindService(new DataStoreServiceImpl(server_id, min_delay,max_delay)) },
                Ports = { new ServerPort(host_name, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("I'm ready to work");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
