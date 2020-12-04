﻿using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using System.Collections.Generic;
using DataStoreServer.Domain;
using System;
using DataStoreServer;
using Shared.Util;
using System.Linq;

namespace DataStoreServer
{
    public class DataStoreServiceImpl : DataStoreService.DataStoreServiceBase
    {
        private ServerImp server;
        private static readonly object _syncRoot = new object();

        public DataStoreServiceImpl(ServerImp server) {
            this.server = server;
        }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {   server.sleepBeforeProcessingMessage();
            return Task.FromResult(ReadHandler(request));
        }

        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            server.sleepBeforeProcessingMessage();
            return Task.FromResult(WriteHandler(request));
        }

        public override async Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {   
            server.sleepBeforeProcessingMessage();
            return await Task.FromResult(ListServerHandler(request));
        }

        public override async Task<NotifyCrashReply> NotifyCrash(NotifyCrashRequest request, ServerCallContext context)
        {
            server.sleepBeforeProcessingMessage();
            return await Task.FromResult(NotifyCrashHandler(request));
        }

        // ---------- Handlers

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
            int partitionClock = partition.getClock();

            Console.WriteLine(">>> PartitionName=" + request.ObjectKey.PartitionId + ", PartitionClock=" + partitionClock);
            ReadReply reply;
            try
            {
                DataStoreValue value = partition.getData(new DataStoreKey(request.ObjectKey.PartitionId, request.ObjectKey.ObjectId));
                reply = new ReadReply
                {
                    Object = new DataStoreValueDto { Val = value.val },
                    ObjectExists = true,
                    PartitionClock = partitionClock
                };
            }
            catch (Exception)
            {
                reply = new ReadReply
                {
                    Object = new DataStoreValueDto { Val = "NA" },
                    ObjectExists = false,
                    PartitionClock = partitionClock
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
                        Key = DataStoreServer.Util.Utilities.ConvertKeyDomainToDto(key),
                        Value = DataStoreServer.Util.Utilities.ConvertValueDomainToDto(store.getObject(key))
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
            lock (_syncRoot)
            {
                string partitionName = request.PartitionId;
                string crashedMasterServerId = request.CrashedMasterServerId;
                string currentMasterServerId = PartitionMapping.GetPartitionMaster(partitionName);
                Console.WriteLine(">>> Received Message from the client about crashed server: PartitionName=" + partitionName + ", CrashedServerId=" + crashedMasterServerId);

                // check if the crashed server has been dealt with already
                string[] serversOfThePartition = PartitionMapping.partitionMapping[partitionName];
                if (!serversOfThePartition.Contains(crashedMasterServerId))
                {
                    // this means an election already happened and the crashed server was deleted. the current partition master is the election result
                    // another possibility is that the master detected a replica failure. In this case the partition master didn't actually change saying that it did no harm.
                    Console.WriteLine(">>> Election Process Has Already Happened, CurrentMasterServerId=" + currentMasterServerId);
                    return new NotifyCrashReply
                    {
                        Status = "OK",
                        MasterId = currentMasterServerId
                    };
                }

                // confirm that server is actually crashed
                SendValueToReplica svr = new SendValueToReplica(server);

                return svr.HandleServerCrash(partitionName, crashedMasterServerId);
            }
        }
    }
}
