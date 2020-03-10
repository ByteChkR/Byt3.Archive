using System;
using System.IO;
using CommandRunner;

namespace Byt3.Archive.CLI.Commands
{
    public class ExtractCommand : AbstractCommand
    {
        private static string[] Keys => new[] { "--extract", "-e" };
        private static string HelpText => "--extract <Path/To/Archive> <Path/To/TargetFolder>\n\tThe Target folder has to exist.";
        public ExtractCommand() : base(Extract, Keys, HelpText, true) { }

        private static void Extract(StartupInfo info, string[] args)
        {
            string path = args[0];
            bool create = !File.Exists(path);
            Archiver a = new Archiver(path, create ? ArchiveOpenMode.CREATE : ArchiveOpenMode.OPEN);
            a.Extract(Path.GetFullPath(args[1]));
            a.Dispose();
        }
    }
}