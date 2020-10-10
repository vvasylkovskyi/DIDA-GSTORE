using System.Text;

namespace Shared
{
    public static class Program
    {
        static void Main(string[] args)
        {

        }

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
