using System;
using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Client;
using PuppetMaster.Protos;
using Shared.Util;

namespace PCS
{
    public class ProcessCreationService
    {
        private DataStoreClient.Program client;
        private DataStoreServer.Program server;
        private string pcsRole;


        public static void Main(string[] args)
        {
            new ProcessCreationService();
        }

        public ProcessCreationService()
        {
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            int port = Utilities.RandomNumber(10000, 11000);
            Console.WriteLine(">>> Starting PCS on port: " + port);

            try {
                InitPCSServer(port);
                NotifyPuppetMaster(port.ToString());
            } catch
            {
                Console.WriteLine(">>> Error: Something went wrong");
            }

            Console.WriteLine(">>> Press 'q' to exit");
            while (true)
            {
                if (client == null)
                    waitingLoop();
                else
                    client.readCommandsLoop();
            }
        }

        public void waitingLoop()
        {
            if (Console.ReadLine().Equals("q"))
            {
                Environment.Exit(1);
            }
        }

        public void InitPCSServer(int port)
        {
            Server server = new Server
            {
                Services = { PCSServices.BindService(new PCSImpl(this)) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine(">>> PCS Server started");   
        }

        public static void NotifyPuppetMaster(string port)
        {
            Console.WriteLine(">>> Initializing Grpc Connection to puppet master on port: " + Utilities.puppetMasterPort);
            try
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + Utilities.puppetMasterPort);
                PuppetMasterServices.PuppetMasterServicesClient client = new PuppetMasterServices.PuppetMasterServicesClient(channel);
                Console.WriteLine("Invoking Notify Puppet Master...");
                NotifyPuppetMasterReply notifyPuppetMasterReply = client.NotifyPuppetMaster(new NotifyPuppetMasterRequest { Port = port });
                Console.WriteLine(notifyPuppetMasterReply.Port);
            }
            catch (UriFormatException)
            {
                Console.WriteLine(">>> Exception. URI format is incorrect");
            }
            catch
            {
                Console.WriteLine(">>> Error. Something went wrong");
            }
        }

        public void StartServer(string[] args)
        {
            Console.WriteLine(">>> Starting Server...");
            DataStoreServer.Program server = new DataStoreServer.Program().StartServer(args);
            this.server = server;
            pcsRole = "server";

            // When creating a new Server Program it has no context
            // These two functions will update the new server context with the partitions and server that PCS knows
            this.server.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            this.server.UpdatePartitionsContext(PartitionMapping.partitionToReplicationFactorMapping, PartitionMapping.partitionMapping);

            // after updating partition information, create local partitions to store data
            this.server.CreateLocalPartitions();
        }

        public void StartClient(string[] args)
        {
            Console.WriteLine(">>> Starting Client...");
            DataStoreClient.Program client = DataStoreClient.Program.StartClientWithPCS(args);
            this.client = client;
            pcsRole = "client";

            // When creating a new Client Program it has no context
            // These two functions will update the new client context with the partitions and server that PCS knows
            this.client.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            this.client.UpdatePartitionsContext(PartitionMapping.partitionToReplicationFactorMapping, PartitionMapping.partitionMapping);

            string scriptFile = args[2];
            this.client.ReadScriptFile(scriptFile);
            Console.WriteLine(">>> Finished executing script file");
        }

        public void Crash()
        {
             server.Crash();
        }

        public void Freeze()
        {
            server.Freeze();
        }

        public void Unfreeze()
        {
             server.Unfreeze();   
        }

        public void GlobalStatus()
        {
            if (pcsRole == "server")
            {
                server.GetStatus();
            }
            else if (pcsRole == "client")
            {
                client.GetStatus();
            }
        }

        public void UpdateReplicationFactor(string replicationFactor)
        {
            PartitionMapping.UpdateReplicationFactor(replicationFactor);
        }

        public void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            PartitionMapping.CreatePartition(replicationFactor, partitionName, serverIds);
        }

        public void UpdateServers(string serverId, string serverUrl)
        {
            ServerUrlMapping.AddServerToServerUrlMapping(serverId, serverUrl);

            if(pcsRole == "server")
            {
                server.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            } else if(pcsRole == "client")
            {
                client.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            }
        }
    }
}
