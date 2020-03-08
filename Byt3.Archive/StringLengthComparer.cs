using System.Collections.Generic;

namespace Byt3.Archive
{
    internal class StringLengthComparer : IComparer<string>
    {
        public int Compare(string left, string right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.Length - right.Length;
        }
    }
}