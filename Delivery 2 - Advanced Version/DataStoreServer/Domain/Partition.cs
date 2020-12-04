using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using Shared.Util;
using Grpc.Net.Client;

namespace DataStoreServer.Domain
{
    public class Partition
    {
        private string id;
        private DataStore data;
        private bool connected_to_replicas = false;
        private Dictionary<string, GrpcChannel> replica_channels;
        private Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> replica_clients;
        private int clock;

        public bool is_master;


        public Partition(string id, bool is_master, int partition_clock)
        {
            this.id = id;
            this.is_master = is_master;
            this.data = new DataStore();
            this.replica_channels = new Dictionary<string, GrpcChannel>();
            this.replica_clients = new Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient>();
            this.clock = partition_clock;
        }

        public string getName()
        {
            return id;
        }

        private void updateConnectionToReplicas(string partition_id)
        {
            string[] replicas = PartitionMapping.GetPartitionReplicas(partition_id);

            foreach (string replica in replicas)
            {
                GrpcChannel channel;

                if (replica_channels.TryGetValue(replica, out channel))
                {
                    channel.ShutdownAsync();
                }

                string url = ServerUrlMapping.GetServerUrl(replica);
                channel = GrpcChannel.ForAddress(url);
                replica_channels[replica] = channel;
                replica_clients[replica] = new ServerCommunicationService.ServerCommunicationServiceClient(channel);
            }
        }

        public Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> getReplicas()
        {
            if (!connected_to_replicas)
            {
                updateConnectionToReplicas(id);
                connected_to_replicas = true;
            }

            return replica_clients;
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

        public int getClock()
        {
            return clock;
        }

        public int incrementClock()
        {
            return clock++;
        }

        public void setClock(int clock)
        {
            this.clock = clock;
        }

        public DataStore getDataStore()
        {
            return data;
        }

        public bool dataExists(DataStoreKey key)
        {
            return data.objectExists(key);
        }

        public string getMasterID()
        {
            return PartitionMapping.GetPartitionMaster(this.id);
        }

        public ServerCommunicationService.ServerCommunicationServiceClient getReplicaById(string serverId)
        {
            return replica_clients[serverId];
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
