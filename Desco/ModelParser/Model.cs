using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class Model : ParsableData, IComponent
    {
        public uint NumNodeIndices { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }
        public uint Unknown0x10 { get; private set; }
        public uint Unknown0x14 { get; private set; }
        public uint Unknown0x18 { get; private set; }
        public uint[] NodeIndices { get; private set; }

        public Model(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            NumNodeIndices = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();

            NodeIndices = new uint[NumNodeIndices];
            for (int i = 0; i < NodeIndices.Length; i++) NodeIndices[i] = reader.ReadUInt32();
        }
    }
}
