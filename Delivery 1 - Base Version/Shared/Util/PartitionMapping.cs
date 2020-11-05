using System;
using System.Collections.Generic;

namespace Shared.Util
{
    class PartitionMapping
    {
        // ----------------------
        //       ATTENTION
        // ----------------------
        // If you add a server to the partition mapping, do not forget to also add it to the server url mapping.
        // Always make sure that a server id always has an entry in both dictionaries.


        // This dictionary stores the mapping between partitions and nodes.

        public static Dictionary<string, List<string>> partitionMapping = new Dictionary<string, List<string>>()
        {
            { 
                "1", new List<string> { "server1", "server2" }
            },
            {
                "2", new List<string> { "server2", "server3" }
            }
        };

        public static int replicasNumber;


        public static void UpdateReplicasNumber(int r)
        {
            replicasNumber = r;
            Console.WriteLine(">>> Replication Factor updated successfully");
        }

        private static bool TryGetPartition(string partitionName, out List<string> serverIds)
        {
            return partitionMapping.TryGetValue(partitionName, out serverIds);
        }

        public static void AddPartition(string partitionName, List<string> serverIds)
        {
            List<string> existingServerIds;
            if (TryGetPartition(partitionName, out existingServerIds))
            {
                partitionMapping.Remove(partitionName);
                Console.WriteLine(">>> Removing old partition...");
            }
            partitionMapping.Add(partitionName, serverIds);
            Console.WriteLine(">>> New partition created with success");

        }

        public static string getPartitionMaster(string partition_id)
        {
            // the first element in the node array is considered the partition master
            return partitionMapping[partition_id][0];
        }

        public static List<string> getPartitionNodes(string partition_id)
        {
            return partitionMapping[partition_id];
        }
    }
}
