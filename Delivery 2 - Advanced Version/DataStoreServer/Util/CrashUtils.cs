using System;
using System.Collections.Generic;
using DataStoreServer.Domain;
using Shared.GrpcDataStore;

namespace DataStoreServer
{
    public static class CrashUtils
    {


        public static IsAliveReply CheckIfServerIsStillAlive(Partition partition, string crashedMasterServerId)
        {
            try
            {
                ServerCommunicationService.ServerCommunicationServiceClient crashedServer = partition.getReplicaById(crashedMasterServerId);
                Console.WriteLine(">>> Check if Server is Alive...");
                return crashedServer.IsAlive(new IsAliveRequest());
            }
            catch
            {
                Console.WriteLine(">>> Server has not replied. It is crashed");
                return null;
            }
        }

        public static void RemoveCrashedServerFromMyLocalPartition(bool isMasterCrashed, string partitionName, string crashedMasterServerId)
        {
            Console.WriteLine(">>> Removing the crashed server...");
            Shared.Util.PartitionMapping.RemoveCrashedServerFromAllPartitions(crashedMasterServerId);
            Shared.Util.ServerUrlMapping.RemoveCrashedServer(crashedMasterServerId);
            if(isMasterCrashed)
            {
                Shared.Util.PartitionMapping.partitionToMasterMapping.Remove(partitionName);
            }
            Console.WriteLine(">>> Server was removed successfully!");
            Console.WriteLine(">>> Current Partition Status: PartitionName=" + partitionName + ", List of Servers: " + string.Join(", ", Shared.Util.PartitionMapping.partitionMapping[partitionName]));
        }

        public static NotifyCrashReply NotifyAllReplicasAboutCrashedServer(Partition partition, string partitionName, string crashedMasterServerId, bool isMasterCrashed)
        {
            int number_of_crash_acks = 0;
            Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> replicas = partition.getReplicas();
            Console.WriteLine(">>> Notifying all replicas in a partition about the crashed server...");
            Console.WriteLine(">>> Number of Replicas: " + replicas.Keys.Count);
            foreach (string replica_id in replicas.Keys)
            {
                try
                {
                    Console.WriteLine(">>> Notifying replica: ReplicaId=" + replica_id);
                    NotifyReplicaAboutCrashReply notifyReplicaAboutCrashReply = replicas[replica_id].NotifyReplicaAboutCrash(
                        new NotifyReplicaAboutCrashRequest { CrashedMasterServerId = crashedMasterServerId, PartitionId = partitionName,  });

                    if (notifyReplicaAboutCrashReply.Ok)
                    {
                        number_of_crash_acks++;
                        Console.WriteLine(">>> Notification Successfull, ackCrash=" + number_of_crash_acks);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine(">>> Error. Replica could not be reached: ReplicaId=" + replica_id + ", Removing Replica from list of replicas");
                    replicas.Remove(replica_id);
                }
            }
            return new NotifyCrashReply { Status = "OK" };
        }

    }
}
