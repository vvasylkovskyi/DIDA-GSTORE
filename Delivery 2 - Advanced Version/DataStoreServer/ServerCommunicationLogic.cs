using DataStoreServer.Domain;
using Grpc.Core;
using Shared.GrpcDataStore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataStoreServer
{
    public class ServerCommunicationLogic:ServerCommunicationService.ServerCommunicationServiceBase
    {
        private ServerImp server;
        private DataStoreKey current_key;
        private readonly string atomic_lock = "ATOMIC_LOCK";
        private static readonly object _syncRoot = new object();

        public ServerCommunicationLogic(ServerImp server)
        {
            this.server = server;
        }

        public void AtomicWriteAndUpdateClock(NewValueRequest request, Partition partition, DataStoreValue value)
        {
            try
            {
                // the server shouldn't respond to client queries during this atomic operation
                Monitor.Enter(atomic_lock);
                Console.WriteLine(">>> Replica: Atomic operation Update<Value, Clock> = <" + value.val + "," + request.Clock + ">");
                partition.addNewOrUpdateExisting(current_key, value);
                partition.setClock(request.Clock);
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

        public override Task<NewValueReply> WriteNewValue(NewValueRequest request, ServerCallContext context)
        {
            Partition partition = server.getPartition(current_key.partition_id);
            DataStoreValue value = new DataStoreValue();
            value.val = request.Val;
            partition.addNewOrUpdateExisting(current_key, value);
            partition.lockObject(current_key, false);
            return Task.FromResult(new NewValueReply
            {
                Ok = true
            });
        }

        public override Task<IsAliveReply> IsAlive(IsAliveRequest request, ServerCallContext context)
        {
            return Task.FromResult(IsAliveHandler());
        }

        public override Task<NotifyReplicaAboutCrashReply> NotifyReplicaAboutCrash(NotifyReplicaAboutCrashRequest request, ServerCallContext context)
        {
            return Task.FromResult(NotifyReplicaAboutCrashHandler(request));
        }

        public override Task<ClockReply> GetPartitionClock(ClockRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetPartitionClockHandler(request));
        }

        public override Task<GrantPermissionReply> GrantPermissionToBecomeLeader(GrantPermissionRequest request, ServerCallContext context)
        {
            return Task.FromResult(GrantPermissionToBecomeLeaderHandler(request));
        }

        public override Task<SetPartitionMasterReply> SetNewPartitionMaster(SetPartitionMasterRequest request, ServerCallContext context)
        {
            return Task.FromResult(SetNewPartitionMasterHandler(request));
        }

        // ---------- Handler --------------

        public IsAliveReply IsAliveHandler()
        {
            return new IsAliveReply { Ok = true };
        }

        public NotifyReplicaAboutCrashReply NotifyReplicaAboutCrashHandler(NotifyReplicaAboutCrashRequest request)
        {
            Console.WriteLine(">>> Received Notification about crashed server...");
            Console.WriteLine(">>> PartitionName=" + request.PartitionId + ", Crashed Server=" + request.CrashedMasterServerId);
            CrashUtils.RemoveCrashedServerFromMyLocalPartition(request.IsMasterCrashed, request.PartitionId, request.CrashedMasterServerId);
            return new NotifyReplicaAboutCrashReply { Ok = true };
        }

        public ClockReply GetPartitionClockHandler(ClockRequest request)
        {
            string part = request.PartitionId;
            int clock = server.getPartition(part).getClock();
            return new ClockReply { Clock = clock };
        }

        public GrantPermissionReply GrantPermissionToBecomeLeaderHandler(GrantPermissionRequest request)
        {
            server.becomeLeader(request.PartitionId);
            return new GrantPermissionReply { Status = "OK" };
        }

        public SetPartitionMasterReply SetNewPartitionMasterHandler(SetPartitionMasterRequest request)
        {
            server.setNewPartitionMaster(request.PartitionId, request.NewMasterId);
            return new SetPartitionMasterReply { Status = "OK" };
        }

    }
}
