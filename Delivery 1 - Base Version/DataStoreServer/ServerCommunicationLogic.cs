using DataStoreServer.Domain;
using Grpc.Core;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataStoreServer
{
    public class ServerCommunicationLogic:ServerCommunicationService.ServerCommunicationServiceBase
    {
        private ServerImp server;
        private DataStoreKey current_key;

        public ServerCommunicationLogic(ServerImp server)
        {
            this.server = server;
        }
        public override Task<lockReply> LockObject(lockRequest request, ServerCallContext context)
        {
            lock (this)
            {
                this.current_key = new DataStoreKey(request.PartitionId, request.ObjectId);
                Partition p = server.getPartition(request.PartitionId);
                p.lockObject(current_key, true);
            }
            return Task.FromResult(new lockReply());
        }

        public override Task<NewValueReplay> WriteNewValue(NewValueRequest request, ServerCallContext context)
        {
            Partition partition = server.getPartition(current_key.partition_id);
            DataStoreValue value = new DataStoreValue();
            value.val = request.Val;
            partition.addNewOrUpdateExisting(current_key, value);
            partition.lockObject(current_key, false);
            return Task.FromResult(new NewValueReplay
            {
                Ok = true
            });
        }
    }
}
