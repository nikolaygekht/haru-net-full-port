using System;
using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests that text is measured with the widths of the glyphs it is drawn with: the bytes of the
    /// font's encoding are decoded to Unicode and looked up in the font's metrics (as in the C library).
    /// </summary>
    public class HpdfFontWidthTests
    {
        // ---------- standard fonts (widths from the Adobe Font Metrics) ----------

        [Theory]
        [InlineData("é", 556)]
        [InlineData("©", 737)]
        [InlineData("€", 556)]
        [InlineData("ß", 611)]
        [InlineData("ü", 556)]
        [InlineData("A", 667)]
        public void Helvetica_WinAnsi_MeasuresAccentedCharacters(string text, int width)
        {
            var font = new HpdfDocument().GetFont("Helvetica", null);

            font.MeasureText(text, 1000).Should().Be(width);
        }

        [Fact]
        public void TimesRoman_WinAnsi_MeasuresAccentedCharacters()
        {
            var font = new HpdfDocument().GetFont("Times-Roman", null);

            font.MeasureText("é", 1000).Should().Be(444);
        }

        [Theory]
        [InlineData("Ł", 556)]
        [InlineData("ř", 333)]
        [InlineData("ő", 556)]
        public void Helvetica_CentralEuropean_MeasuresCodePageCharacters(string text, int width)
        {
            var doc = new HpdfDocument();
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1", 1250);

            font.MeasureText(text, 1000).Should().Be(width);
        }

        [Fact]
        public void Symbol_MeasuresByBuiltInEncoding()
        {
            var font = new HpdfFont(new HpdfDocument().Xref, HpdfStandardFont.Symbol, "F1");

            // "a" is alpha in the Symbol font's own encoding
            font.MeasureText("a", 1000).Should().Be(631);
        }

        [Fact]
        public void GetCharWidth_IsTheWidthOfTheEncodingByte()
        {
            var font = new HpdfDocument().GetFont("Helvetica", null);

            // 0xE9 is "é" in WinAnsi; in StandardEncoding it would be "Oslash" (778)
            font.GetCharWidth(0xE9).Should().Be(556);
        }

        // ---------- TrueType and Type1 fonts ----------

        [Fact]
        public void TrueType_Cyrillic_IsMeasuredWithItsGlyphs()
        {
            var doc = new HpdfDocument();
            var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", DemoResource("demo/ttfont/Roboto-Regular.ttf"), true, 1251);
            var font = ttf.AsFont();

            // 0xDF is "Я" in code page 1251; the width must come from the glyph of U+042F
            font.GetCharWidth(0xDF).Should().Be(ttf.GetGlyphWidth(ttf.GetGlyphId(0x042F)));
            font.MeasureText("Я", 1000).Should().NotBe(font.MeasureText("/", 1000), "(byte)'Я' is '/'");
            font.MeasureText("ЯЯ", 1000).Should().BeApproximately(2 * font.MeasureText("Я", 1000), 0.001f);
        }

        [Fact]
        public void Type1_CentralEuropean_IsMeasuredWithItsGlyphs()
        {
            var doc = new HpdfDocument();
            var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                DemoResource("demo/type1/a010013l.afm"), DemoResource("demo/type1/a010013l.pfb"), 1250);
            var font = type1.AsFont();

            // "ł" (lslash) is 300 units wide in this font; (byte)'ł' would be 'B'
            font.MeasureText("ł", 1000).Should().Be(300);
        }

        private static string DemoResource(string relativePath)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "Haru.Demos", "Resource", relativePath);
                if (File.Exists(candidate))
                    return candidate;
            }
            throw new FileNotFoundException("Demo resource not found", relativePath);
        }
    }
}
