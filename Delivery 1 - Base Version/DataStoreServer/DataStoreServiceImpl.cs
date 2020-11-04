using System;
using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using Shared.Domain;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel.DataAnnotations;

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

        








    }
}