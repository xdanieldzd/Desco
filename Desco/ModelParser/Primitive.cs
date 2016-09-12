using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class Primitive : ParsableData, IComponent
    {
        public int NumVertices { get; private set; }
        public int TextureIndex { get; private set; }
        public int Unknown0x08 { get; private set; }
        public float Unknown0x0C { get; private set; }
        public float Unknown0x10 { get; private set; }
        public float Unknown0x14 { get; private set; }
        public float Unknown0x18 { get; private set; }
        public float Unknown0x1C { get; private set; }
        public byte Unknown0x20 { get; private set; }
        public byte Unknown0x21 { get; private set; }
        public byte Unknown0x22 { get; private set; }
        public byte Unknown0x23 { get; private set; }
        public byte Unknown0x24 { get; private set; }
        public byte Unknown0x25 { get; private set; }
        public byte Unknown0x26 { get; private set; }
        public byte Unknown0x27 { get; private set; }
        public byte Unknown0x28 { get; private set; }
        public byte Unknown0x29 { get; private set; }
        public byte Unknown0x2A { get; private set; }
        public byte Unknown0x2B { get; private set; }
        public int Unknown0x2C { get; private set; }
        public uint[] VertexIndices { get; private set; }

        public Primitive(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            NumVertices = reader.ReadInt32();
            TextureIndex = reader.ReadInt32();
            Unknown0x08 = reader.ReadInt32();
            Unknown0x0C = reader.ReadSingle();
            Unknown0x10 = reader.ReadSingle();
            Unknown0x14 = reader.ReadSingle();
            Unknown0x18 = reader.ReadSingle();
            Unknown0x1C = reader.ReadSingle();
            Unknown0x20 = reader.ReadByte();
            Unknown0x21 = reader.ReadByte();
            Unknown0x22 = reader.ReadByte();
            Unknown0x23 = reader.ReadByte();
            Unknown0x24 = reader.ReadByte();
            Unknown0x25 = reader.ReadByte();
            Unknown0x26 = reader.ReadByte();
            Unknown0x27 = reader.ReadByte();
            Unknown0x28 = reader.ReadByte();
            Unknown0x29 = reader.ReadByte();
            Unknown0x2A = reader.ReadByte();
            Unknown0x2B = reader.ReadByte();
            Unknown0x2C = reader.ReadInt32();

            VertexIndices = new uint[NumVertices];
            for (int i = 0; i < VertexIndices.Length; i++) VertexIndices[i] = reader.ReadUInt32();
        }
    }
}
