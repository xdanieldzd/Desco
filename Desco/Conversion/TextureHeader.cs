using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.Conversion
{
    public enum TextureDataFormat : byte
    {
        ARGB8888 = 0x00,
        DXT1 = 0x02,
        DXT3 = 0x04,
        DXT5 = 0x06,
        Indexed8bpp = 0x09,
        ARGB1555 = 0x0B,
        ARGB4444 = 0x0C,
        RGB565 = 0x0D   // TODO: verify me!
    };

    public class TextureHeader : ParsableData
    {
        public TextureDataFormat Format { get; private set; }
        public byte Unknown0x01 { get; private set; }
        public ushort Unknown0x02 { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort Unknown0x08 { get; private set; }
        public ushort Unknown0x0A { get; private set; }
        public uint DataSize { get; private set; }

        public TextureHeader(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            Format = (TextureDataFormat)reader.ReadByte();
            Unknown0x01 = reader.ReadByte();
            Unknown0x02 = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x08 = reader.ReadUInt16();
            Unknown0x0A = reader.ReadUInt16();
            DataSize = reader.ReadUInt32();
        }
    }
}
