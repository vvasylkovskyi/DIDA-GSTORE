using Grpc.Core;
using PCS;
using PuppetMaster.Protos;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PuppetMaster
{
    public class Program
    {

        #region Puppet Master
        static void Main(string[] args)
        {
            new Program().Init();
        }

        public void InitPuppetMasterServer()
        {
            try
            {
                Server server = new Server
                {
                    Services = { PuppetMasterServices.BindService(new PuppetMasterServiceImpl()) },
                    Ports = { new ServerPort("localhost", Utilities.puppetMasterPort, ServerCredentials.Insecure) }
                };
                server.Start();

                Console.WriteLine(">>> Puppet Master Server started on port: " + Utilities.puppetMasterPort);
            }
            catch
            {
                Console.WriteLine(">>> Error. Something went wrong");
            }
        }

        void Init()
        {
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            InitPuppetMasterServer();

            Console.WriteLine(">>> Started Running Puppet Master");

            listenToCommands();
        }

        #endregion

        #region read commands

        private void listenToCommands()
        {
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
                else if (command == "q")
                {
                    Environment.Exit(1);
                }
                

            }
        }

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
                case "ReplicationFactor":
                    string r = commandsList[1];
                    if (int.TryParse(r, out int replicationFactor) == false)
                    {
                        Console.WriteLine(" >>> Invalid Argument. Replicas Number should be a number");
                    }
                    Task.Run(() => UpdateReplicasNumber(r));
                    break;
                case "Partition":
                    string replicasNumberString = commandsList[1];
                    string partitionName = commandsList[2];

                    int serversNumber = 0;
                    if (int.TryParse(replicasNumberString, out int replicasNumber) == false)
                    {
                        Console.WriteLine(">>> Invalid Argument. First argument should be a number");
                        break;
                    }

                    List<string> serverIds = new List<string>();

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

                    Task.Run(() => CreatePartition(replicasNumberString, partitionName, serverIds.ToArray()));
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
            }
        }

        private void UpdateReplicasNumber(string replicationFactor)
        {
            Console.WriteLine(">>> Updating Replication Factor...");
            Parallel.ForEach(ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values, gRPCpuppetMasterToPCSconnetion => {
                UpdateReplicasNumberReply updateReplicasNumberReply = gRPCpuppetMasterToPCSconnetion.UpdateReplicasNumber(new UpdateReplicasNumberRequest { ReplicationFactor = replicationFactor });
                Console.WriteLine(">>> Replication Factor response from PCS: " + updateReplicasNumberReply.UpdateReplicasNumber);
            });    
        }

        private void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            Console.WriteLine(">>> Creating a partition...");
            string[] stringArray = new string[] { replicationFactor, partitionName };
            string[] args = stringArray.Concat(serverIds).ToArray();
            string argsString = Utilities.BuildArgumentsString(args);
            Parallel.ForEach(ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values, gRPCpuppetMasterToPCSconnetion => {
                CreatePartitionReply createPartitionReply = gRPCpuppetMasterToPCSconnetion.CreatePartition(new CreatePartitionRequest { Args = argsString });
                Console.WriteLine(">>> Create Partition response from PCS: " + createPartitionReply.CreateParititon);
            }); 
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

            if (!ConnectionUtils.TryGetPCS(serverId, out PCSServices.PCSServicesClient pcs))
            {
                Console.WriteLine("Cannot find connected PCS");
                return;
            }

            Console.WriteLine("Invoking Start Server...");
            StartServerReply startServerReply = pcs.StartServer(new StartServerRequest { Args = argsString });
            Console.WriteLine(startServerReply.StartServer);
        }

        private void StartClientProcess(string username, string clientUrl, string scriptFile)
        {
            string argsString = Utilities.BuildArgumentsString(new string[] { username, clientUrl, scriptFile });
            Console.WriteLine(">>> PCS Starting on url: " + clientUrl);
            Console.WriteLine(">>> With Args: " + argsString);

            if (!ConnectionUtils.TryGetPCS(username, out PCSServices.PCSServicesClient pcs))
            {
                Console.WriteLine("Cannot not establish connection with PCS");
                return;
            }

            Console.WriteLine("Invoking Start Client...");
            StartClientReply startClientReply = pcs.StartClient(new StartClientRequest { Args = argsString });
            Console.WriteLine(startClientReply.StartClient);
        }

        #endregion
    }
}
 