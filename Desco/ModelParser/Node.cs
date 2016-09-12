using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class Node : ParsableData, IComponent
    {
        public int GroupIndex { get; private set; }
        public short RelatedNodeIndex { get; private set; }
        public ushort Unknown0x06 { get; private set; }
        public uint NumTransformIndices { get; private set; }
        public uint[] TransformIndices { get; private set; }

        public Node(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            GroupIndex = reader.ReadInt32();
            RelatedNodeIndex = reader.ReadInt16();
            Unknown0x06 = reader.ReadUInt16();
            NumTransformIndices = reader.ReadUInt32();

            TransformIndices = new uint[NumTransformIndices];
            for (int i = 0; i < TransformIndices.Length; i++) TransformIndices[i] = reader.ReadUInt32();
        }
    }
}
