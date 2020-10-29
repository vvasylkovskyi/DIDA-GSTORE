using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

namespace DataStoreServer
{
    class Data
    {
        private Dictionary<DataStoreKeyDto, DataStoreValueDto> dataStore = new Dictionary<DataStoreKeyDto, DataStoreValueDto>();
        private List<DataStoreKeyDto> readQueue = new List<DataStoreKeyDto>();

        private DataStoreKeyDto getCorrectKey(DataStoreKeyDto key) {
            foreach (DataStoreKeyDto objectkey in dataStore.Keys)
            {
                if (key.ObjectId == objectkey.ObjectId && key.PartitionId == objectkey.PartitionId)
                {
                    return objectkey;
                }
            }
            return null;
        }
        public DataStoreValueDto getObject(DataStoreKeyDto key)  {
            DataStoreKeyDto keyCorrect = getCorrectKey(key);
            if (keyCorrect != null)
            {
                lock (this) {
                    readQueue.Add(key);
                    while (keyCorrect.Islocked || !readQueue[0].Equals(key)) {
                        Monitor.Wait(this);
                    }
                    DataStoreValueDto result = dataStore[keyCorrect];
                    readQueue.RemoveAt(0);
                    Monitor.PulseAll(this);
                    return result;
                }
            }
            else
                throw new Exception("Object does not exist");
        }

        public void CreateNewOrUpdateExisting(DataStoreKeyDto key, DataStoreValueDto value) {
            key = getCorrectKey(key);
            if (key != null)
                dataStore[key] = value;
            else {
                DataStoreKeyDto newKey = new DataStoreKeyDto
                {
                    PartitionId = key.PartitionId,
                    ObjectId = key.ObjectId,
                    Islocked = true
                };
                dataStore.Add(newKey, value);
            }

        }

        public bool objectExists(DataStoreKeyDto key ) {
            key = getCorrectKey(key);
            if(key != null)
                return true;
            return false;
        }


        public void setLockObject(DataStoreKeyDto key, bool objectLock) {
            foreach (DataStoreKeyDto objectkey in dataStore.Keys) {
                if (key.ObjectId == objectkey.ObjectId && key.PartitionId == objectkey.PartitionId) {
                    objectkey.Islocked = objectLock;
                    return;
                }
            }
        }
    }

    public class Partition{
        private int id;
        private Dictionary<int, DataStoreService.DataStoreServiceClient> replicas;
        private int master;
        private Data data = new Data();

        public Partition(int id, Dictionary<int, DataStoreService.DataStoreServiceClient> replicas, int master_id) {
            this.id = id;
            this.replicas = replicas;
            this.master = master_id;
        }

        public int getName() {
            return id;
        }

        public Dictionary<int, DataStoreService.DataStoreServiceClient> getReplicas() {
            return replicas;
        }

        public void addNewOrUpdateExisting(DataStoreKeyDto key, DataStoreValueDto value) {
            lock (this)
            {
                data.CreateNewOrUpdateExisting(key, value);
            }
        }


        public DataStoreValueDto getData(DataStoreKeyDto key)
        {
            return data.getObject(key);
        }

        public bool dataExists(DataStoreKeyDto key) {
            return data.objectExists(key);
        }

        public int getMasterID() {
            return master;
        }

        public void lockObject(DataStoreKeyDto key, bool locked) {
            lock (this)
            {
                data.setLockObject(key, locked);
            }
        }

       /* public override string ToString()
        {
            String resul = "";
            foreach (Replica replica in replicas) {
                resul += replica.getID() + " " + replica.getIP() + " " + replica.getPort()+ " ";
            }
            return resul;
        }*/

    }
  /*  public class Replica {
        private String server_ip;
        private int port;
        private int id;

        public Replica(String ip, int port, int id) {
            this.server_ip = ip;
            this.port = port;
            this.id = id;
        }

        public String getIP() {
            return server_ip;
        }

        public int getPort() {
            return this.port;
        }

        public int getID() {
            return this.id;
        }
    }*/


}
