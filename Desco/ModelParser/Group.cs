using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class Group : ParsableData, IComponent
    {
        public int NumPrimitives { get; private set; }
        public float TextureAnimationOffsetX { get; private set; }
        public float TextureAnimationOffsetY { get; private set; }
        public ushort Unknown0x0C { get; private set; }
        public ushort Unknown0x0E { get; private set; }
        public uint Unknown0x10 { get; private set; }
        public uint Unknown0x14 { get; private set; }
        public uint Unknown0x18 { get; private set; }
        public uint[] PrimitiveIndices { get; private set; }

        public Group(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            NumPrimitives = reader.ReadInt32();
            TextureAnimationOffsetX = reader.ReadSingle();
            TextureAnimationOffsetY = reader.ReadSingle();
            Unknown0x0C = reader.ReadUInt16();
            Unknown0x0E = reader.ReadUInt16();
            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();

            PrimitiveIndices = new uint[NumPrimitives];
            for (int i = 0; i < PrimitiveIndices.Length; i++) PrimitiveIndices[i] = reader.ReadUInt32();
        }
    }
}
