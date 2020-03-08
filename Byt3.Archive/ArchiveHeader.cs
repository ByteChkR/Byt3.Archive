using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Byt3.Archive
{
    internal class ArchiveHeader
    {
        public const char PATH_SEPARATOR = '/';
        public const char ALT_PATH_SEPARATOR = '\\';
        public const char INTERNAL_SEPARATOR = '|';
        public enum ArchiveNodeType { Local, Web }
        private List<ArchiveNode> Nodes;
        internal int LastEnd => Nodes.Count == 0 ? -1 : Nodes.Max(x => x.End);


        internal ArchiveHeader(List<ArchiveNode> nodes)
        {
            Nodes = nodes;
        }

        internal ArchiveHeader()
        {
            Nodes = new List<ArchiveNode>();
        }


        internal ArchiveNodeType GetNodeType(string name)
        {
            return GetNode(name).NodeType;
        }

        private ArchiveNode GetNode(string name)
        {
            return Nodes.FirstOrDefault(x => x.QualifiedName == name);
        }

        internal int GetNodeDataStart(string name)
        {
            ArchiveNode node = GetNode(name);
            if (node == null) return 0;
            return node.Start;
        }

        internal int GetNodeDataEnd(string name)
        {
            ArchiveNode node = GetNode(name);
            if (node == null) return 0;
            return node.End;
        }

        internal string[] GetAllFolders()
        {
            List<string> folder = new List<string>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                string dir = Nodes[i].Parent;
                while (dir != "")
                {
                    if (!folder.Contains(dir))
                    {
                        folder.Add(dir);
                        dir = ArchiveNode.GetParent(dir);
                    }
                    else break;
                }
            }

            return folder.ToArray();
        }

        internal string[] GetChildren(string folder, bool recursive)
        {
            List<ArchiveNode> children = new List<ArchiveNode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].QualifiedName.StartsWith(folder))
                {
                    children.Add(Nodes[i]);
                }
            }

            if (recursive) return children.Select(x => x.QualifiedName).ToArray();

            int folderDepth = folder.Count(x => x == INTERNAL_SEPARATOR);
            List<ArchiveNode> immediateChildren = new List<ArchiveNode>();
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].QualifiedName.Count(x => x == INTERNAL_SEPARATOR) == folderDepth + 1)
                    immediateChildren.Add(children[i]);
            }

            return immediateChildren.Select(x => x.QualifiedName).ToArray(); ;
        }


        internal int CreateDataNode(string name, int size, ArchiveNodeType type = ArchiveNodeType.Local)
        {
            int start = LastEnd + 1;
            Nodes.Add(new ArchiveNode() { Start = start, NodeType = type, End = start + size, QualifiedName = name });
            return start;
        }

        internal int Serialize(Stream s)
        {
            long p = s.Position;
            s.Write(BitConverter.GetBytes(Nodes.Count), 0, sizeof(int));
            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Serialize(s);
            }

            return (int)(s.Position - p);
        }

        internal static ArchiveHeader Deserialize(Stream s)
        {
            byte[] nodeCountBlock = new byte[sizeof(int)];
            s.Read(nodeCountBlock, 0, nodeCountBlock.Length);
            int nodeCount = BitConverter.ToInt32(nodeCountBlock, 0);
            List<ArchiveNode> nodes = new List<ArchiveNode>();
            for (int i = 0; i < nodeCount; i++)
            {
                nodes.Add(ArchiveNode.Deserialize(s));
            }

            return new ArchiveHeader(nodes);
        }

        internal bool Find(string path, out ArchiveNode node)
        {
            string internalPath = path.Replace(PATH_SEPARATOR, INTERNAL_SEPARATOR);
            node = Nodes.FirstOrDefault(x => x.QualifiedName == internalPath);
            return node == null;
        }
    }

}