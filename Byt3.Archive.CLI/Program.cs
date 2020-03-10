using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Byt3.Archive;
using System.Threading.Tasks;
using CommandRunner;

namespace Byt3.Archive.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string msbpathfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msbuildpath.txt");
            if (File.Exists(msbpathfile))
            {
                string msbPath = File.ReadAllText(msbpathfile);
                Console.WriteLine("MSBuild Path File: " + msbPath);
                Archiver.SetMSBuildExecutablePath(msbPath);
            }


            Stopwatch sw = Stopwatch.StartNew();
            Runner.AddAssembly(Assembly.GetExecutingAssembly());
            Runner.RunCommands(args);
            Runner.RemoveAllCommands();


            //if (args.Length >= 3)
            //{
            //    string path = Path.GetFullPath(args[1]);
            //    ArchiveOpenMode mode =
            //        File.Exists(path) ? ArchiveOpenMode.OPEN : ArchiveOpenMode.CREATE;
            //    if (args[0] == "-a"|| args[0] == "-ac")
            //    {
            //        bool compress = args[0] == "-ac";
            //        Archiver a = new Archiver(path, mode);
            //        a.AddLocal(args[1], args[2]);
            //        a.Dispose(true,compress);
            //    }
            //    else if (args[0] == "-aD"|| args[0] == "-aDc")
            //    {
            //        bool compress = args[0] == "-aDc";
            //        Archiver a = new Archiver(path, mode);
            //        string target = args.Length > 3 ? args[3] : "";
            //        a.AddFolder(Path.GetFullPath(args[2]), target);
            //        a.Dispose(true, compress);
            //    }
            //    else if (args[0] == "-aW"|| args[0] == "-aWc")
            //    {
            //        bool compress = args[0] == "-aWc";
            //        Archiver a = new Archiver(path, mode);
            //        a.AddWeb(args[1], args[2]);
            //        a.Dispose(true, compress);
            //    }
            //    else if (args[0] == "-e")
            //    {
            //        Archiver a = new Archiver(path, mode);
            //        a.Extract(Path.GetFullPath(args[2]));
            //        a.Dispose();
            //    }
            //    else if (args[0] == "-sfxD" || args[0] == "-sfxDc")
            //    {
            //        bool compress = args[0] == "-sfxDc";
            //        string cmd = "";
            //        for (int i = 3; i < args.Length; i++)
            //        {
            //            cmd += " " + args[i];
            //        }
            //        Archiver.CreateSFXArchiveFromFolder(args[1], compress, args[2], cmd.Trim());
            //    }
            //    else if (args[0] == "-sfx" || args[0] == "-sfxc")
            //    {
            //        bool compress =   args[0] == "-sfxc";
            //        string cmd = "";
            //        for (int i = 3; i < args.Length; i++)
            //        {
            //            cmd += " " + args[i];
            //        }
            //        Archiver.CreateSFXArchive(args[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), compress, args[2], cmd.Trim());
            //    }
            //    else if (args.Length >= 4)
            //    {
            //        if (args[0] == "-aWD" || args[0] == "-aWDc")
            //        {
            //            bool compress = args[0] == "-aWDc";
            //            Archiver a = new Archiver(path, mode);
            //            string target = args.Length > 4 ? args[4] : "";
            //            a.AddWebFolder(args[2], args[3], target);
            //            a.Dispose(true, compress);
            //        }
            //    }
            //}


            Console.WriteLine("Finished!");
            Console.WriteLine($"Milliseconds: " + sw.ElapsedMilliseconds);
#if DEBUG
            Console.Read();
#endif
        }
    }
}
