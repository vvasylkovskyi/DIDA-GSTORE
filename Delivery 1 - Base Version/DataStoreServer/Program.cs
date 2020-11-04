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

        private static ServerImp server;
        public Program() {}

        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("this program needs 4 arguments <server_id>, <URL>, <Min_delay>  and <Max_delay>");
                return;
            }
            int server_id = int.Parse(args[0]);
            String url = args[1];
            int min_delay = int.Parse(args[2]);
            int max_delay = int.Parse(args[3]);
            String host_name = url.Split(":")[0];
            int port = int.Parse(url.Split(":")[1]);

            server = new ServerImp(server_id, url, min_delay, max_delay);
            server.init_servers() ;


            Console.WriteLine("I'm ready to work");
            Console.ReadKey();
            //server.ShutdownAsync().Wait();
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
