using System;
using System.Collections.Generic;
using System.Linq;

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
                partitionMapping[partitionName] = serverIds;
                Console.WriteLine(">>> Partition was updated with success");
                return;
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

        public static void UpdatePartition(string partitionName, string crashedServerId)
        {
            if(TryGetPartition(partitionName, out string[] currentServerIds))
            {
                string[] updatedListOfServerIds = currentServerIds.Where(val => val != crashedServerId).ToArray();
                if(updatedListOfServerIds.Length != 0)
                {
                    Console.WriteLine(">>> Updating a partition " + partitionName + " That contains Server " + crashedServerId);
                    partitionMapping[partitionName] = updatedListOfServerIds;
                }
                else
                {
                    Console.WriteLine(">>> Removing empty partition: " + partitionName);
                    partitionMapping.Remove(partitionName);
                }
            }
        }

        public static void RemoveCrashedServerFromAllPartitions(string serverId)
        {
            List<string> partitionsToUpdate = new List<string>();
           
            foreach (KeyValuePair<string, string[]> partition in partitionMapping)
            {
                foreach (string serverInPartition in partition.Value)
                {
                    if (serverInPartition == serverId)
                    {
                        partitionsToUpdate.Add(partition.Key);
                    }
                }
            }

            foreach(string partitionName in partitionsToUpdate)
            {
                UpdatePartition(partitionName, serverId);
            }
        }
    }
}
