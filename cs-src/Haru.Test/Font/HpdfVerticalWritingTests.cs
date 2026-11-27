using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
            CIDEncoder horizontal = CIDEncoder.Create((name ?? string.Empty).Replace("-V", "-H", StringComparison.Ordinal));

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
            CIDEncoder horizontal = CIDEncoder.Create((name ?? string.Empty).Replace("-V", "-H", StringComparison.Ordinal));
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

        // ---------- TextRect: columns from the right edge to the left ----------

        // Twelve characters; with size 20 a column of height 100 holds five of them
        private const string Twelve = "一二三四五六七八九十百千";

        [Fact]
        public void TextRect_VerticalFont_FillsColumnsFromTheRightEdge()
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            int printed = page.TextRect(100, 700, 200, 600, Twelve, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(12);
            var columns = ShownColumns(page);
            columns.Select(c => c.Text).Should().Equal(Twelve.Substring(0, 5), Twelve.Substring(5, 5), Twelve.Substring(10));
            // A column hangs centered under its position: half a column from the right edge,
            // the next ones one column further to the left
            columns.Select(c => c.X).Should().Equal(190, 170, 150);
            columns.Select(c => c.Y).Should().Equal(700, 700, 700);
        }

        [Fact]
        public void TextRect_VerticalFont_StopsAtTheLeftEdge()
        {
            var (page, _) = NewVerticalPage();
            string text = Twelve + Twelve + Twelve;

            page.BeginText();
            int printed = page.TextRect(100, 700, 200, 600, text, HpdfTextAlignment.Left);
            page.EndText();

            // Five columns of five characters
            printed.Should().Be(25);
            var columns = ShownColumns(page);
            columns.Select(c => c.X).Should().Equal(190, 170, 150, 130, 110);
            string.Concat(columns.Select(c => c.Text)).Should().Be(text.Substring(0, 25));
        }

        [Theory]
        [InlineData(HpdfTextAlignment.Left, 700)]      // starts at the top
        [InlineData(HpdfTextAlignment.Right, 640)]     // ends at the bottom: two characters of size 20
        [InlineData(HpdfTextAlignment.Center, 670)]
        public void TextRect_VerticalFont_AlignsAlongTheColumn(HpdfTextAlignment alignment, float lastColumnY)
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            page.TextRect(100, 700, 200, 600, Twelve, alignment);
            page.EndText();

            var columns = ShownColumns(page);
            columns.Should().HaveCount(3);
            columns[0].Y.Should().Be(700, "a full column takes the whole height");
            columns[2].Y.Should().Be(lastColumnY);
        }

        [Fact]
        public void TextRect_VerticalFont_Justify_StretchesFullColumnsOverTheHeight()
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            page.TextRect(100, 710, 200, 600, Twelve, HpdfTextAlignment.Justify);
            page.EndText();

            // Five characters take 100 of the 110: the four gaps get 2.5 each. In vertical writing
            // a negative character spacing moves the characters apart
            var columns = ShownColumns(page);
            columns.Select(c => c.CharSpace).Should().Equal(-2.5f, -2.5f, 0);
            page.GraphicsState.CharSpace.Should().Be(0);
        }

        [Fact]
        public void TextRect_VerticalFont_ColumnsAreOneLeadingApart()
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            page.SetTextLeading(30);
            int printed = page.TextRect(100, 700, 200, 600, Twelve + Twelve, HpdfTextAlignment.Left);
            page.EndText();

            // The fourth column would be centered on the left edge
            ShownColumns(page).Select(c => c.X).Should().Equal(190, 160, 130);
            printed.Should().Be(15);
        }

        [Fact]
        public void TextRect_VerticalFont_LineFeedStartsANewColumn()
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            int printed = page.TextRect(100, 700, 200, 600, "一二\n三", HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(4);
            ShownColumns(page).Select(c => c.Text).Should().Equal("一二", "三");
        }

        [Fact]
        public void TextRect_VerticalFont_WithoutLeading_SetsItToTheColumnWidth()
        {
            var (page, _) = NewVerticalPage();

            page.BeginText();
            page.TextRect(100, 700, 200, 600, Twelve, HpdfTextAlignment.Left);
            page.EndText();

            page.GraphicsState.TextLeading.Should().Be(20);
        }

        [Theory]
        [InlineData(0f, 5)]     // five characters of size 20 in 100
        [InlineData(-5f, 4)]    // a negative character spacing moves the characters apart
        [InlineData(5f, 6)]     // a positive one brings them closer
        public void MeasureText_VerticalFont_MeasuresCharacterSpacingAlongTheColumn(float charSpace, int fits)
        {
            var (_, font) = NewVerticalPage();

            font.MeasureText(Twelve, 20, 100, charSpace, 0, false, out _).Should().Be(fits);
        }

        private static (HpdfPage Page, HpdfFont Font) NewVerticalPage()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            // The font is used as long as the page of the test; a predefined font holds nothing to release
#pragma warning disable CA2000
            var font = HpdfCIDFontType0.Create(doc, "MS-Mincho", "V", "90ms-RKSJ-V").AsFont();
#pragma warning restore CA2000
            page.BeginText();
            page.SetFontAndSize(font, 20);
            page.EndText();
            return (page, font);
        }

        /// <summary>
        /// Gets the strings shown with the Tj operator, with the position of the text line and the
        /// character spacing they are shown with.
        /// </summary>
        private static List<(float X, float Y, string Text, float CharSpace)> ShownColumns(HpdfPage page)
        {
            string content = System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());
            var shiftJis = System.Text.Encoding.GetEncoding(932);
            var columns = new List<(float X, float Y, string Text, float CharSpace)>();
            float x = 0, y = 0, charSpace = 0;

            foreach (Match match in Regex.Matches(content, @"BT|(-?[\d.]+) (-?[\d.]+) Td|(-?[\d.]+) Tc|<([0-9A-Fa-f]*)> Tj"))
            {
                if (match.Value == "BT")
                {
                    x = 0;
                    y = 0;
                }
                else if (match.Groups[1].Success)
                {
                    x += float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    y += float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                }
                else if (match.Groups[3].Success)
                {
                    charSpace = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    columns.Add((x, y, shiftJis.GetString(Convert.FromHexString(match.Groups[4].Value)), charSpace));
                }
            }

            return columns;
        }
    }
}
