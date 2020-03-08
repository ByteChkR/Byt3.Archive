using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Byt3.Archive
{
    internal class ArchiveNode
    {
        public int SerializedSize => sizeof(int) * 4 + Encoding.UTF8.GetByteCount(QualifiedName);
        private int BlockReadSize => SerializedSize - sizeof(int);
        public int Start;
        public int End;
        public string QualifiedName;
        public string NodeName => GetNodeName(QualifiedName);
        public string Parent => GetParent(QualifiedName);
        public ArchiveHeader.ArchiveNodeType NodeType;
        public static string GetParent(string qualifiedName) =>
            qualifiedName.Remove(qualifiedName.LastIndexOf(ArchiveHeader.INTERNAL_SEPARATOR));
        public static string GetNodeName(string qualifiedName) => qualifiedName.Split(ArchiveHeader.INTERNAL_SEPARATOR).Last();

        public void Serialize(Stream s)
        {
            List<byte> ret = new List<byte>();
            ret.AddRange(BitConverter.GetBytes(BlockReadSize));
            ret.AddRange(BitConverter.GetBytes(Start));
            ret.AddRange(BitConverter.GetBytes(End));
            ret.AddRange(BitConverter.GetBytes((int)NodeType));
            ret.AddRange(Encoding.UTF8.GetBytes(QualifiedName));
            s.Write(ret.ToArray(), 0, ret.Count);
        }

        public static ArchiveNode Deserialize(Stream s)
        {
            byte[] sSize = new byte[sizeof(int)];
            s.Read(sSize, 0, sSize.Length);
            int blockSize = BitConverter.ToInt32(sSize, 0);
            byte[] block = new byte[blockSize];
            s.Read(block, 0, block.Length);


            ArchiveNode node = new ArchiveNode
            {
                Start = BitConverter.ToInt32(block, 0),
                End = BitConverter.ToInt32(block, sizeof(int)),
                NodeType = (ArchiveHeader.ArchiveNodeType)BitConverter.ToInt32(block, sizeof(int) * 2),
                QualifiedName = Encoding.UTF8.GetString(block, sizeof(int) * 3, blockSize - sizeof(int) * 3)
            };
            return node;
        }
    }

}