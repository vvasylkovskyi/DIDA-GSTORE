using System;
using System.Text;

namespace Shared.Util
{
    public static class Utilities
    {
        public static string BuildArgumentsString(string[] args)
        {
            StringBuilder stringbuilder = new StringBuilder();
            foreach (string argument in args)
            {
                stringbuilder.Append(argument);
                stringbuilder.Append(' ');
            }
            return stringbuilder.ToString();
        }

        public static string[] BuildArgsArrayFromArgsString(string argsString)
        {
            string[] args = argsString.Split(' ');
            return args;
        }

        public static string getHostNameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            string hostName = uri.DnsSafeHost;
            return hostName;
        }

        public static string getPortFromUrl(string url)
        {
            Uri uri = new Uri(url);
            string port = uri.Port.ToString();
            return port;
        }
    }
}
