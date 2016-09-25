using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.Conversion
{
    public class PacFileEntry : ParsableData
    {
        public uint Offset { get; private set; }
        public string Filename { get; private set; }

        public uint CalculatedLength { get; private set; }

        public PacFileEntry(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            Offset = reader.ReadUInt32();
            Filename = Encoding.ASCII.GetString(reader.ReadBytes(0x1C)).TrimEnd('\0');
        }

        public void SetLength(int pacFileSize, PacFileEntry[] files)
        {
            int nextFileIdx = (Array.IndexOf(files, this) + 1);
            if (nextFileIdx < files.Length)
                CalculatedLength = files[nextFileIdx].Offset - Offset;
            else
                CalculatedLength = (uint)(pacFileSize - Offset);
        }
    }

    public class PacFile : ParsableData
    {
        public uint NumFiles { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }

        public long DataStartPosition { get; private set; }
        public PacFileEntry[] Files { get; private set; }

        public PacFile(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            NumFiles = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();

            Files = new PacFileEntry[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new PacFileEntry(stream);

            DataStartPosition = stream.Position;
            foreach (PacFileEntry file in Files) file.SetLength((int)stream.Length, Files);
        }
    }
}
