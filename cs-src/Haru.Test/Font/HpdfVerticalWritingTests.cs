using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

        // ---------- embedded CID fonts (Identity-V) ----------

        // The expected glyphs are those of the 'vert' feature of the fonts' GSUB tables (read with
        // fontTools); the Traditional Chinese font keeps its comma, which sits in the middle of its box
        [Theory]
        [InlineData(TestFonts.Japanese, 0x3001, 1139, 15203)]
        [InlineData(TestFonts.Japanese, 0x3002, 1140, 15204)]
        [InlineData(TestFonts.Japanese, 0x300C, 1150, 15229)]
        [InlineData(TestFonts.Japanese, 0x30FC, 1387, 16939)]
        [InlineData(TestFonts.Japanese, 0xFF08, 15277, 15217)]
        [InlineData(TestFonts.Japanese, 0x2026, 479, 15211)]
        [InlineData(TestFonts.Japanese, 0x41, 34, 34)]
        [InlineData(TestFonts.Japanese, 0x4E00, 2336, 2336)]
        [InlineData(TestFonts.SimplifiedChinese, 0x3001, 1128, 29622)]
        [InlineData(TestFonts.SimplifiedChinese, 0xFF08, 29696, 29636)]
        [InlineData(TestFonts.TraditionalChinese, 0x3001, 20336, 20336)]
        [InlineData(TestFonts.TraditionalChinese, 0x300C, 1149, 18069)]
        [InlineData(TestFonts.Korean, 0x3001, 1395, 21585)]
        [InlineData(TestFonts.Korean, 0x30FC, 1643, 24700)]
        public void EmbeddedVerticalFont_ShowsACharacterWithItsVerticalGlyph(string fontFile, int codePoint, int horizontalGlyph, int verticalGlyph)
        {
            string path = TestFonts.DemoResource(fontFile);
            var doc = new HpdfDocument();
            using var horizontal = HpdfCIDFont.LoadFromTrueTypeFile(doc, "H", path, 65001);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 65001, HpdfWritingMode.Vertical);
            var character = new Rune(codePoint);

            horizontal.GetGlyphId(character).Should().Be((ushort)horizontalGlyph);
            vertical.GetGlyphId(character).Should().Be((ushort)verticalGlyph);
            vertical.GetGlyphId((ushort)codePoint).Should().Be((ushort)verticalGlyph);

            // The glyph that is measured is the glyph that is written
            byte[] glyphs = vertical.ConvertTextToGlyphIDs(character.ToString());
            ((glyphs[0] << 8) | glyphs[1]).Should().Be(verticalGlyph);
        }

        [Fact]
        public void EmbeddedVerticalFont_IsWrittenWithIdentityVAndVerticalMetrics()
        {
            string path = TestFonts.DemoResource(TestFonts.Japanese);
            var doc = new HpdfDocument();
            using var horizontal = HpdfCIDFont.LoadFromTrueTypeFile(doc, "H", path, 932);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 932, HpdfWritingMode.Vertical);

            vertical.WritingMode.Should().Be(HpdfWritingMode.Vertical);
            vertical.AsFont().WritingMode.Should().Be(HpdfWritingMode.Vertical);
            ((HpdfName)vertical.Dict["Encoding"]).Value.Should().Be("Identity-V");

            // The typographic ascender of the font (880) is the top of its em box
            var dw2 = (HpdfArray)Descendant(vertical)["DW2"];
            ((HpdfNumber)dw2[0]).Value.Should().Be(880);
            ((HpdfNumber)dw2[1]).Value.Should().Be(-1000);

            horizontal.WritingMode.Should().Be(HpdfWritingMode.Horizontal);
            ((HpdfName)horizontal.Dict["Encoding"]).Value.Should().Be("Identity-H");
            Descendant(horizontal).ContainsKey("DW2").Should().BeFalse();
        }

        [Fact]
        public void EmbeddedVerticalFont_IsMeasuredAlongTheColumn()
        {
            var doc = new HpdfDocument();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001, HpdfWritingMode.Vertical);

            // The font gives these glyphs an advance height of one em (vmtx): wide, narrow, outside the
            // Basic Multilingual Plane, missing
            vertical.MeasureText("日本語", 12).Should().Be(36);
            vertical.MeasureText("i.", 12).Should().Be(24);
            vertical.MeasureText("\U00020BB7", 12).Should().Be(12);
            vertical.MeasureText("\U0001F600", 12).Should().Be(12);
            vertical.GetCharWidth((byte)'i').Should().Be(1000);
        }

        [Fact]
        public void EmbeddedVerticalFont_TextIsCopiedAsItWasWritten()
        {
            const string text = "日本語、「テストー」（A）。";
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 932, HpdfWritingMode.Vertical);

            page.BeginText();
            page.SetFontAndSize(vertical.AsFont(), 16);
            page.TextOut(300, 700, text);
            page.EndText();
            byte[] pdf = doc.SaveToMemory();

            // The vertical glyphs are the ones written, with their widths ...
            var descendant = Descendant(vertical);
            var w = (HpdfArray)descendant["W"];
            var glyphs = new List<int>();
            for (int i = 0; i < w.Count; i += 2)
            {
                int first = ((HpdfNumber)w[i]).Value;
                for (int k = 0; k < ((HpdfArray)w[i + 1]).Count; k++)
                    glyphs.Add(first + k);
            }
            glyphs.Should().Contain(new[] { 15203, 15229, 16939, 15217, 15204 });
            glyphs.Should().NotContain(new[] { 1139, 1150, 1387, 15277, 1140 });

            // ... and a reader maps them back to the characters of the text
            using var read = UglyToad.PdfPig.PdfDocument.Open(pdf);
            string copied = string.Concat(read.GetPage(1).Letters.Select(letter => letter.Value));
            copied.Replace(" ", string.Empty, StringComparison.Ordinal).Should().Be(text);
        }

        [Fact]
        public void EmbeddedVerticalFont_WithoutVerticalGlyphs_KeepsItsGlyphs()
        {
            // A Latin font: no 'vert' feature, and typographic metrics that are not an em box
            string path = TestFonts.DemoResource(TestFonts.Roboto);
            var doc = new HpdfDocument();
            using var horizontal = HpdfCIDFont.LoadFromTrueTypeFile(doc, "H", path, 65001);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 65001, HpdfWritingMode.Vertical);

            foreach (char c in "A(-)")
                vertical.GetGlyphId(c).Should().Be(horizontal.GetGlyphId(c));

            var dw2 = (HpdfArray)Descendant(vertical)["DW2"];
            ((HpdfNumber)dw2[0]).Value.Should().Be(880);
            ((HpdfNumber)dw2[1]).Value.Should().Be(-1000);

            // No vertical metrics either: every glyph advances one em
            vertical.MeasureText("A(-)", 10).Should().Be(40);
            ShowAndSave(doc, vertical, "A(-)");
            Descendant(vertical).ContainsKey("W2").Should().BeFalse();
        }

        [Theory]
        [InlineData(null, 0x3001, 15203)]     // both features: the comma from the lookup they share
        [InlineData(null, 0xFF1A, 17053)]     // the colon from a lookup of 'vert' only
        [InlineData("vert", 0x3001, 15203)]   // 'vrt2' only: its lookup still gives the comma
        [InlineData("vert", 0xFF1A, 15295)]   // ... but not the colon
        [InlineData("vrt2", 0x3001, 15203)]   // 'vert' only
        [InlineData("vrt2", 0xFF1A, 17053)]
        public void EmbeddedVerticalFont_TakesTheVerticalGlyphsOfVertAndVrt2(string? removedFeature, int codePoint, int glyph)
        {
            byte[] data = System.IO.File.ReadAllBytes(TestFonts.DemoResource(TestFonts.Japanese));
            if (removedFeature != null)
                data = TestFonts.WithRenamedFeature(data, removedFeature, "xxxx");
            string path = TestFonts.WriteTempFile(data);
            try
            {
                var doc = new HpdfDocument();
                using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 65001, HpdfWritingMode.Vertical);

                vertical.GetGlyphId(new Rune(codePoint)).Should().Be((ushort)glyph);
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void EmbeddedVerticalFont_GlyphTallerThanAnEm_IsMeasuredWithItsAdvanceHeight()
        {
            var doc = new HpdfDocument();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001, HpdfWritingMode.Vertical);

            // The vertical kana repeat mark is two ems tall ('vmtx')
            vertical.MeasureText("\u3031", 10).Should().Be(20);
            vertical.MeasureText("\u65E5\u3031\u672C", 10).Should().Be(40);
        }

        [Fact]
        public void EmbeddedVerticalFont_WritesTheGlyphsThatDifferFromDW2ToW2()
        {
            var doc = new HpdfDocument();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001, HpdfWritingMode.Vertical);

            ShowAndSave(doc, vertical, "\u65E5\u672C\u3031A");

            // Only the repeat mark (glyph 1187): advance 2000 down, origin centered over its width
            // of 1000, 1380 above the baseline
            Describe(Descendant(vertical)["W2"]).Should().Be("[1187 [-2000 500 1380]]");
        }

        [Fact]
        public void EmbeddedVerticalFont_GlyphsWithTheMetricsOfDW2_NeedNoW2()
        {
            var doc = new HpdfDocument();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001, HpdfWritingMode.Vertical);

            ShowAndSave(doc, vertical, "\u65E5\u672C\u8A9E\u3001A g");

            Descendant(vertical).ContainsKey("W2").Should().BeFalse();
        }

        [Fact]
        public void EmbeddedVerticalFont_W2FollowsTheGlyphsShown_AndSavingIsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001, HpdfWritingMode.Vertical);
            page.BeginText();
            page.SetFontAndSize(vertical.AsFont(), 16);
            page.TextOut(300, 700, "\u65E5\u672C");
            doc.SaveToMemory();
            Descendant(vertical).ContainsKey("W2").Should().BeFalse();

            page.TextOut(300, 600, "\u3032");
            page.EndText();
            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            Describe(Descendant(vertical)["W2"]).Should().Be("[1188 [-2000 500 1380]]");
            second.Should().Equal(first);
        }

        [Fact]
        public void EmbeddedVerticalFont_WritesTheOriginOfAGlyphAsTheFontPlacesIt()
        {
            // In font units of 2000 to the em, the values of the file are half as large in glyph
            // space; the typographic metrics (880, -120) no longer span the em, so /DW2 has the
            // origin of a reader (880) and every glyph shown has its own entry
            string path = TestFonts.WriteTempFile(TestFonts.WithUnitsPerEm(
                System.IO.File.ReadAllBytes(TestFonts.DemoResource(TestFonts.Japanese)), 2000));
            try
            {
                var doc = new HpdfDocument();
                using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 65001, HpdfWritingMode.Vertical);

                vertical.MeasureText("\u3031A", 10).Should().Be(15);
                ShowAndSave(doc, vertical, "\u3031A ");

                var w2 = ReadW2(Descendant(vertical));
                w2[vertical.GetGlyphId(new Rune(0x3031))].Should().Be((-1000, 250f, 690));  // top 1323 + 57
                w2[vertical.GetGlyphId('A')].Should().Be((-500, 152f, 440));                  // top 733 + 147
                w2[vertical.GetGlyphId(' ')].Should().Be((-500, 56f, 880));                   // no outline: the origin of /DW2
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void EmbeddedVerticalFont_WithoutVerticalMetrics_AdvancesEveryGlyphOneEm()
        {
            string path = TestFonts.WriteTempFile(TestFonts.WithRenamedTable(
                System.IO.File.ReadAllBytes(TestFonts.DemoResource(TestFonts.Japanese)), "vmtx", "xmtx"));
            try
            {
                var doc = new HpdfDocument();
                using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", path, 65001, HpdfWritingMode.Vertical);

                vertical.MeasureText("\u65E5\u3031", 10).Should().Be(20);
                ShowAndSave(doc, vertical, "\u65E5\u3031");
                Descendant(vertical).ContainsKey("W2").Should().BeFalse();
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void EmbeddedFont_InvalidWritingMode_Throws()
        {
            var doc = new HpdfDocument();
            Action load = () => HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 932, HpdfWritingMode.Eof).Dispose();

            load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void TextRect_EmbeddedVerticalFont_FillsColumns()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(
                doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 932, HpdfWritingMode.Vertical);

            page.BeginText();
            page.SetFontAndSize(vertical.AsFont(), 20);
            int printed = page.TextRect(100, 700, 200, 600, Twelve, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(12);
            var columns = ShownColumns(page);
            columns.Select(c => c.X).Should().Equal(190, 170, 150);
            columns.Select(c => c.Y).Should().Equal(700, 700, 700);
        }

        private static HpdfDict Descendant(HpdfCIDFont font) => (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];

        private static void ShowAndSave(HpdfDocument doc, HpdfCIDFont font, string text)
        {
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(font.AsFont(), 16);
            page.TextOut(300, 700, text);
            page.EndText();
            doc.SaveToMemory();
        }

        private static Dictionary<int, (int Advance, float OriginX, int OriginY)> ReadW2(HpdfDict descendant)
        {
            var result = new Dictionary<int, (int, float, int)>();
            var w2 = (HpdfArray)descendant["W2"];
            for (int i = 0; i < w2.Count; i += 2)
            {
                int cid = ((HpdfNumber)w2[i]).Value;
                var run = (HpdfArray)w2[i + 1];
                for (int k = 0; k < run.Count; k += 3)
                    result[cid + k / 3] = (((HpdfNumber)run[k]).Value, ((HpdfReal)run[k + 1]).Value, ((HpdfNumber)run[k + 2]).Value);
            }
            return result;
        }

        private static string Describe(HpdfObject value) => value switch
        {
            HpdfArray array => "[" + string.Join(" ", array.Select(Describe)) + "]",
            HpdfNumber number => number.Value.ToString(CultureInfo.InvariantCulture),
            HpdfReal real => real.Value.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

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
