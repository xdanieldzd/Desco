using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Cobalt.IO;

using Desco.Conversion;

namespace Desco.ModelParser
{
    public class Texture : ParsableData, IComponent
    {
        delegate void ProviderFunctionDelegate(EndianBinaryReader reader, TextureHeader header, out PixelFormat pixelFormat, out byte[] pixelData);

        static readonly Dictionary<TextureDataFormat, ProviderFunctionDelegate> pixelFormatProviders = new Dictionary<TextureDataFormat, ProviderFunctionDelegate>()
        {
            { TextureDataFormat.ARGB8888, PixelProviderDirect },
            { TextureDataFormat.DXT1, PixelProviderDXTx },
            { TextureDataFormat.DXT3, PixelProviderDXTx },
            { TextureDataFormat.DXT5, PixelProviderDXTx },
            /* Indexed8bpp */
            { TextureDataFormat.ARGB1555, PixelProviderDirect },
            { TextureDataFormat.ARGB4444, PixelProviderARGB4444 },
            { TextureDataFormat.RGB565, PixelProviderDirect },
        };

        static readonly Dictionary<TextureDataFormat, PixelFormat> pixelFormatMap = new Dictionary<TextureDataFormat, PixelFormat>()
        {
            { TextureDataFormat.ARGB8888, PixelFormat.Format32bppArgb },
            { TextureDataFormat.DXT1, PixelFormat.Format32bppArgb },
            { TextureDataFormat.DXT3, PixelFormat.Format32bppArgb },
            { TextureDataFormat.DXT5, PixelFormat.Format32bppArgb },
            /* Indexed8bpp */
            { TextureDataFormat.ARGB1555, PixelFormat.Format16bppArgb1555 },
            { TextureDataFormat.ARGB4444, PixelFormat.Format32bppArgb },
            { TextureDataFormat.RGB565, PixelFormat.Format16bppRgb565 },
        };

        public uint Unknown0x00 { get; private set; }
        public TextureHeader TextureHeader { get; private set; }

        public Bitmap Image { get; private set; }

        public Texture(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            Unknown0x00 = reader.ReadUInt32();
            TextureHeader = new TextureHeader(stream);

            PixelFormat pixelFormat;
            byte[] pixelData;

            if (!pixelFormatMap.ContainsKey(TextureHeader.Format) || !pixelFormatProviders.ContainsKey(TextureHeader.Format))
            {
                pixelFormat = PixelFormat.Format32bppArgb;
                pixelData = new byte[TextureHeader.Width * TextureHeader.Height * (Bitmap.GetPixelFormatSize(pixelFormat) / 8)];
            }
            else
                pixelFormatProviders[TextureHeader.Format](reader, TextureHeader, out pixelFormat, out pixelData);

            Image = new Bitmap(TextureHeader.Width, TextureHeader.Height, pixelFormat);
            BitmapData bmpData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadWrite, Image.PixelFormat);

            byte[] pixelsForBmp = new byte[bmpData.Height * bmpData.Stride];
            int bytesPerPixel = (Bitmap.GetPixelFormatSize(Image.PixelFormat) / 8);
            for (int y = 0; y < bmpData.Height; y++)
                Buffer.BlockCopy(pixelData, y * bmpData.Width * bytesPerPixel, pixelsForBmp, y * bmpData.Stride, bmpData.Width * bytesPerPixel);

            Marshal.Copy(pixelsForBmp, 0, bmpData.Scan0, pixelsForBmp.Length);
            Image.UnlockBits(bmpData);
        }

        private static void PixelProviderDirect(EndianBinaryReader reader, TextureHeader header, out PixelFormat pixelFormat, out byte[] pixelData)
        {
            pixelFormat = pixelFormatMap[header.Format];

            int bytesPerPixel = (Bitmap.GetPixelFormatSize(pixelFormat) / 8);
            pixelData = new byte[header.Width * header.Height * bytesPerPixel];

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                for (int j = bytesPerPixel - 1; j >= 0; j--)
                    pixelData[i + j] = reader.ReadByte();
        }

        private static void PixelProviderDXTx(EndianBinaryReader reader, TextureHeader header, out PixelFormat pixelFormat, out byte[] pixelData)
        {
            pixelFormat = pixelFormatMap[header.Format];
            pixelData = DXTx.Decompress(reader, header);
        }

        private static void PixelProviderARGB4444(EndianBinaryReader reader, TextureHeader header, out PixelFormat pixelFormat, out byte[] pixelData)
        {
            byte[] tempData;
            PixelProviderDirect(reader, header, out pixelFormat, out tempData);

            pixelFormat = pixelFormatMap[header.Format];
            pixelData = new byte[tempData.Length << 1];
            for (int i = 0, j = 0; i < tempData.Length; i += 2, j += 4)
            {
                pixelData[j + 0] = (byte)((tempData[i] & 0x0F) | ((tempData[i] & 0x0F) << 4));
                pixelData[j + 1] = (byte)((tempData[i] & 0xF0) | ((tempData[i] & 0xF0) >> 4));
                pixelData[j + 2] = (byte)((tempData[i + 1] & 0x0F) | ((tempData[i + 1] & 0x0F) << 4));
                pixelData[j + 3] = (byte)((tempData[i + 1] & 0xF0) | ((tempData[i + 1] & 0xF0) >> 4));
            }
        }
    }
}
