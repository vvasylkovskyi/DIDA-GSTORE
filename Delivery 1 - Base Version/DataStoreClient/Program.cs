using Grpc.Net.Client;
using Shared.GrpcDataStore;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shared.Util;
using System.Threading;
using Grpc.Core;

namespace DataStoreClient
{
    public class Program
    {
        private GrpcChannel channel;
        private DataStoreService.DataStoreServiceClient client;
        private string attached_server_id;

        private List<string> commands_in_repeat_block;
        private bool repeat_block_active = false;
        private int repeat_block_total_cycles = 0;


        static void Main(string[] args)
        {
            new Program().Init(args);
        }

        public Program()
        {
        }

        public Program StartClient(string[] args)
        {
            Program program = new Program();
            program.startProgram();
            return program;
        }

        public void Init(string[] args)
        {
            startProgram();
            Console.WriteLine(">>> Started client process");
            Console.WriteLine(">>> Please write a command (use 'help' to get a list of available commands)");
            while (true)
            {
                readCommandFromCommandLine(Console.ReadLine());
                Console.WriteLine("\n>>> Please write a command");
            }
        }

        private void readCommandFromCommandLine(string commands)
        {
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

                case "exit":
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

            // if the client is attached to a server
            if (!string.IsNullOrEmpty(attached_server_id))
            {
                // read value from attached server
                reply = client.Read(new ReadRequest { ObjectKey = object_key });

                if (reply.ObjectExists)
                {
                    result = reply.Object.Val;
                    got_result = true;
                }
            }

            // if theres no result yet and there is a valid server_id parameter
            if ((!got_result) && (!server_id.Equals("-1")))
            {
                // read value from alternative server
                reattachServer(server_id);
                reply = client.Read(new ReadRequest { ObjectKey = object_key });
                if (reply.ObjectExists)
                {
                    result = reply.Object.Val;
                    got_result = true;
                }
            }

            Console.WriteLine(result);
        }

        private void write(string partition_id, string object_id, string value)
        {
            WriteReply reply;

            string partition_master_server_id = PartitionMapping.getPartitionMaster(partition_id);
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

            reply = client.Write(new WriteRequest { ObjectKey = object_key, Object = object_value });
        }

        private void listServer(string server_id)
        {
            ListServerReply reply;

            string previous_attached_server = attached_server_id;
            reattachServer(server_id);

            reply = client.ListServer(new ListServerRequest { Msg = "" });

            Console.WriteLine("Displaying objects stored in server: " + server_id);

            foreach (DataStorePartitionDto partition in reply.PartitionList)
            {
                Console.WriteLine("Partition " + partition.PartitionId);
                Console.WriteLine("The server is the master of this partition: " + (partition.PartitionMasterServerId.Equals(server_id)));

                foreach (DataStoreObjectDto store_object in partition.ObjectList)
                {
                    Console.WriteLine(store_object);
                }
            }

            reattachServer(previous_attached_server);
        }

        private void listGlobal()
        {
            foreach(string server_id in ServerUrlMapping.serverUrlMapping.Keys)
            {
                listServer(server_id);
            }
        }

        private void wait(int duration)
        {
            Thread.Sleep(duration);
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
                {"exit", new string[]{} }
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
            for (int curr_cycle = 0; curr_cycle < repeat_block_total_cycles; curr_cycle++)
            {
                foreach (string command in commands_in_repeat_block)
                {
                    string replacedCommand = command.Replace("$i", curr_cycle.ToString());
                    readCommandFromCommandLine(replacedCommand);
                }
            }

            repeat_block_active = false;
        }

        private void deattachServer()
        {
            if (channel != null)
                channel.ShutdownAsync();
        }

        private void attachServer(string server_id)
        {
            string url = ServerUrlMapping.getServerUrl(server_id);
            channel = GrpcChannel.ForAddress("http://localhost:9080");
            client = new DataStoreService.DataStoreServiceClient(channel);
            attached_server_id = server_id;
        }

        private void reattachServer(string server_id)
        {
            deattachServer();
            attachServer(server_id);
        }


        private void startProgram()
        {
            // allow http traffic in grpc
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.WriteLine("I'm ready to work");
        }

        public void GetStatus()
        {
            Console.WriteLine("Printing status...");
            Console.WriteLine("I am client");
            Console.WriteLine("My id: ");
        }

        private void exitProgram()
        {
            deattachServer();
            Environment.Exit(1);
        }
    }
}
