using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfTextMarkupAnnotationTests
    {
        private static readonly HpdfRect Line1 = new HpdfRect(100, 700, 300, 714);
        private static readonly HpdfRect Line2 = new HpdfRect(100, 680, 200, 694);

        [Theory]
        [InlineData(HpdfAnnotationType.Highlight, "Highlight", HpdfVersion.Version14)]
        [InlineData(HpdfAnnotationType.Underline, "Underline", HpdfVersion.Version13)]
        [InlineData(HpdfAnnotationType.Squiggly, "Squiggly", HpdfVersion.Version14)]
        [InlineData(HpdfAnnotationType.StrikeOut, "StrikeOut", HpdfVersion.Version13)]
        public void Create_WritesSubtypeQuadPointsAndVersion(HpdfAnnotationType type, string subtype, HpdfVersion version)
        {
            var doc = new HpdfDocument();
            var annot = Create(doc.AddPage(), type, Line1, "Comment");

            annot.Type.Should().Be(type);
            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be(subtype);
            ((HpdfString)annot.Dict["Contents"]).GetText().Should().Be("Comment");
            Numbers(annot.Dict["QuadPoints"]).Should().Equal(100f, 714f, 300f, 714f, 100f, 700f, 300f, 700f);
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(version);
        }

        [Fact]
        public void Highlight_IsYellowByDefault_OthersRed()
        {
            var page = new HpdfDocument().AddPage();

            Numbers(page.CreateHighlightAnnotation(Line1, "").Dict["C"]).Should().Equal(1f, 1f, 0f);
            Numbers(page.CreateUnderlineAnnotation(Line1, "").Dict["C"]).Should().Equal(1f, 0f, 0f);
        }

        [Fact]
        public void SetQuadPoints_SeveralLines_WritesQuadsAndEnclosingRect()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateHighlightAnnotation(Line1, "");

            annot.SetQuadPoints(Line1, Line2);
            doc.SaveToMemory();

            Numbers(annot.Dict["QuadPoints"]).Should().Equal(
                100f, 714f, 300f, 714f, 100f, 700f, 300f, 700f,
                100f, 694f, 200f, 694f, 100f, 680f, 200f, 680f);
            // Padded by height / 14 + 1
            Numbers(annot.Dict["Rect"]).Should().Equal(98f, 678f, 302f, 716f);
        }

        [Fact]
        public void SetQuadPoints_FourPoints_WritesAcrobatOrder()
        {
            var annot = new HpdfDocument().AddPage().CreateUnderlineAnnotation(Line1, "");

            annot.SetQuadPoints(new HpdfPoint(1, 2), new HpdfPoint(3, 4), new HpdfPoint(5, 6), new HpdfPoint(7, 8));

            // Upper left, upper right, lower left, lower right
            Numbers(annot.Dict["QuadPoints"]).Should().Equal(5f, 6f, 7f, 8f, 1f, 2f, 3f, 4f);
        }

        [Fact]
        public void SetQuadPoints_Invalid_Throws()
        {
            var annot = new HpdfDocument().AddPage().CreateHighlightAnnotation(Line1, "");

            annot.Invoking(a => a.SetQuadPoints()).Should().Throw<HpdfException>();
            annot.Invoking(a => a.SetQuadPoints(new HpdfRect(0, 0, float.NaN, 1))).Should().Throw<HpdfException>();
            annot.Invoking(a => a.SetQuadPoints(new HpdfPoint(float.PositiveInfinity, 0), default, default, default))
                .Should().Throw<HpdfException>();
            Numbers(annot.Dict["QuadPoints"]).Should().HaveCount(8);
        }

        [Fact]
        public void Highlight_IsFilledWithMultiplyBlendMode()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateHighlightAnnotation(Line1, "");

            doc.SaveToMemory();

            var graphicsState = (HpdfDict)((HpdfDict)((HpdfDict)Appearance(annot)["Resources"])["ExtGState"])["GS0"];
            ((HpdfName)graphicsState["BM"]).Value.Should().Be("Multiply");
            graphicsState.ContainsKey("ca").Should().BeFalse();
            string content = AppearanceContent(annot);
            content.Should().StartWith("/GS0 gs\n");
            content.Should().Contain("1 1 0 rg");
            content.Should().Contain("100 700 m\n300 700 l\n300 714 l\n100 714 l\nh\nf");
        }

        [Fact]
        public void Highlight_InPdfA_IsAnOutlineWithoutBlendMode()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            var annot = doc.AddPage().CreateHighlightAnnotation(Line1, "");

            doc.SaveToMemory();

            ((HpdfDict)Appearance(annot)["Resources"]).ContainsKey("ExtGState").Should().BeFalse();
            string content = AppearanceContent(annot);
            content.Should().Contain("1 1 0 RG");
            content.Should().EndWith("h\nS\nQ\n");
        }

        [Fact]
        public void Underline_IsALineAboveTheBottom()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateUnderlineAnnotation(Line1, "");

            doc.SaveToMemory();

            // Height 14: width 1, at 1/7 of the height
            AppearanceContent(annot).Should().Contain("1 w\n100 702 m\n300 702 l\nS");
        }

        [Fact]
        public void StrikeOut_IsALineInTheMiddle()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateStrikeOutAnnotation(Line1, "");

            doc.SaveToMemory();

            AppearanceContent(annot).Should().Contain("100 707 m\n300 707 l\nS");
        }

        [Fact]
        public void Squiggly_IsAZigzagAlongTheBottom()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquigglyAnnotation(Line1, "");

            doc.SaveToMemory();

            string content = AppearanceContent(annot);
            content.Should().Contain("100 700.5 m");
            content.Split('\n').Count(op => op.EndsWith(" l")).Should().Be(100);
        }

        [Fact]
        public void NoColor_DrawsNothing()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateStrikeOutAnnotation(Line1, "");

            annot.SetNoColor();
            doc.SaveToMemory();

            AppearanceContent(annot).Should().BeEmpty();
        }

        [Fact]
        public void HighlightWithTransparency_CombinesOpacityAndBlendMode()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateHighlightAnnotation(Line1, "");

            annot.SetTransparency(0.4f);
            doc.SaveToMemory();

            var graphicsState = (HpdfDict)((HpdfDict)((HpdfDict)Appearance(annot)["Resources"])["ExtGState"])["GS0"];
            ((HpdfReal)graphicsState["ca"]).Value.Should().Be(0.4f);
            ((HpdfName)graphicsState["BM"]).Value.Should().Be("Multiply");
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.CreateHighlightAnnotation(Line1, "").SetQuadPoints(Line1, Line2);
            page.CreateSquigglyAnnotation(Line2, "");

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedHighlight_IsReadBackWithQuadPoints()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateHighlightAnnotation(Line1, "Read me").SetQuadPoints(Line1, Line2);

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Highlight);
            annotation.Content.Should().Be("Read me");
            annotation.QuadPoints.Should().HaveCount(2);
            annotation.QuadPoints[0].Points[0].X.Should().Be(100);
            annotation.QuadPoints[0].Points[0].Y.Should().Be(714);
        }

        private static HpdfTextMarkupAnnotation Create(HpdfPage page, HpdfAnnotationType type, HpdfRect rect, string text) =>
            type switch
            {
                HpdfAnnotationType.Highlight => page.CreateHighlightAnnotation(rect, text),
                HpdfAnnotationType.Underline => page.CreateUnderlineAnnotation(rect, text),
                HpdfAnnotationType.Squiggly => page.CreateSquigglyAnnotation(rect, text),
                _ => page.CreateStrikeOutAnnotation(rect, text)
            };
    }
}
