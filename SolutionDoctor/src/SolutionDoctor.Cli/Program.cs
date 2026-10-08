using System;
using System.Text;

namespace SolutionDoctor.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            return CliRunner.Run(args, Console.Out, Console.Error);
        }
    }
}
