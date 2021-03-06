﻿using Grpc.Core;
using PCS;
using PuppetMaster.Protos;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

            ReadCommandFromCommandLine();
        }

        #endregion

        #region read commands

        private void ReadScriptFile(string fileName)
        {
            string command;
            Console.WriteLine(">>> Please Write file name: ");
            StreamReader file;

            // the result should be something like: C:\......\DIDA - GSTORE\Delivery 1 - Base Version\PCS\bin\Debug\netcoreapp3.1
            string _filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 4; i++)
            {
                _filePath = Utilities.getParentDir(_filePath);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                fileName = _filePath + "/scripts/" + fileName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileName = _filePath + "\\scripts\\" + fileName;

            Console.WriteLine("Trying to open file at location: " + fileName);
            try
            {
                file = new StreamReader(fileName);
            }
            // DirectoryNotFoundException or FileNotFoundException
            catch (Exception)
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
                Console.WriteLine(">>> use 'help' to get a list of available commands");
                Console.WriteLine(">>> use 'q' to exit");
                string command = Console.ReadLine();
                ProcessCommand(command);
            }
        }

        private void ProcessCommand(string commands)
        {
            if (string.IsNullOrWhiteSpace(commands))
                return;
            Console.WriteLine("======== Executing command: " + commands);
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
                    UpdateReplicasNumber(r);
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

                    CreatePartition(replicasNumberString, partitionName, serverIds.ToArray());
                    break;
                case "Server":
                    string serverId = commandsList[1];
                    string url = commandsList[2];
                    string minDelay = commandsList[3];
                    string maxDelay = commandsList[4];
                    StartServerProcess(serverId, url, minDelay, maxDelay);
                    break;
                case "Client":
                    string username = commandsList[1];
                    string clientUrl = commandsList[2];
                    string scriptFile = commandsList[3];
                    StartClientProcess(username, clientUrl, scriptFile);
                    break;
                case "Status":
                    GlobalStatus();
                    break;
                case "Crash":
                    string crashServerId = commandsList[1];
                    CrashServer(crashServerId);
                    break;
                case "Freeze":
                    string freezeServerId = commandsList[1];
                    FreezeServer(freezeServerId);
                    break;
                case "Unfreeze":
                    string unfreezeServerId = commandsList[1];
                    UnfreezeServer(unfreezeServerId);
                    break;
                case "Wait":
                    string timeMs = commandsList[1];
                    Console.WriteLine(">>> Waiting...");
                    Thread.Sleep(int.Parse(timeMs));
                    break;
                case "script":
                    ReadScriptFile(commandsList[1]);
                    break;
                case "help":
                    showHelp();
                    break;
                case "q":
                    Environment.Exit(1);
                    break;
                default:
                    Console.WriteLine("This is an invalid command:" + mainCommand);
                    break;
            }
        }

        private void UpdateReplicasNumber(string replicationFactor)
        {
            Console.WriteLine(">>> Updating Replication Factor...");
            foreach(PCSServices.PCSServicesClient gRPCpuppetMasterToPCSconnetion in ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values) {
                UpdateReplicasNumberReply updateReplicasNumberReply = gRPCpuppetMasterToPCSconnetion.UpdateReplicasNumber(new UpdateReplicasNumberRequest { ReplicationFactor = replicationFactor });
                Console.WriteLine(">>> Replication Factor response from PCS: " + updateReplicasNumberReply.UpdateReplicasNumber);
            };    
        }

        private void CreatePartition(string replicationFactor, string partitionName, string[] serverIds)
        {
            Console.WriteLine(">>> Creating a partition...");
            string[] stringArray = new string[] { replicationFactor, partitionName };
            string[] args = stringArray.Concat(serverIds).ToArray();
            string argsString = Utilities.BuildArgumentsString(args);
            foreach (PCSServices.PCSServicesClient gRPCpuppetMasterToPCSconnetion in ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values) {
                CreatePartitionReply createPartitionReply = gRPCpuppetMasterToPCSconnetion.CreatePartition(new CreatePartitionRequest { Args = argsString });
                Console.WriteLine(">>> Create Partition response from PCS: " + createPartitionReply.CreatePartititon);
            };
        }

        private void GlobalStatus()
        {
            Console.WriteLine(">>> Getting Processes Status");
            foreach (PCSServices.PCSServicesClient gRPCpuppetMasterToPCSconnetion in ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values)
            {
                StatusReply statusReply = gRPCpuppetMasterToPCSconnetion.GlobalStatus(new StatusRequest { Localhost = "1" });
                Console.WriteLine(statusReply.Status);
            };
        }

        private void UnfreezeServer(string serverId)
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

        private void FreezeServer(string serverId)
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

            try
            {
                CrashReply crashReply = server.Crash(new CrashRequest { ServerId = serverId });
            }
            catch
            {
                 // Crash Command makes a server crash. So no CrashReply is ever going to be returned
            }

            string port = "";
            // Remove server from port to id dictionary as it is crashed
            foreach (KeyValuePair<string, string> pcsPortToServerOrClientIdKvp in ConnectionUtils.pcsPortToServerOrClientIdDictionary)
            {
                if(pcsPortToServerOrClientIdKvp.Key == serverId)
                {
                    port = pcsPortToServerOrClientIdKvp.Value;
                    ConnectionUtils.pcsPortToServerOrClientIdDictionary.Remove(pcsPortToServerOrClientIdKvp.Key);
                    break;
                }
            }

            // Remove server from gRPC dictionary as it is crashed
            ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Remove(port);
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

            // Before starting a server, need to notify all the PCS about the new server.
            // Update the serverUrlMapping by adding the new server
            foreach (PCSServices.PCSServicesClient gRPCpuppetMasterToPCSconnetion in ConnectionUtils.gRPCpuppetMasterToPCSconnetionsDictionary.Values)
            {
                UpdateServersReply UpdateServersReply = gRPCpuppetMasterToPCSconnetion.UpdateServers(new UpdateServersRequest { ServerId = serverId, ServerUrl = url });
                Console.WriteLine(">>> Servers Updated response: " + UpdateServersReply.UpdateServers);
            };

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

        private void showHelp()
        {
            Dictionary<string, string[]> commands = new Dictionary<string, string[]>()
            {
                {"ReplicationFactor", new string[]{"r_factor"} },
                {"Partition", new string[]{"r_factor", "partition_id", "server_id_1", "...", "server_id_n"} },
                {"Server", new string[]{"server_id", "url", "min_delay", "max_delay" } },
                {"Client", new string[]{"username", "url", "script_file"} },
                {"Status", new string[]{} },
                {"Crash", new string[]{"server_id" } },
                {"Freeze", new string[]{"server_id" } },
                {"Unfreeze", new string[]{"server_id" } },
                {"Wait", new string[]{"x_miliseconds"} },
                {"Script", new string[]{"script_filename"} },
                {"q", new string[]{} }
            };

            Console.WriteLine("Available commands are:");

            foreach (var command in commands)
            {
                Console.Write("  {0}", command.Key);

                for (int i = command.Key.Length; i < 15; i++)
                {
                    Console.Write(" ");
                }

                foreach (var arg in command.Value)
                {
                    Console.Write("<{0}> ", arg);
                }

                Console.Write("\n");
            }
        }

        #endregion
    }
}
 