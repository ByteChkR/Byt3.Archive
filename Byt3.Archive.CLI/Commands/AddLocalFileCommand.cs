using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class AddLocalFileCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--add-file", "-a" };
        private static string HelpText => "--add-file <Path/To/OutputArchive> <Path/To/File/To/Add> <Archive/Target/Path>\n\tAdds a file to an archive. Creates the archive if not existing.";
        public AddLocalFileCommand() : base(AddLocalFile, Keys, HelpText, false) { }

        private static void AddLocalFile(StartupInfo info, string[] args)
        {
            string path = args[0];
            ArchiveOpenMode mode =
                File.Exists(path) ? ArchiveOpenMode.OPEN : ArchiveOpenMode.CREATE;
            Archiver a = new Archiver(path, mode);
            a.AddLocal(args[1], args[2]);
            a.Dispose(true);
        }
    }
}