using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Byt3.Archive
{
    /// <summary>
    /// Archiver Class that funtions as an API for the Archive System.
    /// </summary>
    public class Archiver : IDisposable
    {
        //Archive Data
        private readonly Stream _archiveStream;
        private readonly ArchiveHeader _archiveHeader;

        //Global Temp Path that this implementation uses
        private static readonly string TempPath =
            Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));

        //Unique Temp File per instance of the Archiver
        private readonly string TempFile = Path.Combine(TempPath, Path.GetTempFileName());

        //The Original File path(when saving it will write it to this file
        private readonly string SourceFile;

        //Hack for getting the MSBUILD executable.
        private static string MSBUILD_PATH = @"D:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe";

        //When compiling a Self Extracting Archive, it will split a directory in multiple chunks to prevent the msbuild engine to run out of memory
        //This happens when a embedded file is too big.
        private const int MAX_SFX_ARCHIVE_SIZE_MB = 128;

        //Helper const, that converts the MAX_SFX_ARCHIVE_SIZE_MB into bytes
        private const int MAX_SFX_ARCHIVE_SIZE_B = MAX_SFX_ARCHIVE_SIZE_MB * 1024 * 1024;

        #region Constructor
        /// <summary>
        /// Creates an Archiver Instance from a stream
        /// </summary>
        /// <param name="archiveStream">The stream to be read from</param>
        /// <param name="compression">Does the stream contain compressed data?</param>
        public Archiver(Stream archiveStream, bool compression)
        {
            SourceFile = null;
            _archiveStream = HandleArchiveCompression(archiveStream, compression);
            _archiveHeader = ReadArchiveHeader();
        }

        /// <summary>
        /// Creates an Archiver instance from a file and a read mode
        /// </summary>
        /// <param name="path">The path to open</param>
        /// <param name="mode">The Open Mode</param>
        public Archiver(string path, ArchiveOpenMode mode)
        {
            SourceFile = path;
            Console.WriteLine($"Created Archiver with path: {path}");

            _archiveStream = GetStream(path, mode);

            _archiveHeader = mode == ArchiveOpenMode.CREATE
                ? new ArchiveHeader()
                : ReadArchiveHeader();
            //Read Header
        }
        #endregion

        #region Archive Initialization

        /// <summary>
        /// Function that Reads the Archive Header from the _archiveStream
        /// </summary>
        /// <returns>The Parsed Archive Header</returns>
        private ArchiveHeader ReadArchiveHeader()
        {

            Console.WriteLine("Parsing Archive Header");
            int pD = (int)_archiveStream.Length - 1 - sizeof(int);
            _archiveStream.Position = pD;
            byte[] pdBlock = new byte[sizeof(int)];
            _archiveStream.Read(pdBlock, 0, sizeof(int));
            int pDelta = BitConverter.ToInt32(pdBlock, 0);
            _archiveStream.Position = pD - pDelta;
            return ArchiveHeader.Deserialize(_archiveStream);
        }

        /// <summary>
        /// Function that will return a stream that is readable for the Archive Implementation
        /// The Decompression Step is happening here, also the File to be opened is copied to a temp file before trying to open it.
        /// This allows to keep the original file until the very end.
        /// </summary>
        /// <param name="s">The Stream</param>
        /// <param name="compression">Does the Stream </param>
        /// <returns>Uncompressed/Save to edit stream</returns>
        private Stream HandleArchiveCompression(Stream s, bool compression)
        {

            if (compression)
            {
                GZipStream zip = new GZipStream(s, CompressionMode.Decompress);
                Stream tmp = File.Open(TempFile, FileMode.Create);
                zip.CopyTo(tmp);
                zip.Close();
                s.Close();
                tmp.Position = 0;
                return tmp;
            }
            else
            {
                Stream tmp = File.Open(TempFile, FileMode.Create);
                s.CopyTo(tmp);
                s.Close();
                tmp.Position = 0;
                return tmp;
            }
        }

        /// <summary>
        /// Returns a Stream that is readable for the Archive Implementation.
        /// This contains the logic that works out if the archive is compressed
        /// </summary>
        /// <param name="path">Path of the File</param>
        /// <param name="mode">The Open mode</param>
        /// <returns></returns>
        private Stream GetStream(string path, ArchiveOpenMode mode)
        {
            if (mode == ArchiveOpenMode.CREATE)
            {
                return File.Open(TempFile, FileMode.Create);
            }

            string s = Path.GetFileName(path);
            bool c = IsCompressed(s);

            return HandleArchiveCompression(File.Open(path, FileMode.Open), c);
        }

        private static bool IsCompressed(string path)
        {
            bool c = false;
            string[] ss = path.Split('.');
            for (int i = ss.Length - 2; i < ss.Length; i++)
            {
                c |= ss[i] == "archc";
            }

            return c;
        }


        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the Archiver without saving the changes made.
        /// </summary>
        public void Dispose()
        {
            Dispose(false, false);
        }

        public void Dispose(bool saveArchiveHeader)
        {
            Dispose(saveArchiveHeader, IsCompressed(SourceFile));
        }

        /// <summary>
        /// Disposes the Archiver with saving the archive header at the end of the archive. and optionally applying compression
        /// </summary>
        /// <param name="saveArchiveHeader"></param>
        /// <param name="compression"></param>
        public void Dispose(bool saveArchiveHeader, bool compression)
        {
            if (saveArchiveHeader)
            {
                Console.WriteLine("Saving Archive Header");
                _archiveStream.Position = _archiveHeader.LastEnd + 1;
                int bytesWritten = _archiveHeader.Serialize(_archiveStream);
                _archiveStream.Write(BitConverter.GetBytes(bytesWritten), 0, sizeof(int));
                _archiveStream.SetLength(_archiveStream.Position + 1);
            }
            if (compression && SourceFile != null)
            {
                File.Delete(SourceFile);
                Stream s = File.Open(SourceFile, FileMode.Create);
                GZipStream zip = new GZipStream(s, CompressionLevel.Optimal);
                _archiveStream.Position = 0;
                _archiveStream.CopyTo(zip);
                zip.Close();
                s.Close();
                _archiveStream.Close();
                return;
            }
            _archiveStream.Close();
            if (SourceFile != null)
            {
                File.Delete(SourceFile);
                File.Move(TempFile, SourceFile);
            }
            if (File.Exists(TempFile)) File.Delete(TempFile);
        }


        #endregion

        #region AddImplementation

        /// <summary>
        /// Adds a Web Resource that will be downloaded when extracting.
        /// </summary>
        /// <param name="url">URL of the Web Resource to be used.</param>
        /// <param name="archiveFile">The Archive Path where the WebResource should be stored.</param>
        public void AddWeb(string url, string archiveFile)
        {
            Console.WriteLine($"Adding Web Node: {url} to {archiveFile}");
            byte[] b = Encoding.UTF8.GetBytes(url);
            int startIdx = _archiveHeader.CreateDataNode(ConvertToArchivePath(archiveFile), b.Length, ArchiveHeader.ArchiveNodeType.Web);
            _archiveStream.Position = startIdx;
            _archiveStream.Write(b, 0, b.Length);
        }

        /// <summary>
        /// Adds a Directory of Files as WebResources.
        /// </summary>
        /// <param name="url">The URL to the Folder you want to add</param>
        /// <param name="localFolder">A copy of the folder with the File structure the same as the webresource.</param>
        /// <param name="archiveFolder">The Target Folder in the archive where the webfolder will be added.</param>
        public void AddWebFolder(string url, string localFolder, string archiveFolder)
        {
            string folder = Path.GetFullPath(localFolder);
            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativeFilePath = files[i].Replace(folder, "");
                string archivePath = ConvertToArchivePath(Path.Combine(archiveFolder, relativeFilePath.Remove(0, 1)));

                string fileUrl = url + relativeFilePath.Remove(0, 1).Replace('\\', '/');
                AddWeb(fileUrl, archivePath);
            }
        }

        /// <summary>
        /// Adds a File to the archive
        /// </summary>
        /// <param name="data">The Data that the file should contain</param>
        /// <param name="archiveFile">The Archive Path of the File</param>
        public void AddLocal(byte[] data, string archiveFile)
        {
            int startIdx = _archiveHeader.CreateDataNode(ConvertToArchivePath(archiveFile), data.Length);
            _archiveStream.Position = startIdx;
            _archiveStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Adds a File to the Archive
        /// </summary>
        /// <param name="file">The File you want to add.</param>
        /// <param name="archiveFile">The Archive Path of the File</param>
        public void AddLocal(string file, string archiveFile)
        {
            AddLocal(File.ReadAllBytes(file), archiveFile);
        }

        /// <summary>
        /// Adds a File to the Archives Root Directory
        /// </summary>
        /// <param name="file">The File you want to add.</param>
        public void AddLocal(string file)
        {
            AddLocal(file, Path.GetFileName(file));
        }

        /// <summary>
        /// Adds a Folder to the Archive at a specified location
        /// </summary>
        /// <param name="folder">The File you want to add.</param>
        /// <param name="targetFolder">The Archive Folder that will contain the Folder Structure/Files</param>
        public void AddFolder(string folder, string targetFolder = "")
        {
            string dir = Path.GetFullPath(folder);
            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            Console.WriteLine($"Adding Folder({files.Length} Files)");
            string[] archiveFiles = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                archiveFiles[i] = ConvertToArchivePath(targetFolder + files[i].Replace(dir, ""));
            }

            for (int i = 0; i < files.Length; i++)
            {
                if (files.Length < 100)
                {
                    Console.WriteLine($"Packing file:" + files[i]);
                }
                else if (i % 50 == 0)
                {
                    Console.WriteLine($"Packing files.. {i / (float)files.Length * 100}%");
                }
                AddLocal(files[i], archiveFiles[i]);
            }
        }

        #endregion

        #region ReadImplementation

        /// <summary>
        /// Reads a File from the archive
        /// </summary>
        /// <param name="archiveFile">Qualified path of the ArchiveFile</param>
        /// <returns>The bytes that were stored.</returns>
        public byte[] ReadFile(string archiveFile)
        {
            int start = _archiveHeader.GetNodeDataStart(ConvertToArchivePath(archiveFile));
            int length = _archiveHeader.GetNodeDataEnd(ConvertToArchivePath(archiveFile)) - start;
            byte[] b = new byte[length];
            _archiveStream.Position = start;
            _archiveStream.Read(b, 0, b.Length);

            if (_archiveHeader.GetNodeType(ConvertToArchivePath(archiveFile)) == ArchiveHeader.ArchiveNodeType.Local)
            {
                return b;
            }

            WebClient wc = new WebClient();
            Console.WriteLine($"Downloading Web Node: {Encoding.UTF8.GetString(b)}");
            byte[] ret = wc.DownloadData(new Uri(Encoding.UTF8.GetString(b)));
            wc.Dispose();
            return ret;

        }

        /// <summary>
        /// Extracts the Archive in a Folder.
        /// </summary>
        /// <param name="folder">The folder the archive should be extracted to.</param>
        public void Extract(string folder)
        {
            string fldr = Path.GetFullPath(folder);
            Console.WriteLine($"Extracting archive to folder: {fldr}");
            List<string> dirs = _archiveHeader.GetAllFolders().ToList();
            dirs.Sort(new StringLengthComparer());
            for (int i = 0; i < dirs.Count; i++)
            {
                string pp = ConvertToPath(dirs[i]);
                string p = Path.Combine(fldr, pp);
                if (!Directory.Exists(p))
                {
                    Directory.CreateDirectory(p);
                }
            }

            Console.WriteLine($"Extracting files...");
            string[] files = _archiveHeader.GetChildren("", true);
            for (int i = 0; i < files.Length; i++)
            {
                if (files.Length < 100)
                {
                    Console.WriteLine($"Extracting file:" + files[i]);
                }
                else if (i % 50 == 0)
                {
                    Console.WriteLine($"Extracting files.. {i / (float)files.Length * 100}%");
                }
                string p = Path.Combine(fldr, ConvertToPath(files[i]));
                File.WriteAllBytes(p, ReadFile(files[i]));
            }
        }


        #endregion

        #region Queries

        /// <summary>
        /// Returns All Children of the specified archiveFile
        /// </summary>
        /// <param name="archiveFile">The Archive Path that is going to be searched</param>
        /// <param name="recursive">When true it will recurse through each sub directory</param>
        /// <returns></returns>
        public string[] GetChildren(string archiveFile, bool recursive)
        {
            return _archiveHeader.GetChildren(ConvertToArchivePath(archiveFile), recursive).Select(ConvertToPath).ToArray();
        }


        #endregion


        /// <summary>
        /// Helper Class that is Changing the OS Default Path Notation(WIN: .\File UNIX: ./File) to the internal Notation: .|File
        /// </summary>
        /// <param name="path">The Path to Convert</param>
        /// <returns>The Converted Path</returns>
        private string ConvertToArchivePath(string path)
        {
            string s = path.Replace(ArchiveHeader.PATH_SEPARATOR, ArchiveHeader.INTERNAL_SEPARATOR).Replace(ArchiveHeader.ALT_PATH_SEPARATOR, ArchiveHeader.INTERNAL_SEPARATOR);
            if (!s.StartsWith("" + ArchiveHeader.INTERNAL_SEPARATOR)) s = ArchiveHeader.INTERNAL_SEPARATOR + s;
            return s;
        }
        /// <summary>
        /// Helper Class that is Changing the internal Notation: .|File to the OS Default Path Notation(WIN: .\File UNIX: ./File)
        /// </summary>
        /// <param name="path">The Path to Convert</param>
        /// <returns>The Converted Path</returns>
        private string ConvertToPath(string path) =>
            path.Remove(0, 1).Replace(ArchiveHeader.INTERNAL_SEPARATOR, ArchiveHeader.PATH_SEPARATOR);






        #region SFXArchiveCreation



        /// <summary>
        /// IInitializes the SFX Creation Process.
        /// </summary>
        /// <returns>The directory that the Source is unpacked to.</returns>
        private static string InitializeSFXSource()
        {
#if DEBUG
            string path = Path.GetFullPath(".\\sfx\\");
#else
            string path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName())) + "\\";
