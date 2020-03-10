using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class CreateSFXFromDirectoryCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--sfx-from-directory", "-sfxD" };
        private static string HelpText => "--sfx-from-directory <Path/To/Directory> <Path/To/OutputFile> (<StartCommand> <StartCommandArg0> <...>)\n\tCreates a Self extracting application with the contents of the specified folder. If a startup command is specified, the sfx will directly extract into a temp dir and start the command.";
        public CreateSFXFromDirectoryCommand() : base(CreateSFXFromDirectory, Keys, HelpText, false) { }

        private static void CreateSFXFromDirectory(StartupInfo info, string[] args)
        {
            bool compress = info.GetCommandEntries("--compression") != 0;
            string cmd = "";
            for (int i = 2; i < args.Length; i++)
            {
                cmd += " " + args[i];
            }
            Archiver.CreateSFXArchiveFromFolder(args[0], compress, args[1], cmd.Trim());

        }

    }
}