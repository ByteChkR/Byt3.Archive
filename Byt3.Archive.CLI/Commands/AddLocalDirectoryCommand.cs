using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class AddLocalDirectoryCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--add-directory", "-aD" };
        private static string HelpText => "--add-directory <Path/To/OutputArchive> <Path/To/Directory/To/Add> optional:<Archive/Target/Path>\n\tAdds a directory to an archive. Creates the archive if not existing.";
        public AddLocalDirectoryCommand() : base(AddLocalDirectory, Keys, HelpText, false) { }

        private static void AddLocalDirectory(StartupInfo info, string[] args)
        {
            string path = args[0];
            ArchiveOpenMode mode =
                File.Exists(path) ? ArchiveOpenMode.OPEN : ArchiveOpenMode.CREATE;
            Archiver a = new Archiver(path, mode);
            string target = args.Length > 2 ? args[2] : "";
            a.AddFolder(Path.GetFullPath(args[1]), target);
            a.Dispose(true);
        }
    }
}