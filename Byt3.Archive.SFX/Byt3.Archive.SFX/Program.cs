using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Byt3.Archive.SFX
{
    static class Program
    {

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            TextReader tr = new StreamReader(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Byt3.Archive.SFX.content.cmd.txt"));
            string cmd = tr.ReadToEnd();
            tr.Close();
            if (cmd != "")
            {
                string dir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
                Directory.CreateDirectory(dir);
                ExtractContent(dir);
                try
                {
                    Console.WriteLine("Command: " + cmd);
                    string exec = cmd.Split(' ')[0];
                    string args = cmd.Remove(0, Math.Min(exec.Length + 1, cmd.Length));
                    Console.WriteLine("Running Command: " + exec);
                    Console.WriteLine("Running Arguments: " + args);
                    ProcessStartInfo info = new ProcessStartInfo(exec, args);
                    info.WorkingDirectory = dir;
                    Process p = Process.Start(info);
                    Console.WriteLine("Do not close this window. It will close automatically.");
                    while (!p.HasExited)
                    {

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Thread.Sleep(1500);
                Directory.Delete(dir, true);
                return;
            }

            ShowWindow(GetConsoleWindow(), SW_HIDE);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void ExtractContent(string path)
        {
            if (!Directory.Exists(path)) return;
            string[] packs = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(x => x.StartsWith("Byt3.Archive.SFX.content.archive.arch")).ToArray();
            Console.WriteLine();
            for (int i = 0; i < packs.Length; i++)
            {
                Console.WriteLine($"Extracting Package: {packs[i]}");
                Stream s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(packs[i]);

                bool c = false;
                string fileName = Path.GetFileName(packs[i]);
                string[] fileNameParts = fileName.Split('.');
                for (int j = fileNameParts.Length - 2; j < fileNameParts.Length; j++)
                {
                    c |= fileNameParts[j] == "archc";
                }
                Archive.Archiver a = new Archive.Archiver(s, c);
                a.Extract(path);
                a.Dispose();
            }

        }
    }
}
