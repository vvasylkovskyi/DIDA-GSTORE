using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Domain
{
    public class Partition
    {
        private String id;
        private Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> replicas;
        private int master;
        private Data data = new Data();

        public Partition(String id, Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> replicas, int master_id)
        {
            this.id = id;
            this.replicas = replicas;
            this.master = master_id;
        }

        public String getName()
        {
            return id;
        }

        public Dictionary<int, ServerCommunicationService.ServerCommunicationServiceClient> getReplicas()
        {
            return replicas;
        }

          public void addNewOrUpdateExisting(DataStoreKey key, DataStoreValue value)
          {
              lock (this)
              {
                data.CreateNewOrUpdateExisting(key, value);
              }
          }


          public DataStoreValue getData(DataStoreKey key)
          {
              return data.getObject(key);
          }

          public bool dataExists(DataStoreKey key)
          {
              return data.objectExists(key);
          }

          public int getMasterID()
          {
              return master;
          }

          public void lockObject(DataStoreKey key, bool locked)
          {
              lock (this)
              {
                data.SetLockObject(key, locked);
              }
          }
    }
}
