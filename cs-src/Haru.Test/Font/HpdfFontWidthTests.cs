using System;
using System.Collections.Generic;
using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
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
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", DemoResource("demo/ttfont/Roboto-Regular.ttf"), true, 1251);
            var font = ttf.AsFont();

            // 0xDF is "Я" in code page 1251; the width must come from the glyph of U+042F,
            // in 1000-unit glyph space as for every other font (Roboto has 2048 units per em)
            int glyphWidth = ttf.GetGlyphWidth(ttf.GetGlyphId(0x042F));
            font.GetCharWidth(0xDF).Should().Be((float)Math.Round(glyphWidth * 1000.0 / 2048));
            font.GetCharWidth((byte)'A').Should().BeInRange(600, 700);
            font.MeasureText("Я", 1000).Should().NotBe(font.MeasureText("/", 1000), "(byte)'Я' is '/'");
            font.MeasureText("ЯЯ", 1000).Should().BeApproximately(2 * font.MeasureText("Я", 1000), 0.001f);
        }

        [Fact]
        public void Type1_CentralEuropean_IsMeasuredWithItsGlyphs()
        {
            var doc = new HpdfDocument();
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                DemoResource("demo/type1/a010013l.afm"), DemoResource("demo/type1/a010013l.pfb"), 1250);
            var font = type1.AsFont();

            // "ł" (lslash) is 300 units wide in this font; (byte)'ł' would be 'B'
            font.MeasureText("ł", 1000).Should().Be(300);
        }

        // ---------- characters that share a glyph ----------

        [Theory]
        [InlineData("Helvetica", 278, 333)]
        [InlineData("Courier", 600, 600)]
        [InlineData("Times-Roman", 250, 333)]
        public void StandardFont_NonBreakingSpaceAndSoftHyphen_AreTheSpaceAndHyphenGlyphs(string name, int space, int hyphen)
        {
            var font = new HpdfDocument().GetFont(name, null);

            font.MeasureText(" ", 1000).Should().Be(space);
            font.MeasureText("\u00A0", 1000).Should().Be(space);
            font.MeasureText("-", 1000).Should().Be(hyphen);
            font.MeasureText("\u00AD", 1000).Should().Be(hyphen);
            font.GetCharWidth(0xA0).Should().Be(space);
            font.GetCharWidth(0xAD).Should().Be(hyphen);
        }

        [Fact]
        public void StandardFont_NonBreakingSpace_InCentralEuropeanEncoding()
        {
            var font = new HpdfFont(new HpdfDocument().Xref, HpdfStandardFont.Helvetica, "F1", 1250);

            font.MeasureText("\u00A0", 1000).Should().Be(278);
            font.MeasureText("\u00AD", 1000).Should().Be(333);
        }

        [Fact]
        public void StandardFontWidths_HaveNoGlyphForTheEndOfTableMark()
        {
            var widths = new HpdfStandardFontWidths(HpdfStandardFont.Helvetica);

            widths.HasGlyph(0xFFFF).Should().BeFalse();
            widths.GetWidthForUnicode(0xFFFF).Should().Be(500);
            widths.HasGlyph(0x00A0).Should().BeTrue();
        }

        [Fact]
        public void Type1_NonBreakingSpaceAndSoftHyphen_AreTheSpaceAndHyphenGlyphs()
        {
            var doc = new HpdfDocument();
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                DemoResource("demo/type1/a010013l.afm"), DemoResource("demo/type1/a010013l.pfb"), 1252);
            var font = type1.AsFont();

            font.MeasureText("\u00A0", 1000).Should().Be(font.MeasureText(" ", 1000));
            font.MeasureText("\u00AD", 1000).Should().Be(font.MeasureText("-", 1000));
        }

        [Fact]
        public void Type1_Greek_MuIsDrawnWithTheMuGlyph()
        {
            var doc = new HpdfDocument();
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                DemoResource("demo/type1/a010013l.afm"), DemoResource("demo/type1/a010013l.pfb"), 1253);
            var font = type1.AsFont();

            // 0xEC is the Greek letter mu (U+03BC) in code page 1253; the font names the glyph "mu"
            // and lists it as the micro sign (U+00B5)
            DifferenceNames(font)[0xEC].Should().Be("mu");
            font.GetCharWidth(0xEC).Should().Be(font.GetCharWidth(0xB5)).And.NotBe(500);
        }

        // ---------- measured widths are the widths written to the file ----------

        [Theory]
        [InlineData(1252)]
        [InlineData(1251)]
        public void TrueType_MeasuresWithTheWidthsOfTheWidthsArray(int codePage)
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", DemoResource("demo/ttfont/Roboto-Regular.ttf"), true, codePage);
            var font = ttf.AsFont();

            int[] widths = WidthsArray(font, out int firstChar);
            for (int code = firstChar; code < firstChar + widths.Length; code++)
                font.GetCharWidth((byte)code).Should().Be(widths[code - firstChar], "code {0}", code);

            string text = codePage == 1251 ? "Съешь ещё этих булок" : "The quick brown fox";
            int expected = 0;
            foreach (byte b in System.Text.Encoding.GetEncoding(codePage).GetBytes(text))
                expected += widths[b - firstChar];
            font.MeasureText(text, 1000).Should().Be(expected);
        }

        [Fact]
        public void Type1_MeasuresWithTheWidthsOfTheWidthsArray()
        {
            var doc = new HpdfDocument();
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                DemoResource("demo/type1/a010013l.afm"), DemoResource("demo/type1/a010013l.pfb"), 1250);
            var font = type1.AsFont();

            int[] widths = WidthsArray(font, out int firstChar);
            for (int code = firstChar; code < firstChar + widths.Length; code++)
                font.GetCharWidth((byte)code).Should().Be(widths[code - firstChar], "code {0}", code);
        }

        // ---------- /Differences: the glyph drawn for a byte is the glyph measured for it ----------

        [Theory]
        [InlineData(1250, 0xFF, "dotaccent")]
        [InlineData(1251, 0xC0, "afii10017")]
        [InlineData(1251, 0xFF, "afii10097")]
        [InlineData(1253, 0xD3, "Sigma")]
        [InlineData(1253, 0xFE, "omegatonos")]
        [InlineData(1257, 0xFE, "zcaron")]
        [InlineData(20866, 0xC0, "afii10096")]
        [InlineData(20866, 0xFF, "afii10044")]
        [InlineData(28592, 0xFF, "dotaccent")]
        [InlineData(28593, 0xFE, "scircumflex")]
        [InlineData(28595, 0xFF, "afii10193")]
        [InlineData(28597, 0xFE, "omegatonos")]
        [InlineData(28604, 0xFE, "ycircumflex")]
        [InlineData(28606, 0xFE, "tcommaaccent")]
        public void StandardFont_Differences_PutEveryGlyphNameOnItsOwnCode(int codePage, int code, string glyphName)
        {
            var font = new HpdfFont(new HpdfDocument().Xref, HpdfStandardFont.Helvetica, "F1", codePage);

            DifferenceNames(font).Should().Contain(code, glyphName);
        }

        [Fact]
        public void StandardFont_Differences_KeepTheCodesAfterACharacterWithoutAGlyphName()
        {
            var font = new HpdfFont(new HpdfDocument().Xref, HpdfStandardFont.Helvetica, "F1", 1251);

            var names = DifferenceNames(font);

            // 0x7F has no glyph name; it must keep its place, otherwise every name after it
            // moves down one code
            names[0x7F].Should().Be(".notdef");
            names[0x80].Should().Be("afii10051");
            names[0x81].Should().Be("afii10052");
            names[0x83].Should().Be("afii10100");
        }

        [Fact]
        public void StandardFont_Romanian_CommaBelowLettersAreDrawnAndMeasured()
        {
            var font = new HpdfFont(new HpdfDocument().Xref, HpdfStandardFont.Helvetica, "F1", 28606);

            var names = DifferenceNames(font);

            // ISO 8859-16: 0xAA / 0xBA are S / s with comma below, 0xDE / 0xFE are T / t with comma below
            names[0xAA].Should().Be("Scommaaccent");
            names[0xBA].Should().Be("scommaaccent");
            names[0xDE].Should().Be("Tcommaaccent");
            names[0xFE].Should().Be("tcommaaccent");
            font.GetCharWidth(0xDE).Should().Be(611);
            font.GetCharWidth(0xFE).Should().Be(278);
        }

        /// <summary>Reads /Encoding /Differences of a font as code -> glyph name.</summary>
        private static Dictionary<int, string> DifferenceNames(HpdfFont font)
        {
            var names = new Dictionary<int, string>();
            if (font.Dict["Encoding"] is not HpdfDict encoding || !encoding.TryGetValue("Differences", out var value) || value is not HpdfArray differences)
                return names;

            int code = -1;
            foreach (var item in differences)
            {
                if (item is HpdfNumber number)
                    code = number.Value;
                else
                    names[code++] = ((HpdfName)item).Value;
            }
            return names;
        }

        /// <summary>Reads /Widths and /FirstChar of a simple font.</summary>
        private static int[] WidthsArray(HpdfFont font, out int firstChar)
        {
            firstChar = ((HpdfNumber)font.Dict["FirstChar"]).Value;
            var array = (HpdfArray)font.Dict["Widths"];
            var widths = new int[array.Count];
            for (int i = 0; i < widths.Length; i++)
                widths[i] = ((HpdfNumber)array[i]).Value;
            return widths;
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
