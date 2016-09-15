using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class TransformIndices : ParsableData
    {
        public int TranslationXIndex { get; private set; }
        public int TranslationYIndex { get; private set; }
        public int TranslationZIndex { get; private set; }
        public int RotationXIndex { get; private set; }
        public int RotationYIndex { get; private set; }
        public int RotationZIndex { get; private set; }
        public int ScaleXIndex { get; private set; }
        public int ScaleYIndex { get; private set; }
        public int ScaleZIndex { get; private set; }
        public uint Unknown0x24 { get; private set; }

        public TransformIndices(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            TranslationXIndex = reader.ReadInt32();
            TranslationYIndex = reader.ReadInt32();
            TranslationZIndex = reader.ReadInt32();
            RotationXIndex = reader.ReadInt32();
            RotationYIndex = reader.ReadInt32();
            RotationZIndex = reader.ReadInt32();
            ScaleXIndex = reader.ReadInt32();
            ScaleYIndex = reader.ReadInt32();
            ScaleZIndex = reader.ReadInt32();
            Unknown0x24 = reader.ReadUInt32();
        }
    }
}
