using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using DataStoreServer.Domain;
using System;
using DataStoreServer.Util;

namespace DataStoreServer
{
    public class DataStoreServiceImpl : DataStoreService.DataStoreServiceBase
    {
        private ServerImp server;

        public DataStoreServiceImpl(ServerImp server) {
            this.server = server;
        }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReadHandler(request));
        }

        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(WriteHandler(request));
        }

        public override async Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            return await Task.FromResult(ListServerHandler(request));
        }

        public override async Task<NotifyCrashReply> NotifyCrash(NotifyCrashRequest request, ServerCallContext context)
        {
            return await Task.FromResult(NotifyCrashHandler(request));
        }

        public WriteReply WriteHandler(WriteRequest request)
        {
            SendValueToReplica svr = new SendValueToReplica(server, request);
            server.tpool.AssyncInvoke(new ThrWork(svr.doWork));
            WriteReply reply = server.getWriteResult(request);
            return reply;
        }

        public ReadReply ReadHandler(ReadRequest request)
        {
            Partition partition = server.getPartition(request.ObjectKey.PartitionId);
            ReadReply reply = null;
            try
            {
                DataStoreValue value = partition.getData(new DataStoreKey(request.ObjectKey.PartitionId, request.ObjectKey.ObjectId));
                reply = new ReadReply
                {
                    Object = new DataStoreValueDto { Val = value.val },
                    ObjectExists = true
                };
            }
            catch (Exception)
            {
                reply = new ReadReply
                {
                    Object = new DataStoreValueDto { Val = "NA" },
                    ObjectExists = false
                };
            }

            return reply;
        }

        public ListServerReply ListServerHandler(ListServerRequest request)
        {
            ListServerReply reply = null;
            List<DataStorePartitionDto> partitionList = new List<DataStorePartitionDto>();

            foreach (Partition p in server.getPartitions())
            {
                List<DataStoreObjectDto> objectList = new List<DataStoreObjectDto>();
                DataStore store = p.getDataStore();

                foreach (DataStoreKey key in store.getKeys())
                {
                    Shared.GrpcDataStore.DataStoreObjectDto dto_obj = new Shared.GrpcDataStore.DataStoreObjectDto
                    {
                        Key = Utilities.ConvertKeyDomainToDto(key),
                        Value = Utilities.ConvertValueDomainToDto(store.getObject(key))
                    };

                    objectList.Add(dto_obj);
                }

                Shared.GrpcDataStore.DataStorePartitionDto dto_part = new Shared.GrpcDataStore.DataStorePartitionDto
                {
                    PartitionId = p.getName(),
                    IsMaster = p.is_master,
                    ObjectList = { objectList }
                };

                partitionList.Add(dto_part);
            }

            reply = new ListServerReply
            {
                PartitionList = { partitionList }
            };

            return reply;
        }

        public NotifyCrashReply NotifyCrashHandler(NotifyCrashRequest request)
        {
            NotifyCrashReply reply;
            server.dealWithServerCrash(request.PartitionId, request.CrashedMasterServerId);

            reply = new NotifyCrashReply
            {
                Status = "OK"
            };
            return reply;
        }

    }
}
