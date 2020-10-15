using System;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Shared.Domain;
using Shared.GrpcDataStore;

namespace DataStoreClient
{
    class Program
    {

        private string attached_server_id;






        static void Main(string[] args)
        {
            Console.WriteLine("Hello! I'm the client");



            Thread.Sleep(1000);



            // allow http traffic
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);



            // connect to server
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:9080");
            var client = new DataStoreService.DataStoreServiceClient(channel);



            // write value
            var write_key = new DataStoreKeyDto
            {
                PartitionId = 1,
                ObjectId = 1
            };

            var write_value = new DataStoreValueDto
            {
                StringVal = "Hello! I'm the client"
            };

            var reply = client.Write( new WriteRequest { Key = write_key, Val = write_value });
            Console.WriteLine("Write response: " + reply);



            // read value
            var read_key = new DataStoreKeyDto
            {
                PartitionId = 1,
                ObjectId = 1
            };

            var reply2 = client.Read(new ReadRequest { Key = write_key });
            Console.WriteLine("Read response: " + reply2);



            // exit program
            channel.ShutdownAsync();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }







        private void read(int partition_id, long object_id, string server_id)
        {
            DataStoreValue result;

            var key = new DataStoreKey()
            {
                object_id = object_id,
                partition_id = partition_id
            };

            //reply = attached_server.read()
            //if (reply.ok)
            //{
            //    result = reply.value
            //} 
            //else
            //{
            //    reply = server_id.read()
            //    if reply.ok
            //        result = reply.value
            //    else
            //        result = "N/A"
            //}
            
            // Console.WriteLine(result.val);
        }

        private void write(string partition_id, string object_id, string value)
        {

        }

        private void listServer(string server_id)
        {

        }

        private void listGlobal()
        {

        }

        private void wait(int x)
        {

        }






        private void deatachServer(int x)
        {

        }

        private void atachServer(int x)
        {

        }
    }
}
