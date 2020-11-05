using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using DataStoreServer.Domain;

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

        public ReadReply ReadHandler(ReadRequest request)
        {

            return null;
        }
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(server.WriteHandler(request));
        }


        public override async Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            return await Task.FromResult(ListServerHandler(request));
        }

        // TODO needs implementation
        public ListServerReply ListServerHandler(ListServerRequest request)
        {
            ListServerReply reply = null;

            List<Partition> partitionList = new List<Partition>();

            reply = new ListServerReply
            {
                //PartitionList = { partitionList }
            };

            return reply;
        }
    }
}
