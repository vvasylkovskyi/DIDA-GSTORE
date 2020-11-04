using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Collections;
using DataStoreServer.Domain;
using DataStoreServer.Util;

namespace DataStoreServer
{
    public class DataStoreServiceImpl : DataStoreService.DataStoreServiceBase
    {
        //private Data database;
        private List<Partition> partitions = new List<Partition>();
        private int server_id;
        private int min_delay;
        private int max_delay;
        private ThreadPool trhpool;
        private Dictionary<WriteRequest, WriteReply> writeResults = new Dictionary<WriteRequest, WriteReply>();

        public DataStoreServiceImpl(int server_id, int min_delay, int max_delay) {
            this.server_id = server_id;
            this.min_delay = min_delay;
            this.max_delay = max_delay;
            trhpool = new ThreadPool(1, this);  //only one for this fase
        }

        public Partition getPartition(string partition_id) {
            foreach (Partition p in partitions) {
                if (p.getName() == partition_id) {
                    return p;
                }
            }
            return null;
        }

        public int getID() {
            return server_id;
        }



        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReadHandler(request));
        }

        public ReadReply ReadHandler(ReadRequest request)
        {
            Partition partition = getPartition(request.ObjectKey.PartitionId);
            ReadReply reply = null;
            try
            {
                DataStoreValue value = partition.getData(Utilities.ConvertKeyDtoToDomain(request.ObjectKey));
                reply = new ReadReply
                {
                    Object = value,
                    ObjectExists = true
                };
            }
            catch (Exception) {
                reply = new ReadReply
                {
                    ObjectExists = false
                };
            }

            return reply;
        }
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(WriteHandler(request));
        }

        public WriteReply WriteHandler(WriteRequest request)
        {
            trhpool.submit(request);
            WriteReply reply = getWriteResult(request);
            return reply;
        }


        public WriteReply getWriteResult(WriteRequest request) {
            lock (writeResults) {
                while (!writeResults.ContainsKey(request)) {
                    Monitor.Wait(writeResults);
                }
                WriteReply reply = writeResults[request];
                writeResults.Remove(request);
                return reply;
            }
        }

        public void setWriteResult(WriteRequest request, WriteReply reply) {
            lock (writeResults) {
                writeResults.Add(request, reply);
                Monitor.PulseAll(writeResults);
            }

        }


        public override Task<lockReply> LockObject(lockRequest request, ServerCallContext context)
        {
            lock (this) {
                Partition p = getPartition(request.ObjectKey.PartitionId);
                p.lockObject(request.ObjectKey, true);
            }
            return Task.FromResult(new lockReply());
        }


        public override Task<NewValueReplay> WriteNewValue(NewValueRequest request, ServerCallContext context) {
            Partition partion = getPartition(request.Value.ObjectKey.PartitionId);
            partion.addNewOrUpdateExisting(request.Value.ObjectKey, request.Value.Object);
            partion.lockObject(request.Value.ObjectKey, false);
            return Task.FromResult(new NewValueReplay
            {
                Ok = true
            }) ;
        }

        public override async Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            return await Task.FromResult(ListServerHandler(request));
        }

        // TODO needs implementation
        public ListServerReply ListServerHandler(ListServerRequest request)
        {
            ListServerReply reply = null;

            List<DataStorePartition> partitionList = new List<DataStorePartition>();

            reply = new ListServerReply
            {
                PartitionList = { partitionList }
            };

            return reply;
        }
    }
}
