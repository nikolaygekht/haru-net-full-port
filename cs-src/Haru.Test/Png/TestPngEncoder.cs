using System;
using System.IO;
using System.IO.Compression;

namespace Haru.Test.Png
{
    /// <summary>
    /// Builds minimal PNG files in memory for tests (8-bit RGBA, color type 6, no row filtering).
    /// </summary>
    internal static class TestPngEncoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        /// <summary>
        /// Encodes 8-bit RGBA pixels (4 bytes per pixel, row-major) as a PNG file.
        /// </summary>
        public static byte[] EncodeRgba(byte[] rgba, int width, int height) => EncodeRgba(rgba, width, height, null);

        /// <summary>
        /// Encodes 8-bit RGBA pixels as a PNG file with an iCCP chunk holding <paramref name="iccChunkData"/> after
        /// the name and the compression method: the zlib-compressed profile, or any bytes for a damaged chunk.
        /// </summary>
        public static byte[] EncodeRgba(byte[] rgba, int width, int height, byte[]? iccChunkData)
        {
            if (rgba.Length != width * height * 4)
                throw new ArgumentException("Pixel data size does not match dimensions", nameof(rgba));

            using var output = new MemoryStream();
            output.Write(Signature);

            var ihdr = new byte[13];
            WriteBigEndian(ihdr, 0, (uint)width);
            WriteBigEndian(ihdr, 4, (uint)height);
            ihdr[8] = 8; // bit depth
            ihdr[9] = 6; // color type: RGBA
            WriteChunk(output, "IHDR", ihdr);

            if (iccChunkData != null)
            {
                var name = System.Text.Encoding.ASCII.GetBytes("Test profile\0\0");   // name, NUL, method 0
                var chunk = new byte[name.Length + iccChunkData.Length];
                name.CopyTo(chunk, 0);
                iccChunkData.CopyTo(chunk, name.Length);
                WriteChunk(output, "iCCP", chunk);
            }

            using (var raw = new MemoryStream())
            {
                using (var zlib = new ZLibStream(raw, CompressionLevel.Fastest, leaveOpen: true))
                {
                    for (int y = 0; y < height; y++)
                    {
                        zlib.WriteByte(0); // filter: none
                        zlib.Write(rgba, y * width * 4, width * 4);
                    }
                }
                WriteChunk(output, "IDAT", raw.ToArray());
            }

            WriteChunk(output, "IEND", Array.Empty<byte>());
            return output.ToArray();
        }

        /// <summary>
        /// Compresses an ICC profile as an iCCP chunk stores it.
        /// </summary>
        public static byte[] CompressProfile(byte[] profile)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                zlib.Write(profile);
            return output.ToArray();
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            var header = new byte[8];
            WriteBigEndian(header, 0, (uint)data.Length);
            System.Text.Encoding.ASCII.GetBytes(type, 0, 4, header, 4);
            output.Write(header);
            output.Write(data);

            uint crc = UpdateCrc(0xFFFFFFFF, header.AsSpan(4));
            crc = UpdateCrc(crc, data);

            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, ~crc);
            output.Write(crcBytes);
        }

        private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (byte b in data)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++)
                    crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
            return crc;
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
