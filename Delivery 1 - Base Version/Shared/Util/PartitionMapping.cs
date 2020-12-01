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
        public static Dictionary<string, string> partitionToReplicationFactorMapping = new Dictionary<string, string>();
        public static string starting_replication_factor;

        public static void UpdateReplcationFactor(string replicationFactor)
        {
            starting_replication_factor = replicationFactor;

            foreach (string partitionName in partitionToReplicationFactorMapping.Keys)
            {
                partitionToReplicationFactorMapping[partitionName] = replicationFactor;
            }

            Console.WriteLine(">>> Replication Factor updated successfully");
        }

        private static bool TryGetPartition(string partitionName, out string[] serverIds)
        {
            return partitionMapping.TryGetValue(partitionName, out serverIds);
        }

        public static void CreatePartitionMapping(Dictionary<string, string> partitionToReplicationFactorMapping, Dictionary<string, string[]> partitionMapping)
        {
            PartitionMapping.partitionToReplicationFactorMapping = partitionToReplicationFactorMapping;
            PartitionMapping.partitionMapping = partitionMapping;
        }

        public static void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            partitionToReplicationFactorMapping[partitionName] = replicationFactor;

            if (TryGetPartition(partitionName, out string[] existingServerIds))
            {
                partitionMapping[partitionName] = serverIds;

                Console.WriteLine(">>> Partition was updated with success");
                return;
            }
            partitionMapping.Add(partitionName, serverIds);
            Console.WriteLine(">>> New partition created with success");
            getPartitionMaster(partitionName);

        }

        public static string getPartitionMaster(string partitionName)
        {
            return partitionMapping[partitionName][0];
        }

        public static string[] getPartitionNodes(string partitionName)
        {
            return partitionMapping[partitionName];
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
    }
}
