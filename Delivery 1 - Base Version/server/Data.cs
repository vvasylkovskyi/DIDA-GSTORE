using Shared.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataStoreServer
{
    class Data
    {
        public Dictionary<DataStoreKey, DataStoreValue> dataStore = new Dictionary<DataStoreKey, DataStoreValue>();
    }
}
