using System;
using System.Text;
using System.Threading.Tasks;

namespace Talking_ben_bot
{
    public class Program
    {
        private static string[] _args;
        private static async Task Main(string[] args)
        {
            _args = args;

            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            var token = GetArg("-token");
            var ben = new Ben(token);

            await ben.Start();
            await Task.Delay(-1);
        }

        private static string GetArg(string argName)
        {
            var argIndex = Array.IndexOf(_args, argName);
            var argValueIndex = argIndex + 1;
            if (argIndex == -1 || _args.Length <= argValueIndex)
                return null;

            return _args[argValueIndex];
        }
    }
}
