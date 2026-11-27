using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Tokens;
using Xunit;

namespace Haru.Test.Annotations
{
    public class HpdfShapeAnnotationTests
    {
        private static readonly HpdfRect ShapeRect = new HpdfRect(100, 100, 300, 200);

        internal static HpdfStreamObject Appearance(HpdfAnnotation annot) =>
            (HpdfStreamObject)((HpdfDict)annot.Dict["AP"])["N"];

        internal static string AppearanceContent(HpdfAnnotation annot) =>
            System.Text.Encoding.ASCII.GetString(Appearance(annot).Stream.ToArray());

        internal static float[] Numbers(HpdfObject array) =>
            ((HpdfArray)array).Select(item => item switch
            {
                HpdfReal real => real.Value,
                HpdfNumber number => number.Value,
                _ => float.NaN
            }).ToArray();

        [Fact]
        public void CreateSquareAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "Square text");

            annot.Type.Should().Be(HpdfAnnotationType.Square);
            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be("Square");
            ((HpdfString)annot.Dict["Contents"]).GetText().Should().Be("Square text");
            Numbers(annot.Dict["C"]).Should().Equal(1f, 0f, 0f);
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void CreateCircleAnnotation_WritesCircleSubtype()
        {
            var annot = new HpdfDocument().AddPage().CreateCircleAnnotation(ShapeRect, "");

            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be("Circle");
        }

        [Fact]
        public void CreateSquareAnnotation_NullText_Throws()
        {
            var page = new HpdfDocument().AddPage();

            var act = () => page.CreateSquareAnnotation(ShapeRect, null!);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void Save_GeneratesSquareAppearanceInsideRect()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");
            annot.SetBorderStyle(HpdfBorderStyle.Solid, 4);

            doc.SaveToMemory();

            var stream = Appearance(annot);
            ((HpdfName)stream["Subtype"]).Value.Should().Be("Form");
            Numbers(stream["BBox"]).Should().Equal(100f, 100f, 300f, 200f);
            string content = AppearanceContent(annot);
            content.Should().Contain("1 0 0 RG");
            content.Should().Contain("4 w");
            content.Should().Contain("102 102 196 96 re");
            content.Should().Contain("S\n");
        }

        [Fact]
        public void Save_CircleAppearanceIsEllipse()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateCircleAnnotation(ShapeRect, "");

            doc.SaveToMemory();

