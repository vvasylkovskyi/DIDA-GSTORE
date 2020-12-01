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

        private Dictionary<string, GrpcChannel> replica_channels;
        private Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> replica_clients;

        public Partition(string id)
        {
            this.id = id;
            this.data = new DataStore();
            this.replica_channels = new Dictionary<string, GrpcChannel>();
            this.replica_clients = new Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient>();
            updateConnectionToReplicas(id);
        }

        public string getName()
        {
            return id;
        }

        private void updateConnectionToReplicas(string partition_id)
        {
            string[] replicas = PartitionMapping.getPartitionNodes(partition_id);

            foreach (string replica in replicas)
            {
                GrpcChannel channel;

                channel = replica_channels[replica];
                if (channel != null)
                    channel.ShutdownAsync();

                string url = ServerUrlMapping.GetServerUrl(replica);
                channel = GrpcChannel.ForAddress(url);
                replica_channels[replica] = channel;
                replica_clients[replica] = new ServerCommunicationService.ServerCommunicationServiceClient(channel);
            }
        }

        public Dictionary<string, ServerCommunicationService.ServerCommunicationServiceClient> getReplicas()
        {
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

        public bool dataExists(DataStoreKey key)
        {
            return data.objectExists(key);
        }

        public string getMasterID()
        {
            return PartitionMapping.getPartitionMaster(this.id);
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
