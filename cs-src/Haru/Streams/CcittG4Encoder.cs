/* CCITT Group 4 (ITU-T T.6) encoding of 1-bit images, ported from the libtiff based encoder of the C library. */

using System;
using System.Numerics;

namespace Haru.Streams
{
    /// <summary>
    /// Encodes a 1-bit image with CCITT Group 4 two-dimensional coding (the data of a CCITTFaxDecode stream with
    /// <c>/K -1</c>): every row is coded against the row above it, the first against a white row, and the data ends
    /// with EOFB. A 1 bit of the input is a black pixel, as in the encoder of the C library (libtiff).
    /// </summary>
    internal sealed class CcittG4Encoder
    {
        private readonly byte[] _data;
        private readonly int _width;
        private readonly int _height;
        private readonly int _stride;
        private readonly bool _topFirst;
        private readonly CcittBitWriter _writer = new CcittBitWriter();

        /// <param name="data">The rows of the image, one bit per pixel, the first pixel in the high bit.</param>
        /// <param name="width">The width in pixels.</param>
        /// <param name="height">The height in pixels.</param>
        /// <param name="stride">The length of a row in bytes, at least (width + 7) / 8.</param>
        /// <param name="topFirst">Whether the first row of the data is the top row (false: the bottom row).</param>
        private CcittG4Encoder(byte[] data, int width, int height, int stride, bool topFirst)
        {
            _data = data;
            _width = width;
            _height = height;
            _stride = stride;
            _topFirst = topFirst;
        }

        /// <summary>
        /// Encodes an image; the arguments are checked by the caller.
        /// </summary>
        /// <returns>The Group 4 coded data.</returns>
        internal static byte[] Encode(byte[] data, int width, int height, int stride, bool topFirst) =>
            new CcittG4Encoder(data, width, height, stride, topFirst).Encode();

        private byte[] Encode()
        {
            // The reference line of the first row is white
            var reference = new byte[_stride];
            for (int i = 0; i < _height; i++)
            {
                int row = _topFirst ? i : _height - 1 - i;
                var line = new ReadOnlySpan<byte>(_data, row * _stride, _stride);
                EncodeRow(line, reference);
                line.CopyTo(reference);
            }

            // EOFB: two EOL codes
            _writer.Write(0x001, 12);
            _writer.Write(0x001, 12);
            return _writer.ToArray();
        }

        // The codes of the two-dimensional modes (ITU-T T.4, table 4)
        private static readonly (int Length, int Code) Horizontal = (3, 0x1);      // 001
        private static readonly (int Length, int Code) Pass = (4, 0x1);            // 0001
        private static readonly (int Length, int Code)[] Vertical =
        {
            (7, 0x03), (6, 0x03), (3, 0x03), (1, 0x1), (3, 0x2), (6, 0x02), (7, 0x02)   // VL3 ... V0 ... VR3
        };

        /// <summary>
        /// Codes a row against its reference row (HPDF_Fax3Encode2DRow): a0 is the current position, a1 and a2 the
        /// next changes of the row, b1 and b2 the next changes of the reference row of the opposite color.
        /// </summary>
        private void EncodeRow(ReadOnlySpan<byte> line, ReadOnlySpan<byte> reference)
        {
            int bits = _width;
            int a0 = 0;
            int a1 = Pixel(line, 0) != 0 ? 0 : FindDiff(line, 0, bits, 0);
            int b1 = Pixel(reference, 0) != 0 ? 0 : FindDiff(reference, 0, bits, 0);

            for (;;)
            {
                int b2 = FindDiff2(reference, b1, bits, Pixel(reference, b1));
                if (b2 >= a1)
                {
                    int d = b1 - a1;
                    if (d < -3 || d > 3)
                    {
                        // Horizontal mode: the run from a0 to a1 and the run from a1 to a2
                        int a2 = FindDiff2(line, a1, bits, Pixel(line, a1));
                        _writer.Write(Horizontal.Code, Horizontal.Length);
                        if (a0 + a1 == 0 || Pixel(line, a0) == 0)
                        {
                            WriteRun(a1 - a0, CcittRunCodes.White);
                            WriteRun(a2 - a1, CcittRunCodes.Black);
                        }
                        else
                        {
                            WriteRun(a1 - a0, CcittRunCodes.Black);
                            WriteRun(a2 - a1, CcittRunCodes.White);
                        }
                        a0 = a2;
                    }
                    else
                    {
                        // Vertical mode: a1 is within three pixels of b1
                        var code = Vertical[d + 3];
                        _writer.Write(code.Code, code.Length);
                        a0 = a1;
                    }
                }
                else
                {
                    // Pass mode: b2 lies before a1
                    _writer.Write(Pass.Code, Pass.Length);
                    a0 = b2;
                }

                if (a0 >= bits)
                    break;

                int color = Pixel(line, a0);
                a1 = FindDiff(line, a0, bits, color);
                b1 = FindDiff(reference, a0, bits, color ^ 1);
                b1 = FindDiff(reference, b1, bits, color);
            }
        }

