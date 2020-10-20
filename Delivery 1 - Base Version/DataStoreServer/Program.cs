using Grpc.Core;
using Shared.Domain;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;

namespace DataStoreServer
{
    class Program
    {
        /*public static Replica getReplica() {
           Console.WriteLine("insert the replica ip:");
            String ip = Console.ReadLine();

            Console.WriteLine("insert the replica port:");
            int port = int.Parse(Console.ReadLine());

            Console.WriteLine("insert the replica ID:");
            int id = int.Parse(Console.ReadLine());
            Replica replica = new Replica(ip, port, id);
            return replica;
        }*/

      /*  public static List<Replica> setupReplicas(int numberReplicas) {
            List<Replica> replicas = new List<Replica>();
            Console.WriteLine("creating replicas");
            for (int i =1; i<= numberReplicas; i++) {
                Console.WriteLine("Replica "+i);
                Replica replica = getReplica();
                replicas.Add(replica);
            }
            return replicas;
        }*/
        static void Main(string[] args)
        {

            /*    Console.WriteLine("Hello! I'm the server");
                List<Partition> partitions = new List<Partition>();

                Console.WriteLine("insert number of partitions for this server");
                int numPartitions = int.Parse(Console.ReadLine());

                Console.WriteLine("now lets configure server partitions");
                for(int i =1; i<= numPartitions; i++)
                {
                    Console.WriteLine("Partition " + i);
                    Console.WriteLine("insert Partition ID:");
                    int id = int.Parse(Console.ReadLine());

                    Console.WriteLine("setup replicas... please inser number of replicas for this partition");

                    int numReplicas = int.Parse(Console.ReadLine());
                    List<Replica> replicas = setupReplicas(numReplicas);

                    Console.WriteLine("setup master replicas:");
                    Replica master = getReplica();

                    Partition partition = new Partition(id,replicas, master);

                    partitions.Add(partition);
                }

                Console.WriteLine("Last configuration, please set server id");
                int server_id = int.Parse(Console.ReadLine());

                Console.WriteLine("server port");
                int server_port = int.Parse(Console.ReadLine());

                foreach (Partition p in partitions) {
                    Console.WriteLine("Partition id: "+p.getID()+ " partition replicas "+p.ToString());
                }*/
            int server_id = int.Parse(args[1]);
            String url = args[2];
            int min_delay = int.Parse(args[3]);
            int max_delay = int.Parse(args[4]);
            String host_name = url.Split(":")[0];
            int port = int.Parse(url.Split(":")[1]);

            Server server = new Server
            {
                Services = { DataStoreService.BindService(new DataStoreServiceImpl(server_id, min_delay,max_delay)) },
                Ports = { new ServerPort(host_name, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("I'm ready to work");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
