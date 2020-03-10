using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class AddWebFileCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--add-web-file", "-aW" };
        private static string HelpText => "--add-web-file <Path/To/OutputArchive> <http://url/to/file> <Archive/Target/Path>\n\tAdds a web file to an archive. Creates the archive if not existing.";
        public AddWebFileCommand() : base(AddWebFile, Keys, HelpText, false) { }

        private static void AddWebFile(StartupInfo info, string[] args)
        {
            string path = args[0];
            ArchiveOpenMode mode =
                File.Exists(path) ? ArchiveOpenMode.OPEN : ArchiveOpenMode.CREATE;
            Archiver a = new Archiver(path, mode);
            a.AddWeb(args[1], args[2]);
            a.Dispose(true);
        }
    }
}