#endif
            Directory.CreateDirectory(path);
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Byt3.Archive.content.sfx-src.arch");
            if (s == null) throw new Exception("This Version of Byt3.Archive was compiled without SFX Source.");
            Archiver a = new Archiver(s, false);
            a.Extract(path);
            a.Dispose();
            return path;
        }

        #region CSPROJ Automated Resource Embedding

        /// <summary>
        /// Packages a Folder with a maximum size for the archive files.
        /// </summary>
        /// <param name="folderPath">Folder to Archive</param>
        /// <param name="compression">Use Compression?</param>
        /// <returns>All archives paths that were created</returns>
        private static string[] PackageSplitted(string folderPath, bool compression)
        {
            List<string> ArchivePaths = new List<string>();

            int cur = 0;
            string tmpDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
            Directory.CreateDirectory(tmpDir);
            string[] files = Directory.GetFiles(Path.GetFullPath(folderPath), "*", SearchOption.AllDirectories);
            Archiver a = null;
            for (int i = 0; i < files.Length; i++)
            {
                if (a == null || a._archiveHeader.LastEnd > MAX_SFX_ARCHIVE_SIZE_B)
                {
                    string f = Path.Combine(tmpDir, "sfxarchive.arch." + cur);
                    a?.Dispose(true, compression);
                    a = new Archiver(f, ArchiveOpenMode.CREATE);
                    ArchivePaths.Add(f);
                    cur++;
                }
                a.AddLocal(files[i], files[i].Replace(Path.GetFullPath(folderPath), ""));
            }
            a.Dispose(true, compression);

            return ArchivePaths.ToArray();
        }

        /// <summary>
        /// Embeds a List of files into a .csproj file.
        /// </summary>
        /// <param name="csFile">The .csproj File to edit</param>
        /// <param name="fileList">Files to embed into the project</param>
        public static void EmbedFilesIntoProject(string csFile, string[] fileList)
        {
            List<string> f = new List<string>();
            for (int i = 0; i < fileList.Length; i++)
            {
                f.Add(fileList[i].Replace("/", "\\"));
            }

            XmlDocument doc = new XmlDocument();
            string filename = csFile + ".backup";
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            File.Copy(csFile, filename);

            doc.Load(csFile);
            XmlNode n = FindTag(doc);

            Uri pdir = new Uri(Path.GetDirectoryName(csFile) + "\\content");
            for (int i = 0; i < f.Count; i++)
            {
                Uri furi = new Uri(f[i]);
                string cont = pdir.MakeRelativeUri(furi).ToString();
                string entry = GenerateFileEntry(cont);

                Console.WriteLine("Adding File to csproj File: " + cont);
                if (!n.InnerXml.Contains(entry))
                {
                    n.InnerXml += "\n" + entry;
                }
            }

            File.Delete(csFile);
            doc.Save(csFile);
        }

        /// <summary>
        /// Finds the Right Tag that is used to embed files.
        /// </summary>
        /// <param name="doc">The document to be searched.</param>
        /// <returns>A valid ItemGroup xml node where files can be embedded.</returns>
        private static XmlNode FindTag(XmlDocument doc)
        {
            XmlNode s = doc.FirstChild.Name == "xml" ? doc.ChildNodes[1] : doc.FirstChild;

            for (int i = 0; i < s.ChildNodes.Count; i++)
            {
                if (s.ChildNodes[i].Name == "ItemGroup")
                {
                    if (s.ChildNodes[i].HasChildNodes && s.ChildNodes[i].FirstChild.Name == "EmbeddedResource")
                    {
                        s.ChildNodes[i].InnerXml = "";
                        return s.ChildNodes[i];
                    }
                }
            }

            XmlNode n = doc.CreateNode(XmlNodeType.Element, "ItemGroup", "");
            s.AppendChild(n);

            return n;
        }

        /// <summary>
        /// Returns the XML File entry to insert
        /// </summary>
        /// <param name="filepath">File to embed into the CSProject file.</param>
        /// <returns></returns>
        private static string GenerateFileEntry(string filepath)
        {
            return "  <EmbeddedResource Include=\"" + filepath.Replace("/", "\\") + "\" />";
        }

        #endregion


        public static void SetMSBuildExecutablePath(string path)
        {
            if (File.Exists(path) && path.ToLower().EndsWith("msbuild.exe"))
                MSBUILD_PATH = path;
        }

        /// <summary>
        /// Creates a Self Extracting Archive from a folder.
        /// </summary>
        /// <param name="folder">The Folder to Archive</param>
        /// <param name="compression">Compression?</param>
        /// <param name="SFXPath">Output File</param>
        /// <param name="autoStartCommand">The Command that is executed when unpacked(empty = ExtratTargetDialog, command = ExtractToTemp and Execute)</param>
        public static void CreateSFXArchiveFromFolder(string folder, bool compression, string SFXPath, string autoStartCommand)
        {


            string[] packs = PackageSplitted(folder, compression);
            CreateSFXArchive(packs, SFXPath, autoStartCommand);

            if (packs.Length > 0)
                Directory.Delete(Path.GetDirectoryName(Path.GetFullPath(packs[0])));
        }

        /// <summary>
        /// Creates a Self Extracting Archive from a List of Archives
        /// </summary>
        /// <param name="archivePaths">The Archives to embed into an SFX App</param>
        /// <param name="compression">Uses Compression on the archives?</param>
        /// <param name="SFXPath">The Output File</param>
        /// <param name="autoStartCommand">The Command that is executed when unpacked(empty = ExtratTargetDialog, command = ExtractToTemp and Execute)</param>
        public static void CreateSFXArchive(string[] archivePaths, string SFXPath, string autoStartCommand)
        {

            string sfxSrcPath = InitializeSFXSource();
            bool compression = IsCompressed(archivePaths[0]);
            string csFolder = Path.Combine(sfxSrcPath, "Byt3.Archive.SFX");
            string csFile = Path.Combine(csFolder, "Byt3.Archive.SFX.csproj");
            string dstArchPath = Path.Combine(csFolder, "content", compression ? "archive.archc" : "archive.arch");
            string dstCmd = Path.Combine(csFolder, "content", "cmd.txt");
            string format = "{0:D" + archivePaths.Length.ToString().Length + "}";
            string[] archPaths = new string[archivePaths.Length + 1];
            for (int i = 0; i < archivePaths.Length; i++)
            {
                string dst = dstArchPath + "." + string.Format(format, i);
                archPaths[i] = dst;
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(archivePaths[i], dst);
            }

            archPaths[archPaths.Length - 1] = dstCmd;
            EmbedFilesIntoProject(csFile, archPaths);
            Console.WriteLine("Start Command: " + autoStartCommand);
            File.WriteAllText(dstCmd, autoStartCommand);

            ProcessStartInfo info = new ProcessStartInfo(MSBUILD_PATH, @"Byt3.Archive.SFX.sln /t:Rebuild /p:Configuration=Release");
            info.WorkingDirectory = sfxSrcPath;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            Process p = Process.Start(info);
            while (!p.HasExited)
            {
                Console.WriteLine((p.StandardOutput as TextReader).ReadLine());
            }
            Console.Write((p.StandardOutput as TextReader).ReadToEnd());
            File.Copy(Path.Combine(sfxSrcPath, "Byt3.Archive.SFX", "bin", "Release", "Byt3.Archive.SFX.exe"), SFXPath, true);
            Directory.Delete(sfxSrcPath, true);
        }



        #endregion


    }
}