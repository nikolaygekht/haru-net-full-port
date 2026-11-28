using AwesomeAssertions;
using Haru.Doc;
using Haru.Encoding;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests the ports of HPDF_Font_GetCapHeight, HPDF_Font_GetEncodingName, HPDF_Font_TextWidth,
    /// HPDF_Encoder_GetType and HPDF_Encoder_GetByteType.
    /// </summary>
    public class HpdfFontMetricsAndEncoderTests
    {
        [Fact]
        public void GetCapHeight_OfEveryFontKind()
        {
            var doc = new HpdfDocument();
            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "T1", TestFonts.DemoResource(TestFonts.Roboto), false, 1252);
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "P1",
                TestFonts.DemoResource("demo/type1/a010013l.afm"), TestFonts.DemoResource("demo/type1/a010013l.pfb"), 1252);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "C1", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            using var predefined = HpdfCIDFontType0.Create(doc, "MS-Gothic", "C2", "90ms-RKSJ-H");

            doc.GetFont("Helvetica", null).GetCapHeight().Should().Be(718, "the AFM of Helvetica");
            doc.GetFont("Times-Roman", null).GetCapHeight().Should().Be(662);
            trueType.AsFont().GetCapHeight().Should().BeInRange(650, 750);
            type1.AsFont().GetCapHeight().Should().BeGreaterThan(0);
            cid.AsFont().GetCapHeight().Should().BeGreaterThan(0);
            predefined.AsFont().GetCapHeight().Should().BeGreaterThan(0);
        }

        [Fact]
        public void EncodingName_OfEveryFontKind()
        {
            var doc = new HpdfDocument();
            using var cyrillic = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "T1", TestFonts.DemoResource(TestFonts.Roboto), false, 1251);
            using var latin2 = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "T2", TestFonts.DemoResource(TestFonts.Roboto), false, 28592);
            using var horizontal = HpdfCIDFont.LoadFromTrueTypeFile(doc, "C1", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "C2", TestFonts.DemoResource(TestFonts.Japanese), 65001,
                HpdfWritingMode.Vertical);
            using var predefined = HpdfCIDFontType0.Create(doc, "MS-Gothic", "C3", "90ms-RKSJ-H");

            doc.GetFont("Helvetica", null).EncodingName.Should().Be("WinAnsiEncoding");
            doc.GetFont("Symbol", null).EncodingName.Should().Be("FontSpecific");
            cyrillic.AsFont().EncodingName.Should().Be("CP1251");
            latin2.AsFont().EncodingName.Should().Be("ISO8859-2");
            horizontal.AsFont().EncodingName.Should().Be("Identity-H");
            vertical.AsFont().EncodingName.Should().Be("Identity-V");
            predefined.AsFont().EncodingName.Should().Be("90ms-RKSJ-H");
        }

        [Theory]
        [InlineData("ab c", 4u, 2u, 1u)]
        [InlineData("ab ", 3u, 1u, 1u)]
        [InlineData("word", 4u, 1u, 0u)]
        [InlineData("", 0u, 0u, 0u)]
        public void TextWidth_CountsAsTheCLibrary(string text, uint chars, uint words, uint spaces)
        {
            var font = new HpdfDocument().GetFont("Helvetica", null);

            var width = font.TextWidth(text);

            width.NumChars.Should().Be(chars);
            width.NumWords.Should().Be(words);
            width.NumSpace.Should().Be(spaces);
            width.Width.Should().Be((uint)System.Math.Round(font.MeasureText(text, 1000)));
        }

        [Fact]
        public void TextWidth_SurrogatePairIsOneCharacter()
        {
            var doc = new HpdfDocument();
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "C1", TestFonts.DemoResource(TestFonts.Japanese), 65001);

            cid.AsFont().TextWidth("a\U00020B9F").NumChars.Should().Be(2);
        }

        [Fact]
        public void SingleByteEncoder_TypeAndByteType()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding");
            byte[] text = { 0x41, 0x82 };

            encoder.Type.Should().Be(HpdfEncoderType.SingleByte);
            encoder.WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            encoder.GetByteType(text, 1).Should().Be(HpdfByteType.Single);
            encoder.GetByteType(text, 2).Should().Be(HpdfByteType.Unknown);
        }

        [Fact]
        public void CMapEncoder_TypeAndByteType()
        {
            var encoder = CIDEncoder.Create("90ms-RKSJ-H");
            byte[] text = encoder.EncodeText("aあb");

            encoder.Type.Should().Be(HpdfEncoderType.DoubleByte);
            text.Length.Should().Be(4);
            encoder.GetByteType(text, 0).Should().Be(HpdfByteType.Single);
            encoder.GetByteType(text, 1).Should().Be(HpdfByteType.Lead);
            encoder.GetByteType(text, 2).Should().Be(HpdfByteType.Trial);
            encoder.GetByteType(text, 3).Should().Be(HpdfByteType.Single);
            encoder.GetByteType(text, -1).Should().Be(HpdfByteType.Unknown);
            CIDEncoder.Create("90ms-RKSJ-V").WritingMode.Should().Be(HpdfWritingMode.Vertical);
        }
    }
}
