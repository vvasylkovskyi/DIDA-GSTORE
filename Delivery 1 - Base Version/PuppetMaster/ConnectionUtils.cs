using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using PCS;

namespace PuppetMaster
{
    public static class ConnectionUtils
    {
        public static Dictionary<string, PCSServices.PCSServicesClient> gRPCpuppetMasterToPCSconnetionsDictionary = new Dictionary<string, PCSServices.PCSServicesClient>();
        public static Dictionary<string, string> processCreationServiceUrlsDictionary = new Dictionary<string, string>();
        public static int nextPCSPort = 10000;

        public static bool TryGetPCS(string processId, out PCSServices.PCSServicesClient pcs)
        {
            return gRPCpuppetMasterToPCSconnetionsDictionary.TryGetValue(processId, out pcs);
        }

        // Example url : http://localhost:3000
        public static bool AddGrpcConnection(string url, string id)
        {
            Console.WriteLine(">>> Initializing Grpc Connection for url: " + url);
            try
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + nextPCSPort);
                PCSServices.PCSServicesClient client = new PCSServices.PCSServicesClient(channel);
                gRPCpuppetMasterToPCSconnetionsDictionary.Add(id, client);
                nextPCSPort++;
                return true;
            }
            catch (UriFormatException)
            {
                Console.WriteLine(">>> Exception. URI format is incorrect");
                return false;
            }
        }

        public static bool EstablishGrpcConnection(string url, string id)
        {
            bool establishedConnection = AddGrpcConnection(url, id);
            if (establishedConnection)
            {
                processCreationServiceUrlsDictionary.Add(id, url);
                return true;
            }
            return false;
        }

        public static bool EstablishPCSConnection(string url, string id)
        {
            if (IsUrlAvailable(url))
            {
                Console.WriteLine(">>> Starting new PCS with url: " + url);
                return EstablishGrpcConnection(url, id);
            }
            return true;
        }

        public static bool IsUrlAvailable(string url)
        {
            return !processCreationServiceUrlsDictionary.ContainsValue(url);
        }
    }
}
