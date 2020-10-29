using System;
using System.Collections.Generic;

namespace Shared.Util
{
    class ServerMapping
    {
        // ----------------------
        //       ATTENTION
        // ----------------------
        // If you add a server to the partition mapping, do not forget to also add it to the server url mapping.
        // Always make sure that a server id always has an entry in both dictionaries.


        // ----- PARTITIONS -----

        // This dictionary stores the mapping between partitions and nodes.
        public static Dictionary<int, string[]> partitionMappingById = new Dictionary<int, string[]>()
        {
            { 1, new string[] {"server1", "server2"} }
        };

        public static Dictionary<string, string[]> partitionMappingByName = new Dictionary<string, string[]>();

        public static int replicasNumber;


        public static void UpdateReplicasNumber(int r)
        {
            replicasNumber = r;
            Console.WriteLine(">>> Replication Factor updated successfully");
        }

        private static bool TryGetPartition(string partitionName, out string[] serverIds)
        {
            return partitionMappingByName.TryGetValue(partitionName, out serverIds);
        }

        public static void AddPartition(string partitionName, string[] serverIds)
        {
            string[] existingServerIds;
            if (TryGetPartition(partitionName, out existingServerIds))
            {
                partitionMappingByName.Remove(partitionName);
                Console.WriteLine(">>> Removing old partition...");
            }
            partitionMappingByName.Add(partitionName, serverIds);
            Console.WriteLine(">>> New partition created with success");

        }

        public static string getPartitionMaster(int partition_id)
        {
            // the first element in the node array is considered the partition master
            return partitionMappingById[partition_id][0];
        }

        public static string[] getPartitionNodes(int partition_id)
        {
            return partitionMappingById[partition_id];
        }

        // ----- SERVER URLs -----

        // This dictionary stores the mapping between server id and url
        public static Dictionary<string, string> serverUrlMapping = new Dictionary<string, string>()
        {
            { "server1", "http://localhost:9081"},
            { "server2", "http://localhost:9082"}
        };

        public static string getServerUrl(string server_id)
        {
            return serverUrlMapping[server_id];
        }
    }
}
