using System;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Tests of the text shown with an embedded CID font (<see cref="HpdfCIDFont"/>): the content
    /// holds glyph IDs, so /ToUnicode and /W are written from the glyphs in use when the document
    /// is saved, and text is measured with the glyphs it is drawn with.
    /// </summary>
    public class HpdfCIDFontTextTests
    {
        private const string Japanese = TestFonts.Japanese;
        private const string SimplifiedChinese = TestFonts.SimplifiedChinese;
        private const string TraditionalChinese = TestFonts.TraditionalChinese;
        private const string Korean = TestFonts.Korean;

        // ---------- text extraction (/ToUnicode) ----------

        [Theory]
        [InlineData(Japanese, 932, "こんにちは世界")]
        [InlineData(Japanese, 65001, "こんにちは世界")]
        [InlineData(Japanese, 932, "日本語 ABC 123")]
        [InlineData(SimplifiedChinese, 936, "你好世界")]
        [InlineData(SimplifiedChinese, 65001, "简体中文")]
        [InlineData(TraditionalChinese, 950, "繁體中文")]
        [InlineData(Korean, 949, "안녕하세요")]
        [InlineData(Korean, 65001, "한국어")]
        public void ShownText_IsExtractedAsItWasWritten(string fontFile, int codePage, string text)
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(fontFile), codePage);
            ShowText(doc, cidFont, text);

            ExtractText(Save(doc)).Should().Be(WithoutSpaces(text));
        }

        [Fact]
        public void GlyphSharedByTwoCharacters_IsExtractedAsTheCharacterShown()
        {
            // The font draws U+65E5 (the kanji "day") and the Kangxi radical U+2F47 with one glyph
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 65001);
            ShowText(doc, cidFont, "日");

            ExtractText(Save(doc)).Should().Be("日");
        }

        [Fact]
        public void TextAddedAfterASave_IsExtractedAfterTheNextSave()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont, "世界");
            ExtractText(Save(doc)).Should().Be("世界");

            ShowText(doc, cidFont, "こんにちは");

            ExtractText(Save(doc)).Should().Be("世界こんにちは");
        }

        [Fact]
        public void Save_Twice_WritesTheSameFile()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont, "こんにちは世界");

            byte[] first = Save(doc);
            byte[] second = Save(doc);

            second.Should().Equal(first);
        }

        [Fact]
        public void FontWithoutText_IsSavedAndReadable()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);

            byte[] pdf = Save(doc);

            using var read = PdfDocument.Open(pdf);
            read.NumberOfPages.Should().Be(1);
            WidthCount(cidFont).Should().Be(1, "only .notdef has a width");
        }

        // ---------- widths (/W) ----------

        [Fact]
        public void Widths_AreWrittenForTheGlyphsInUseOnly()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont, "こんにちは世界、世界");

            Save(doc);

            // 8 different characters and .notdef
            WidthCount(cidFont).Should().Be(9);
        }

        [Fact]
        public void Widths_HoldTheWidthOfEveryGlyphShown()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ShowText(doc, cidFont, "Hi 世界");
            Save(doc);

            var widths = Widths(cidFont);

            foreach (char c in "Hi 世界")
            {
                ushort glyph = cidFont.GetGlyphId(c);
                widths.Should().ContainKey(glyph);
                cidFont.AsFont().MeasureText(c.ToString(), 1000).Should().Be(widths[glyph], "character {0}", c);
            }
        }

        // ---------- one path from text to glyphs ----------

        [Theory]
        [InlineData(932)]
        [InlineData(936)]
        [InlineData(949)]
        [InlineData(950)]
        [InlineData(65001)]
        public void MeasureText_DoesNotDependOnTheCodePage(int codePage)
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), codePage);
            var font = cidFont.AsFont();

            // "é" is not in the Japanese, Chinese and Korean code pages, where it becomes "?" (or "e")
            // when the text is encoded; it is drawn with its own glyph and must be measured with it
            const string text = "Café 世界";
            float expected = text.Sum(c => (float)Math.Round(cidFont.GetGlyphWidth(cidFont.GetGlyphId(c)) * 1000.0 / UnitsPerEm(cidFont)));

            cidFont.GetGlyphId(0x00E9).Should().NotBe(0, "the test needs a font with this glyph");
            font.MeasureText(text, 1000).Should().Be(expected);
        }

        [Fact]
        public void MeasureText_IsTheSumOfTheWidthsWritten()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            const string text = "こんにちは世界 Hello";
            ShowText(doc, cidFont, text);
            Save(doc);

            var widths = Widths(cidFont);

            cidFont.AsFont().MeasureText(text, 1000).Should().Be(text.Sum(c => widths[cidFont.GetGlyphId(c)]));
        }

        [Fact]
        public void CharacterWithoutGlyph_IsDrawnAsNotdefAndLeftOutOfToUnicode()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            cidFont.GetGlyphId(0x0E01).Should().Be(0, "the test needs a character the font lacks (Thai)");

            byte[] glyphs = cidFont.ConvertTextToGlyphIDs("Aก");

            glyphs.Should().HaveCount(4);
            glyphs.Skip(2).Should().Equal(0, 0);
            ShowText(doc, cidFont, "AกB");
            ExtractText(Save(doc)).Should().StartWith("A").And.EndWith("B").And.NotContain("ก");
        }

        // ---------- characters above U+FFFF (surrogate pairs) ----------

        // U+20BB7, a kanji of CJK Extension B that the Japanese font has
        private const string ExtensionBKanji = "\U00020BB7";

        [Fact]
        public void CharacterOutsideTheBasicPlane_IsDrawnMeasuredAndExtracted()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            ushort glyph = cidFont.GetGlyphId(new Rune(0x20BB7));
            glyph.Should().NotBe(0, "the test needs a font with this glyph");

            // One character, so one glyph, although it is two chars in the string
            byte[] glyphs = cidFont.ConvertTextToGlyphIDs("A" + ExtensionBKanji + "B");
            glyphs.Should().HaveCount(6);
            ((glyphs[2] << 8) | glyphs[3]).Should().Be(glyph);

            float width = (float)Math.Round(cidFont.GetGlyphWidth(glyph) * 1000.0 / UnitsPerEm(cidFont));
            cidFont.AsFont().MeasureText(ExtensionBKanji, 1000).Should().Be(width);

            ShowText(doc, cidFont, "A" + ExtensionBKanji + "B");
            using var read = PdfDocument.Open(Save(doc));
            var letters = read.GetPage(1).Letters;
            string.Concat(letters.Select(letter => letter.Value)).Should().Be("A" + ExtensionBKanji + "B");
            letters.Should().OnlyContain(letter => letter.GlyphRectangle.Width > 0, "the subset has the outlines");
        }

        [Fact]
        public void CharacterOutsideTheBasicPlane_WithoutGlyph_IsOneNotdef()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 65001);
            cidFont.GetGlyphId(new Rune(0x1F600)).Should().Be(0, "the test needs a character the font lacks (an emoji)");

            // A surrogate pair in a string is one character, so one (missing) glyph
            byte[] glyphs = cidFont.ConvertTextToGlyphIDs("A\U0001F600B");

            glyphs.Should().HaveCount(6);
            glyphs.Skip(2).Take(2).Should().Equal(0, 0);
        }

        [Fact]
        public void MeasureText_WithAWidthLimit_KeepsASurrogatePairTogether()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);
            var font = cidFont.AsFont();
            float one = font.MeasureText(ExtensionBKanji, 12);

            // Three characters (six chars); two and a half fit
            int fit = font.MeasureText(ExtensionBKanji + ExtensionBKanji + ExtensionBKanji, 12, one * 2.5f, 0, 0, false, out float realWidth);

            fit.Should().Be(4);
            realWidth.Should().BeApproximately(one * 2, 0.001f);
        }

        [Fact]
        public void FontWithACmapForAllOfUnicodeOnly_MapsEveryCharacter()
        {
            string original = TestFonts.DemoResource(TestFonts.Roboto);
            string path = TestFonts.WriteTempFile(TestFonts.WithFullUnicodeCmapOnly(File.ReadAllBytes(original)));
            try
            {
                var doc = new HpdfDocument();
                using var expected = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", original, 65001);
                using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);

                foreach (char c in "Az \u00E9\u042F\u03A9\u20AC")
                    cidFont.GetGlyphId(c).Should().Be(expected.GetGlyphId(c), "U+{0:X4}", (int)c).And.NotBe(0);

                cidFont.GetGlyphId(new Rune(0x1F16A)).Should().Be(expected.GetGlyphId(new Rune(0x1F16A))).And.NotBe(0);
                cidFont.GetGlyphId(0x4E00).Should().Be(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void GetGlyphId_FindsCharactersOfEverySegment()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(Japanese), 932);

            foreach (char c in " Az~éあア一日龠Ａ")
                cidFont.GetGlyphId(c).Should().NotBe(0, "U+{0:X4}", (int)c);

            cidFont.GetGlyphId(0x0000).Should().Be(0);
            cidFont.GetGlyphId(0x0E01).Should().Be(0);
            cidFont.GetGlyphId(0xFFFF).Should().Be(0);
        }

        // ---------- helpers ----------

        private static void ShowText(HpdfDocument doc, HpdfCIDFont cidFont, string text)
        {
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(cidFont.AsFont(), 12);
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

        private static string ExtractText(byte[] pdf)
        {
            using var read = PdfDocument.Open(pdf);
            return WithoutSpaces(string.Concat(read.GetPages().Select(page => string.Concat(page.Letters.Select(letter => letter.Value)))));
        }

        private static string WithoutSpaces(string? text) => (text ?? string.Empty).Replace(" ", "", StringComparison.Ordinal);

        private static HpdfArray WidthArray(HpdfCIDFont cidFont)
        {
            var descendant = (HpdfDict)((HpdfArray)cidFont.Dict["DescendantFonts"])[0];
            return (HpdfArray)descendant["W"];
        }

        /// <summary>Reads /W of the descendant font as glyph ID -> width.</summary>
        private static System.Collections.Generic.Dictionary<ushort, int> Widths(HpdfCIDFont cidFont)
        {
            var result = new System.Collections.Generic.Dictionary<ushort, int>();
            var w = WidthArray(cidFont);
            for (int i = 0; i < w.Count; i += 2)
            {
                int glyph = ((HpdfNumber)w[i]).Value;
                foreach (var width in (HpdfArray)w[i + 1])
                    result[(ushort)glyph++] = ((HpdfNumber)width).Value;
            }
            return result;
        }

        private static int WidthCount(HpdfCIDFont cidFont) => Widths(cidFont).Count;

        private static int UnitsPerEm(HpdfCIDFont cidFont)
        {
            // The glyph of the ideographic space is one em wide in a CJK font
            return cidFont.GetGlyphWidth(cidFont.GetGlyphId(0x3000));
        }
    }
}
