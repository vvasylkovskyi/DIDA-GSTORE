using System;
using System.Collections.Generic;
using System.Text;

namespace DataStoreServer.Domain
{
    public class Partition
    {
        private string partition_id;
        private List<string> replica_id_list;
        private string master_replica_id;
        private DataStore dataStore = new DataStore();
        private List<DataStoreKey> readQueue = new List<DataStoreKey>();

        public Partition(string partition_id, List<string> replica_id_list, string master_id)
        {
            this.partition_id = partition_id;
            this.replica_id_list = replica_id_list;
            this.master_replica_id = master_id;
        }

        public string getName()
        {
            return partition_id;
        }

        public List<string> getReplicas()
        {
            return replica_id_list;
        }

        public void addNewOrUpdateExisting(DataStoreKey key, DataStoreValue value)
        {
            lock (this)
            {
                dataStore.CreateNewOrUpdateExisting(key, value);
            }
        }


        public DataStoreValue getData(DataStoreKey key)
        {
            return dataStore.getObject(key);
        }

        public bool dataExists(DataStoreKey key)
        {
            return dataStore.objectExists(key);
        }

        public string getMasterID()
        {
            return master_replica_id;
        }

        public void lockObject(DataStoreKey key, bool locked)
        {
            lock (this)
            {
                dataStore.setLockObject(key, locked);
            }
        }
    }
}
