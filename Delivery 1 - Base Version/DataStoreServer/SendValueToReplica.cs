using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataStoreServer
{
    public class SendValueToReplica
    {
        private ServerImp server;
        private WriteRequest request;

        public SendValueToReplica(ServerImp server, WriteRequest request) {
            this.server = server;
            this.request = request;
        }

        public void doWork() {
            Partition partition = server.getPartition(request.ObjectKey.PartitionId);
            Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> PartitionReplicas = partition.getReplicas();
            lockReplicas(PartitionReplicas, this.request.ObjectKey);
            WriteReply reply = write_new_value(PartitionReplicas, request);
            server.setWriteResult(request,reply);
        }

        public void lockReplicas(Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> replicas, DataStoreKeyDto key)
        {
            foreach (int replica_id in replicas.Keys)
            {
                try
                {
                    replicas[replica_id].LockObject(new lockRequest
                    {
                       PartitionId = key.PartitionId,
                       ObjectId = key.ObjectId
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    replicas.Remove(replica_id);
                }

            }
        }

        public WriteReply write_new_value(Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> replicas, WriteRequest request)
        {
            foreach (int replica_id in replicas.Keys)
            {
                try
                {
                    replicas[replica_id].WriteNewValue(new NewValueRequest
                    {
                        Val = request.Object.Val
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    replicas.Remove(replica_id);
                }
            }
            return new WriteReply { WriteStatus = 200 };
        }

    }
}
