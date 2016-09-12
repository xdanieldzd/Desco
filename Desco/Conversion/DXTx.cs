using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Cobalt.IO;

namespace Desco.Conversion
{
    public static class DXTx
    {
        public static byte[] Decompress(EndianBinaryReader reader, TextureHeader info)
        {
            byte[] pixelData = new byte[info.DataSize * 8];

            for (int y = 0; y < info.Height; y += 4)
            {
                for (int x = 0; x < info.Width; x += 4)
                {
                    using (EndianBinaryReader decodedReader = new EndianBinaryReader(new MemoryStream(DecompressDxtBlock(reader, info.Format))))
                    {
                        for (int py = 0; py < 4; py++)
                        {
                            for (int px = 0; px < 4; px++)
                            {
                                int ix = (x + px);
                                int iy = (y + py);
                                if (ix >= info.Width || iy >= info.Height) continue;

                                int pixelOffset = (int)(((iy * info.Width) + ix) * 4);
                                Buffer.BlockCopy(decodedReader.ReadBytes(4), 0, pixelData, pixelOffset, 4);
                            }
                        }
                    }
                }
            }

            return pixelData;
        }

        private static byte[] DecompressDxtBlock(EndianBinaryReader reader, TextureDataFormat format)
        {
            byte[] outputData = new byte[(4 * 4) * 4];
            byte[] colorData = null, alphaData = null;

            if (format != TextureDataFormat.DXT1)
                alphaData = DecompressDxtAlpha(reader, format);

            colorData = DecompressDxtColor(reader, format);

            for (int i = 0; i < colorData.Length; i += 4)
            {
                outputData[i] = colorData[i];
                outputData[i + 1] = colorData[i + 1];
                outputData[i + 2] = colorData[i + 2];
                outputData[i + 3] = (alphaData != null ? alphaData[i + 3] : colorData[i + 3]);
            }

            return outputData;
        }

        private static byte[] DecompressDxtColor(EndianBinaryReader reader, TextureDataFormat format)
        {
            byte[] colorOut = new byte[(4 * 4) * 4];

            ushort color0 = reader.ReadUInt16(Endian.LittleEndian);
            ushort color1 = reader.ReadUInt16(Endian.LittleEndian);
            uint bits = reader.ReadUInt32(Endian.LittleEndian);

            byte c0r, c0g, c0b, c1r, c1g, c1b;
            UnpackRgb565(color0, out c0r, out c0g, out c0b);
            UnpackRgb565(color1, out c1r, out c1g, out c1b);

            byte[] bitsExt = new byte[16];
            for (int i = 0; i < bitsExt.Length; i++)
                bitsExt[i] = (byte)((bits >> (i * 2)) & 0x3);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    byte code = bitsExt[(y * 4) + x];
                    int destOffset = ((y * 4) + x) * 4;

                    if (format == TextureDataFormat.DXT1)
                        colorOut[destOffset + 3] = (byte)((color0 <= color1 && code == 3) ? 0 : 0xFF);

                    if (format == TextureDataFormat.DXT1 && color0 <= color1)
                    {
                        switch (code)
                        {
                            case 0x00:
                                colorOut[destOffset + 0] = c0b;
                                colorOut[destOffset + 1] = c0g;
                                colorOut[destOffset + 2] = c0r;
                                break;

                            case 0x01:
                                colorOut[destOffset + 0] = c1b;
                                colorOut[destOffset + 1] = c1g;
                                colorOut[destOffset + 2] = c1r;
                                break;

                            case 0x02:
                                colorOut[destOffset + 0] = (byte)((c0b + c1b) / 2);
                                colorOut[destOffset + 1] = (byte)((c0g + c1g) / 2);
                                colorOut[destOffset + 2] = (byte)((c0r + c1r) / 2);
                                break;

                            case 0x03:
                                colorOut[destOffset + 0] = 0;
                                colorOut[destOffset + 1] = 0;
                                colorOut[destOffset + 2] = 0;
                                break;
                        }
                    }
                    else
                    {
                        switch (code)
                        {
                            case 0x00:
                                colorOut[destOffset + 0] = c0b;
                                colorOut[destOffset + 1] = c0g;
                                colorOut[destOffset + 2] = c0r;
                                break;

                            case 0x01:
                                colorOut[destOffset + 0] = c1b;
                                colorOut[destOffset + 1] = c1g;
                                colorOut[destOffset + 2] = c1r;
                                break;

                            case 0x02:
                                colorOut[destOffset + 0] = (byte)((2 * c0b + c1b) / 3);
                                colorOut[destOffset + 1] = (byte)((2 * c0g + c1g) / 3);
                                colorOut[destOffset + 2] = (byte)((2 * c0r + c1r) / 3);
                                break;

                            case 0x03:
                                colorOut[destOffset + 0] = (byte)((c0b + 2 * c1b) / 3);
                                colorOut[destOffset + 1] = (byte)((c0g + 2 * c1g) / 3);
                                colorOut[destOffset + 2] = (byte)((c0r + 2 * c1r) / 3);
                                break;
                        }
                    }
                }
            }

