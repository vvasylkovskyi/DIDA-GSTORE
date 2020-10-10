using System;
using Shared;

namespace server
{
    class Program : IServerClientCommands
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        public void getStatus()
        {
            Console.WriteLine(">>> Status");
        }
    }
}
