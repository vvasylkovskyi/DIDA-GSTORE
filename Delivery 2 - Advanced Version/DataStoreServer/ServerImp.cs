using DataStoreServer.Domain;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataStoreServer
{
    public class ServerImp
    {
        private List<Partition> partitions = new List<Partition>();
        private string server_id;
        private int min_delay;
        private int max_delay;
        internal ThrPool tpool = new ThrPool(1, 10);
        private Dictionary<WriteRequest, WriteReply> writeResults = new Dictionary<WriteRequest, WriteReply>();
        private string url;
        private bool _isFrozen = false;

        public ServerImp(string server_id, string url, int min_delay, int max_delay)
        {
            this.server_id = server_id;
            this.min_delay = min_delay;
            this.max_delay = max_delay;
            this.url = url;
        }

        public void init_servers()
        {
            string portString = Utilities.getPortFromUrl(url);
            string hostName = Utilities.getHostNameFromUrl(url);
            int port;

            int.TryParse(portString, out port);
            Server server = new Server
            {
                Services = { ServerCommunicationService.BindService(new ServerCommunicationLogic(this)), DataStoreService.BindService(new DataStoreServiceImpl(this)) },
                Ports = { new ServerPort(hostName, port, ServerCredentials.Insecure) }
            };
            server.Start();
        }

        public void Freeze() {
            this._isFrozen = true;
            tpool.setFreeze(_isFrozen);
        }

        public void unFreeze()
        {
            this._isFrozen = false;
            tpool.setFreeze(_isFrozen);
        }

        public void sleepBeforeProcessingMessage(){
            int sleepTime = Utilities.RandomNumber(min_delay, max_delay);
            Console.WriteLine("sleeping "+ sleepTime + " before processing message");
            Thread.Sleep(sleepTime);
        }

        public Partition getPartition(string partition_id)
        {
            foreach (Partition p in partitions)
            {
                if (p.getName().Equals(partition_id))
                {
                    return p;
                }
            }
            return null;
        }

        public List<Partition> getPartitions()
        {
            return partitions;
        }

        public int getNumberOfPartitions()
        {
            return partitions.Count;
        }

        public void createPartition(string partition_id)
        {
            string master_id = PartitionMapping.GetPartitionMaster(partition_id);
            bool is_master = master_id.Equals(server_id);
            int partition_clock = PartitionMapping.GetPartitionClock(partition_id);

            Partition p = new Partition(partition_id, is_master, partition_clock);
            partitions.Add(p);
        }

        public string getID()
        {
            return this.server_id;
        }

        public WriteReply getWriteResult(WriteRequest request)
        {
            lock (writeResults)
            {
                while (!writeResults.ContainsKey(request))
                {
                    Monitor.Wait(writeResults);
                }
                WriteReply reply = writeResults[request];
                writeResults.Remove(request);
                return reply;
            }
        }

        public void setWriteResult(WriteRequest request, WriteReply reply)
        {
            lock (writeResults)
            {
                writeResults.Add(request, reply);
                Monitor.PulseAll(writeResults);
            }
        }

        public void setNewPartitionMaster(string partition_id, string new_master_id)
        {
            // removeing partition master means deleting the old one and assigning the new one as a master
            PartitionMapping.RemovePartitionMaster(partition_id);
            PartitionMapping.SetPartitionMaster(partition_id, new_master_id);
        }


        public void becomeLeader(string partitionId)
        {
            // send message to all servers in partition informing them of the new leader

            Partition partition = getPartition(partitionId);
            Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> partitionReplicas = partition.getReplicas();

            foreach (string replica_id in partitionReplicas.Keys)
            {
                try
                {
                    string my_id =getID();
                    if (replica_id.Equals(my_id))
                    {
                        setNewPartitionMaster(partitionId, my_id);
                    }
                    else
                    {
                        partitionReplicas[replica_id].SetNewPartitionMaster(new SetPartitionMasterRequest
                        {
                            PartitionId = partitionId,
                            NewMasterId = my_id
                        });
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine("Replica cannot be reached: " + replica_id);
                }
            }

        }


    }
}
