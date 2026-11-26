using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for <see cref="HpdfPageText.TextRect"/>, text line matrix tracking and text measurement.
    /// </summary>
    public class HpdfPageTextRectTests
    {
        private const string Paragraph =
            "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs. " +
            "How vexingly quick daft zebras jump.";

        private const float Left = 100;
        private const float Top = 700;
        private const float Right = 300;
        private const float Bottom = 500;

        // ---------- line layout ----------

        [Fact]
        public void TextRect_TextFits_PrintsAllCharactersAsWrappedLines()
        {
            var (doc, page, _) = NewPage();

            page.BeginText();
            int printed = page.TextRect(Left, Top, Right, Bottom, Paragraph, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(Paragraph.Length);
            var lines = ShownLines(page);
            lines.Length.Should().BeGreaterThan(1);
            string.Join(" ", lines).Should().Be(Paragraph);
            lines.Should().OnlyContain(line => !line.EndsWith(' '), "trailing spaces are not printed");
            doc.Should().NotBeNull();
        }

        [Fact]
        public void TextRect_WordsStayInsideTheRectangle()
        {
            var (doc, page, _) = NewPage();

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, Paragraph, HpdfTextAlignment.Left);
            page.EndText();

            var words = ReadWords(doc);
            string.Join(" ", words.Select(w => w.Text)).Should().Be(Paragraph);
            foreach (var word in words)
            {
                StartX(word).Should().BeGreaterThanOrEqualTo(Left - 0.01);
                EndX(word).Should().BeLessThanOrEqualTo(Right + 0.01);
                word.BoundingBox.Top.Should().BeLessThanOrEqualTo(Top + 0.5);
                word.BoundingBox.Bottom.Should().BeGreaterThanOrEqualTo(Bottom - 0.5);
            }

            StartX(words[0]).Should().BeApproximately(Left, 0.01);
        }

        [Fact]
        public void TextRect_NoLeadingSet_UsesFontBoundingBoxHeight()
        {
            var (_, page, font) = NewPage();

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, "Hello", HpdfTextAlignment.Left);
            page.EndText();

            var bbox = font.GetBBox();
            page.GraphicsState.TextLeading.Should().BeApproximately((bbox.Top - bbox.Bottom) / 1000f * 12, 0.001f);
        }

        [Fact]
        public void TextRect_FirstBaseline_PlacesFontBoxAtTop()
        {
            var (_, page, font) = NewPage();
            page.SetTextLeading(14);

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, "Hello", HpdfTextAlignment.Left);
            page.EndText();

            page.TextLineMatrix.Y.Should().BeApproximately(Top - font.GetBBox().Top / 1000f * 12, 0.001f);
        }

        [Fact]
        public void TextRect_LineFeed_StartsNewLine()
        {
            var (_, page, _) = NewPage();

            page.BeginText();
            int printed = page.TextRect(Left, Top, Right, Bottom, "First\nSecond\r\n\nFourth", HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be("First\nSecond\r\n\nFourth".Length);
            ShownLines(page).Should().Equal("First", "Second", "Fourth");
            Content(page).Should().Contain("T*", "the empty line moves down without showing text");
        }

        [Fact]
        public void TextRect_RectangleTooLow_ReturnsCharactersPrinted()
        {
            var (_, page, _) = NewPage();

            page.BeginText();
            int printed = page.TextRect(Left, Top, Right, Top - 30, Paragraph, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().BeGreaterThan(0).And.BeLessThan(Paragraph.Length);
            string.Join(" ", ShownLines(page)).Should().Be(Paragraph.Substring(0, printed).TrimEnd());
        }

        [Fact]
        public void TextRect_RectangleLowerThanOneLine_PrintsNothing()
        {
            var (_, page, _) = NewPage();

            page.BeginText();
            int printed = page.TextRect(Left, Top, Right, Top - 5, Paragraph, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(0);
            ShownLines(page).Should().BeEmpty();
        }

        [Fact]
        public void TextRect_WordWiderThanRectangle_IsBrokenBetweenCharacters()
        {
            var (_, page, _) = NewPage();
            const string longWord = "Supercalifragilisticexpialidocious";

            page.BeginText();
            int printed = page.TextRect(Left, Top, Left + 60, Bottom, longWord, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(longWord.Length);
            var lines = ShownLines(page);
            lines.Length.Should().BeGreaterThan(1);
            string.Concat(lines).Should().Be(longWord);
        }

        [Fact]
        public void TextRect_EmptyText_PrintsNothing()
        {
            var (_, page, _) = NewPage();

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, string.Empty, HpdfTextAlignment.Left).Should().Be(0);
            page.EndText();

            Content(page).Should().NotContain("'");
        }

        [Fact]
        public void TextRect_WithoutFont_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            page.BeginText();
            Action act = () => page.TextRect(Left, Top, Right, Bottom, "Hello", HpdfTextAlignment.Left);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageFontNotFound);
        }

        // ---------- alignment ----------

        [Fact]
        public void TextRect_RightAlignment_EndsLinesAtRightEdge()
        {
            var (doc, page, _) = NewPage();

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, Paragraph, HpdfTextAlignment.Right);
            page.EndText();

            foreach (var line in LinesOfWords(ReadWords(doc)))
                EndX(line.Last()).Should().BeApproximately(Right, 0.05);
        }

        [Fact]
        public void TextRect_CenterAlignment_CentersEachLine()
        {
            var (doc, page, _) = NewPage();

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, Paragraph, HpdfTextAlignment.Center);
            page.EndText();

            foreach (var line in LinesOfWords(ReadWords(doc)))
            {
                double middle = (StartX(line.First()) + EndX(line.Last())) / 2;
                middle.Should().BeApproximately((Left + Right) / 2, 0.05);
            }
        }

        [Fact]
        public void TextRect_Justify_StretchesAllButLastLineAndRestoresCharSpace()
        {
            var (doc, page, _) = NewPage();
            page.SetCharSpace(0.5f);

            page.BeginText();
            page.TextRect(Left, Top, Right, Bottom, Paragraph, HpdfTextAlignment.Justify);
            page.EndText();

            page.GraphicsState.CharSpace.Should().Be(0.5f);
            Regex.Matches(Content(page), @"(\S+) Tc").Last().Groups[1].Value.Should().Be("0.5");

            var lines = LinesOfWords(ReadWords(doc));
            lines.Count.Should().BeGreaterThan(2);
            foreach (var line in lines.Take(lines.Count - 1))
            {
                StartX(line.First()).Should().BeApproximately(Left, 0.05);
                EndX(line.Last()).Should().BeApproximately(Right, 0.05);
            }

            EndX(lines.Last().Last()).Should().BeLessThan(Right - 10, "the last line is not stretched");
        }

        // ---------- text line matrix ----------

        [Fact]
        public void MoveTextPos_ScaledTextMatrix_MovesLineInTextSpace()
        {
            var (_, page, _) = NewPage();

            page.BeginText();
            page.SetTextMatrix(2, 0, 0, 2, 10, 20);
            page.MoveTextPos(5, 7);

            page.TextLineMatrix.X.Should().Be(20);
            page.TextLineMatrix.Y.Should().Be(34);
            page.EndText();
        }

        [Fact]
        public void ShowTextNextLine_ScaledTextMatrix_MovesDownByScaledLeading()
        {
            var (_, page, _) = NewPage();
            page.SetTextLeading(10);

            page.BeginText();
            page.SetTextMatrix(1, 0, 0, 2, 0, 100);
            page.ShowTextNextLine("Hello");

            page.TextLineMatrix.Y.Should().Be(80);
            page.EndText();
        }

        // ---------- measurement ----------

        [Fact]
        public void MeasureText_UnicodeCharacterWithLineFeedLowByte_IsNotALineBreak()
        {
            var (_, _, font) = NewPage();

            // U+010A has the low byte 0x0A (line feed)
            int length = font.MeasureText("AĊB C", 12, 1000, 0, 0, true, out _);

            length.Should().Be(5);
        }

        // ---------- helpers ----------

        private static (HpdfDocument Doc, HpdfPage Page, HpdfFont Font) NewPage()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = doc.GetFont("Helvetica", null);
            page.BeginText();
            page.SetFontAndSize(font, 12);
            page.EndText();
            return (doc, page, font);
        }

        private static string Content(HpdfPage page) =>
            System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());

        /// <summary>
        /// Gets the strings shown with the ' operator, in order.
        /// </summary>
        private static string[] ShownLines(HpdfPage page) =>
            Regex.Matches(Content(page), @"\((.*)\) '").Select(m => m.Groups[1].Value).ToArray();

        private static UglyToad.PdfPig.Content.Word[] ReadWords(HpdfDocument doc)
        {
            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));
            return pdf.GetPage(1).GetWords().ToArray();
        }

        private static System.Collections.Generic.List<UglyToad.PdfPig.Content.Word[]> LinesOfWords(UglyToad.PdfPig.Content.Word[] words) =>
            words.GroupBy(w => Math.Round(w.Letters[0].StartBaseLine.Y, 1).ToString(CultureInfo.InvariantCulture))
                 .Select(g => g.OrderBy(StartX).ToArray())
                 .ToList();

        /// <summary>
        /// Gets where the first glyph of a word starts on the baseline.
        /// </summary>
        private static double StartX(UglyToad.PdfPig.Content.Word word) => word.Letters[0].StartBaseLine.X;

        /// <summary>
        /// Gets where the advance of the last glyph of a word ends on the baseline.
        /// </summary>
        private static double EndX(UglyToad.PdfPig.Content.Word word) => word.Letters[word.Letters.Count - 1].EndBaseLine.X;
    }
}
