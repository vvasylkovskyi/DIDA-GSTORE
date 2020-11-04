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
            /*      Partition partition = getPartition(request.ObjectKey.PartitionId);
                  ReadReply reply = null;
                  try
                  {
                  //    DataStoreValueDto value = partition.getData(request.ObjectKey);
                      reply = new ReadReply
                      {
                          Object = value,
                          ObjectExists = true
                      };
                  }
                  catch (Exception e) {
                      reply = new ReadReply
                      {
                          ObjectExists = false
                      };
                  }

                  return reply;*/
            return null;
        }
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(server.WriteHandler(request));
        }

        








    }
}