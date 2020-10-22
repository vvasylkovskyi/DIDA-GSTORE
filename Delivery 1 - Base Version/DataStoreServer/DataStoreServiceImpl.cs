using System;
using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using Shared.Domain;
using System.Collections.Generic;
using System.Threading;

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
        private bool isLocked = false;

        public DataStoreServiceImpl(int server_id, int min_delay, int max_delay) {
            this.server_id = server_id;
            this.min_delay = min_delay;
            this.max_delay = max_delay;
            ThreadPool trhpool = new ThreadPool(1, this);  //only one for this fase
        }

        public Partition getPartition(int partition_id) {
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
            /*  ReadReply result;
              DataStoreKey key = Utilities.ConvertKeyDtoToDomain(request.ObjectKey);
              Partition suppost_partition = getPartition(key.partition_id);
              if (suppost_partition != null)
              {
                  bool value_exists = suppost_partition.dataExists(key);

                  if (value_exists)
                  {
                      result = new ReadReply
                      {
                          Object = Utilities.ConvertValueDomainToDto(suppost_partition.getData(key)),
                          ObjectExists = true
                      };
                  }
                  else
                  {
                      result = new ReadReply
                      {
                          Object = new DataStoreValueDto
                          {
                              Val = ""
                          },
                          ObjectExists = false
                      };
                  }
              }
              else {
                  result = new ReadReply
                  {
                      Object = new DataStoreValueDto
                      {
                          Val = ""
                      },
                      ObjectExists = false
                  };
              }

              return result;*/
            return null;
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
            }
            return writeResults[request];
        }

        public void setWriteResult(WriteRequest request, WriteReply reply) {
            lock (writeResults) {
                writeResults.Add(request, reply);
                Monitor.PulseAll(writeResults);
            }

        }


        public override Task<lockReply> LockServer(lockRequest request, ServerCallContext context)
        {
            lock (this) {
                while (this.isLocked) {
                    Monitor.Wait(this);
                }
            }
            return Task.FromResult(new lockReply());
        }


        public override Task<NewValueReplay> WriteNewValue(NewValueRequest request, ServerCallContext context) {
            Partition partion = getPartition(request.Value.ObjectKey.PartitionId);
            partion.addData(request.Value.ObjectKey, request.Value.Object);
            this.isLocked = false;
            Monitor.PulseAll(this);
            return Task.FromResult(new NewValueReplay
            {
                Ok = true
            }) ;
        }
    }
}