using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class TransformData : ParsableData
    {
        public float Value0x00 { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public float Value0x08 { get; private set; }
        public float Value0x0C { get; private set; }

        public TransformData(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            Value0x00 = reader.ReadSingle();
            Unknown0x04 = reader.ReadUInt32();
            Value0x08 = reader.ReadSingle();
            Value0x0C = reader.ReadSingle();
        }
    }
}
