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
                List<Replica> PartitionReplicas = partition.getReplicas();
                partition.addData(task.ObjectKey, task.Object);
                WriteReply replay = sendMessageToAllReplicas(task);
                server.setWriteResult(task, replay);
            }
        }

        public WriteReply sendMessageToAllReplicas(WriteRequest request) {
            //send message to block object
            //send request to write
            return null;
        }

        public void start()
        {
            Thread oThread = new Thread(new ThreadStart(run));
            oThread.Start();
        }
    }
}
