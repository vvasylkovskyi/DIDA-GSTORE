using System;
using System.Collections.Generic;
using System.Text;

namespace DataStoreServer.Util
{
    class Utilities
    {
        // functions used to convert between DTO and domain classes

        // DataStore Key

        public static Domain.DataStoreKey ConvertKeyDtoToDomain(Shared.GrpcDataStore.DataStoreKeyDto dto_key)
        {
            Domain.DataStoreKey domain_key = new Domain.DataStoreKey(dto_key.PartitionId, dto_key.ObjectId);
            return domain_key;
        }

        public static Shared.GrpcDataStore.DataStoreKeyDto ConvertKeyDomainToDto(Domain.DataStoreKey domain_key)
        {
            Shared.GrpcDataStore.DataStoreKeyDto dto_key = new Shared.GrpcDataStore.DataStoreKeyDto
            {
                PartitionId = domain_key.partition_id,
                ObjectId = domain_key.object_id
            };
            return dto_key;
        }

        // DataStore Value

        public static Domain.DataStoreValue ConvertValueDtoToDomain(Shared.GrpcDataStore.DataStoreValueDto dto_value)
        {
            Domain.DataStoreValue domain_value = new Domain.DataStoreValue
            {
                val = dto_value.Val
            };
            return domain_value;
        }

        public static Shared.GrpcDataStore.DataStoreValueDto ConvertValueDomainToDto(Domain.DataStoreValue domain_value)
        {
            Shared.GrpcDataStore.DataStoreValueDto dto_value = new Shared.GrpcDataStore.DataStoreValueDto
            {
                Val = domain_value.val
            };
            return dto_value;
        }

    }
}
