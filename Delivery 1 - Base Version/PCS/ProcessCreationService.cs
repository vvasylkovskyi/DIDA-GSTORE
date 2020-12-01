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

            while(true)
            {
                
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

            // When creating a new Server Program it has no context
            // These two functions will update the new server context with the partitions and server that PCS knows
            this.server.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            this.server.UpdatePartitionsContext(PartitionMapping.partitionToReplicationFactorMapping, PartitionMapping.partitionMapping);

            pcsRole = "server";
        }

        public void StartClient(string[] args)
        {
            Console.WriteLine(">>> Starting Client...");
            bool fromCMD = true;
            DataStoreClient.Program client = new DataStoreClient.Program();
            this.client = client;
             
            // When creating a new Client Program it has no context
            // These two functions will update the new client context with the partitions and server that PCS knows
            this.client.UpdateServersContext(ServerUrlMapping.serverUrlMapping);
            this.client.UpdatePartitionsContext(PartitionMapping.partitionToReplicationFactorMapping, PartitionMapping.partitionMapping);

            this.client.StartClient(args, fromCMD);
            pcsRole = "client";
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
            PartitionMapping.UpdateReplcationFactor(replicationFactor);
        }

        public void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            this.server.GetServer().createPartition(partitionName);
        }

    }
}
