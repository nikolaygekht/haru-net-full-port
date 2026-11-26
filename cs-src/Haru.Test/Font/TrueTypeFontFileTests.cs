using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests that a font file is read in one way: the simple TrueType font, the CID font and the fonts
    /// created from the font data pool get the same glyphs, widths and metrics from it.
    /// </summary>
    public class TrueTypeFontFileTests
    {
        private const string Sample = "Az éЯΩ€";

        private static readonly string[] MetricEntries =
        {
            "Flags", "FontBBox", "ItalicAngle", "Ascent", "Descent", "CapHeight", "StemV",
        };

        [Fact]
        public void SimpleFontAndCidFont_MapCharactersToTheSameGlyphs()
        {
            string path = TestFonts.DemoResource(TestFonts.Roboto);
            var doc = new HpdfDocument();
            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, true);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);

            foreach (char c in Sample)
            {
                ushort glyph = trueType.GetGlyphId(c);

                glyph.Should().Be(cid.GetGlyphId(c), "U+{0:X4}", (int)c).And.NotBe(0);
                trueType.GetGlyphWidth(glyph).Should().Be(cid.GetGlyphWidth(glyph), "U+{0:X4}", (int)c);
            }

            trueType.GetGlyphId(0x4E00).Should().Be(0);
            trueType.GetGlyphWidth(ushort.MaxValue).Should().Be(0);
        }

        [Fact]
        public void SimpleFontAndCidFont_DescribeAFontFileInTheSameWay()
        {
            string path = TestFonts.DemoResource(TestFonts.Roboto);
            var doc = new HpdfDocument();
            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, true);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);

            var descendant = (HpdfDict)((HpdfArray)cid.Dict["DescendantFonts"])[0];
            var cidDescriptor = (HpdfDict)descendant["FontDescriptor"];

            foreach (string entry in MetricEntries)
                Describe(trueType.Descriptor[entry]).Should().Be(Describe(cidDescriptor[entry]), entry);

            trueType.Ascent.Should().Be(cid.Ascent);
            trueType.Descent.Should().Be(cid.Descent);
            trueType.XHeight.Should().Be(cid.XHeight);
            trueType.FontBBox.Should().Be(cid.FontBBox);
        }

        // The expected values are those of the font files (read with fontTools), in 1000-unit glyph
        // space: Roboto and PenguinAttack have 2048 units per em, the Noto fonts 1000. The heights of
        // the letters are the tops of 'H' and 'x'; PenguinAttack has an OS/2 table of version 1,
        // which does not hold them, and in noto-cht the OS/2 values (729, 514) differ from the outlines
        [Theory]
        [InlineData(TestFonts.Roboto, 1048, -271, 711, 528, "-891 -271 2045 1056")]
        [InlineData(TestFonts.PenguinAttack, 888, -354, 655, 369, "-77 -355 933 888")]
        [InlineData(TestFonts.TraditionalChinese, 1151, -286, 728, 516, "-997 -1049 2929 1809")]
        public void Descriptor_HoldsTheMetricsInGlyphSpace(string fontFile, int ascent, int descent, int capHeight, int xHeight, string bbox)
        {
            string path = TestFonts.DemoResource(fontFile);
            var doc = new HpdfDocument();
            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, false);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);

            var descendant = (HpdfDict)((HpdfArray)cid.Dict["DescendantFonts"])[0];

            foreach (HpdfDict descriptor in new[] { trueType.Descriptor, (HpdfDict)descendant["FontDescriptor"] })
            {
                Describe(descriptor["Ascent"]).Should().Be(ascent.ToString(CultureInfo.InvariantCulture));
                Describe(descriptor["Descent"]).Should().Be(descent.ToString(CultureInfo.InvariantCulture));
                Describe(descriptor["CapHeight"]).Should().Be(capHeight.ToString(CultureInfo.InvariantCulture));
                Describe(descriptor["XHeight"]).Should().Be(xHeight.ToString(CultureInfo.InvariantCulture));
                Describe(descriptor["FontBBox"]).Should().Be(bbox);
            }

            trueType.Ascent.Should().Be(ascent);
            trueType.Descent.Should().Be(descent);
            trueType.XHeight.Should().Be(xHeight);
            cid.XHeight.Should().Be(xHeight);
        }

        [Fact]
        public void FontWithoutOutlines_TakesTheLetterHeightsFromTheOS2Table()
        {
            byte[] fontData = TestFonts.WithRenamedTable(
                File.ReadAllBytes(TestFonts.DemoResource(TestFonts.TraditionalChinese)), "glyf", "glyx");
            string path = TestFonts.WriteTempFile(fontData);
            try
            {
                var doc = new HpdfDocument();
                using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, false);

                Describe(font.Descriptor["CapHeight"]).Should().Be("729");
                Describe(font.Descriptor["XHeight"]).Should().Be("514");
                font.XHeight.Should().Be(514);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void FontThatDoesNotTellTheLetterHeights_GetsItsAscentAndNoXHeight()
        {
            // No outlines, and an OS/2 table of version 1
            byte[] fontData = TestFonts.WithRenamedTable(
                File.ReadAllBytes(TestFonts.DemoResource(TestFonts.PenguinAttack)), "glyf", "glyx");
            string path = TestFonts.WriteTempFile(fontData);
            try
            {
                var doc = new HpdfDocument();
                using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, false);

                Describe(font.Descriptor["CapHeight"]).Should().Be("888");
                font.Descriptor.ContainsKey("XHeight").Should().BeFalse();
                font.XHeight.Should().Be(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SimpleFont_ReadsAFontWithACmapForAllOfUnicodeOnly()
        {
            string original = TestFonts.DemoResource(TestFonts.Roboto);
            string path = TestFonts.WriteTempFile(TestFonts.WithFullUnicodeCmapOnly(File.ReadAllBytes(original)));
            try
            {
                var doc = new HpdfDocument();
                using var expected = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", original, true, 1251);
                using var font = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F2", path, true, 1251);

                foreach (char c in Sample)
                    font.GetGlyphId(c).Should().Be(expected.GetGlyphId(c), "U+{0:X4}", (int)c).And.NotBe(0);

                font.MeasureText("Az Я", 10).Should().Be(expected.MeasureText("Az Я", 10));
                doc.SaveToMemory().Should().NotBeEmpty();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(1252)]
        [InlineData(1251)]
        public void FontFromPooledData_IsTheFontOfTheFile(int codePage)
        {
            string path = TestFonts.DemoResource(TestFonts.Roboto);
            var doc = new HpdfDocument();
            using var expected = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, true, codePage);

            // The data of the pool, and data made by hand: its font file is parsed when a font needs it
            var pooled = FontDataPool.GetOrLoadTrueType(path, codePage);
            var manual = new ParsedTrueTypeData(path, File.ReadAllBytes(path), "Roboto-Regular", codePage);

            var fonts = new List<HpdfTrueTypeFont>
            {
                HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F2", pooled, true),
                HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F3", manual, true),
                HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F4", pooled, true, codePage),
            };
            try
            {
                foreach (HpdfTrueTypeFont font in fonts)
                {
                    font.BaseFont.Should().Be(expected.BaseFont);
                    font.CodePage.Should().Be(codePage);
                    font.Ascent.Should().Be(expected.Ascent);
                    font.Descent.Should().Be(expected.Descent);
                    font.FontBBox.Should().Be(expected.FontBBox);
                    font.MeasureText(Sample, 12).Should().Be(expected.MeasureText(Sample, 12));

                    foreach (string entry in MetricEntries)
                        Describe(font.Descriptor[entry]).Should().Be(Describe(expected.Descriptor[entry]), entry);
                }

                doc.SaveToMemory().Should().NotBeEmpty();
            }
            finally
            {
                foreach (HpdfTrueTypeFont font in fonts)
                    font.Dispose();
            }
        }

        [Fact]
        public void Pool_RejectsAFileThatIsNotAFont()
        {
            string path = TestFonts.WriteTempFile(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            try
            {
                var load = () => FontDataPool.GetOrLoadTrueType(path);

                load.Should().Throw<System.InvalidOperationException>();
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Gets the value of a number or of an array of numbers as text.
        /// </summary>
        private static string Describe(HpdfObject value)
        {
            if (value is not HpdfArray array)
                return value.ToString() ?? string.Empty;

            var items = new List<string>();
            for (int i = 0; i < array.Count; i++)
                items.Add(array[i].ToString() ?? string.Empty);
            return string.Join(" ", items);
        }
    }
}
