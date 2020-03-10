using System;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class CompressionCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--compression", "-c" };
        private static string HelpText => "--compression\n\tFlag that can be set when using the --sfx-from-directory command.";
        public CompressionCommand() : base(Compression, Keys, HelpText, false) { }

        private static void Compression(StartupInfo info, string[] args)
        {
            Console.WriteLine("Compression flag Set.");
        }
    }
}