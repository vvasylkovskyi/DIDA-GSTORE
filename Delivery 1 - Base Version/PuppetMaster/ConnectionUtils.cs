using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Net.Client;
using PCS;

namespace PuppetMaster
{
    public static class ConnectionUtils
    {
        public static Dictionary<string, PCSServices.PCSServicesClient> gRPCpuppetMasterToPCSconnetionsDictionary = new Dictionary<string, PCSServices.PCSServicesClient>();
        public static Dictionary<string, string> pcsPortToServerOrClientIdDictionary = new Dictionary<string, string>();


        public static string TryGetClientOrServer(string clientOrServerId)
        {
            if (pcsPortToServerOrClientIdDictionary.TryGetValue(clientOrServerId, out string pcsPort))
            {
                return pcsPort;
            }

            return null;
        }

        public static bool TryGetPCS(string clientOrServerId, out PCSServices.PCSServicesClient pcs)
        {
            string pcsPort = TryGetClientOrServer(clientOrServerId);
            if (pcsPort != null)
            {
                return gRPCpuppetMasterToPCSconnetionsDictionary.TryGetValue(pcsPort, out pcs);                
            } else
            {
                return TryGetFirstFreePCS(clientOrServerId, out pcs);
            }
        }

        public static bool TryGetFirstFreePCS(string clientOrServerId, out PCSServices.PCSServicesClient pcs)
        {
            string[] arrayOfPorts = gRPCpuppetMasterToPCSconnetionsDictionary.Keys.ToArray();

            if(pcsPortToServerOrClientIdDictionary.Count != 0)
            {
                foreach(string port in arrayOfPorts) 
                {
                    bool portIsFree = true;
                    foreach (string pcsPort in pcsPortToServerOrClientIdDictionary.Values)
                    {
                        // PCS on 'port' is busy
                        if (port == pcsPort)
                        {
                            portIsFree = false;
                            break;
                        }
                    }

                    if(portIsFree) {
                        AddNewPcsPortToServerOrClientUrlDictionary(port, clientOrServerId);
                        return gRPCpuppetMasterToPCSconnetionsDictionary.TryGetValue(port, out pcs);
                    }
                }
            } else {
                // First Port
                AddNewPcsPortToServerOrClientUrlDictionary(arrayOfPorts.First(), clientOrServerId);
                return gRPCpuppetMasterToPCSconnetionsDictionary.TryGetValue(arrayOfPorts.First(), out pcs);
            }
            
            pcs = null;
            return false;
        }

        public static void AddNewPcsPortToServerOrClientUrlDictionary(string port, string clientOrServerId)
        {
            pcsPortToServerOrClientIdDictionary.Add(clientOrServerId, port);
        }

        public static bool EstablishPCSConnection(string port)
        {
            string url = "http://localhost:" + port;
            Console.WriteLine(">>> Initializing Grpc Connection for url: " + url);
            try
            {
                GrpcChannel channel = GrpcChannel.ForAddress(url);
                PCSServices.PCSServicesClient pcsClient = new PCSServices.PCSServicesClient(channel);
                gRPCpuppetMasterToPCSconnetionsDictionary.Add(port, pcsClient);

                Console.WriteLine(">>> Connections: " + gRPCpuppetMasterToPCSconnetionsDictionary.Count);
                return true;
            }
            catch (UriFormatException)
            {
                Console.WriteLine(">>> Exception. URI format is incorrect");
                return false;
            }
        }
    }
}
