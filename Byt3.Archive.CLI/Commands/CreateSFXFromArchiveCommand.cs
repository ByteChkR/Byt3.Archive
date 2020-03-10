using System;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class CreateSFXFromArchiveCommand : AbstractCommand
    {

        private static string[] Keys => new[] { "--sfx-from-archive", "-sfx" };
        private static string HelpText => "--sfx-from-archive <Path/To/Archive> <Path/To/OutputFile> (<StartCommand> <StartCommandArg0> <...>)\n\tCreates a Self extracting application with the contents of the specified archive. If a startup command is specified, the sfx will directly extract into a temp dir and start the command.";
        public CreateSFXFromArchiveCommand() : base(CreateSFXFromArchive, Keys, HelpText, false) { }

        private static void CreateSFXFromArchive(StartupInfo info, string[] args)
        {
            string cmd = "";
            for (int i = 2; i < args.Length; i++)
            {
                cmd += " " + args[i];
            }
            Archiver.CreateSFXArchive(args[0].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), args[1], cmd.Trim());

        }

    }
}