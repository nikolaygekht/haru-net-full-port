using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests the 1-bit images coded with CCITT Group 4 (HPDF_Image_LoadRaw1BitImageFromMem): the coded data is read
    /// back with the CCITT decoder of PdfPig and compared bit by bit (MuPDF decodes the same images to the same
    /// pixels, checked while porting).
    /// </summary>
    public class HpdfImage1BitTests
    {
        /// <summary>
        /// Packs pixels (true = 1 bit) into rows of <paramref name="stride"/> bytes; the bytes after the pixels of a
        /// row are filled with garbage, which the encoder must ignore.
        /// </summary>
        private static byte[] Pack(int width, int height, int stride, Func<int, int, bool> isOne)
        {
            var data = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (isOne(x, y))
                        data[y * stride + x / 8] |= (byte)(0x80 >> (x % 8));
                }
                for (int b = (width + 7) / 8; b < stride; b++)
                    data[y * stride + b] = 0xA5;
                if (width % 8 != 0)
                    data[y * stride + width / 8] |= (byte)(0xFF >> (width % 8));   // garbage padding bits
            }
            return data;
        }

        /// <summary>
        /// The pixels of packed rows, row by row: 1 bits as true.
        /// </summary>
        private static bool[][] Unpack(ReadOnlySpan<byte> data, int width, int height, int stride)
        {
            var rows = new bool[height][];
            for (int y = 0; y < height; y++)
            {
                rows[y] = new bool[width];
                for (int x = 0; x < width; x++)
                    rows[y][x] = ((data[y * stride + x / 8] >> (7 - x % 8)) & 1) != 0;
            }
            return rows;
        }

        private static bool[][] Decode(HpdfDocument doc, int width, int height)
        {
            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));
            var image = pdf.GetPage(1).GetImages().Single();
            image.TryGetBytesAsMemory(out var decoded).Should().BeTrue();
            return Unpack(decoded.Span, width, height, (width + 7) / 8);
        }

        public static TheoryData<string, int, int> Patterns => new()
        {
            { "white", 37, 20 },
            { "black", 37, 20 },
            { "noise", 61, 45 },
            { "stripes", 100, 30 },
            { "column", 1, 7 },
            { "checker", 9, 9 },
            { "long runs", 6000, 4 },
            { "diagonals", 64, 64 },
        };

        private static Func<int, int, bool> Pattern(string name)
        {
            var random = new Random(42);
            return name switch
            {
                "white" => (x, y) => false,
                "black" => (x, y) => true,
                "noise" => (x, y) => random.Next(2) == 1,
                "stripes" => (x, y) => (x / 3) % 2 == 0,
                "column" or "checker" => (x, y) => (x + y) % 2 == 0,
                "long runs" => (x, y) => x > 5700 || (x > 64 && x < 2690 + y) ,
                "diagonals" => (x, y) => Math.Abs(x - y) < 2 || Math.Abs(x + y - 63) < 2,
                _ => throw new ArgumentException(name)
            };
        }

        [Theory]
        [MemberData(nameof(Patterns))]
        public void LoadRaw1BitImageFromMem_DecodesToTheSameBits(string pattern, int width, int height)
        {
            int stride = (width + 7) / 8 + 1;
            byte[] data = Pack(width, height, stride, Pattern(pattern));
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            using var image = HpdfImage.LoadRaw1BitImageFromMem(doc.Xref, "Im1", data, (uint)width, (uint)height,
                (uint)stride);
            page.DrawImage(image, 10, 10, 100, 100);

            Decode(doc, width, height).Should().BeEquivalentTo(Unpack(data, width, height, stride),
                options => options.WithStrictOrdering());
        }

        [Fact]
        public void OneIsBlack_DecodesToTheInvertedBits()
        {
            // The encoder treats 1 bits as black; /BlackIs1 false makes the decoder output black as 0, which
            // DeviceGray shows as black: the 1 bits of the data are shown black
            const int width = 45, height = 13;
            byte[] data = Pack(width, height, 6, (x, y) => (x * y) % 7 == 0);
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            using var image = HpdfImage.LoadRaw1BitImageFromMem(doc.Xref, "Im1", data, width, height, 6,
                oneIsBlack: true);
            page.DrawImage(image, 10, 10, 100, 100);

            var inverted = Unpack(data, width, height, 6).Select(row => row.Select(bit => !bit).ToArray()).ToArray();
            Decode(doc, width, height).Should().BeEquivalentTo(inverted, options => options.WithStrictOrdering());
        }

        [Fact]
        public void BottomUpRows_AreStoredTopDown()
        {
            const int width = 23, height = 11;
            byte[] data = Pack(width, height, 3, (x, y) => x == y || x == 2 * y);
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            using var image = HpdfImage.LoadRaw1BitImageFromMem(doc.Xref, "Im1", data, width, height, 3,
                topIsFirst: false);
            page.DrawImage(image, 10, 10, 100, 100);

            Decode(doc, width, height).Should().BeEquivalentTo(Unpack(data, width, height, 3).Reverse().ToArray(),
                options => options.WithStrictOrdering());
        }

        [Fact]
        public void LoadRaw1BitImageFromMem_WritesGroup4Dictionary()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRaw1BitImageFromMem(doc.Xref, "Im1", new byte[] { 0xF0, 0x0F }, 8, 2, 1,
                oneIsBlack: true);

            image.StreamObject.Filter.Should().Be(HpdfStreamFilter.CcittFaxDecode);
            image.BitsPerComponent.Should().Be(1);
            ((HpdfName)image.Dict["ColorSpace"]).Value.Should().Be("DeviceGray");
            var parameters = (HpdfDict)((HpdfArray)image.Dict["DecodeParms"])[0];
            ((HpdfNumber)parameters["K"]).Value.Should().Be(-1);
            ((HpdfNumber)parameters["Columns"]).Value.Should().Be(8);
            ((HpdfNumber)parameters["Rows"]).Value.Should().Be(2);
            ((HpdfBoolean)parameters["BlackIs1"]).Value.Should().BeFalse();
        }

        [Fact]
        public void LoadRawImageFromMem_OneBitGray_IsCodedWithGroup4()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 0xF0, 0x0F }, 8, 2,
                HpdfColorSpace.DeviceGray, 1);

            image.StreamObject.Filter.Should().Be(HpdfStreamFilter.CcittFaxDecode);
            ((HpdfBoolean)((HpdfDict)((HpdfArray)image.Dict["DecodeParms"])[0])["BlackIs1"]).Value.Should().BeTrue();
        }

        [Fact]
        public void LoadRaw1BitImageFromMem_InvalidArguments_Throw()
        {
            var xref = new HpdfDocument().Xref;

            Action empty = () => HpdfImage.LoadRaw1BitImageFromMem(xref, "Im1", Array.Empty<byte>(), 8, 1, 1);
            Action zeroWidth = () => HpdfImage.LoadRaw1BitImageFromMem(xref, "Im1", new byte[4], 0, 1, 1);
            Action shortLine = () => HpdfImage.LoadRaw1BitImageFromMem(xref, "Im1", new byte[4], 9, 2, 1);
            Action shortData = () => HpdfImage.LoadRaw1BitImageFromMem(xref, "Im1", new byte[3], 8, 4, 1);

            empty.Should().Throw<HpdfException>();
            zeroWidth.Should().Throw<HpdfException>();
            shortLine.Should().Throw<HpdfException>();
            shortData.Should().Throw<HpdfException>();
        }
    }
}
