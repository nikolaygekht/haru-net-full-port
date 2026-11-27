using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Haru.Test.Font;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfLineAnnotationTests
    {
        private static readonly HpdfPoint Start = new HpdfPoint(100, 300);
        private static readonly HpdfPoint End = new HpdfPoint(300, 300);

        private static HpdfLineAnnotation CreateLine(HpdfDocument doc, string text = "") =>
            doc.AddPage().CreateLineAnnotation(Start, End, text);

        private static HpdfFont Helvetica(HpdfDocument doc) => new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1");

        [Fact]
        public void CreateLineAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "Line text");

            line.Type.Should().Be(HpdfAnnotationType.Line);
            ((HpdfName)line.Dict["Subtype"]).Value.Should().Be("Line");
            Numbers(line.Dict["L"]).Should().Equal(100f, 300f, 300f, 300f);
            ((HpdfString)line.Dict["Contents"]).GetText().Should().Be("Line text");
            Numbers(line.Dict["C"]).Should().Equal(1f, 0f, 0f);
            line.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void CreateLineAnnotation_NotFinitePoint_Throws()
        {
            var page = new HpdfDocument().AddPage();

            var act = () => page.CreateLineAnnotation(new HpdfPoint(float.NaN, 0), End, "");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void Save_RectEnclosesLineAndStroke()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            doc.SaveToMemory();

            // Line width 1: padded by 2
            Numbers(line.Dict["Rect"]).Should().Equal(98f, 298f, 302f, 302f);
            Numbers(Appearance(line)["BBox"]).Should().Equal(98f, 298f, 302f, 302f);
            string content = AppearanceContent(line);
            content.Should().Contain("100 300 m\n300 300 l\nS");
        }

        [Fact]
        public void Save_RectEnclosesLineEndings()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetLineEndingStyle(HpdfLineEndingStyle.OpenArrow, HpdfLineEndingStyle.Circle);
            doc.SaveToMemory();

            // Ending size 9 for line width 1, plus the padding of 2
            Numbers(line.Dict["Rect"]).Should().Equal(89f, 289f, 311f, 311f);
        }

        [Fact]
        public void SetLineEndingStyle_WritesNamesAndRequiresPdf14()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetLineEndingStyle(HpdfLineEndingStyle.ROpenArrow, HpdfLineEndingStyle.Slash);

            ((HpdfArray)line.Dict["LE"]).Select(item => ((HpdfName)item).Value).Should().Equal("ROpenArrow", "Slash");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void SetLineEndingStyle_Invalid_Throws()
        {
            var line = CreateLine(new HpdfDocument());

            var act = () => line.SetLineEndingStyle(HpdfLineEndingStyle.None, (HpdfLineEndingStyle)42);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            line.Dict.ContainsKey("LE").Should().BeFalse();
        }

        [Fact]
        public void ClosedEndings_AreFilledWithInteriorColor()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow, HpdfLineEndingStyle.OpenArrow);
            line.SetInteriorRgbColor(0, 0, 1);
            doc.SaveToMemory();

            string content = AppearanceContent(line);
            content.Should().Contain("0 0 1 rg");
            content.Split('\n').Count(op => op == "b" || op == "B").Should().Be(1);
            // The line stops at the base of the closed arrowhead (9 * cos 30)
            content.Should().Contain("107.7942 300 m");
        }

        [Fact]
        public void SetLeader_WritesEntriesAndMovesTheLine()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetLeader(20, 5);
            doc.SaveToMemory();

            ((HpdfReal)line.Dict["LL"]).Value.Should().Be(20f);
            ((HpdfReal)line.Dict["LLE"]).Value.Should().Be(5f);
            line.Dict.ContainsKey("LLO").Should().BeFalse();
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
            string content = AppearanceContent(line);
            content.Should().Contain("100 320 m\n300 320 l");
            content.Should().Contain("100 300 m\n100 325 l");
            Numbers(line.Dict["Rect"]).Should().Equal(98f, 298f, 302f, 327f);
        }

        [Fact]
        public void SetLeader_NegativeLength_GoesTheOtherWay()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetLeader(-20, 5, 3);
            doc.SaveToMemory();

            ((HpdfReal)line.Dict["LLO"]).Value.Should().Be(3f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
            AppearanceContent(line).Should().Contain("100 297 m\n100 275 l");
        }

        [Theory]
        [InlineData(10f, -1f, 0f)]
        [InlineData(10f, 0f, -1f)]
        [InlineData(float.NaN, 0f, 0f)]
        public void SetLeader_Invalid_Throws(float length, float extension, float offset)
        {
            var line = CreateLine(new HpdfDocument());

            var act = () => line.SetLeader(length, extension, offset);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetCaption_Inline_BreaksTheLineAroundTheText()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "Caption");
            var font = Helvetica(doc);

            line.SetCaption(font, 10);
            doc.SaveToMemory();

            ((HpdfBoolean)line.Dict["Cap"]).Value.Should().BeTrue();
            line.Dict.ContainsKey("CP").Should().BeFalse();
            line.Dict.ContainsKey("CO").Should().BeFalse();
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);

            var fonts = (HpdfDict)((HpdfDict)Appearance(line)["Resources"])["Font"];
            fonts["F1"].Should().BeSameAs(font.Dict);
            string content = AppearanceContent(line);
            content.Should().Contain("/F1 10 Tf");
            content.Should().Contain("(Caption) Tj");
            content.Split('\n').Count(op => op.EndsWith(" m")).Should().Be(2, "the line is drawn in two parts");
        }

        [Fact]
        public void SetCaption_TopWithOffsets_WritesPositionAndOffsets()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "Caption");

            line.SetCaption(Helvetica(doc), 10, HpdfLineCaptionPosition.Top, 15, 4);
            doc.SaveToMemory();

            ((HpdfName)line.Dict["CP"]).Value.Should().Be("Top");
            Numbers(line.Dict["CO"]).Should().Equal(15f, 4f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
            AppearanceContent(line).Split('\n').Count(op => op.EndsWith(" m")).Should().Be(1, "the line is not broken");
            // The caption is inside the annotation rectangle
            Numbers(line.Dict["Rect"])[3].Should().BeGreaterThan(310f);
        }

        [Fact]
        public void RemoveCaption_RemovesEntries()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "Caption");
            line.SetCaption(Helvetica(doc), 10, HpdfLineCaptionPosition.Top, 1, 1);

            line.RemoveCaption();
            doc.SaveToMemory();

            line.Dict.ContainsKey("Cap").Should().BeFalse();
            line.Dict.ContainsKey("CP").Should().BeFalse();
            line.Dict.ContainsKey("CO").Should().BeFalse();
            AppearanceContent(line).Should().NotContain("Tj");
        }

        [Fact]
        public void SetCaption_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);
            var font = Helvetica(doc);

            line.Invoking(l => l.SetCaption(null!, 10)).Should().Throw<HpdfException>();
            line.Invoking(l => l.SetCaption(font, 0)).Should().Throw<HpdfException>();
            line.Invoking(l => l.SetCaption(font, 10, (HpdfLineCaptionPosition)5)).Should().Throw<HpdfException>();
            line.Invoking(l => l.SetCaption(font, 10, HpdfLineCaptionPosition.Top, float.PositiveInfinity)).Should().Throw<HpdfException>();
            line.Dict.ContainsKey("Cap").Should().BeFalse();
        }

        [Fact]
        public void SetCaption_VerticalFont_Throws()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001,
                HpdfWritingMode.Vertical);

            var act = () => line.SetCaption(vertical.AsFont(), 10);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetCaption_CidFont_GlyphsAreInTheFont()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "日本語");
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "J", TestFonts.DemoResource(TestFonts.Japanese), 65001);

            line.SetCaption(cidFont.AsFont(), 10);
            doc.SaveToMemory();

            // The caption is shown as glyph IDs, and the font writes their widths: the appearance was
            // drawn before the font wrote its subset
            AppearanceContent(line).Should().MatchRegex("<[0-9A-F]{12}> Tj");
            var descendant = (HpdfDict)((HpdfArray)cidFont.Dict["DescendantFonts"])[0];
            ((HpdfArray)descendant["W"]).Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void SetIntent_WritesNameAndRequiresPdf16()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetIntent(HpdfLineIntent.Dimension);

            ((HpdfName)line.Dict["IT"]).Value.Should().Be("LineDimension");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
        }

        [Fact]
        public void DashedLine_DrawsEndingsSolid()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);
            line.SetBorderStyle(HpdfBorderStyle.Dashed, 1, 4, 2);
            line.SetLineEndingStyle(HpdfLineEndingStyle.Butt, HpdfLineEndingStyle.Butt);

            doc.SaveToMemory();

            string content = AppearanceContent(line);
            content.IndexOf("[4 2] 0 d").Should().BeLessThan(content.IndexOf("[] 0 d"));
        }

        [Fact]
        public void SetPosition_MovesTheLineAndTheRect()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc);

            line.SetPosition(new HpdfPoint(10, 10), new HpdfPoint(10, 110));
            doc.SaveToMemory();

            Numbers(line.Dict["L"]).Should().Equal(10f, 10f, 10f, 110f);
            Numbers(line.Dict["Rect"]).Should().Equal(8f, 8f, 12f, 112f);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var line = CreateLine(doc, "320 mm");
            line.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow, HpdfLineEndingStyle.ClosedArrow);
            line.SetLeader(20, 5, 2);
            line.SetCaption(Helvetica(doc), 10);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedLine_IsReadBack()
        {
            var doc = new HpdfDocument();
            CreateLine(doc, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Line);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
