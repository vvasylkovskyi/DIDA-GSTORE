using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shared.Util;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace DataStoreClient
{
    public class Program
    {
        private bool debug_console = false;

        private GrpcChannel channel;
        private DataStoreService.DataStoreServiceClient client;
        private string attached_server_id;

        private List<string> commands_in_repeat_block;
        private bool repeat_block_active = false;
        private int repeat_block_total_cycles = 0;

        private int retry_time = 1000;


        static void Main(string[] args)
        {
            StartClientManually(args);
        }

        public Program(string[] args, bool fromPCS)
        {
            string username = args[0];
            string clientUrl = args[1];
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine(">>> Started client process.");
            Console.WriteLine(">>> clientID= " + username + "; url= " + clientUrl);
            Console.WriteLine(">>> I'm ready to work");

            if (!fromPCS) {
                readCommandsLoop();
            }
        }

        public static Program StartClientWithPCS(string[] args)
        {
            return new Program(args, true);
        }

        public static Program StartClientManually(string[] args)
        {
            return new Program(args, false);
        }

        public void readCommandsLoop()
        {
            Console.WriteLine(">>> Please write a command (use 'help' to get a list of available commands)");
            while (true)
            {
                ReadCommandFromCommandLine(Console.ReadLine());
                Console.WriteLine("\n>>> Please write a command");
            }
        }

        public void UpdatePartitionsContext(Dictionary<string, string> partitionToReplicationFactorMapping, Dictionary<string, string[]> partitionMapping,
            Dictionary<string, int> partitionToClockMapping, Dictionary<string, string> partitionToMasterMapping)
        {
            PartitionMapping.CreatePartitionMapping(partitionToReplicationFactorMapping, partitionMapping, partitionToClockMapping, partitionToMasterMapping);
        }

        public void UpdateServersContext(Dictionary<string, string> serverUrlMapping)
        {
            ServerUrlMapping.CreateServerUrlMapping(serverUrlMapping);
        }

        private void ReadCommandFromCommandLine(string commands)
        {
            Console.WriteLine("\n>>> Executing command...");
            Console.WriteLine("======== " + commands);
            if (string.IsNullOrWhiteSpace(commands))
                return;
            string[] commandsList = commands.Split(' ');
            string mainCommand = commandsList[0];
            switch (mainCommand)
            {
                case "read":

                    if (repeat_block_active)
                    {
                        commands_in_repeat_block.Add(commands);
                    }
                    else
                    {
                        read(commandsList[1], commandsList[2], commandsList[3]);
                    }

                    break;

                case "write":

                    if (repeat_block_active)
                    {
                        commands_in_repeat_block.Add(commands);
                    }
                    else
                    {
                        string objectValue = parseObjectValue(commands);
                        write(commandsList[1], commandsList[2], objectValue);
                    }

                    break;

                case "listServer":

                    if (repeat_block_active)
                    {
                        commands_in_repeat_block.Add(commands);
                    }
                    else
                    {
                        listServer(commandsList[1]);
                    }

                    break;

                case "listGlobal":

                    if (repeat_block_active)
                    {
                        commands_in_repeat_block.Add(commands);
                    }
                    else
                    {
                        listGlobal();
                    }

                    break;

                case "wait":

                    if (repeat_block_active)
                    {
                        commands_in_repeat_block.Add(commands);
                    }
                    else
                    {
                        wait(int.Parse(commandsList[1]));
                    }

                    break;

                case "begin-repeat":
                    startRepeatBlock(commandsList[1]);
                    break;

                case "end-repeat":
                    executeRepeatBlock(commands_in_repeat_block);
                    break;

                case "help":
                    showHelp();
                    break;

                case "script":
                    ReadScriptFile(commandsList[1]);
                    break;

                case "exit":
                    exitProgram();
                    break;

                case "q":
                    exitProgram();
                    break;

                default:
                    Console.WriteLine("This is an invalid command:" + mainCommand);
                    // showHelp();
                    break;
            }
        }

        // Change name to reflect boolean return
        private bool CompareClock(string partition_id, int reply_clock, DataStoreKeyDto object_key)
        {
            // Do Clock related staff
            int partition_highest_clock = PartitionMapping.partitionToClockMapping[partition_id];

            Console.WriteLine(">>> PartitionClock=" + partition_highest_clock + ", ReplyClock=" + reply_clock);
            if (reply_clock > partition_highest_clock)
            {
                Console.WriteLine(">>> ReplyClock > PartitionClock. Reply clock is acceptable. ");
                PartitionMapping.UpdatePartitionClock(partition_id, reply_clock);
                return true;
            }
            else if (reply_clock < partition_highest_clock)
            {
                Console.WriteLine(">>> ReplyClock < PartitionClock. Waiting " + retry_time.ToString() + "And trying again...");
                wait(retry_time);
                return TryReadValue(object_key, partition_id);
            }

            else if (reply_clock == partition_highest_clock)
            {
                Console.WriteLine(">>> PartitionClock == ReplyClock. Reply clock is acceptable. ");
            }
            return true;
        }

        private bool TryReadValue(DataStoreKeyDto object_key, string partition_id)
        {
            string result;
            ReadReply reply;

            try
            {
                reply = client.Read(new ReadRequest { ObjectKey = object_key });
                if (reply.ObjectExists)
                {
                    result = reply.Object.Val;
                    Console.WriteLine(">>> Read Result: " + result);
                    return CompareClock(partition_id, reply.PartitionClock, object_key);
                }

                Console.WriteLine(">>> Object does not exist...");
                return false;
            }
            catch
            {
                // server is crashed
                bool canRetryOperation = HandleCrashedServer(attached_server_id);
                if (canRetryOperation)
                {
                    Console.WriteLine(">>> Retrying <Read> after reattaching to new master");
                    TryReadValue(object_key, partition_id);
                }

                return false;
            }
        }

        private void read(string partition_id, string object_id, string server_id)
        {
            bool got_result = false;

            var object_key = new DataStoreKeyDto
            {
                PartitionId = partition_id,
                ObjectId = object_id
            };

            if (debug_console) Console.WriteLine("Reading from the server...");

            Console.WriteLine(">>> Read request...");

            // if the client is attached to a server and that server contains the desired partition
            List<string> available_partitions_in_server = PartitionMapping.GetPartitionsByServerID(attached_server_id);
            if (!string.IsNullOrEmpty(attached_server_id) && available_partitions_in_server.Contains(partition_id))
            {
                if (debug_console) Console.WriteLine("Reading from the Attached Server: " + attached_server_id);

                // read value from attached server
                got_result = TryReadValue(object_key, partition_id);
            }

            // if theres no result yet and there is a valid server_id parameter
            if ((!got_result) && (!server_id.Equals("-1")))
            {
                // check if the server hint even has the partition
                available_partitions_in_server = PartitionMapping.GetPartitionsByServerID(server_id);
                if (available_partitions_in_server.Contains(partition_id))
                {
                    if (debug_console) Console.WriteLine("Attach to new Server: " + server_id);
                    reattachServer(server_id);

                    // read value from alternative server
                    got_result = TryReadValue(object_key, partition_id);
                }

            }

            // if theres no result yet, the client should find a server serving partition_id on its own
            // it will try to connect to every single node in that partition
            if (!got_result)
            {
                string[] partition_nodes = PartitionMapping.GetPartitionAllNodes(partition_id);

                foreach (string node_id in partition_nodes)
                {
                    if (debug_console) Console.WriteLine("Attach to new Server: " + node_id);
                    reattachServer(node_id);

                    // read value from one of the partition servers
                    got_result = TryReadValue(object_key, partition_id);
                }
            }

            if (got_result == false)
            {
                Console.WriteLine("Read Result: N/A");
            }
        }

        private void write(string partition_id, string object_id, string value)
        {
            WriteReply reply;
            if (debug_console) Console.WriteLine("Get Partition Master from Partition Named: " + partition_id);
            string partition_master_server_id = PartitionMapping.GetPartitionMaster(partition_id);
            if (debug_console) Console.WriteLine("Partition Master Server ID: " + partition_master_server_id);
            reattachServer(partition_master_server_id);

            var object_key = new DataStoreKeyDto
            {
                PartitionId = partition_id,
                ObjectId = object_id
            };

            var object_value = new DataStoreValueDto
            {
                Val = value
            };

            Console.WriteLine(">>> Write request...");

            try
            {
                reply = client.Write(new WriteRequest { ObjectKey = object_key, Object = object_value });
                Console.WriteLine("Write result: " + reply);
            }
            catch
            {
                bool canRetryOperation = HandleCrashedServer(attached_server_id);
                if (canRetryOperation)
                {
                    Console.WriteLine(">>> Retrying <Write> after reattaching to new master");
                    write(partition_id, object_id, value);
                }

                return;
            }
        }

        private void listServer(string server_id)
        {
            ListServerReply reply;
            string previous_attached_server = attached_server_id;
            
            try
            {
                reattachServer(server_id);
                reply = client.ListServer(new ListServerRequest { Msg = "" });

                Console.WriteLine("> Start displaying objects stored in server: " + server_id);

                foreach (DataStorePartitionDto partition in reply.PartitionList)
                {
                    Console.WriteLine(">> Partition " + partition.PartitionId + ", is the server the master of this partition: " + partition.IsMaster);
                    foreach (DataStoreObjectDto store_object in partition.ObjectList)
                    {
                        Console.WriteLine(store_object);
                    }
                    Console.WriteLine(">> End of partition: " + partition.PartitionId);
                }

                Console.WriteLine("> End of objects stored in server: " + server_id);
            }
            catch
            {
                bool canRetryOperation = HandleCrashedServer(server_id);
                if(canRetryOperation)
                {
                    Console.WriteLine(">>> Retrying <ListServer> after reattaching to new master");
                    listServer(server_id);
                }

                return;
            }

            reattachServer(previous_attached_server);
        }

        private void listGlobal()
        {
            foreach(string server_id in ServerUrlMapping.serverUrlMapping.Keys)
            {
                listServer(server_id);
                Console.WriteLine("----------");
            }
        }


        private bool TryNotifyServerAboutCrashedServer(string partitionName, string serverId, string crashedServerId)
        {
            Console.WriteLine(">>> Notifying Server=" + serverId + " about CrashedServer=" + crashedServerId);

            try
            {
                reattachServer(serverId);
                NotifyCrashReply notifyCrashReply = client.NotifyCrash(new NotifyCrashRequest { PartitionId = partitionName, CrashedMasterServerId = crashedServerId });
                Console.WriteLine(">>> Got Reply from ServerId=" + serverId);
                Console.WriteLine(">>> New Partition Master: PartitionName=" + partitionName + "PartitionMasterId=" + notifyCrashReply.MasterId);
                string masterId = notifyCrashReply.MasterId;

                PartitionMapping.SetPartitionMaster(partitionName, masterId);

                if(serverId != masterId)
                {
                    Console.WriteLine(">>> Reataching to new master. MasterId=" + masterId);
                    reattachServer(masterId);
                } else
                {
                    Console.WriteLine(">>> Already Attached to new master. MasterId=" + masterId);
                }
                return true;
            }
            catch
            {
                Console.WriteLine(">>> No Reply...");
                return false;
            }
        }

        private bool NotifyPartitionsAboutCrashedServerAndReattachNewMaster(string crashedServerId)
        {
            List<string> partitions = PartitionMapping.GetPartitionsThatContainServer(crashedServerId);

            foreach(string partitionName in partitions)
            {
                Console.WriteLine(">>> Notify Servers in Partition about Crash...");
                Console.WriteLine(">>> Partition=" + partitionName);
                string[] partitionNodes = PartitionMapping.GetPartitionAllNodes(partitionName);

                bool newMasterWasReattachedSuccessfully = false;
                foreach(string serverId in partitionNodes)
                {
                    if(serverId == crashedServerId)
                    {
                        continue; // Skip crashed server
                    }

                    else if (TryNotifyServerAboutCrashedServer(partitionName, serverId, crashedServerId)) {
                        newMasterWasReattachedSuccessfully = true;
                        break;
                    }
                }

                return newMasterWasReattachedSuccessfully;
            }

            return false;
        }

        private bool HandleCrashedServer(string crashed_server_id)
        {
            Console.WriteLine("--------------------");
            Console.WriteLine(">>> CLIENT: Notify Crash... " + crashed_server_id);
            Console.WriteLine(">>> The server is not responding. It seems to have crashed. ServerID: " + crashed_server_id);
            Console.WriteLine("--------------------");

            if(NotifyPartitionsAboutCrashedServerAndReattachNewMaster(crashed_server_id))
            {
                PartitionMapping.RemoveCrashedServerFromAllPartitions(crashed_server_id);
                ServerUrlMapping.RemoveCrashedServer(crashed_server_id);
                Console.WriteLine("--------------------");
                return true;
            }

            return false;
        }

        private void wait(int duration)
        {
            Thread.Sleep(duration);
        }

        public void ReadScriptFile(string fileName)
        {
            string command;
            Console.WriteLine(">>> The File name is: " + fileName);
            StreamReader file;

            // the result should be something like: C:\......\DIDA - GSTORE\Delivery 1 - Base Version\PCS\bin\Debug\netcoreapp3.1
            string _filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 4; i++)
            {
                _filePath = Utilities.getParentDir(_filePath);
            }

            string path = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                path = _filePath + "/scripts/" + fileName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                path = _filePath + "\\scripts\\" + fileName;

            try
            {
                file = new StreamReader(path);
            }
            catch (Exception)
            {
                Console.WriteLine("Exception. File Not Found. Please Try again");
                return;
            }
            while ((command = file.ReadLine()) != null)
            {
                ReadCommandFromCommandLine(command);
            }
        }

        private void showHelp()
        {
            Dictionary<string, string[]> commands = new Dictionary<string, string[]>()
            {
                {"read", new string[]{"partition_id", "object_id", "server_id"} },
                {"write", new string[]{"partition_id", "object_id", "value"} },
                {"listServer", new string[]{"server_id"} },
                {"listGlobal", new string[]{} },
                {"wait", new string[]{"x_milliseconds"} },
                {"begin-repeat", new string[]{"x_times"} },
                {"end-repeat", new string[]{} },
                {"help", new string[]{} },
                {"script", new string[]{"filename"} },
                {"exit", new string[]{} },
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

        private string parseObjectValue(string commands)
        {
            // use regex to find string inbetween quotes
            Regex regexObj = new Regex("\"([^\"]*)\"");

            // match with the whole command
            Match matchResult = regexObj.Match(commands);

            // save the first string inbetween quotation marks
            string objectValue = matchResult.Groups[1].Value;

            return objectValue;
        }

        private void startRepeatBlock(string cycles)
        {
            commands_in_repeat_block = new List<string>();
            repeat_block_active = true;
            repeat_block_total_cycles = int.Parse(cycles);
        }

        private void executeRepeatBlock(List<string> command_list)
        {
            repeat_block_active = false;

            for (int curr_cycle = 1; curr_cycle <= repeat_block_total_cycles; curr_cycle++)
            {
                foreach (string command in commands_in_repeat_block)
                {
                    string replacedCommand = command.Replace("$i", curr_cycle.ToString());
                    ReadCommandFromCommandLine(replacedCommand);
                }
            }
        }

        private void deattachServer()
        {
            if (channel != null)
                channel.ShutdownAsync();
        }

        private void attachServer(string server_id)
        {
            if (debug_console) Console.WriteLine("Attaching to server with id: " + server_id);
            string url = ServerUrlMapping.GetServerUrl(server_id);
            if (debug_console) Console.WriteLine("Server URL is: " + url);
            channel = GrpcChannel.ForAddress(url);
            client = new DataStoreService.DataStoreServiceClient(channel);
            attached_server_id = server_id;
        }

        private void reattachServer(string server_id)
        {
            deattachServer();
            attachServer(server_id);
        }

        public void GetStatus()
        {
            Console.WriteLine("--------------------");
            Console.WriteLine(">>> Printing status...");
            Console.WriteLine("--------------------");
            Console.WriteLine(">>> Role: client, Attached server: " + attached_server_id);
            Console.WriteLine("   ");
            Console.WriteLine("--------------------");
        }

        private void exitProgram()
        {
            deattachServer();
            Environment.Exit(1);
        }
    }
}
