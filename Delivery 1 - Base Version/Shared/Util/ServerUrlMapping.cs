using System;
using System.Collections.Generic;

namespace Shared.Util
{
    class ServerUrlMapping
    {
        // This dictionary stores the mapping between server id and url

        public static Dictionary<string, string> serverUrlMapping = new Dictionary<string, string>()
        {
            { "server1", "http://localhost:9081"},
            { "server2", "http://localhost:9082"}
        };

        public static string getServerUrl(string server_id)
        {
            return serverUrlMapping[server_id];
        }
    }
}
