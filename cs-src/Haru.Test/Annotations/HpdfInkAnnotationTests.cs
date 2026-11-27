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
    public class HpdfInkAnnotationTests
    {
        private static readonly HpdfPoint[][] Strokes =
        {
            new HpdfPoint[] { new(100, 100), new(120, 130), new(140, 110) },
            new HpdfPoint[] { new(200, 200) }
        };

        [Fact]
        public void CreateInkAnnotation_WritesInkList()
        {
            var doc = new HpdfDocument();
            var ink = doc.AddPage().CreateInkAnnotation(Strokes, "Signature");

            ink.Type.Should().Be(HpdfAnnotationType.Ink);
            ((HpdfName)ink.Dict["Subtype"]).Value.Should().Be("Ink");
            var list = (HpdfArray)ink.Dict["InkList"];
            list.Count.Should().Be(2);
            Numbers(list[0]).Should().Equal(100f, 100f, 120f, 130f, 140f, 110f);
            Numbers(list[1]).Should().Equal(200f, 200f);
            ink.Strokes.Should().HaveCount(2);
            Numbers(ink.Dict["C"]).Should().Equal(1f, 0f, 0f);
            ink.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void CreateInkAnnotation_InvalidStrokes_Throw()
        {
            var page = new HpdfDocument().AddPage();

            page.Invoking(p => p.CreateInkAnnotation(System.Array.Empty<HpdfPoint[]>(), "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateInkAnnotation(new[] { System.Array.Empty<HpdfPoint>() }, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateInkAnnotation(new[] { new HpdfPoint[] { new(float.NaN, 0) } }, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateInkAnnotation(Strokes, null!)).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void Save_DrawsStrokesWithRoundEnds()
        {
            var doc = new HpdfDocument();
            var ink = doc.AddPage().CreateInkAnnotation(Strokes, "");
            ink.SetBorderStyle(HpdfBorderStyle.Solid, 2);

            doc.SaveToMemory();

            Numbers(ink.Dict["Rect"]).Should().Equal(98f, 98f, 202f, 202f);
            string content = AppearanceContent(ink);
            content.Should().Contain("2 w\n1 J\n1 j\n");
            content.Should().Contain("100 100 m\n120 130 l\n140 110 l\n200 200 m\n200 200 l\nS");
        }

        [Fact]
        public void SetInkList_ReplacesStrokes()
        {
            var doc = new HpdfDocument();
            var ink = doc.AddPage().CreateInkAnnotation(Strokes, "");

            ink.SetInkList(new HpdfPoint[] { new(0, 0), new(10, 10) });
            doc.SaveToMemory();

            ((HpdfArray)ink.Dict["InkList"]).Count.Should().Be(1);
            Numbers(ink.Dict["Rect"]).Should().Equal(-1.5f, -1.5f, 11.5f, 11.5f);
        }

        [Fact]
        public void NoColor_DrawsNothing()
        {
            var doc = new HpdfDocument();
            var ink = doc.AddPage().CreateInkAnnotation(Strokes, "");

            ink.SetNoColor();
            doc.SaveToMemory();

            AppearanceContent(ink).Should().BeEmpty();
        }

        [Fact]
        public void PdfA_AllowsInk()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().CreateInkAnnotation(Strokes, "");

            var act = () => doc.SaveToMemory();

            act.Should().NotThrow();
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateInkAnnotation(Strokes, "").SetTransparency(0.5f);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedInk_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateInkAnnotation(Strokes, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Ink);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
