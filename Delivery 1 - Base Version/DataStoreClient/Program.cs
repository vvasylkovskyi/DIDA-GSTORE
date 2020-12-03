using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shared.Util;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

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

        static void Main(string[] args)
        {
            StartClientManually(args);
        }

        public Program(string[] args, bool fromPCS)
        {
            string username = args[0];
            string clientUrl = args[1];
            startProgram(username, clientUrl);

            Console.WriteLine(">>> Started client process");

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

        private void startProgram(string username, string clientUrl)
        {
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.WriteLine("I'm ready to work");

            if (debug_console)
                Console.WriteLine("clientID= " + username + "; url= " + clientUrl);
        }

        public void UpdatePartitionsContext(Dictionary<string, string> partitionToReplicationFactorMapping, Dictionary<string, string[]> partitionMapping)
        {
            PartitionMapping.CreatePartitionMapping(partitionToReplicationFactorMapping, partitionMapping);
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

        private void read(string partition_id, string object_id, string server_id)
        {
            string result = "N/A";
            bool got_result = false;
            ReadReply reply;

            var object_key = new DataStoreKeyDto
            {
                PartitionId = partition_id,
                ObjectId = object_id
            };

            if (debug_console) Console.WriteLine("Reading from the server...");

            Console.WriteLine(">>> Read request...");

            // if the client is attached to a server and that server contains the desired partition
            List<string> available_partitions_in_server = PartitionMapping.getPartitionsByServerID(attached_server_id);
            if (!string.IsNullOrEmpty(attached_server_id) && available_partitions_in_server.Contains(partition_id))
            {
                if (debug_console) Console.WriteLine("Reading from the Attached Server: " + attached_server_id);

                // read value from attached server
                try
                {
                    reply = client.Read(new ReadRequest { ObjectKey = object_key });
                    if (reply.ObjectExists)
                    {
                        result = reply.Object.Val;
                        got_result = true;
                    }
                // server is crashed
                } catch
                {
                    handle_crashed_server(attached_server_id);
                    got_result = false;
                }                
            }

            // if theres no result yet and there is a valid server_id parameter
            if ((!got_result) && (!server_id.Equals("-1")))
            {
                // check if the server hint even has the partition
                available_partitions_in_server = PartitionMapping.getPartitionsByServerID(server_id);
                if (available_partitions_in_server.Contains(partition_id))
                {
                    if (debug_console) Console.WriteLine("Attach to new Server: " + server_id);
                    reattachServer(server_id);

                    // read value from alternative server
                    try
                    {
                        reply = client.Read(new ReadRequest { ObjectKey = object_key });
                        if (reply.ObjectExists)
                        {
                            result = reply.Object.Val;
                            got_result = true;
                        }
                    // server is crashed
                    } catch
                    {
                        handle_crashed_server(attached_server_id);
                        got_result = false;
                    }
                }
                
            }

            // if theres no result yet, the client should find a server serving partition_id on its own
            // it will try to connect to every single node in that partition
            if (!got_result)
            {
                string[] partition_nodes = PartitionMapping.getPartitionAllNodes(partition_id);

                foreach (string node_id in partition_nodes)
                {
                    if (debug_console) Console.WriteLine("Attach to new Server: " + node_id);
                    reattachServer(node_id);

                    // read value from one of the partition servers
                    try
                    {
                        reply = client.Read(new ReadRequest { ObjectKey = object_key });
                        if (reply.ObjectExists)
                        {
                            result = reply.Object.Val;
                            got_result = true;
                        }
                    // server is crashed
                    } catch
                    {
                        handle_crashed_server(attached_server_id);
                        got_result = false;
                    }
                }
            }

            if (got_result == false) result = "N/A";
            Console.WriteLine("Read Result: " + result);
        }

        private void write(string partition_id, string object_id, string value)
        {
            WriteReply reply;
            if (debug_console) Console.WriteLine("Get Partition Master from Partition Named: " + partition_id);
            string partition_master_server_id = PartitionMapping.getPartitionMaster(partition_id);
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
                handle_crashed_server(attached_server_id);
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
                handle_crashed_server(server_id);
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

        private void handle_crashed_server(string server_id)
        {
            Console.WriteLine("The server is not responding. It seems to have crashed. ServerID: " + server_id);
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
            Console.WriteLine(">>> Printing status...");
            Console.WriteLine("Role: client, Attached server: " + attached_server_id);
        }

        private void exitProgram()
        {
            deattachServer();
            Environment.Exit(1);
        }
    }
}