        /// <summary>
        /// Writes the make-up and terminating codes of a run (putspan).
        /// </summary>
        private void WriteRun(int span, (int Length, int Code)[] codes)
        {
            while (span >= 2624)
            {
                var makeUp = codes[63 + (2560 >> 6)];
                _writer.Write(makeUp.Code, makeUp.Length);
                span -= 2560;
            }
            if (span >= 64)
            {
                var makeUp = codes[63 + (span >> 6)];
                _writer.Write(makeUp.Code, makeUp.Length);
                span -= 64 * (span >> 6);
            }
            var terminating = codes[span];
            _writer.Write(terminating.Code, terminating.Length);
        }

        /// <summary>
        /// The pixel at a position: 1 black, 0 white; a position past the row reads as white.
        /// </summary>
        private static int Pixel(ReadOnlySpan<byte> line, int index) =>
            index >> 3 < line.Length ? (line[index >> 3] >> (7 - (index & 7))) & 1 : 0;

        /// <summary>
        /// The position of the first pixel at or after <paramref name="start"/> whose color differs from
        /// <paramref name="color"/>, or <paramref name="end"/> if there is none.
        /// </summary>
        private static int FindDiff(ReadOnlySpan<byte> line, int start, int end, int color) =>
            start + (color != 0 ? FindSpan(line, start, end, 0xFF) : FindSpan(line, start, end, 0x00));

        /// <summary>
        /// Like <see cref="FindDiff"/>, but <paramref name="end"/> when the start is not before it.
        /// </summary>
        private static int FindDiff2(ReadOnlySpan<byte> line, int start, int end, int color) =>
            start < end ? FindDiff(line, start, end, color) : end;

        /// <summary>
        /// The length of the run of pixels equal to the bits of <paramref name="fill"/> (0x00 white, 0xFF black)
        /// from <paramref name="start"/>, at most up to <paramref name="end"/> (find0span, find1span).
        /// </summary>
        private static int FindSpan(ReadOnlySpan<byte> line, int start, int end, byte fill)
        {
            int bits = end - start;
            int index = start >> 3;
            int span = 0;

            int offset = start & 7;
            if (bits > 0 && offset != 0)
            {
                // The partial byte on the left
                span = LeadingRun((byte)((line[index] ^ fill) << offset));
                if (span > 8 - offset)
                    span = 8 - offset;
                if (span > bits)
                    span = bits;
                if (offset + span < 8)
                    return span;
                bits -= span;
                index++;
            }

            // Whole bytes
            while (bits >= 8)
            {
                if (line[index] != fill)
                    return span + LeadingRun((byte)(line[index] ^ fill));
                span += 8;
                bits -= 8;
                index++;
            }

            // The partial byte on the right
            if (bits > 0)
            {
                int run = LeadingRun((byte)(line[index] ^ fill));
                span += run > bits ? bits : run;
            }
            return span;
        }

        /// <summary>
        /// The number of leading zero bits of a byte (8 for 0).
        /// </summary>
        private static int LeadingRun(byte value) => value == 0 ? 8 : BitOperations.LeadingZeroCount((uint)value) - 24;
    }

    /// <summary>
    /// Writes codes of up to 16 bits to a byte array, the first bit in the high bit of a byte.
    /// </summary>
    internal sealed class CcittBitWriter
    {
        private readonly System.Collections.Generic.List<byte> _bytes = new System.Collections.Generic.List<byte>();
        private int _current;
        private int _free = 8;

        /// <summary>
        /// Writes the low <paramref name="length"/> bits of <paramref name="code"/>, the highest first.
        /// </summary>
        internal void Write(int code, int length)
        {
            while (length > _free)
            {
                _current |= (code >> (length - _free)) & ((1 << _free) - 1);
                length -= _free;
                Flush();
            }
            _current |= (code & ((1 << length) - 1)) << (_free - length);
            _free -= length;
            if (_free == 0)
                Flush();
        }

        /// <summary>
        /// The bytes written; a partial last byte is padded with 0 bits.
        /// </summary>
        internal byte[] ToArray()
        {
            if (_free != 8)
                Flush();
            return _bytes.ToArray();
        }

        private void Flush()
        {
            _bytes.Add((byte)_current);
            _current = 0;
            _free = 8;
        }
    }
}
