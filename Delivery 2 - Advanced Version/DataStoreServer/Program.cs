using DataStoreServer.Domain;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using System;
using System.Collections.Generic;

namespace DataStoreServer
{
    public class Program
    {
        private bool debug_console = true;
        private bool _isFrozen = false;
        private string serverId = "";

        private static ServerImp server;

        static void Main(string[] args)
        {
            new Program().Init(args);
        }

        public Program(string[] args)
        {
        } 

        public Program()
        {
        }

        public Program StartServer(string[] args)
        {
            Program program = new Program();
            program.StartProgram(args);
            return program;
        }

        public void StartProgram(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("this program needs 4 arguments <server_id>, <URL>, <Min_delay>  and <Max_delay>");
                return;
            }
            string server_id = args[0];
            SetServerId(server_id);

            String url = args[1];

            int min_delay = int.Parse(args[2]);
            int max_delay = int.Parse(args[3]);

            server = new ServerImp(server_id, url, min_delay, max_delay);
            server.init_servers();


            Console.WriteLine("I'm ready to work");
            
            if (debug_console)
                Console.WriteLine("serverID= " + server_id + "; url= " + url + "; min_delay= " + min_delay + "; max_delay= " + max_delay);
        }

        public void Init(string[] args)
        {
            StartProgram(args);
            Console.ReadKey();
            //server.ShutdownAsync().Wait();
        }

        public void UpdatePartitionsContext(Dictionary<string, string> partitionToReplicationFactorMapping, Dictionary<string, string[]> partitionMapping,
            Dictionary<string, int> partitionToClockMapping, Dictionary<string, string> partitionToMasterMapping)
        {
            PartitionMapping.CreatePartitionMapping(partitionToReplicationFactorMapping, partitionMapping, partitionToClockMapping, partitionToMasterMapping);
        }

        public void UpdateServersContext(Dictionary<string, string> serverUrlMapping)
        {
            ServerUrlMapping.CreateServerUrlMapping(serverUrlMapping);
        }

        public void GetStatus() 
        {
            Console.WriteLine("--------------------");
            Console.WriteLine(">>> Printing status...");
            Console.WriteLine("--------------------");
            List<Partition> partitions = server.getPartitions();
            List<string> partitionsString = new List<string>();
            List<string> masterOfPartitions = new List<string>();
            foreach(Partition partition in partitions)
            {
                if(partition.getMasterID() == serverId)
                {
                    masterOfPartitions.Add(partition.getName());
                }
                partitionsString.Add(partition.getName());
            }
            
            Console.WriteLine(">>> Role: server. ID: " + serverId + ", Frozen: " + _isFrozen);
            Console.WriteLine(">>> Partitions: " + string.Join(", ", partitionsString.ToArray()) + ", Num of partitions: " + server.getNumberOfPartitions());
            if(masterOfPartitions.Count > 0)
            {
                Console.WriteLine(">>> I am Master of: " + string.Join(", ", masterOfPartitions.ToArray()));
            } else
            {
                Console.WriteLine(">>> I am Replica in every partition");
            }
            Console.WriteLine("--------------------");
        }

        public void Crash() 
        {
            Console.WriteLine(">>> I am going to crash");
            Environment.Exit(1);
        }

        public void Freeze() 
        {
            Console.WriteLine(">>> I am going to freeze");
            server.Freeze();
        }

        public void Unfreeze()
        {
            Console.WriteLine(">>> I am going to unfreeze");
            server.unFreeze();
        }

        public void SetServerId(string serverId) 
        {
            this.serverId = serverId;
        }

        public string GetServerId()
        {
            return serverId;
        }

        public ServerImp GetServer()
        {
            return server;
        }

        public void CreateLocalPartitions()
        {
            List<string> partitions = PartitionMapping.GetPartitionsByServerID(serverId);
            
            foreach (string partition_id in partitions)
            {
                server.createPartition(partition_id);
            }
        }
    }
}
