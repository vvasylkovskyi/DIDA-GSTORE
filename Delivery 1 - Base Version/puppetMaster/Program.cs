using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using processCreationService;
using Shared;
using server;

namespace puppetMaster
{
    class Program
    {
        #region private fields
        private Dictionary<string, IProcessCreationService> processCreationServiceDictionary = new Dictionary<string, IProcessCreationService>();
        private Dictionary<string, IServerClientCommands> activators = new Dictionary<string, IServerClientCommands>();
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
                    break;
                case "Server":
                    string serverId = commandsList[1];
                    string url = commandsList[2];
                    string minDelay = commandsList[3];
                    string maxDelay = commandsList[4];
                    Task.Run(() => StartServerProcess(serverId, url, minDelay, maxDelay));
                    break;
                case "Partition":
                    string replicasNumber = commandsList[1];
                    string partitionName = commandsList[2];
                    List<string> serverIds = new List<string>();
                    for (int i = 3; i < commandsList.Length; i++)
                    {
                        serverIds.Add(commandsList[i]);
                        Console.WriteLine(" >>> Adding server " + commandsList[i]);
                    }
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
                    break;
                case "Freeze":
                    string freezeServerId = commandsList[1];
                    break;
                case "Unfreeze":
                    string unfreezeServerId = commandsList[1];
                    break;
                case "Wait":
                    string timeMs = commandsList[1];
                    break;
            }
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
            string argsString = Shared.Program.BuildArgumentsString(serverId, minDelay, maxDelay);
            Console.WriteLine(">>> PCS Starting on url: " + url);
            Console.WriteLine(">>> With Args: " + argsString);
            CheckPCSConnection(url);
            processCreationServiceDictionary[url].StartServer(argsString);

            SaveServerProgram(url);
        }

        private void StartClientProcess(string username, string clientUrl, string scriptFile)
        {
            string argsString = Shared.Program.BuildArgumentsString(username, clientUrl, scriptFile);
            Console.WriteLine(">>> PCS Starting on url: " + clientUrl);
            Console.WriteLine(">>> With Args: " + argsString);
            CheckPCSConnection(clientUrl);
            processCreationServiceDictionary[clientUrl].StartClient(argsString);

        }

        private void SaveServerProgram(string url)
        {
            IServerClientCommands program = (IServerClientCommands)Activator.CreateInstance<processCreationService.Program>();
            if (program == null)
            {
                Console.WriteLine("Process was not found");
            }
            else
            {
                activators.Add(url, program);
            }
        }

        private void CheckPCSConnection(string url)
        {
            if (!processCreationServiceDictionary.ContainsKey(url))
            {
                Console.WriteLine(">>> Starting new PCS with url: " + url);
                processCreationServiceDictionary.Add(url, Activator.CreateInstance<processCreationService.Program>());
            }
        }

        #endregion
    }
}
