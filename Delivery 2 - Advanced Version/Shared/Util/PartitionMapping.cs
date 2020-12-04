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
        public static Dictionary<string, int> partitionToClockMapping = new Dictionary<string, int>();
        public static Dictionary<string, string> partitionToMasterMapping = new Dictionary<string, string>();

        public static string starting_replication_factor;

        public static void UpdateReplicationFactor(string replicationFactor)
        {
            starting_replication_factor = replicationFactor;

            foreach (string partitionName in partitionToReplicationFactorMapping.Keys)
            {
                partitionToReplicationFactorMapping[partitionName] = replicationFactor;
            }

            Console.WriteLine(">>> Replication Factor updated successfully");
        }

        public static void UpdatePartitionClock(string partitionName, int partitionClock)
        {
            partitionToClockMapping[partitionName] = partitionClock;

            Console.WriteLine(">>> Partition Clock was updated sucessfully: Partition=" + partitionName + ", Clock=" + partitionClock);
        }

        private static bool TryGetPartition(string partitionName, out string[] serverIds)
        {
            return partitionMapping.TryGetValue(partitionName, out serverIds);
        }

        public static void CreatePartitionMapping(Dictionary<string, string> partitionToReplicationFactorMapping, Dictionary<string, string[]> partitionMapping,
            Dictionary<string, int> partitionToClockMapping, Dictionary<string, string> partitionToMasterMapping)
        {
            PartitionMapping.partitionToReplicationFactorMapping = partitionToReplicationFactorMapping;
            PartitionMapping.partitionMapping = partitionMapping;
            PartitionMapping.partitionToClockMapping = partitionToClockMapping;
            PartitionMapping.partitionToMasterMapping = partitionToMasterMapping;
        }

        public static void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            serverIds = serverIds.Where(o => o.Length > 0).ToArray();

            partitionToReplicationFactorMapping[partitionName] = replicationFactor;
            partitionToClockMapping[partitionName] = 1; // Clock starts with 1
            partitionToMasterMapping[partitionName] = serverIds[0]; // At the beginnig thhe master is the first server
            if (TryGetPartition(partitionName, out string[] existingServerIds))
            {
                partitionMapping[partitionName] = serverIds;
                return;
            }
            partitionMapping.Add(partitionName, serverIds);
            Console.WriteLine(">>> New partition created with success! Servers List: " + string.Join(", ", serverIds));
            Console.WriteLine(">>> PartitionName: " + partitionName + ", Replication Factor: " + replicationFactor + ", Logical Clock: 1"); 
        }

        public static void SetPartitionMaster(string partitionName, string masterId)
        {
            partitionToMasterMapping[partitionName] = masterId;
        }

        public static string GetPartitionMaster(string partitionName)
        {
            return partitionToMasterMapping[partitionName];
        }

        public static bool IsMaster(string partitionName, string serverId)
        {
            return partitionToMasterMapping[partitionName] == serverId;
        }

        public static int GetPartitionClock(string partitionName)
        {
            return partitionToClockMapping[partitionName];
        }

        public static string[] GetPartitionReplicas(string partitionName)
        {
            return partitionMapping[partitionName].Skip(1).ToArray();
        }

        public static string[] GetPartitionAllNodes(string partitionName)
        {
            return partitionMapping[partitionName].ToArray();
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
                    Console.WriteLine(">>> Partition Updated with success! PartitionName=" + partitionName + ", Servers List: " + string.Join(", ", partitionMapping[partitionName].ToArray()));
                }
                else
                {
                    Console.WriteLine(">>> Removing empty partition: " + partitionName);
                    partitionMapping.Remove(partitionName);
                }
            }
        }

        public static List<string> GetPartitionsThatContainServer(string serverId)
        {
            List<string> partitionsWithServer = new List<string>();

            foreach (KeyValuePair<string, string[]> partition in partitionMapping)
            {
                if(partition.Value.Contains(serverId))
                {
                    partitionsWithServer.Add(partition.Key);
                }
            }

            return partitionsWithServer;
        }

        public static void RemoveCrashedServerFromAllPartitions(string serverId)
        {
            List<string> partitionsToUpdate = GetPartitionsThatContainServer(serverId);

            foreach (string partitionName in partitionsToUpdate)
            {
                UpdatePartition(partitionName, serverId);
            }
        }

        public static List<string> GetPartitionsByServerID(string serverId)
        {
            // list of partitions that use the server with serverID
            List<string> result = new List<string>();

            foreach (string partition_id in partitionMapping.Keys)
            {
                if (partitionMapping[partition_id].Contains(serverId))
                    result.Add(partition_id);
            }

            return result;
        }

        public static void RemovePartitionMaster(string partition_id)
        {
            string[] new_replicas = partitionMapping[partition_id].Skip(1).ToArray();
            partitionMapping[partition_id] = new_replicas;

            int new_repl_factor = Int32.Parse(partitionToReplicationFactorMapping[partition_id]);
            new_repl_factor -= 1;
            partitionToReplicationFactorMapping[partition_id] = new_repl_factor.ToString();
        }
    }
}
