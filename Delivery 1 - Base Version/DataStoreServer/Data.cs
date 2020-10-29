using Shared.Domain;
using System.Collections.Generic;

namespace DataStoreServer
{
    class Data
    {
        public Dictionary<DataStoreKey, DataStoreValue> dataStore = new Dictionary<DataStoreKey, DataStoreValue>();
    }
}
