using System;
using System.Threading.Tasks;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using Shared.Domain;


namespace DataStoreServer
{
    class DataStoreServiceImpl : DataStoreService.DataStoreServiceBase
    {
        private Data database;
        public DataStoreServiceImpl(Data database)
        {
            this.database = database;
        }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReadHandler(request));
        }

        public ReadReply ReadHandler(ReadRequest request)
        {
            ReadReply result;
            DataStoreKey key = Utilities.ConvertKeyDtoToDomain(request.ObjectKey);
            bool value_exists = database.dataStore.ContainsKey(key);

            if (value_exists)
            {
                result = new ReadReply
                {
                    Object = Utilities.ConvertValueDomainToDto(database.dataStore[key]),
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

            return result;
        }

        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(WriteHandler(request));
        }

        public WriteReply WriteHandler(WriteRequest request)
        {
            lock (database)
            {
                DataStoreKey key = Utilities.ConvertKeyDtoToDomain(request.ObjectKey);
                DataStoreValue val = Utilities.ConvertValueDtoToDomain(request.Object);
                database.dataStore.Add(key, val);
            }
            return new WriteReply
            {
                WriteStatus = 200
            };
        }

    }
}