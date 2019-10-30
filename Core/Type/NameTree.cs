using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UniEnumExtension
{
    public sealed class NameTree
    {
        public readonly NameLeaf Value;
        public readonly NameTree[] Trees;

        public NameTree(NameLeaf value, NameTree[] trees)
        {
            Value = value;
            Trees = trees;
        }

        public bool HasChild => Trees.Length != 0;

        public static NameTree[] Create(IEnumerable<string> names, int index, int length, bool isLittleEndian)
        {
            if (index == length)
                return Array.Empty<NameTree>();
            return names
                .GroupBy(name => new NameLeaf(name, index, isLittleEndian), name => name)
                .OrderBy(grouping => grouping.Key.LVal0)
                .Select(grouping => new NameTree(grouping.Key, Create(grouping, index + grouping.Key.Length, length, isLittleEndian)))
                .ToArray();
        }

        private void ToString(StringBuilder builder, int indent)
        {
            builder.AppendLine();
            for (var i = 0; i < indent; i++)
            {
                builder.Append(' ');
            }
            builder.Append(Value.ToString());
            foreach (var tree in Trees)
            {
                tree.ToString(builder, indent + 1);
            }
        }
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Value.ToString());
            foreach (var tree in Trees)
            {
                tree.ToString(builder, 1);
            }
            return builder.ToString();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct NameLeaf : IEquatable<NameLeaf>, IComparable<NameLeaf>
    {
        [FieldOffset(0)] public readonly long LVal0;
        [FieldOffset(0)] public readonly int IVal0;
        [FieldOffset(4)] public readonly int IVal1;
        [FieldOffset(0)] public readonly char CVal0;
        [FieldOffset(2)] public readonly char CVal1;
        [FieldOffset(4)] public readonly char CVal2;
        [FieldOffset(6)] public readonly char CVal3;
        [FieldOffset(8)] public readonly int Length;

        public NameLeaf(string value, int index, bool isLittleEndian)
        {
            this = default;
            switch (Length = value.Length - index)
            {
                case 0:
                    return;
                case 1:
                    CVal0 = value[index];
                    break;
                case 2:
                    CVal1 = value[index + 1];
                    goto case 1;
                case 3:
                    CVal2 = value[index + 2];
                    goto case 2;
                default:
                    Length = 4;
                    CVal3 = value[index + 3];
                    goto case 3;
            }
            if (BitConverter.IsLittleEndian == isLittleEndian) return;
            unsafe
            {
                fixed (char* c = &CVal0)
                {
                    var ptr = (byte*)c;
                    var z = 0;
                    (ptr[z], ptr[z + 1]) = (ptr[z + 1], ptr[z]);
                    z += 2;
                    (ptr[z], ptr[z + 1]) = (ptr[z + 1], ptr[z]);
                    z += 2;
                    (ptr[z], ptr[z + 1]) = (ptr[z + 1], ptr[z]);
                    z += 2;
                    (ptr[z], ptr[z + 1]) = (ptr[z + 1], ptr[z]);
                }
            }
        }

        public bool Equals(NameLeaf other) => LVal0 == other.LVal0;

        public override bool Equals(object obj) => obj is NameLeaf other && Equals(other);

        public override int GetHashCode() => IVal0 ^ IVal1;

        public int CompareTo(NameLeaf other) => LVal0.CompareTo(other.LVal0);

        public override string ToString() => LVal0.ToString();
    }
}
