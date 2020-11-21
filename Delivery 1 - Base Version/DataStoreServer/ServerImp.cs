﻿using DataStoreServer.Domain;
using Grpc.Core;
using Shared.GrpcDataStore;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataStoreServer
{
    public class ServerImp
    {
        private List<Partition> partitions = new List<Partition>();
        private string server_id;
        private int min_delay;
        private int max_delay;
        private ThrPool tpool = new ThrPool(1, 10);
        private Dictionary<WriteRequest, WriteReply> writeResults = new Dictionary<WriteRequest, WriteReply>();
        private string url;

        public ServerImp(string server_id, string url, int min_delay, int max_delay)
        {
            this.server_id = server_id;
            this.min_delay = min_delay;
            this.max_delay = max_delay;
            this.url = url;

        }

        public void init_servers()
        {
            string portString = Utilities.getPortFromUrl(url);
            string hostName = Utilities.getHostNameFromUrl(url);
            int port;

            int.TryParse(portString, out port);
            init_datastoreservice(hostName, port);
            init_servercommunicationservice(hostName, port);
        }

        private void init_servercommunicationservice(string host_name, int port)
        {
            
            Server server = new Server
            {
                Services = { ServerCommunicationService.BindService(new ServerCommunicationLogic(this)) },
                Ports = { new ServerPort(host_name, port, ServerCredentials.Insecure) }
            };
            server.Start();
        }

        private void init_datastoreservice(string host_name, int port) {
            Server server = new Server
            {
                Services = { DataStoreService.BindService(new DataStoreServiceImpl(this)) },
                Ports = { new ServerPort(host_name, port, ServerCredentials.Insecure) }
            };
            server.Start();
        }

        public Partition getPartition(string partition_id)
        {
            foreach (Partition p in partitions)
            {
                if (p.getName().Equals(partition_id))
                {
                    return p;
                }
            }
            return null;
        }

        public string getID()
        {
            return server_id;
        }

       


        public WriteReply getWriteResult(WriteRequest request)
        {
            lock (writeResults)
            {
                while (!writeResults.ContainsKey(request))
                {
                    Monitor.Wait(writeResults);
                }
                WriteReply reply = writeResults[request];
                writeResults.Remove(request);
                return reply;
            }
        }

        public void setWriteResult(WriteRequest request, WriteReply reply)
        {
            lock (writeResults)
            {
                writeResults.Add(request, reply);
                Monitor.PulseAll(writeResults);
            }
        }

        public WriteReply WriteHandler(WriteRequest request)
        {
            SendValueToReplica svr = new SendValueToReplica(this, request);
            tpool.AssyncInvoke(new ThrWork(svr.doWork));
            WriteReply reply = getWriteResult(request);
            return reply;
        }

        public ReadReply ReadHandler(ReadRequest request) { 
                  Partition partition = getPartition(request.ObjectKey.PartitionId);
                  ReadReply reply = null;
                  try
                  {
                      DataStoreValue value = partition.getData(new DataStoreKey(request.ObjectKey.PartitionId, request.ObjectKey.ObjectId));
                      reply = new ReadReply
                      {
                          Object = new DataStoreValueDto { Val = value.val},
                          ObjectExists = true
                      };
                  }
                  catch (Exception) {
                      reply = new ReadReply
                      {
                          Object = new DataStoreValueDto { Val = "NA" },
                          ObjectExists = false
                      };
                  }

                  return reply;
        }
    }
}