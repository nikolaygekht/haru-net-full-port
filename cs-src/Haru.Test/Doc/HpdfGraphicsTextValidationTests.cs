using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Graphics;
using Haru.Types;
using Haru.Xref;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for operator validation, graphics state tracking, arcs that continue a path,
    /// showing text without a font or with empty strings, and text measurement details.
    /// </summary>
    public class HpdfGraphicsTextValidationTests
    {
        // ---------- graphics state stack ----------

        [Fact]
        public void GRestore_WithoutGSave_ThrowsAndWritesNothing()
        {
            using var page = NewPage();

            Action act = () => page.GRestore();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageCannotRestoreGState);
            Content(page).Should().BeEmpty();
        }

        [Fact]
        public void GSave_DeeperThanLimit_Throws()
        {
            using var page = NewPage();
            for (int i = 0; i < HpdfConst.LimitMaxGState; i++)
                page.GSave();

            Action act = () => page.GSave();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.ExceedGStateLimit);
        }

        [Fact]
        public void Concat_UpdatesTransformationMatrix()
        {
            using var page = NewPage();

            page.Concat(2, 0, 0, 2, 10, 20);
            page.Concat(1, 0, 0, 1, 5, 5);

            page.GraphicsState.TransMatrix.Should().Be(new HpdfTransMatrix(2, 0, 0, 2, 20, 30));
        }

        [Fact]
        public void Concat_IsUndoneByGRestore()
        {
            using var page = NewPage();
            page.GSave();
            page.Concat(2, 0, 0, 2, 0, 0);

            page.GRestore();

            page.GraphicsState.TransMatrix.Should().Be(new HpdfTransMatrix(1, 0, 0, 1, 0, 0));
        }

        [Fact]
        public void NewGraphicsState_HasSolidDashMode()
        {
            using var page = NewPage();

            page.GraphicsState.DashMode.Should().Be(HpdfDashMode.Solid);
            default(HpdfDashMode).Should().Be(HpdfDashMode.Solid);
        }

        // ---------- operator values ----------

        [Fact]
        public void SetLineCap_SentinelValue_Throws()
        {
            using var page = NewPage();

            Action act = () => page.SetLineCap(HpdfLineCap.LineCapEof);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
            Content(page).Should().BeEmpty();
        }

        [Fact]
        public void SetLineJoin_SentinelValue_Throws()
        {
            using var page = NewPage();

            Action act = () => page.SetLineJoin(HpdfLineJoin.LineJoinEof);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
        }

        [Fact]
        public void SetTextRenderingMode_SentinelValue_Throws()
        {
            using var page = NewPage();

            Action act = () => page.SetTextRenderingMode(HpdfTextRenderingMode.RenderingModeEof);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
        }

        [Fact]
        public void SetDash_AllZero_Throws()
        {
            using var page = NewPage();

            Action act = () => page.SetDash(new ushort[] { 0, 0 }, 0);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
        }

        [Fact]
        public void SetDash_ZeroDashWithGap_IsAllowed()
        {
            using var page = NewPage();

            page.SetDash(new ushort[] { 0, 4 }, 0);

            Content(page).Should().Be("[0 4] 0 d\n");
        }

        // ---------- paths and arcs ----------

        [Fact]
        public void Rectangle_SetsCurrentPointToOrigin()
        {
            using var page = NewPage();

            page.Rectangle(10, 20, 30, 40);

            page.CurrentPos.Should().Be(new HpdfPoint(10, 20));
            page.PathInProgress.Should().BeTrue();
        }

        [Fact]
        public void Painting_EndsThePath()
        {
            using var page = NewPage();
            page.MoveTo(0, 0);
            page.LineTo(10, 10);

            page.Stroke();

            page.PathInProgress.Should().BeFalse();
        }

        [Fact]
        public void Arc_WithoutPath_StartsNewSubpath()
        {
            using var page = NewPage();

            page.Arc(100, 100, 50, 0, 90);

            Content(page).Should().StartWith("100 150 m\n");
        }

        [Fact]
        public void Arc_AfterMoveTo_ContinuesPathWithLine()
        {
            using var page = NewPage();
            page.MoveTo(100, 100);

            page.Arc(100, 100, 50, 0, 90);
            page.ClosePathFillStroke();

            string content = Content(page);
            content.Should().StartWith("100 100 m\n100 150 l\n", "a pie slice draws its first radius");
            content.Split('\n').Count(line => line.EndsWith(" m", StringComparison.Ordinal)).Should().Be(1);
        }

        [Fact]
        public void Arc_AfterPaintedPath_StartsNewSubpath()
        {
            using var page = NewPage();
            page.MoveTo(0, 0);
            page.LineTo(10, 10);
            page.Stroke();

            page.Arc(100, 100, 50, 0, 90);

            Content(page).Should().Contain("S\n100 150 m\n");
        }

        [Fact]
        public void Arc_EqualAngles_Throws()
        {
            using var page = NewPage();

            Action act = () => page.Arc(100, 100, 50, 45, 45);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
        }

        [Fact]
        public void AppearanceCanvas_TracksPathToo()
        {
            using var stream = new Haru.Objects.HpdfStreamObject();
            var canvas = new HpdfAppearanceCanvas(stream);

            canvas.MoveTo(5, 5);
            canvas.Arc(5, 5, 4, 0, 90);

            System.Text.Encoding.ASCII.GetString(stream.Stream.ToArray()).Should().Contain(" l\n");
        }

        // ---------- text without a font, empty strings ----------

        [Fact]
        public void ShowText_WithoutFont_Throws()
        {
            using var page = NewPage();
            page.BeginText();

            Action act = () => page.ShowText("Hello");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageFontNotFound);
            Content(page).Should().Be("BT\n");
        }

        [Fact]
        public void SetSpacingAndShowText_WithoutFont_ThrowsBeforeWriting()
        {
            using var page = NewPage();

            Action act = () => page.SetSpacingAndShowText(1, 2, "Hello");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageFontNotFound);
            Content(page).Should().BeEmpty();
        }

        [Fact]
        public void TextWidth_WithoutFont_ThrowsFontNotFound()
        {
            using var page = NewPage();

            Action act = () => page.TextWidth("Hello");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageFontNotFound);
        }

        [Fact]
        public void ShowTextNextLine_EmptyText_StillMovesToNextLine()
        {
            using var page = NewPageWithFont();
            page.SetTextLeading(14);
            page.BeginText();

            page.ShowTextNextLine(string.Empty);

            Content(page).Should().EndWith("T*\n");
            page.TextLineMatrix.Y.Should().Be(-14);
        }

        [Fact]
        public void SetSpacingAndShowText_EmptyText_SetsSpacingAndMoves()
        {
            using var page = NewPageWithFont();
            page.SetTextLeading(10);
            page.BeginText();

            page.SetSpacingAndShowText(1, 2, string.Empty);

            Content(page).Should().EndWith("1 2 () \"\n");
            page.GraphicsState.WordSpace.Should().Be(1);
            page.GraphicsState.CharSpace.Should().Be(2);
            page.TextLineMatrix.Y.Should().Be(-10);
        }

        // ---------- measurement ----------

        [Fact]
        public void MeasureText_LoneCarriageReturn_IsALineBreak()
        {
            var font = new HpdfDocument().GetFont("Helvetica", null);

            font.MeasureText("ab\rcd", 12, 1000, 0, 0, true, out _).Should().Be(3);
            font.MeasureText("ab\r\ncd", 12, 1000, 0, 0, true, out _).Should().Be(4);
        }

        [Fact]
        public void MeasureText_CidFont_IgnoresWordSpacing()
        {
            var doc = new HpdfDocument();
            using var cid = HpdfCIDFontType0.Create(doc, "SimSun", "F1", 936, "GBK-EUC-H");
            var font = cid.AsFont();

            font.MeasureText("a b c", 10, 1000, 0, 0, true, out float without);
            font.MeasureText("a b c", 10, 1000, 0, 50, true, out float with);

            with.Should().Be(without);
        }

        [Fact]
        public void TextRect_HorizontalScaling_FitsScaledLinesIntoRectangle()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetFontAndSize(doc.GetFont("Helvetica", null), 12);
            page.SetHorizontalScaling(50);
            const string text = "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs.";

            page.BeginText();
            page.TextRect(100, 700, 300, 500, text, HpdfTextAlignment.Right);
            page.EndText();

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));
            var words = pdf.GetPage(1).GetWords().ToArray();
            string.Join(" ", words.Select(w => w.Text)).Should().Be(text);
            var lines = words.GroupBy(w => Math.Round(w.Letters[0].StartBaseLine.Y, 1)).ToArray();
            lines.Length.Should().BeLessThanOrEqualTo(2, "at half width twice as much text fits on a line");
            foreach (var line in lines)
                line.Last().Letters.Last().EndBaseLine.X.Should().BeApproximately(300, 0.05);
        }

        private static HpdfPage NewPage() => new HpdfPage(new HpdfXref(0));

        private static HpdfPage NewPageWithFont()
        {
            var xref = new HpdfXref(0);
            var page = new HpdfPage(xref);
            page.SetFontAndSize(new HpdfFont(xref, HpdfStandardFont.Helvetica, "F1"), 12);
            return page;
        }

        private static string Content(HpdfPage page)
        {
            string content = System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());
            int fontOperator = content.IndexOf(" Tf\n", StringComparison.Ordinal);
            return fontOperator < 0 ? content : content.Substring(fontOperator + 4);
        }
    }
}
