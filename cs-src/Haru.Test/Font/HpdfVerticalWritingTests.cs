using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of vertical writing: a CID font with a vertical encoding shows text from top to bottom,
    /// and is measured along that direction.
    /// </summary>
    public class HpdfVerticalWritingTests
    {
        // ---------- the vertical CMaps of the predefined CJK fonts ----------

        [Theory]
        [InlineData("GBK-EUC-V", "GB1", 2, 936)]
        [InlineData("GB-EUC-V", "GB1", 0, 20936)]
        [InlineData("ETen-B5-V", "CNS1", 0, 950)]
        [InlineData("90ms-RKSJ-V", "Japan1", 2, 932)]
        [InlineData("EUC-V", "Japan1", 1, 20932)]
        [InlineData("KSCms-UHC-HW-V", "Korea1", 1, 949)]
        [InlineData("KSC-EUC-V", "Korea1", 0, 51949)]
        public void VerticalEncoder_IsCreatedByTheNameOfItsCMap(string name, string ordering, int supplement, int codePage)
        {
            CIDEncoder encoder = CIDEncoder.Create(name);
            CIDEncoder horizontal = CIDEncoder.Create((name ?? string.Empty).Replace("-V", "-H", System.StringComparison.Ordinal));

            encoder.Name.Should().Be(name);
            encoder.WritingMode.Should().Be(HpdfWritingMode.Vertical);
            encoder.Ordering.Should().Be(ordering);
            encoder.Supplement.Should().Be(supplement);
            encoder.CodePage.Should().Be(codePage);

            // The same text, the same codes; only some of the glyphs differ
            horizontal.WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            encoder.EncodeText("A一、").Should().Equal(horizontal.EncodeText("A一、"));
            encoder.GetCID(0x41).Should().Be(horizontal.GetCID(0x41)).And.NotBe(0);
        }

        // The expected CIDs are those of the CMap arrays of libharu (the horizontal array with the
        // vertical one over it). The CIDs of EUC-V are the decimal ones of the Adobe CMap: libharu
        // writes them with a "0x" prefix by mistake
        [Theory]
        // Punctuation and brackets get the glyphs for vertical writing
        [InlineData("GBK-EUC-V", "、", 575, 97)]
        [InlineData("GBK-EUC-V", "（", 580, 269)]
        [InlineData("GB-EUC-V", "（", 580, 269)]
        [InlineData("ETen-B5-V", "（", 130, 128)]
        [InlineData("90ms-RKSJ-V", "、", 7887, 634)]
        [InlineData("90ms-RKSJ-V", "ー", 7891, 660)]
        [InlineData("90ms-RKSJ-V", "「", 7911, 686)]
        [InlineData("EUC-V", "、", 7887, 634)]
        [InlineData("KSCms-UHC-HW-V", "、", 8056, 102)]
        [InlineData("KSC-EUC-V", "（", 8077, 271)]
        // Letters and ideographs are the same glyphs
        [InlineData("GBK-EUC-V", "一", 4162, 4162)]
        [InlineData("90ms-RKSJ-V", "一", 1200, 1200)]
        [InlineData("90ms-RKSJ-V", "A", 264, 264)]
        [InlineData("ETen-B5-V", "、", 101, 101)]
        public void VerticalEncoder_MapsACharacterToItsVerticalGlyph(string name, string character, int verticalCid, int horizontalCid)
        {
            CIDEncoder vertical = CIDEncoder.Create(name);
            CIDEncoder horizontal = CIDEncoder.Create((name ?? string.Empty).Replace("-V", "-H", System.StringComparison.Ordinal));
            byte[] encoded = vertical.EncodeText(character);

            int position = 0;
            vertical.ReadCID(encoded, ref position).Should().Be((ushort)verticalCid);
            position = 0;
            horizontal.ReadCID(encoded, ref position).Should().Be((ushort)horizontalCid);
        }

        [Theory]
        [InlineData("SimSun", "GBK-EUC-V")]
        [InlineData("SimHei", "GB-EUC-V")]
        [InlineData("MingLiU", "ETen-B5-V")]
        [InlineData("MS-Mincho", "90ms-RKSJ-V")]
        [InlineData("MS-Gothic", "EUC-V")]
        [InlineData("BatangChe", "KSCms-UHC-HW-V")]
        [InlineData("DotumChe", "KSC-EUC-V")]
        public void PredefinedFont_WithAVerticalCMap_IsAVerticalFont(string fontName, string encoding)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), fontName, "F1", encoding);
            var descendant = (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];

            font.WritingMode.Should().Be(HpdfWritingMode.Vertical);
            font.AsFont().WritingMode.Should().Be(HpdfWritingMode.Vertical);
            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be(encoding);

            // The origin of a glyph is 880 above its baseline, a glyph moves the text position
            // down by 1000
            var dw2 = (HpdfArray)descendant["DW2"];
            dw2.Count.Should().Be(2);
            ((HpdfNumber)dw2[0]).Value.Should().Be(880);
            ((HpdfNumber)dw2[1]).Value.Should().Be(-1000);
        }

        [Fact]
        public void VerticalFont_IsMeasuredAlongTheColumn()
        {
            var doc = new HpdfDocument();
            using var vertical = HpdfCIDFontType0.Create(doc, "MS-PMincho", "F1", "90ms-RKSJ-V");
            using var horizontal = HpdfCIDFontType0.Create(doc, "MS-PMincho", "F2", "90ms-RKSJ-H");

            // Every character takes the font size of the column, the narrow ones too
            vertical.MeasureText("日本語i.", 12).Should().Be(5 * 12);
            vertical.GetCharWidth((byte)'i').Should().Be(1000);
            vertical.AsFont().MeasureText("日本", 10).Should().Be(20);

            horizontal.MeasureText("日本語i.", 12).Should().BeLessThan(5 * 12);
        }

        [Fact]
        public void OtherFonts_AreHorizontal()
        {
            var doc = new HpdfDocument();
            using var predefined = HpdfCIDFontType0.Create(doc, "SimSun", "F1", "GBK-EUC-H");
            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F2", TestFonts.DemoResource(TestFonts.Roboto), true);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F3", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            var descendant = (HpdfDict)((HpdfArray)predefined.Dict["DescendantFonts"])[0];

            predefined.WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            descendant.ContainsKey("DW2").Should().BeFalse();
            trueType.AsFont().WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            cid.AsFont().WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            doc.GetFont("Helvetica").WritingMode.Should().Be(HpdfWritingMode.Horizontal);
        }

        [Fact]
        public void RegisteredVerticalFont_IsFoundByTheNameOfItsEncoding()
        {
            var doc = new HpdfDocument();
            using var mincho = HpdfCIDFontType0.Create(doc, "MS-Mincho", "F1", "90ms-RKSJ-V");
            var font = mincho.AsFont();

            doc.RegisterFont(font, "90ms-RKSJ-V");

            doc.GetFont("MS-Mincho", "90ms-rksj-v").Should().BeSameAs(font);
        }
    }
}