            string content = AppearanceContent(annot);
            content.Should().Contain("100.5 150 m");
            content.Split('\n').Count(line => line.EndsWith(" c")).Should().Be(4);
        }

        [Fact]
        public void InteriorColor_FillsAndStrokes()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetInteriorRgbColor(0, 1, 0);
            doc.SaveToMemory();

            Numbers(annot.Dict["IC"]).Should().Equal(0f, 1f, 0f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version14);
            string content = AppearanceContent(annot);
            content.Should().Contain("0 1 0 rg");
            content.Should().Contain("B\n");
        }

        [Fact]
        public void InteriorTransparent_WritesEmptyArray()
        {
            var annot = new HpdfDocument().AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetInteriorTransparent();

            ((HpdfArray)annot.Dict["IC"]).Count.Should().Be(0);
        }

        [Fact]
        public void NoColorAndNoInterior_DrawsNothing()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetNoColor();
            doc.SaveToMemory();

            AppearanceContent(annot).Should().BeEmpty();
        }

        [Fact]
        public void DashedBorder_SetsDashPattern()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetBorderStyle(HpdfBorderStyle.Dashed, 2, 5, 2);
            doc.SaveToMemory();

            AppearanceContent(annot).Should().Contain("[5 2] 0 d");
        }

        [Fact]
        public void CloudEffect_WritesBorderEffectAndRectDifference()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetCloudEffect(2);
            doc.SaveToMemory();

            var effect = (HpdfDict)annot.Dict["BE"];
            ((HpdfName)effect["S"]).Value.Should().Be("C");
            ((HpdfNumber)effect["I"]).Value.Should().Be(2);
            // Bump radius 9.5 plus half the default line width
            Numbers(annot.Dict["RD"]).Should().Equal(10f, 10f, 10f, 10f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
            Numbers(annot.Dict["Rect"]).Should().Equal(100f, 100f, 300f, 200f);
            AppearanceContent(annot).Split('\n').Count(line => line.EndsWith(" c")).Should().BeGreaterThan(20);
        }

        [Fact]
        public void CloudEffectZero_RemovesBorderEffectAndRectDifference()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateCircleAnnotation(ShapeRect, "");
            annot.SetCloudEffect(1);
            doc.SaveToMemory();

            annot.SetCloudEffect(0);
            doc.SaveToMemory();

            annot.Dict.ContainsKey("BE").Should().BeFalse();
            annot.Dict.ContainsKey("RD").Should().BeFalse();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void CloudEffect_OutOfRange_Throws(int intensity)
        {
            var annot = new HpdfDocument().AddPage().CreateSquareAnnotation(ShapeRect, "");

            var act = () => annot.SetCloudEffect(intensity);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetRectDifference_WritesLeftTopRightBottomAndShrinksShape()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetRectDifference(10, 20, 30, 40);
            doc.SaveToMemory();

            Numbers(annot.Dict["RD"]).Should().Equal(10f, 20f, 30f, 40f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
            // left 110, bottom 140, right 270, top 180, less half the line width
            AppearanceContent(annot).Should().Contain("110.5 140.5 159 39 re");
        }

        [Theory]
        [InlineData(-1f, 0f, 0f, 0f)]
        [InlineData(100f, 0f, 100f, 0f)]
        [InlineData(0f, float.NaN, 0f, 0f)]
        public void SetRectDifference_Invalid_Throws(float left, float top, float right, float bottom)
        {
            var annot = new HpdfDocument().AddPage().CreateSquareAnnotation(ShapeRect, "");

            var act = () => annot.SetRectDifference(left, top, right, bottom);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void Transparency_IsDrawnIntoAppearance()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            annot.SetTransparency(0.5f);
            doc.SaveToMemory();

            var stream = Appearance(annot);
            var graphicsState = (HpdfDict)((HpdfDict)((HpdfDict)stream["Resources"])["ExtGState"])["GS0"];
            ((HpdfReal)graphicsState["CA"]).Value.Should().Be(0.5f);
            ((HpdfReal)graphicsState["ca"]).Value.Should().Be(0.5f);
            AppearanceContent(annot).Should().StartWith("/GS0 gs\n");
        }

        [Fact]
        public void PdfA_WithTransparency_FailsToSave()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");
            annot.SetTransparency(0.5f);

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void CompressedDocument_CompressesAppearance()
        {
            var doc = new HpdfDocument();
            doc.SetCompressionMode(HpdfCompressionMode.All);
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");

            doc.SaveToMemory();

            Appearance(annot).Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var square = page.CreateSquareAnnotation(ShapeRect, "");
            square.SetCloudEffect(1);
            square.SetTransparency(0.7f);
            page.CreateCircleAnnotation(new HpdfRect(100, 300, 200, 400), "").SetInteriorGrayColor(0.5f);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedSquare_IsReadBackWithAppearance()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateSquareAnnotation(ShapeRect, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Square);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
            annotation.AnnotationDictionary.ContainsKey(NameToken.Create("F")).Should().BeTrue();
        }

        [Fact]
        public void CreateWithNotFiniteRect_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = new Haru.Font.HpdfFont(doc.Xref, Haru.Font.HpdfStandardFont.Helvetica, "F1");
            var bad = new HpdfRect(0, 0, float.NaN, 10);
            var note = page.CreateTextAnnotation(ShapeRect, "Note");

            page.Invoking(p => p.CreateSquareAnnotation(bad, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateCircleAnnotation(bad, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFreeTextAnnotation(bad, "", font, 10)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateStampAnnotation(bad, HpdfStampName.Draft, "", font)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreatePopupAnnotation(bad, note)).Should().Throw<HpdfException>();
            page.Annotations.Should().Equal(note);
        }

        [Fact]
        public void PdfA_ClearsHiddenInvisibleAndNoViewFlags()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            var annot = doc.AddPage().CreateSquareAnnotation(ShapeRect, "");
            annot.Flags = HpdfAnnotationFlags.Hidden | HpdfAnnotationFlags.Invisible | HpdfAnnotationFlags.NoView
                | HpdfAnnotationFlags.Locked;

            doc.SaveToMemory();

            annot.Flags.Should().Be(HpdfAnnotationFlags.Print | HpdfAnnotationFlags.Locked);
        }
    }
}
