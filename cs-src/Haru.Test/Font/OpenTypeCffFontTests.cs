using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of fonts made from OpenType files with CFF outlines (.otf, .ttc): a CID font is a
    /// CIDFontType0 font, a simple font a Type1 font, both with a CFF program (FontFile3), never
    /// the OpenType file. A subset holds the glyphs in use without subroutines; the content shows
    /// the glyph IDs of the font file, which the program maps to its glyphs.
    /// </summary>
    public class OpenTypeCffFontTests
    {
        private const string Japanese = "こんにちは世界、「テスト」ABC";

        // ---------- CID fonts ----------

        [Fact]
        public void CidFont_IsACidFontType0WithACffProgram()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932);
            ShowText(doc, font.AsFont(), Japanese);

            Save(doc);

            HpdfDict descendant = CidDescendant(font);
            ((HpdfName)descendant["Subtype"]).Value.Should().Be("CIDFontType0");
            descendant.ContainsKey("CIDToGIDMap").Should().BeFalse("a CIDFontType0 font has no CIDToGIDMap");

            HpdfDict descriptor = CidDescriptor(font);
            descriptor.ContainsKey("FontFile2").Should().BeFalse();
            ((HpdfName)FontFile3(descriptor)["Subtype"]).Value.Should().Be("CIDFontType0C");
            ShouldBeACffProgram(Program(descriptor));
            ((HpdfName)font.Dict["BaseFont"]).Value.Should().MatchRegex(@"^[A-Z]{6}\+NotoSansJP-Regular$");
        }

        // Noto Sans JP has CIDs that are not the glyph IDs (世 is glyph 2358, CID 9513): the
        // program must map the glyph IDs of the content to the right glyphs. The bounds of the
        // glyphs are those of the font (fontTools), at 100 points
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CidFont_ShowsTheGlyphsOfTheText(bool embedSubset)
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 65001);
            font.EmbedSubset = embedSubset;
            ShowText(doc, font.AsFont(), "世A「", 100);

            using var read = PdfDocument.Open(Save(doc));
            var letters = read.GetPage(1).Letters;

            string.Concat(letters.Select(letter => letter.Value)).Should().Be("世A「");
            ShouldHaveTheBounds(letters[0], 51, -76, 952, 831);
            ShouldHaveTheBounds(letters[1], 3, 0, 604, 734);
            ShouldHaveTheBounds(letters[2], 650, 199, 966, 846);
        }

        [Fact]
        public void CidFont_Subset_HoldsTheGlyphsInUseOnly()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932);
            ShowText(doc, font.AsFont(), Japanese);

            byte[] pdf = Save(doc);

            Program(CidDescriptor(font)).Length.Should().BeLessThan(20_000, "the font file has 4.5 MB");
            pdf.Length.Should().BeLessThan(40_000);
        }

        [Fact]
        public void CidFont_EmbedSubsetOff_EmbedsAllGlyphs()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932);
            font.EmbedSubset = false;
            ShowText(doc, font.AsFont(), Japanese);

            Save(doc);

            byte[] program = Program(CidDescriptor(font));
            ShouldBeACffProgram(program);
            program.Length.Should().BeGreaterThan(4_000_000);
            ((HpdfName)font.Dict["BaseFont"]).Value.Should().Be("NotoSansJP-Regular");
        }

        [Fact]
        public void CidFont_SavedTwice_IsTheSameFile()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932);
            ShowText(doc, font.AsFont(), Japanese);

            Save(doc).Should().Equal(Save(doc));
        }

        [Fact]
        public void CidFont_TextAddedAfterASave_IsInTheNextSubset()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932);
            ShowText(doc, font.AsFont(), "世界");
            Save(doc);
            int firstLength = Program(CidDescriptor(font)).Length;

            ShowText(doc, font.AsFont(), "こんにちは");
            using var read = PdfDocument.Open(Save(doc));

            Program(CidDescriptor(font)).Length.Should().BeGreaterThan(firstLength);
            read.GetPage(2).Letters.Should().HaveCount(5).And.OnlyContain(letter => letter.BoundingBox.Width > 0);
        }

        [Fact]
        public void CidFont_ForVerticalWriting_IsACidFontType0()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932, HpdfWritingMode.Vertical);
            ShowText(doc, font.AsFont(), "縦書き「テスト」");

            byte[] pdf = Save(doc);

            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be("Identity-V");
            ((HpdfName)CidDescendant(font)["Subtype"]).Value.Should().Be("CIDFontType0");
            using var read = PdfDocument.Open(pdf);
            string.Concat(read.GetPage(1).Letters.Select(letter => letter.Value)).Should().Be("縦書き「テスト」");
        }

        // A font with CFF outlines tells the vertical origins of its glyphs in its VORG table:
        // 880 by default in Noto Sans JP, 875 for the degree Celsius sign (glyph 499)
        [Fact]
        public void CidFont_ForVerticalWriting_TakesTheOriginsOfTheVorgTable()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.CidKeyedCff, 932, HpdfWritingMode.Vertical);
            ShowText(doc, font.AsFont(), "字℃");

            Save(doc);

            HpdfDict descendant = CidDescendant(font);
            var dw2 = (HpdfArray)descendant["DW2"];
            ((HpdfNumber)dw2[0]).Value.Should().Be(880);

            var w2 = (HpdfArray)descendant["W2"];
            w2.Should().HaveCount(2, "only the degree sign differs from /DW2");
            ((HpdfNumber)w2[0]).Value.Should().Be(499);
            var metrics = (HpdfArray)w2[1];
            ((HpdfNumber)metrics[0]).Value.Should().Be(-1000);
            ((HpdfNumber)metrics[2]).Value.Should().Be(875);
        }

        [Fact]
        public void CidFont_OfACffCollection_IsTheFontOfTheIndex()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F1", TestFonts.CffCollection, 2, 936);
            ShowText(doc, font.AsFont(), "你A", 100);

            byte[] pdf = Save(doc);

            font.BaseFont.Should().Be("NotoSansCJKsc-Regular");
            ShouldBeACffProgram(Program(CidDescriptor(font)));
            Program(CidDescriptor(font)).Length.Should().BeLessThan(10_000, "the collection has one CFF table of 15 MB");
            using var read = PdfDocument.Open(pdf);
            var letters = read.GetPage(1).Letters;
            ShouldHaveTheBounds(letters[0], 20, -77, 955, 833);
            ShouldHaveTheBounds(letters[1], 3, 0, 604, 733);
        }

        [Fact]
        public void CidFont_OfAFontWithGlyphNames_IsACidFontType0()
        {
            // A CFF program with glyph names is written CID-keyed for a CID font
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.NameKeyedCff, 65001);
            ShowText(doc, font.AsFont(), "gAÆ", 100);

            byte[] pdf = Save(doc);

            ((HpdfName)FontFile3(CidDescriptor(font))["Subtype"]).Value.Should().Be("CIDFontType0C");
            using var read = PdfDocument.Open(pdf);
            var letters = read.GetPage(1).Letters;
            string.Concat(letters.Select(letter => letter.Value)).Should().Be("gAÆ");
            ShouldHaveTheBounds(letters[0], 37, -229, 508, 514);
            ShouldHaveTheBounds(letters[2], 29, -10, 822, 654);
        }

        // ---------- simple fonts ----------

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SimpleFont_IsAType1FontWithACffProgram(bool embedSubset)
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.NameKeyedCff, true, 1252);
            font.EmbedSubset = embedSubset;
            ShowText(doc, font.AsFont(), "gAÆ façade", 100);

            byte[] pdf = Save(doc);

            ((HpdfName)font.Dict["Subtype"]).Value.Should().Be("Type1");
            font.Descriptor.ContainsKey("FontFile2").Should().BeFalse();
            ((HpdfName)FontFile3(font.Descriptor)["Subtype"]).Value.Should().Be("Type1C");
            ShouldBeACffProgram(Program(font.Descriptor));

            // The encoding names the glyphs as the font does: 0xC6 is the glyph "AE"
            var differences = (HpdfArray)((HpdfDict)font.Dict["Encoding"])["Differences"];
            int firstCode = ((HpdfNumber)differences[0]).Value;
            ((HpdfName)differences[0xC6 - firstCode + 1]).Value.Should().Be("AE");

            using var read = PdfDocument.Open(pdf);
            var letters = read.GetPage(1).Letters;
            string.Concat(letters.Select(letter => letter.Value)).Should().Be("gAÆ façade");
            ShouldHaveTheBounds(letters[0], 37, -229, 508, 514);
            ShouldHaveTheBounds(letters[2], 29, -10, 822, 654);
        }

        [Fact]
        public void SimpleFont_NotEmbedded_IsAType1FontWithoutProgram()
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.NameKeyedCff, false);
            ShowText(doc, font.AsFont(), "Puritan");

            Save(doc);

            ((HpdfName)font.Dict["Subtype"]).Value.Should().Be("Type1");
            font.Descriptor.ContainsKey("FontFile3").Should().BeFalse();
        }

        [Fact]
        public void SimpleFont_SavedTwice_IsTheSameFile()
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.NameKeyedCff, true);
            ShowText(doc, font.AsFont(), "Puritan");

            Save(doc).Should().Equal(Save(doc));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SimpleFont_OfACidKeyedFont_Throws(bool embedding)
        {
            var doc = new HpdfDocument();

            Action load = () => HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.CidKeyedCff, embedding);
            Action pool = () => HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F2", FontDataPool.GetOrLoadTrueType(TestFonts.CidKeyedCff), embedding);

            load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.UnsupportedFontType);
            pool.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.UnsupportedFontType);
        }

        // ---------- helpers ----------

        private static void ShowText(HpdfDocument doc, HpdfFont font, string text, float size = 12)
        {
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(font, size);
            page.MoveTextPos(50, 500);
            page.ShowText(text);
            page.EndText();
        }

        private static byte[] Save(HpdfDocument doc)
        {
            using var stream = new MemoryStream();
            doc.Save(stream);
            return stream.ToArray();
        }

        private static HpdfDict CidDescendant(HpdfCIDFont font) => (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];

        private static HpdfDict CidDescriptor(HpdfCIDFont font) => (HpdfDict)CidDescendant(font)["FontDescriptor"];

        private static HpdfStreamObject FontFile3(HpdfDict descriptor) => (HpdfStreamObject)descriptor["FontFile3"];

        /// <summary>The embedded font program, not compressed.</summary>
        private static byte[] Program(HpdfDict descriptor) => FontFile3(descriptor).Stream.ToArray();

        /// <summary>
        /// Checks that a font program is a CFF program (major version 1), not an OpenType file
        /// ('OTTO') or a collection ('ttcf').
        /// </summary>
        private static void ShouldBeACffProgram(byte[] program)
        {
            program.Length.Should().BeGreaterThan(4);
            program[0].Should().Be(1, "a CFF program starts with its major version");
            program[2].Should().Be(4, "the CFF header has four bytes");
        }

        /// <summary>
        /// Checks the rectangle a reader finds for a glyph at 100 points: the bounds of the glyph
        /// in the font (1000 units per em), so a wrong glyph is told from the right one.
        /// </summary>
        private static void ShouldHaveTheBounds(UglyToad.PdfPig.Content.Letter letter, double xMin, double yMin, double xMax, double yMax)
        {
            letter.BoundingBox.Width.Should().BeApproximately((xMax - xMin) / 10, 1.0, "the width of {0}", letter.Value);
            letter.BoundingBox.Height.Should().BeApproximately((yMax - yMin) / 10, 1.0, "the height of {0}", letter.Value);
        }
    }
}
