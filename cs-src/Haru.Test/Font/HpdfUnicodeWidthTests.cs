using System.Text;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests HpdfFont.GetUnicodeWidth (HPDF_Font_GetUnicodeWidth): the width of the font's glyph for a Unicode
    /// character, whatever the font's encoding; 0 when the font has no glyph for it.
    /// </summary>
    public class HpdfUnicodeWidthTests
    {
        [Fact]
        public void StandardFont_WidthsByUnicode_WhateverTheEncoding()
        {
            var doc = new HpdfDocument();
            var winAnsi = doc.GetFont("Helvetica", null);
            var centralEuropean = doc.GetFont("Helvetica", "CP1250");

            winAnsi.GetUnicodeWidth('A').Should().Be(667);
            winAnsi.GetUnicodeWidth('€').Should().Be(556);
            winAnsi.GetUnicodeWidth('ł').Should().Be(222, "Helvetica has lslash, which WinAnsi cannot encode");
            centralEuropean.GetUnicodeWidth('ł').Should().Be(222);
            winAnsi.GetUnicodeWidth('Ж').Should().Be(0, "the standard fonts have no Cyrillic glyphs");
            winAnsi.GetUnicodeWidth(new Rune(0x1F600)).Should().Be(0);
        }

        [Fact]
        public void TrueTypeFont_WidthsByTheCmap_WhateverTheCodePage()
        {
            var doc = new HpdfDocument();
            using var western = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1",
                TestFonts.DemoResource(TestFonts.Roboto), false, 1252);
            var font = western.AsFont();

            font.GetUnicodeWidth('A').Should().Be(font.GetCharWidth((byte)'A'));
            font.GetUnicodeWidth('Ж').Should().BeGreaterThan(0, "Roboto has Cyrillic, which CP1252 cannot encode");
            font.GetUnicodeWidth('あ').Should().Be(0);
        }

        [Fact]
        public void Type1Font_WidthsByTheMetrics()
        {
            var doc = new HpdfDocument();
            using var type1 = HpdfType1Font.LoadFromFile(doc.Xref, "F1",
                TestFonts.DemoResource("demo/type1/a010013l.afm"), TestFonts.DemoResource("demo/type1/a010013l.pfb"), 1252);
            var font = type1.AsFont();

            font.GetUnicodeWidth('ł').Should().Be(300, "lslash is 300 units wide, whatever the code page");
            font.GetUnicodeWidth('A').Should().Be(font.GetCharWidth((byte)'A'));
            font.GetUnicodeWidth('あ').Should().Be(0);
        }

        [Fact]
        public void EmbeddedCidFont_WidthsOfTheGlyphs()
        {
            var doc = new HpdfDocument();
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            var font = cid.AsFont();

            font.GetUnicodeWidth('あ').Should().Be(font.MeasureText("あ", 1000));
            font.GetUnicodeWidth('A').Should().Be(font.MeasureText("A", 1000));
            font.GetUnicodeWidth(new Rune(0x1F600)).Should().Be(0, "the font has no emoji");
        }

        [Fact]
        public void EmbeddedVerticalCidFont_HorizontalWidthOfTheVerticalGlyph()
        {
            var doc = new HpdfDocument();
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(TestFonts.Japanese), 65001,
                HpdfWritingMode.Vertical);
            var font = cid.AsFont();

            font.GetUnicodeWidth('あ').Should().Be(1000, "the horizontal advance, not the column advance");
        }

        [Fact]
        public void PredefinedCidFont_WidthsOfTheCidsOfTheCMap()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "MS-Gothic", "F1", "90ms-RKSJ-H");
            var hpdfFont = font.AsFont();

            hpdfFont.GetUnicodeWidth('A').Should().Be(500, "half-width Latin");
            hpdfFont.GetUnicodeWidth('あ').Should().Be(1000);
            hpdfFont.GetUnicodeWidth('한').Should().Be(0, "Hangul is not in Shift-JIS");
        }

        [Fact]
        public void Surrogate_Throws()
        {
            var font = new HpdfDocument().GetFont("Helvetica", null);

            font.Invoking(f => f.GetUnicodeWidth('\uD83D')).Should().Throw<HpdfException>();
        }
    }
}
