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

        public static Dictionary<string, string[]> partitionMapping = new Dictionary<string, string[]>();
        public static string replicationFactor;


        public static void UpdateReplicasNumber(string r)
        {
            replicationFactor = r;
            Console.WriteLine(">>> Replication Factor updated successfully");
        }

        private static bool TryGetPartition(string partitionName, out string[] serverIds)
        {
            return partitionMapping.TryGetValue(partitionName, out serverIds);
        }

        public static void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            UpdateReplicasNumber(replicationFactor);

            if (TryGetPartition(partitionName, out string[] existingServerIds))
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

        public static string[] getPartitionNodes(string partition_id)
        {
            return partitionMapping[partition_id];
        }
    }
}
