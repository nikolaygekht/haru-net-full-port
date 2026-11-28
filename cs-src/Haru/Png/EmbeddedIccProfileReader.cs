/* Reads the ICC profile embedded in a PNG (iCCP chunk) or JPEG (APP2 ICC_PROFILE segments) file. */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Haru.Png
{
    /// <summary>
    /// Finds the ICC profile of an image file. A missing, damaged or incomplete profile gives null: files without a
    /// usable profile load as before.
    /// </summary>
    internal static class EmbeddedIccProfileReader
    {
        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] IccMarker = System.Text.Encoding.ASCII.GetBytes("ICC_PROFILE\0");

        /// <summary>
        /// Reads the profile of the iCCP chunk of a PNG file (PNG specification, 11.3.3.3): a name, a compression
        /// method of 0 and the zlib-compressed profile. The stream position is kept.
        /// </summary>
        /// <param name="stream">A seekable stream positioned at the start of the PNG file.</param>
        /// <returns>The profile data, or null.</returns>
        public static byte[]? ReadPng(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanSeek)
                return null;

            long start = stream.Position;
            try
            {
                using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
                if (!reader.ReadBytes(PngSignature.Length).AsSpan().SequenceEqual(PngSignature))
                    return null;

                while (stream.Position + 8 <= stream.Length)
                {
                    uint length = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
                    string type = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (length > stream.Length - stream.Position)
                        return null;

                    // iCCP comes before the image data
                    if (type is "IDAT" or "IEND")
                        return null;
                    if (type != "iCCP")
                    {
                        stream.Seek(length + 4, SeekOrigin.Current);   // data and CRC
                        continue;
                    }

                    return DecodeIccpChunk(reader.ReadBytes((int)length));
                }
                return null;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or EndOfStreamException)
            {
                return null;
            }
            finally
            {
                stream.Position = start;
            }
        }

        private static byte[]? DecodeIccpChunk(byte[] chunk)
        {
            int nameEnd = Array.IndexOf(chunk, (byte)0);
            if (nameEnd < 1 || nameEnd > 79 || nameEnd + 2 > chunk.Length || chunk[nameEnd + 1] != 0)
                return null;

            using var compressed = new MemoryStream(chunk, nameEnd + 2, chunk.Length - nameEnd - 2);
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            using var profile = new MemoryStream();
            zlib.CopyTo(profile);
            return profile.Length > 0 ? profile.ToArray() : null;
        }

        /// <summary>
        /// Reads the profile of the APP2 ICC_PROFILE segments of a JPEG file (ICC.1, annex B.4): the profile may be
        /// split over several segments, numbered from 1; the parts are joined in order. Segments after the start of
        /// the scan are not read.
        /// </summary>
        /// <param name="jpeg">The JPEG file.</param>
        /// <returns>The profile data, or null if there is none or a part is missing.</returns>
        public static byte[]? ReadJpeg(ReadOnlySpan<byte> jpeg)
        {
            if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
                return null;

            var parts = new SortedDictionary<int, byte[]>();
            int expected = 0;
            int position = 2;
            while (position + 4 <= jpeg.Length && jpeg[position] == 0xFF)
            {
                byte marker = jpeg[position + 1];
                if (marker == 0xFF)
                {
                    position++;   // fill byte
                    continue;
                }
                if (marker == 0xDA || marker == 0xD9)
                    break;   // start of scan, end of image

                int length = BinaryPrimitives.ReadUInt16BigEndian(jpeg.Slice(position + 2));
                if (length < 2 || position + 2 + length > jpeg.Length)
                    return null;

                var segment = jpeg.Slice(position + 4, length - 2);
                if (marker == 0xE2 && segment.Length > IccMarker.Length + 2 && segment.StartsWith(IccMarker))
                {
                    int sequence = segment[IccMarker.Length];
                    int count = segment[IccMarker.Length + 1];
                    if (sequence < 1 || count < 1 || sequence > count || (expected != 0 && count != expected))
                        return null;
                    expected = count;
                    parts[sequence] = segment.Slice(IccMarker.Length + 2).ToArray();
                }
                position += 2 + length;
            }

            if (expected == 0 || parts.Count != expected)
                return null;

            var profile = new List<byte>();
            foreach (var part in parts.Values)
                profile.AddRange(part);
            return profile.ToArray();
        }
    }
}
