using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of the embedded font programs: TrueType and CID fonts are embedded as a subset with the
    /// outlines of the glyphs in use (glyph IDs unchanged), written when the document is saved, and
    /// the embedding permissions of a font (fsType) are honored.
    /// </summary>
    public class HpdfFontSubsetTests
    {
        private const string Japanese = TestFonts.Japanese;
        private const string Roboto = TestFonts.Roboto;

        // ---------- CID fonts ----------

        [Fact]
        public void CidFont_IsEmbeddedAsASubsetOfTheGlyphsShown()
        {
            string fontFile = TestFonts.DemoResource(Japanese);
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", fontFile, 932);
            ShowText(doc, cidFont.AsFont(), "こんにちは世界 ABC");

            byte[] pdf = Save(doc);

            long fileSize = new FileInfo(fontFile).Length;
            EmbeddedLength(CidDescriptor(cidFont)).Should().BeLessThan((int)(fileSize / 10));
            pdf.Length.Should().BeLessThan(300_000, "the font file has {0} bytes", fileSize);

            string name = ((HpdfName)cidFont.Dict["BaseFont"]).Value;
            name.Should().MatchRegex(@"^[A-Z]{6}\+" + cidFont.BaseFont + "$");
            ((HpdfName)CidDescendant(cidFont)["BaseFont"]).Value.Should().Be(name);
            ((HpdfName)CidDescriptor(cidFont)["FontName"]).Value.Should().Be(name);
        }

        [Fact]
        public void CidFont_Subset_KeepsTheOutlinesOfTheGlyphsShown()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont.AsFont(), "こんにちは世界ABC");

            using var read = PdfDocument.Open(Save(doc));
            var letters = read.GetPage(1).Letters;

            string.Concat(letters.Select(letter => letter.Value)).Should().Be("こんにちは世界ABC");
            letters.Should().OnlyContain(letter => letter.GlyphRectangle.Width > 0 && letter.GlyphRectangle.Height > 0);
        }

        [Fact]
        public void CidFont_TextAddedAfterASave_IsInTheNextSubset()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont.AsFont(), "世界");
            Save(doc);
            string firstName = ((HpdfName)cidFont.Dict["BaseFont"]).Value;
            int firstLength = EmbeddedLength(CidDescriptor(cidFont));

            ShowText(doc, cidFont.AsFont(), "こんにちは");
            using var read = PdfDocument.Open(Save(doc));

            ((HpdfName)cidFont.Dict["BaseFont"]).Value.Should().NotBe(firstName, "another subset has another tag");
            EmbeddedLength(CidDescriptor(cidFont)).Should().BeGreaterThan(firstLength);
            read.GetPage(2).Letters.Should().HaveCount(5)
                .And.OnlyContain(letter => letter.GlyphRectangle.Width > 0);
        }

        [Fact]
        public void CidFont_Subset_SavedTwice_IsTheSameFile()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont.AsFont(), "こんにちは世界");

            Save(doc).Should().Equal(Save(doc));
        }

        [Fact]
        public void CidFont_EmbedSubsetOff_EmbedsTheWholeFont()
        {
            string fontFile = TestFonts.DemoResource(Japanese);
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", fontFile, 932);
            cidFont.EmbedSubset = false;
            ShowText(doc, cidFont.AsFont(), "こんにちは世界");

            Save(doc);

            EmbeddedLength(CidDescriptor(cidFont)).Should().Be((int)new FileInfo(fontFile).Length);
            ((HpdfName)cidFont.Dict["BaseFont"]).Value.Should().Be(cidFont.BaseFont);
            ((HpdfName)CidDescriptor(cidFont)["FontName"]).Value.Should().Be(cidFont.BaseFont);
        }

        [Fact]
        public void CidFont_SwitchingBackToTheWholeFont_RemovesTheSubsetTag()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont.AsFont(), "世界");
            Save(doc);
            ((HpdfName)cidFont.Dict["BaseFont"]).Value.Should().Contain("+");

            cidFont.EmbedSubset = false;
            Save(doc);

            ((HpdfName)cidFont.Dict["BaseFont"]).Value.Should().Be(cidFont.BaseFont);
        }

        [Fact]
        public void CidFont_RestrictedLicense_Throws()
        {
            string path = TestFonts.WriteTempFile(TestFonts.WithFsType(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), 0x0002));
            try
            {
                var doc = new HpdfDocument();

                Action load = () => HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", path, 65001).Dispose();

                load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.TtfCannotEmbeddingFont);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ---------- simple TrueType fonts ----------

        [Theory]
        [InlineData(1252, "The quick brown fox: éüß")]
        [InlineData(1251, "Съешь ещё этих булок")]
        public void TrueTypeFont_IsEmbeddedAsASubsetOfItsCodePage(int codePage, string text)
        {
            byte[] fontData = File.ReadAllBytes(TestFonts.DemoResource(Roboto));
            var doc = new HpdfDocument();
            using var ttf = Load(doc, fontData, true, codePage);
            ShowText(doc, ttf.AsFont(), text);

            using var read = PdfDocument.Open(Save(doc));

            EmbeddedLength(Descriptor(ttf)).Should().BeLessThan(fontData.Length / 2);
            string name = ((HpdfName)ttf.Dict["BaseFont"]).Value;
            name.Should().MatchRegex(@"^[A-Z]{6}\+" + ttf.BaseFont + "$");
            ((HpdfName)Descriptor(ttf)["FontName"]).Value.Should().Be(name);

            var letters = read.GetPage(1).Letters.Where(letter => letter.Value != " ").ToList();
            string expected = (text ?? string.Empty).Replace(" ", "", StringComparison.Ordinal);
            string.Concat(letters.Select(letter => letter.Value)).Should().Be(expected);
            letters.Should().OnlyContain(letter => letter.GlyphRectangle.Width > 0 && letter.GlyphRectangle.Height > 0);
        }

        [Fact]
        public void TrueTypeFont_SubsetsOfDifferentCodePages_HaveDifferentNames()
        {
            byte[] fontData = File.ReadAllBytes(TestFonts.DemoResource(Roboto));
            var doc = new HpdfDocument();
            using var western = Load(doc, fontData, true, 1252);
            using var cyrillic = Load(doc, fontData, true, 1251);
            doc.AddPage();

            Save(doc);

            ((HpdfName)western.Dict["BaseFont"]).Value.Should().NotBe(((HpdfName)cyrillic.Dict["BaseFont"]).Value);
        }

        [Fact]
        public void TrueTypeFont_EmbedSubsetOff_EmbedsTheWholeFont()
        {
            byte[] fontData = File.ReadAllBytes(TestFonts.DemoResource(Roboto));
            var doc = new HpdfDocument();
            using var ttf = Load(doc, fontData, true, 1252);
            ttf.EmbedSubset = false;
            ShowText(doc, ttf.AsFont(), "Hello");

            Save(doc);

            EmbeddedLength(Descriptor(ttf)).Should().Be(fontData.Length);
            ((HpdfName)ttf.Dict["BaseFont"]).Value.Should().Be(ttf.BaseFont);
        }

        [Fact]
        public void TrueTypeFont_Subset_SavedTwice_IsTheSameFile()
        {
            var doc = new HpdfDocument();
            using var ttf = Load(doc, File.ReadAllBytes(TestFonts.DemoResource(Roboto)), true, 1252);
            ShowText(doc, ttf.AsFont(), "Hello");

            Save(doc).Should().Equal(Save(doc));
        }

        [Fact]
        public void TrueTypeFont_NotEmbedded_HasNoFontFile()
        {
            var doc = new HpdfDocument();
            using var ttf = Load(doc, File.ReadAllBytes(TestFonts.DemoResource(Roboto)), false, 1252);
            ShowText(doc, ttf.AsFont(), "Hello");

            Save(doc);

            Descriptor(ttf).ContainsKey("FontFile2").Should().BeFalse();
            ((HpdfName)ttf.Dict["BaseFont"]).Value.Should().Be(ttf.BaseFont);
        }

        // ---------- embedding permissions (fsType) ----------

        [Theory]
        [InlineData(0x0002)] // restricted license embedding
        [InlineData(0x0200)] // bitmap embedding only
        [InlineData(0x0202)]
        public void TrueTypeFont_ThatMustNotBeEmbedded_Throws(int fsType)
        {
            byte[] fontData = TestFonts.WithFsType(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), (ushort)fsType);
            var doc = new HpdfDocument();

            Action load = () => Load(doc, fontData, true, 1252).Dispose();

            load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.TtfCannotEmbeddingFont);
        }

        [Fact]
        public void TrueTypeFont_ThatMustNotBeEmbedded_CanBeUsedWithoutEmbedding()
        {
            byte[] fontData = TestFonts.WithFsType(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), 0x0002);
            var doc = new HpdfDocument();

            using var ttf = Load(doc, fontData, false, 1252);

            ttf.AsFont().MeasureText("A", 1000).Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(0x0000)] // installable embedding
        [InlineData(0x0004)] // preview and print embedding
        [InlineData(0x0008)] // editable embedding
        [InlineData(0x0006)] // restricted is in force only when it is the only level
        public void TrueTypeFont_ThatPermitsEmbedding_IsEmbeddedAsASubset(int fsType)
        {
            byte[] fontData = TestFonts.WithFsType(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), (ushort)fsType);
            var doc = new HpdfDocument();
            using var ttf = Load(doc, fontData, true, 1252);
            doc.AddPage();

            Save(doc);

            EmbeddedLength(Descriptor(ttf)).Should().BeLessThan(fontData.Length);
        }

        [Fact]
        public void TrueTypeFont_ThatForbidsSubsetting_IsEmbeddedInFull()
        {
            byte[] fontData = TestFonts.WithFsType(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), 0x0100);
            var doc = new HpdfDocument();
            using var ttf = Load(doc, fontData, true, 1252);
            doc.AddPage();

            Save(doc);

            EmbeddedLength(Descriptor(ttf)).Should().Be(fontData.Length);
            ((HpdfName)ttf.Dict["BaseFont"]).Value.Should().Be(ttf.BaseFont);
        }

        [Fact]
        public void TrueTypeFont_WithoutTrueTypeOutlines_IsEmbeddedInFull()
        {
            // Without a glyf table (as in an OpenType font with CFF outlines) there is nothing to select from
            byte[] fontData = TestFonts.WithRenamedTable(File.ReadAllBytes(TestFonts.DemoResource(Roboto)), "glyf", "glyx");
            var doc = new HpdfDocument();
            using var ttf = Load(doc, fontData, true, 1252);
            doc.AddPage();

            Save(doc);

            EmbeddedLength(Descriptor(ttf)).Should().Be(fontData.Length);
            ((HpdfName)ttf.Dict["BaseFont"]).Value.Should().Be(ttf.BaseFont);
        }

        // ---------- helpers ----------

        private static HpdfTrueTypeFont Load(HpdfDocument doc, byte[] fontData, bool embedding, int codePage)
        {
            using var stream = new MemoryStream(fontData);
            return HpdfTrueTypeFont.LoadFromStream(doc.Xref, "F" + doc.Xref.Entries.Count, stream, fontData, embedding, codePage);
        }

        private static void ShowText(HpdfDocument doc, HpdfFont font, string text)
        {
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(font, 12);
            page.MoveTextPos(50, 700);
            page.ShowText(text);
            page.EndText();
        }

        private static byte[] Save(HpdfDocument doc)
        {
            using var stream = new MemoryStream();
            doc.Save(stream);
            return stream.ToArray();
        }

        private static HpdfDict Descriptor(HpdfTrueTypeFont font) => (HpdfDict)font.Dict["FontDescriptor"];

        private static HpdfDict CidDescendant(HpdfCIDFont font) => (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];

        private static HpdfDict CidDescriptor(HpdfCIDFont font) => (HpdfDict)CidDescendant(font)["FontDescriptor"];

        /// <summary>The length of the embedded font program (Length1 of FontFile2).</summary>
        private static int EmbeddedLength(HpdfDict descriptor)
        {
            var fontFile = (HpdfDict)descriptor["FontFile2"];
            return ((HpdfNumber)fontFile["Length1"]).Value;
        }
    }
}
