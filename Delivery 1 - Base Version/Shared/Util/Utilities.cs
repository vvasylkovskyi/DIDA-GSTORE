using System.Text;

namespace Shared.Util
{
    public static class Utilities
    {
        public static string BuildArgumentsString(params string[] args)
        {
            StringBuilder strinbuilder = new StringBuilder();
            foreach (string argument in args)
            {
                strinbuilder.Append(argument);
                strinbuilder.Append(' ');
            }
            return strinbuilder.ToString();
        }

        public static Domain.DataStoreKey ConvertKeyDtoToDomain(GrpcDataStore.DataStoreKeyDto dto_key)
        {
            Domain.DataStoreKey domain_key = new Domain.DataStoreKey
            {
                partition_id = dto_key.PartitionId,
                object_id = dto_key.ObjectId
            };
            return domain_key;
        }

        public static GrpcDataStore.DataStoreKeyDto ConvertKeyDomainToDto(Domain.DataStoreKey domain_key)
        {
            GrpcDataStore.DataStoreKeyDto dto_key = new GrpcDataStore.DataStoreKeyDto
            {
                PartitionId = domain_key.partition_id,
                ObjectId = domain_key.object_id
            };
            return dto_key;
        }

        public static Domain.DataStoreValue ConvertValueDtoToDomain(GrpcDataStore.DataStoreValueDto dto_value)
        {
            Domain.DataStoreValue domain_value = new Domain.DataStoreValue
            {
                val = dto_value.StringVal
            };
            return domain_value;
        }

        public static GrpcDataStore.DataStoreValueDto ConvertValueDomainToDto(Domain.DataStoreValue domain_value)
        {
            GrpcDataStore.DataStoreValueDto dto_value = new GrpcDataStore.DataStoreValueDto
            {
                StringVal = domain_value.val
            };
            return dto_value;
        }
    }
}
