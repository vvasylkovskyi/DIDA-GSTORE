using Grpc.Net.Client;
using Shared.GrpcDataStore;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataStoreClient
{
    class Program
    {
        private GrpcChannel channel;
        private DataStoreService.DataStoreServiceClient client;
        private string attached_server_id;



        static void Main(string[] args)
        {
            new Program().Init(args);
        }

        void Init(string[] args)
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
                    read(int.Parse(commandsList[1]), long.Parse(commandsList[2]), commandsList[3]);
                    break;
                case "write":
                    // use regex to find string inbetween quotes
                    Regex regexObj = new Regex("\"([^\"]*)\"");

                    // match with the whole command
                    Match matchResult = regexObj.Match(commands);

                    // save the first string inbetween quotation marks
                    string objectValue = matchResult.Groups[1].Value;

                    write(int.Parse(commandsList[1]), long.Parse(commandsList[2]), objectValue);
                    break;
                case "listServer":

                    break;
                case "listGlobal":

                    break;
                case "wait":

                    break;
                case "begin-repeat":

                    break;
                case "end-repeat":

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



        private void read(int partition_id, long object_id, string server_id)
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

        private void write(int partition_id, long object_id, string value)
        {
            WriteReply reply;

            string partition_master_server_id = ServerMapping.getPartitionMaster(partition_id);
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

        }

        private void listGlobal()
        {

        }

        private void wait(int duration)
        {

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



        private void deattachServer()
        {
            if (channel != null)
                channel.ShutdownAsync();
        }

        private void attachServer(string server_id)
        {
            string url = ServerMapping.getServerUrl(server_id);
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
        }
        private void exitProgram()
        {
            deattachServer();
            Environment.Exit(1);
        }
    }
}
