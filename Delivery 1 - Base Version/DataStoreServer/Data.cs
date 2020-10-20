using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace DataStoreServer
{
    class Data
    {
        private Dictionary<DataStoreKeyDto, DataStoreValueDto> dataStore = new Dictionary<DataStoreKeyDto, DataStoreValueDto>();
        
        public DataStoreValueDto getObject(DataStoreKeyDto key) {
            return dataStore[key];
        }

        public void setData(DataStoreKeyDto key, DataStoreValueDto value) {
            dataStore.Add(key, value);
        }

        public bool objectExists(DataStoreKeyDto key ) {
            return dataStore.ContainsKey(key);
        }

    }

    public class Partition{
        private int id;
        private List<Replica> replicas;
        private Replica master;
        private Data data = new Data();

        public Partition(int id, List<Replica> replicas, Replica master) {
            this.id = id;
            this.replicas = replicas;
            this.master = master;
        }

        public int getName() {
            return id;
        }

        public List<Replica> getReplicas() {
            return replicas;
        }

        public void addData(DataStoreKeyDto key, DataStoreValueDto value) {
            lock (this)
            {
                data.setData(key, value);
            }
        }

        public DataStoreValueDto getData(DataStoreKeyDto key)
        {
            return data.getObject(key);
        }

        public bool dataExists(DataStoreKeyDto key) {
            return data.objectExists(key);
        }

        public override string ToString()
        {
            String resul = "";
            foreach (Replica replica in replicas) {
                resul += replica.getID() + " " + replica.getIP() + " " + replica.getPort()+ " ";
            }
            return resul;
        }

    }
    public class Replica {
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
    }


}
