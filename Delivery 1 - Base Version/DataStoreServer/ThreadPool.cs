using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DataStoreServer
{
   public class ThreadPool
    {
        
        private static int MAX = 10;
        private WriteRequest[] buffer = new WriteRequest[MAX];
        private int busy = 0, insIndex = 0, remIndex = 0;
        private int num_of_workers;
        DataStoreServiceImpl server;

        public ThreadPool(int n, DataStoreServiceImpl server)
        {
            this.num_of_workers = n;
            this.server = server;
            init();
        }

        public void submit(WriteRequest job)
        {
            lock (this)
            {
                while (busy == MAX)
                {
                    Monitor.Wait(this);
                }
                buffer[insIndex] = job;
                insIndex = ++insIndex % MAX;
                busy++;
                Monitor.Pulse(this);
            }
        }

        public WriteRequest remove()
        {
            lock (this)
            {
                WriteRequest res;
                while (busy == 0)
                {
                    Monitor.Wait(this);
                }
                res = buffer[remIndex];
                remIndex = ++remIndex % MAX;
                busy--;
                Monitor.Pulse(this);
                return res;
            }
        }

        public void init()
        {
            for (int i = 0; i < this.num_of_workers; i++)
            {
                
                    Worker w = new Worker(this, server);
                    w.start();
            }
        }
    }

    public class Worker {
        private ThreadPool thrpool;
        private DataStoreServiceImpl server;
        public Worker(ThreadPool trhpool, DataStoreServiceImpl server)
        {
            this.thrpool = trhpool;
            this.server = server;
        }

        public void run()
        {
            while (true)
            {
                WriteRequest task = thrpool.remove();
                Partition partition = server.getPartition(task.ObjectKey.PartitionId);
                Dictionary<int, DataStoreService.DataStoreServiceClient> PartitionReplicas = partition.getReplicas();
                WriteReply replay = sendMessageToAllReplicas(task, partition);
                server.setWriteResult(task, replay);
            }
        }

        public WriteReply sendMessageToAllReplicas(WriteRequest request, Partition partition) {
            Dictionary<int, DataStoreService.DataStoreServiceClient> replicas = partition.getReplicas();
            //foreach (int replica_id in replicas.Keys)
            // {
            /*AppContext.SetSwitch(
                 "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
             GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:9080");
             DataStoreService.DataStoreServiceClient client = new DataStoreService.DataStoreServiceClient(channel);*/

            //}
            //send request to write
            lockReplicas(replicas, request.ObjectKey);
            WriteReply reply = write_new_value(replicas, partition.getMasterID(), request);
            return reply;
        }

        public void lockReplicas(Dictionary<int, DataStoreService.DataStoreServiceClient> replicas, DataStoreKeyDto key) {     
            foreach (int replica_id in replicas.Keys){
                    try{
                            replicas[replica_id].LockObject(new lockRequest {
                                ObjectKey = key
                            });
                    }
                    catch (Exception e){
                        Console.WriteLine(e.Message);
                        replicas.Remove(replica_id);
                    }

            }
        }

        public WriteReply write_new_value(Dictionary<int, DataStoreService.DataStoreServiceClient> replicas, int master_id, WriteRequest request) {
            foreach (int replica_id in replicas.Keys)
            {
                    try
                    {
                         replicas[replica_id].WriteNewValue(new NewValueRequest
                         {
                              Value = request
                         });  
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        replicas.Remove(replica_id);
                    }
            }
            return new WriteReply { WriteStatus = 200};
        }

        public void start()
        {
            Thread oThread = new Thread(new ThreadStart(run));
            oThread.Start();
        }
    
    }
}
