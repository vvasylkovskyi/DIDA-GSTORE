using DataStoreServer.Domain;
using DataStoreServer.Util;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DataStoreServer
{
    public class SendValueToReplica
    {
        private ServerImp server;
        private WriteRequest request;
        private readonly string atomic_lock = "ATOMIC_LOCK";

        public SendValueToReplica(ServerImp server)
        {
            this.server = server;
        }

        public SendValueToReplica(ServerImp server, WriteRequest request) {
            this.server = server;
            this.request = request;
        }

        public void atomicWriteLocallyAndUpdateClock(Partition partition)
        {
            try
            {
                Monitor.Enter(atomic_lock);

                // Logs data
                int incrementedClock = partition.incrementClock();
                DataStoreValue value = Utilities.ConvertValueDtoToDomain(request.Object);
                // ---------------

                write_new_value_locally(partition, request);
                Console.WriteLine(">>> Master: Atomic operation Update<clock, value> = <" + incrementedClock + "," + value.val + ">");
            }
            catch
            {
                Console.WriteLine(">>> Exception occured during atomic write");
            }

            finally
            {
                Monitor.Exit(atomic_lock);
            }
        }

        public void doWork() {
            Partition partition = server.getPartition(request.ObjectKey.PartitionId);
            Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> PartitionReplicas = partition.getReplicas();
            atomicWriteLocallyAndUpdateClock(partition);

            int clock = partition.getClock();
            Console.WriteLine(">>> PartitionClock=" + clock);
            Console.WriteLine(">>> Start Updating value and clock on replicas...");
            WriteReply reply = write_new_value_replicas(PartitionReplicas, request, clock);
            server.setWriteResult(request,reply);
        }


        public void write_new_value_locally(Partition partition, WriteRequest request)
        {
            DataStoreKey key = Utilities.ConvertKeyDtoToDomain(request.ObjectKey);
            DataStoreValue value = Utilities.ConvertValueDtoToDomain(request.Object);
            partition.addNewOrUpdateExisting(key, value);
        }

        public WriteReply write_new_value_replicas(Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> replicas, WriteRequest request, int clock)
        {
            int number_of_write_acks = 0;

            Console.WriteLine(">>> Number of Replicas: " + replicas.Keys.Count);
            foreach (string replica_id in replicas.Keys)
            {
                try
                {
                    Console.WriteLine(">>> Writing to the replica: <Value, Clock> = <" + request.Object.Val + ", " + clock + ">");
                    NewValueReply newValueReply = replicas[replica_id].WriteNewValue(new NewValueRequest
                    {
                        Val = request.Object.Val,
                        Clock = clock

                    });

                    if (newValueReply.Ok)
                    {
                        number_of_write_acks++;
                        Console.WriteLine(">>> Write Successful, ackWrite=" + number_of_write_acks);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Replica cannot be reached: " + replica_id);
                    replicas.Remove(replica_id);
                }
            }
            return new WriteReply { WriteStatus = 200 };
        }


        public string ElectPartitionMaster(string partitionId, string crashedMasterServerId)
        {
            Dictionary<string, int> partition_clocks = new Dictionary<string, int>();
            string new_leader_id = "";

            Partition partition = server.getPartition(partitionId);
            Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> partitionReplicas = partition.getReplicas();

            foreach (string replica_id in partitionReplicas.Keys)
            {
                try
                {
                    ClockReply reply = partitionReplicas[replica_id].GetPartitionClock(new ClockRequest
                    {
                        PartitionId = partitionId,
                    });

                    partition_clocks[replica_id] = reply.Clock;
                }
                catch (Exception)
                {
                    Console.WriteLine("Replica cannot be reached: " + replica_id);
                }
            }

            if (partition_clocks.Count == 0)
            {
                new_leader_id = this.server.getID();
                server.becomeLeader(partitionId);
            }
            else
            {
                // find highest clock
                int largest_clock = partition_clocks.Values.Max();

                // filter servers by their clock
                Dictionary<string, int> valid_leaders_list = partition_clocks.Where(x => x.Value == largest_clock).ToDictionary(i => i.Key, i => i.Value);

                // now to find the valid leader with highest ID
                string[] replicas_ordered_by_id = Shared.Util.PartitionMapping.GetPartitionReplicas(partitionId);
                for (int i = 0; i < replicas_ordered_by_id.Length; i++)
                {
                    string server = replicas_ordered_by_id[i];
                    if (valid_leaders_list.Keys.Contains(server)) {
                        new_leader_id = server;

                        // send message granting permission to be a leader
                        partitionReplicas[new_leader_id].GrantPermissionToBecomeLeader(new GrantPermissionRequest
                        {
                            PartitionId = partitionId,
                        });

                        break;
                    }
                }
                
            }
            return new_leader_id;
        }


        public NotifyCrashReply HandleServerCrash(string partitionName, string crashedMasterServerId)
        {
            Console.WriteLine("--------------------");
            Console.WriteLine(">>> SERVER: Handle Crash...");
            Console.WriteLine("--------------------");
            string currentMasterServerId = Shared.Util.PartitionMapping.GetPartitionMaster(partitionName);
            Console.WriteLine(">>> Current Partition Master is: MasterId=" + currentMasterServerId);

            Partition partition = server.getPartition(partitionName);
            IsAliveReply isAliveReply = CrashUtils.CheckIfServerIsStillAlive(partition, crashedMasterServerId);

            if (isAliveReply != null && isAliveReply.Ok)
            {
                Console.WriteLine(">>> Server is Alive! Sending back the MasterId: MasterId=" + currentMasterServerId);
                return new NotifyCrashReply
                {
                    Status = "OK",
                    MasterId = currentMasterServerId
                };
            }

            bool isMasterCrashed = Shared.Util.PartitionMapping.IsMaster(partitionName, crashedMasterServerId);

            if (isMasterCrashed)
            {
                // the crashed server is the previous partition master
                Console.WriteLine(">>> The Crashed server was a master. MasterId=" + crashedMasterServerId);

                CrashUtils.RemoveCrashedServerFromMyLocalPartition(isMasterCrashed, partitionName, crashedMasterServerId);
                CrashUtils.NotifyAllReplicasAboutCrashedServer(partition, partitionName, crashedMasterServerId, isMasterCrashed);

                string newMasterId = ElectPartitionMaster(partitionName, crashedMasterServerId);

                return new NotifyCrashReply
                {
                    Status = "OK",
                    MasterId = newMasterId
                };
            }
            else
            {
                // the crashed server is a replica
                Console.WriteLine(">>> The Crashed server is a replica. The Master Remain the same. MasterId=" + crashedMasterServerId);
                CrashUtils.RemoveCrashedServerFromMyLocalPartition(isMasterCrashed, partitionName, crashedMasterServerId);
                CrashUtils.NotifyAllReplicasAboutCrashedServer(partition, partitionName, crashedMasterServerId, isMasterCrashed);

                Console.WriteLine(">>> Sending Back Current Master Id: MasterId=" + currentMasterServerId);
                Console.WriteLine("--------------------");
                return new NotifyCrashReply
                {
                    Status = "OK",
                    MasterId = currentMasterServerId
                };
            }
        }

    }
}
