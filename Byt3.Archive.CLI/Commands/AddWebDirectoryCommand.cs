using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class AddWebDirectoryCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--add-web-directory", "-aWD" };
        private static string HelpText => "--add-web-directory <Path/To/OutputFile> <http://url/to/folder> <LocalPathOfTheDirectory> optional:<Archive/Target/Path>\n\tAdds a web directory to an archive. Creates the archive if not existing.";
        public AddWebDirectoryCommand() : base(AddWebDirectory, Keys, HelpText, false) { }

        private static void AddWebDirectory(StartupInfo info, string[] args)
        {
            string path = args[0];
            ArchiveOpenMode mode =
                File.Exists(path) ? ArchiveOpenMode.OPEN : ArchiveOpenMode.CREATE;
            Archiver a = new Archiver(path, mode);
            string target = args.Length > 3 ? args[3] : "";
            a.AddWebFolder(args[1], args[2], target);
            a.Dispose(true);
        }
    }
}