using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Cobalt.IO;

namespace Desco.ModelParser
{
    public class Asset : ParsableData, IComponent
    {
        public short AssetID { get; private set; }
        public short ModelIndex { get; private set; }

        public Asset(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            AssetID = reader.ReadInt16();
            ModelIndex = reader.ReadInt16();
        }
    }
}
