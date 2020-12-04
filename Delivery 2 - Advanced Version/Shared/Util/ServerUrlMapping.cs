using System.Collections.Generic;

namespace Shared.Util
{
    class ServerUrlMapping
    {
        // This dictionary stores the mapping between server id and url

        public static Dictionary<string, string> serverUrlMapping = new Dictionary<string, string>();

        public static void CreateServerUrlMapping(Dictionary<string, string> serverUrlMapping)
        {
            ServerUrlMapping.serverUrlMapping = serverUrlMapping;
        }

        public static void RemoveCrashedServer(string crashedServerId)
        {
            serverUrlMapping.Remove(crashedServerId);
        }

        public static void AddServerToServerUrlMapping(string serverId, string serverUrl)
        {
            if(serverUrlMapping.Count == 0)
            {
                serverUrlMapping.Add(serverId, serverUrl);
            }
            else
            {
                if (!serverUrlMapping.ContainsKey(serverId))
                {
                    serverUrlMapping[serverId] = serverUrl;
                }
                else
                {
                    serverUrlMapping.Add(serverId, serverUrl);
                }
            }
        }

        public static string GetServerUrl(string server_id)
        {
            return serverUrlMapping[server_id];
        }
    }
}