            return colorOut;
        }

        private static void UnpackRgb565(ushort rgb565, out byte r, out byte g, out byte b)
        {
            r = (byte)((rgb565 & 0xF800) >> 11);
            r = (byte)((r << 3) | (r >> 2));
            g = (byte)((rgb565 & 0x07E0) >> 5);
            g = (byte)((g << 2) | (g >> 4));
            b = (byte)(rgb565 & 0x1F);
            b = (byte)((b << 3) | (b >> 2));
        }

        private static byte[] DecompressDxtAlpha(EndianBinaryReader reader, TextureDataFormat format)
        {
            byte[] alphaOut = new byte[(4 * 4) * 4];

            if (format == TextureDataFormat.DXT3)
            {
                ulong alpha = reader.ReadUInt64();
                for (int i = 0; i < alphaOut.Length; i += 4)
                {
                    alphaOut[i + 3] = (byte)(((alpha & 0xF) << 4) | (alpha & 0xF));
                    alpha >>= 4;
                }
            }
            else if (format == TextureDataFormat.DXT5)
            {
                byte alpha0 = reader.ReadByte();
                byte alpha1 = reader.ReadByte();
                byte bits_5 = reader.ReadByte();
                byte bits_4 = reader.ReadByte();
                byte bits_3 = reader.ReadByte();
                byte bits_2 = reader.ReadByte();
                byte bits_1 = reader.ReadByte();
                byte bits_0 = reader.ReadByte();

                ulong bits = (ulong)(((ulong)bits_0 << 40) | ((ulong)bits_1 << 32) | ((ulong)bits_2 << 24) | ((ulong)bits_3 << 16) | ((ulong)bits_4 << 8) | (ulong)bits_5);

                byte[] bitsExt = new byte[16];
                for (int i = 0; i < bitsExt.Length; i++)
                    bitsExt[i] = (byte)((bits >> (i * 3)) & 0x7);

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        byte code = bitsExt[(y * 4) + x];
                        int destOffset = (((y * 4) + x) * 4) + 3;

                        if (alpha0 > alpha1)
                        {
                            switch (code)
                            {
                                case 0x00: alphaOut[destOffset] = alpha0; break;
                                case 0x01: alphaOut[destOffset] = alpha1; break;
                                case 0x02: alphaOut[destOffset] = (byte)((6 * alpha0 + 1 * alpha1) / 7); break;
                                case 0x03: alphaOut[destOffset] = (byte)((5 * alpha0 + 2 * alpha1) / 7); break;
                                case 0x04: alphaOut[destOffset] = (byte)((4 * alpha0 + 3 * alpha1) / 7); break;
                                case 0x05: alphaOut[destOffset] = (byte)((3 * alpha0 + 4 * alpha1) / 7); break;
                                case 0x06: alphaOut[destOffset] = (byte)((2 * alpha0 + 5 * alpha1) / 7); break;
                                case 0x07: alphaOut[destOffset] = (byte)((1 * alpha0 + 6 * alpha1) / 7); break;
                            }
                        }
                        else
                        {
                            switch (code)
                            {
                                case 0x00: alphaOut[destOffset] = alpha0; break;
                                case 0x01: alphaOut[destOffset] = alpha1; break;
                                case 0x02: alphaOut[destOffset] = (byte)((4 * alpha0 + 1 * alpha1) / 5); break;
                                case 0x03: alphaOut[destOffset] = (byte)((3 * alpha0 + 2 * alpha1) / 5); break;
                                case 0x04: alphaOut[destOffset] = (byte)((2 * alpha0 + 3 * alpha1) / 5); break;
                                case 0x05: alphaOut[destOffset] = (byte)((1 * alpha0 + 4 * alpha1) / 5); break;
                                case 0x06: alphaOut[destOffset] = 0x00; break;
                                case 0x07: alphaOut[destOffset] = 0xFF; break;
                            }
                        }
                    }
                }
            }

            return alphaOut;
        }
    }
}
