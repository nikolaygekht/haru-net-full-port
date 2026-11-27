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
    public class HpdfPolygonAnnotationTests
    {
        private static readonly HpdfPoint[] Triangle = { new(100, 100), new(200, 100), new(150, 180) };
        private static readonly HpdfPoint[] Path = { new(100, 300), new(150, 350), new(200, 300) };

        [Fact]
        public void CreatePolygonAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var polygon = doc.AddPage().CreatePolygonAnnotation(Triangle, "Area");

            polygon.Type.Should().Be(HpdfAnnotationType.Polygon);
            ((HpdfName)polygon.Dict["Subtype"]).Value.Should().Be("Polygon");
            Numbers(polygon.Dict["Vertices"]).Should().Equal(100f, 100f, 200f, 100f, 150f, 180f);
            polygon.Vertices.Should().Equal(Triangle);
            ((HpdfString)polygon.Dict["Contents"]).GetText().Should().Be("Area");
            Numbers(polygon.Dict["C"]).Should().Equal(1f, 0f, 0f);
            polygon.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void CreatePolyLineAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var polyline = doc.AddPage().CreatePolyLineAnnotation(Path, "Path");

            polyline.Type.Should().Be(HpdfAnnotationType.PolyLine);
            ((HpdfName)polyline.Dict["Subtype"]).Value.Should().Be("PolyLine");
            Numbers(polyline.Dict["Vertices"]).Should().Equal(100f, 300f, 150f, 350f, 200f, 300f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void Create_TooFewOrNotFiniteVertices_Throw()
        {
            var page = new HpdfDocument().AddPage();

            page.Invoking(p => p.CreatePolygonAnnotation(new HpdfPoint[] { new(0, 0), new(1, 1) }, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreatePolyLineAnnotation(new HpdfPoint[] { new(0, 0) }, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreatePolyLineAnnotation(null!, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreatePolyLineAnnotation(new HpdfPoint[] { new(0, 0), new(float.NaN, 1) }, ""))
                .Should().Throw<HpdfException>();
            page.Invoking(p => p.CreatePolygonAnnotation(Triangle, null!)).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void Save_DrawsClosedPathAndComputesRect()
        {
            var doc = new HpdfDocument();
            var polygon = doc.AddPage().CreatePolygonAnnotation(Triangle, "");
            polygon.SetInteriorGrayColor(0.8f);

            doc.SaveToMemory();

            // Half the line width plus 1
            Numbers(polygon.Dict["Rect"]).Should().Equal(98.5f, 98.5f, 201.5f, 181.5f);
            AppearanceContent(polygon).Should().Contain("100 100 m\n200 100 l\n150 180 l\nh\nB");
        }

        [Fact]
        public void SetVertices_ReplacesTheShape()
        {
            var doc = new HpdfDocument();
            var polygon = doc.AddPage().CreatePolygonAnnotation(Triangle, "");

            polygon.SetVertices(new(0, 0), new(10, 0), new(10, 10), new(0, 10));
            doc.SaveToMemory();

            Numbers(polygon.Dict["Vertices"]).Should().HaveCount(8);
            Numbers(polygon.Dict["Rect"]).Should().Equal(-1.5f, -1.5f, 11.5f, 11.5f);
        }

        [Fact]
        public void CloudEffect_GrowsTheRect()
        {
            var doc = new HpdfDocument();
            var polygon = doc.AddPage().CreatePolygonAnnotation(Triangle, "");

            polygon.SetCloudEffect(2);
            polygon.SetIntent(HpdfPolygonIntent.Cloud);
            doc.SaveToMemory();

            ((HpdfNumber)((HpdfDict)polygon.Dict["BE"])["I"]).Value.Should().Be(2);
            ((HpdfName)polygon.Dict["IT"]).Value.Should().Be("PolygonCloud");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
            // Radius 9.5 plus half the line width plus 1
            Numbers(polygon.Dict["Rect"]).Should().Equal(89f, 89f, 211f, 191f);
            AppearanceContent(polygon).Split('\n').Count(op => op.EndsWith(" c")).Should().BeGreaterThan(10);
        }

        [Fact]
        public void PolyLine_EndingsAndIntent()
        {
            var doc = new HpdfDocument();
            var polyline = doc.AddPage().CreatePolyLineAnnotation(Path, "");

            polyline.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.ClosedArrow);
            polyline.SetInteriorRgbColor(0, 0, 1);
            polyline.SetIntent(HpdfPolyLineIntent.Dimension);
            doc.SaveToMemory();

            ((HpdfArray)polyline.Dict["LE"]).Select(n => ((HpdfName)n).Value).Should().Equal("None", "ClosedArrow");
            ((HpdfName)polyline.Dict["IT"]).Value.Should().Be("PolyLineDimension");
            // Ending size 9, half the line width and 1
            Numbers(polyline.Dict["Rect"]).Should().Equal(89.5f, 289.5f, 210.5f, 360.5f);
            string content = AppearanceContent(polyline);
            content.Should().Contain("100 300 m\n150 350 l\n");
            content.Should().Contain("0 0 1 rg");
            content.Split('\n').Should().Contain("B");
        }

        [Fact]
        public void PdfA_DoesNotAllowPolygons()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().CreatePolygonAnnotation(Triangle, "");

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.CreatePolygonAnnotation(Triangle, "").SetCloudEffect(1);
            page.CreatePolyLineAnnotation(Path, "").SetLineEndingStyle(HpdfLineEndingStyle.Circle, HpdfLineEndingStyle.Slash);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedPolygon_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreatePolygonAnnotation(Triangle, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Polygon);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
