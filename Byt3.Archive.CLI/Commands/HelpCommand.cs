using System;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class HelpCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--help", "-h", "-?" };
        private static string HelpText => "--help\n\tDisplays this help message.";
        public HelpCommand() : base(Help, Keys, HelpText, false) { }

        private static void Help(StartupInfo info, string[] args)
        {
            for (int i = 0; i < Runner.CommandCount; i++)
            {
                Console.WriteLine(Runner.GetCommandAt(i));
            }
        }
    }
}