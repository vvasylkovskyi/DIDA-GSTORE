using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Interfaces;
using Shared.PCS;
using Shared.Util;
using DataStoreServer;

namespace PuppetMaster
{
    class Program
    {
        #region private fields
        private Dictionary<string, IProcessCreationService> processCreationServiceDictionary = new Dictionary<string, IProcessCreationService>();
        private Dictionary<string, DataStoreServer.Program> activators = new Dictionary<string, DataStoreServer.Program>();
        #endregion

        #region Puppet Master
        static void Main(string[] args)
        {
            new Program().Init(args);
        }

        void Init(string[] args)
        {
            Console.WriteLine(">>> Started Running Puppet Master");
            Console.WriteLine(">>> Please Write a command");
            while (true)
            {
                readCommandFromCommandLine(Console.ReadLine());
            }
        }

        #endregion

        #region read commands

        private void readCommandFromCommandLine(string commands)
        {
            if (string.IsNullOrWhiteSpace(commands))
                return;
            Console.WriteLine(">>> Executing command: " + commands);
            string[] commandsList = commands.Split(' ');
            string mainCommand = commandsList[0];
            switch (mainCommand)
            {
                case "q":
                    foreach (IProcessCreationService PCS in processCreationServiceDictionary.Values)
                    {
                        try
                        {
                            PCS.ShutdownAllProcesses();
                        }
                        catch
                        {
                            // pragram can be already closed
                        }
                    }
                    Environment.Exit(1);
                    Environment.Exit(1);
                    break;
                case "ReplicationFactor":
                    string r = commandsList[1];
                    int rNumber;
                    if (int.TryParse(r, out rNumber) == false)
                    {
                        Console.WriteLine(" >>> Invalid Argument. First Replicas Number should be a number");
                    }
                    Task.Run(() => UpdateReplicasNumber(rNumber));
                    break;
                case "Server":
                    string serverId = commandsList[1];
                    string url = commandsList[2];
                    string minDelay = commandsList[3];
                    string maxDelay = commandsList[4];
                    Task.Run(() => StartServerProcess(serverId, url, minDelay, maxDelay));
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
                    CreatePartition(replicasNumber, partitionName, serverIds.ToArray());
                    break;
                case "Client":
                    string username = commandsList[1];
                    string clientUrl = commandsList[2];
                    string scriptFile = commandsList[3];
                    Task.Run(() => StartClientProcess(username, clientUrl, scriptFile));
                    break;
                case "Status":
                    Task.Run(() => getStatus());
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
                    break;
            }
        }

        private void UpdateReplicasNumber(int replicasNumber)
        {
            ServerMapping.UpdateReplicasNumber(replicasNumber);
        }

        private void CreatePartition(int replicasNumber, string partitionName, string[] serverIds)
        {
            Console.WriteLine(">>> Creating a Partition " + partitionName);
            ServerMapping.UpdateReplicasNumber(replicasNumber);
            ServerMapping.AddPartition(partitionName, serverIds);
        }

        private void UnfreezServer(string serverId)
        {
            Console.WriteLine("Unfreezing Server: " + serverId);
            DataStoreServer.Program server;

            if (!TryGetProgram(serverId, out server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            server.Unfreez();
        }
        private void FreezServer(string serverId)
        {
            Console.WriteLine("Freezing Server: " + serverId);
            DataStoreServer.Program server;

            if (!TryGetProgram(serverId, out server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            server.Freez();
        }

        private void CrashServer(string serverId)
        {
            Console.WriteLine("Crashing Server: " + serverId);
            DataStoreServer.Program server;

            if (!TryGetProgram(serverId, out server))
            {
                Console.WriteLine("Cannot find server");
                return;
            }

            server.Crash();
            activators.Remove(serverId);

        }

        private void getStatus()
        {
            Console.WriteLine(">>> Getting Processes Status");
            Parallel.ForEach(activators.Values, connection =>
            {
                connection.getStatus();
            });
        }

        private void StartServerProcess(string serverId, string url, string minDelay, string maxDelay)
        {
            string argsString = Utilities.BuildArgumentsString(serverId, minDelay, maxDelay);
            Console.WriteLine(">>> PCS Starting on url: " + url);
            Console.WriteLine(">>> With Args: " + argsString);
            CheckPCSConnection(url);
            processCreationServiceDictionary[url].StartServer(argsString);

            SaveServerProgram(serverId, url);
        }

        private void StartClientProcess(string username, string clientUrl, string scriptFile)
        {
            string argsString = Utilities.BuildArgumentsString(username, clientUrl, scriptFile);
            Console.WriteLine(">>> PCS Starting on url: " + clientUrl);
            Console.WriteLine(">>> With Args: " + argsString);
            CheckPCSConnection(clientUrl);
            processCreationServiceDictionary[clientUrl].StartClient(argsString);

        }

        private void SaveServerProgram(string serverId, string url)
        {
            DataStoreServer.Program program = (DataStoreServer.Program)Activator.CreateInstance<DataStoreServer.Program>();
            if (program == null)
            {
                Console.WriteLine("Process was not found");
            }
            else
            {
                program.setServerId(serverId);
                activators.Add(url, program);
            }
        }

        private bool TryGetProgram(string processId, out DataStoreServer.Program program)
        {
            return activators.TryGetValue(processId, out program);
        }

        private void CheckPCSConnection(string url)
        {
            if (!processCreationServiceDictionary.ContainsKey(url))
            {
                Console.WriteLine(">>> Starting new PCS with url: " + url);
                processCreationServiceDictionary.Add(url, Activator.CreateInstance<ProcessCreationService>());
            }
        }

        #endregion
    }
}
