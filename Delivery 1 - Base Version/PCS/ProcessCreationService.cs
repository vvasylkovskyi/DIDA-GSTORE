using System;
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
            pcsRole = "server";
        }

        public void StartClient(string[] args)
        {
            Console.WriteLine(">>> Starting Client...");
            DataStoreClient.Program client = new DataStoreClient.Program().StartClient(args);
            this.client = client;
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

        public void UpdateReplicasNumber(string replicationFactor)
        {
            PartitionMapping.UpdateReplicasNumber(replicationFactor);
        }

        public void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            PartitionMapping.CreatePartition(replicationFactor, partitionName, serverIds);
        }

        //public void ShutdownAllProcesses()
        //{
        //    Console.WriteLine(">>> Exiting all processes");
        //    foreach (Process process in processesList)
        //    {
        //        try
        //        {
        //            process.CloseMainWindow();
        //            process.Close();
        //        }
        //        catch (InvalidOperationException)
        //        {
        //            Console.WriteLine(">>> Exception, Process is already closed");
        //        }
        //    }
        //    Console.WriteLine(">>> Done.");
        //}
    }
}
