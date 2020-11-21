using PCS;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class Program
    {

        #region Puppet Master
        static void Main(string[] args)
        {
            new Program().Init();
        }

        void Init()
        {
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine(">>> Started Running Puppet Master");
            string command = "";
            while (command != "1" || command != "2" || command != "q")
            {
                Console.WriteLine(">>> Press '1' to read file");
                Console.WriteLine(">>> Press '2' to go to write command menu");
                Console.WriteLine(">>> Press 'q' to exit");
                command = Console.ReadLine();
                if (command == "1")
                {
                    ReadScriptFile();
                }
                else if (command == "2")
                {
                    ReadCommandFromCommandLine();
                }
                else if(command == "q")
                {
                    Environment.Exit(1);
                }
            }
        }

        #endregion

        #region read commands

        private void ReadScriptFile()
        {
            string command;
            Console.WriteLine(">>> Please Write file pathname: ");
            StreamReader file;

            string filePath = Console.ReadLine();
            try
            {
                file = new StreamReader(filePath);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine(">>> Exception. File Not Found. Please Try again");
                return;
            }
            while ((command = file.ReadLine()) != null)
            {
                ProcessCommand(command);
            }
        }

        private void ReadCommandFromCommandLine()
        {
            while (true)
            {
                Console.WriteLine(">>> Please Write a command");
                string command = Console.ReadLine();
                ProcessCommand(command);
            }
        }

        private void ProcessCommand(string commands)
        {
            if (string.IsNullOrWhiteSpace(commands))
                return;
            Console.WriteLine(">>> Executing command: " + commands);
            string[] commandsList = commands.Split(' ');
            string mainCommand = commandsList[0];
            switch (mainCommand)
            {
                //case "q":
                //    foreach (IProcessCreationService PCS in processCreationServiceDictionary.Values)
                //    {
                //        try
                //        {
                //            PCS.ShutdownAllProcesses();
                //        }
                //        catch
                //        {
                //            // pragram can be already closed
                //        }
                //    }
                //    Environment.Exit(1);
                //    break;
                case "ReplicationFactor":
                    string r = commandsList[1];
                    int rNumber;
                    if (int.TryParse(r, out rNumber) == false)
                    {
                        Console.WriteLine(" >>> Invalid Argument. First Replicas Number should be a number");
                    }
                    Task.Run(() => UpdateReplicasNumber(rNumber));
                    break;
                case "Partition":
                    string replicasNumberString = commandsList[1];
                    string partitionName = commandsList[2];
                    List<string> serverIds = new List<string>();

                    int serversNumber = 0;
                    int replicasNumber = 0;
                    if (int.TryParse(replicasNumberString, out replicasNumber) == false)
                    {
                        Console.WriteLine(">>> Invalid Argument. First argument should be a number");
                        break;
                    }

                    for (int i = 3; i < commandsList.Length; i++)
                    {
                        if (commandsList[i] != "")
                        {
                            serverIds.Add(commandsList[i]);
                            serversNumber++;
                            Console.WriteLine(">>> Adding server " + commandsList[i]);
                        }
                    }

                    if (serversNumber != replicasNumber)
                    {
                        Console.WriteLine(">>> Invalid number of servers, should have " + replicasNumber + " servers but " + serversNumber + " were given");
                        break;
                    }
                    CreatePartition(replicasNumber, partitionName, serverIds);
                    break;
                case "Server":
                    string serverId = commandsList[1];
                    string url = commandsList[2];
                    string minDelay = commandsList[3];
                    string maxDelay = commandsList[4];
                    Task.Run(() => StartServerProcess(serverId, url, minDelay, maxDelay));
                    break;
                case "Client":
                    string username = commandsList[1];
                    string clientUrl = commandsList[2];
                    string scriptFile = commandsList[3];
                    Task.Run(() => StartClientProcess(username, clientUrl, scriptFile));
                    break;
                case "Status":
                    Task.Run(() => GlobalStatus());
                    break;
                case "Crash":
                    string crashServerId = commandsList[1];
                    Task.Run(() => CrashServer(crashServerId));
                    break;
                case "Freeze":
                    string freezeServerId = commandsList[1];
                    Task.Run(() => FreezServer(freezeServerId));
                    break;
                case "Unfreeze":
                    string unfreezeServerId = commandsList[1];
                    Task.Run(() => UnfreezServer(unfreezeServerId));
                    break;
                case "Wait":
                    string timeMs = commandsList[1];
                    Console.WriteLine(">>> Waiting...");
                    Thread.Sleep(int.Parse(timeMs));
                    break;
            }
        }

        private void UpdateReplicasNumber(int replicasNumber)
        {
            PartitionMapping.UpdateReplicasNumber(replicasNumber);
        }

        private void CreatePartition(int replicasNumber, string partitionName, List<string> serverIds)
        {
            Console.WriteLine(">>> Creating a Partition " + partitionName);
            PartitionMapping.UpdateReplicasNumber(replicasNumber);
            PartitionMapping.AddPartition(partitionName, serverIds);
        }

        private void UnfreezServer(string serverId)
        {
            Console.WriteLine("Unfreezing Server: " + serverId);

            if (!ConnectionUtils.TryGetPCS(serverId, out PCSServices.PCSServicesClient server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            UnfreezeReply unfreezeReply = server.Unfreeze(new UnfreezeRequest { ServerId = serverId });
            Console.WriteLine(unfreezeReply.Unfreeze);
        }

        private void FreezServer(string serverId)
        {
            Console.WriteLine("Freezing Server: " + serverId);

            if (!ConnectionUtils.TryGetPCS(serverId, out PCSServices.PCSServicesClient server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            FreezeReply freezeReply = server.Freeze(new FreezeRequest { ServerId = serverId });
            Console.WriteLine(freezeReply.Freeze);
        }

        private void CrashServer(string serverId)
        {
            Console.WriteLine("Crashing Server: " + serverId);

            if (!ConnectionUtils.TryGetPCS(serverId, out PCSServices.PCSServicesClient server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            CrashReply crashReply = server.Crash(new CrashRequest { ServerId = serverId });
            Console.WriteLine(crashReply.Crash);
        }

        private void GlobalStatus()
        {
            Console.WriteLine(">>> Getting Processes Status");
            Parallel.ForEach(ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values, gRPCpuppetMasterToPCSconnetion => {
                StatusReply statusReply = gRPCpuppetMasterToPCSconnetion.GlobalStatus(new StatusRequest { Localhost = "1" });
                Console.WriteLine(statusReply.Status);
            });
        }

        private void StartServerProcess(string serverId, string url, string minDelay, string maxDelay)
        {
            string argsString = Utilities.BuildArgumentsString(new string[] { serverId, url, minDelay, maxDelay });
            Console.WriteLine(">>> PCS Starting on url: " + url);
            Console.WriteLine(">>> With Args: " + argsString);
            if(ConnectionUtils.IsUrlAvailable(url))
            {
                ConnectionUtils.EstablishPCSConnection(url, serverId);

                if (!ConnectionUtils.TryGetPCS(serverId, out PCSServices.PCSServicesClient pcs))
                {
                    Console.WriteLine("Cannot not establish connection with PCS");
                    return;
                }
                Console.WriteLine("Invoking Start Server...");
                StartServerReply startServerReply = pcs.StartServer(new StartServerRequest { Args = argsString });
                Console.WriteLine(startServerReply.StartServer);
            }
        }

        private void StartClientProcess(string username, string clientUrl, string scriptFile)
        {
            string argsString = Utilities.BuildArgumentsString(new string[] { username, clientUrl, scriptFile });
            Console.WriteLine(">>> PCS Starting on url: " + clientUrl);
            Console.WriteLine(">>> With Args: " + argsString);
            if(ConnectionUtils.IsUrlAvailable(clientUrl))
            {
                ConnectionUtils.EstablishPCSConnection(clientUrl, username);

                if (!ConnectionUtils.TryGetPCS(username, out PCSServices.PCSServicesClient pcs))
                {
                    Console.WriteLine("Cannot not establish connection with PCS");
                    return;
                }
                Console.WriteLine("Invoking Start Client...");
                StartClientReply startClientReply = pcs.StartClient(new StartClientRequest{ Args = argsString });
                Console.WriteLine(startClientReply.StartClient);
            }
        }
        #endregion
    }
}
 