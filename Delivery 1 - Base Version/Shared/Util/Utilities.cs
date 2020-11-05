using System.Text;

namespace Shared.Util
{
    public static class Utilities
    {
        public static string BuildArgumentsString(params string[] args)
        {
            StringBuilder strinbuilder = new StringBuilder();
            foreach (string argument in args)
            {
                strinbuilder.Append(argument);
                strinbuilder.Append(' ');
            }
            return strinbuilder.ToString();
        }
    }
}
