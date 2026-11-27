using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfRedactAnnotationTests
    {
        private static readonly HpdfRect Area = new HpdfRect(100, 700, 300, 714);

        [Fact]
        public void CreateRedactAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "Personal data");

            redact.Type.Should().Be(HpdfAnnotationType.Redact);
            ((HpdfName)redact.Dict["Subtype"]).Value.Should().Be("Redact");
            ((HpdfString)redact.Dict["Contents"]).GetText().Should().Be("Personal data");
            Numbers(redact.Dict["C"]).Should().Equal(1f, 0f, 0f);
            redact.Dict.ContainsKey("QuadPoints").Should().BeFalse();
            redact.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
        }

        [Fact]
        public void Save_OutlinesTheRect()
        {
            var doc = new HpdfDocument();
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "");

            doc.SaveToMemory();

            Numbers(redact.Dict["Rect"]).Should().Equal(100f, 700f, 300f, 714f);
            AppearanceContent(redact).Should().Contain("1 0 0 RG\n1 w\n100.5 700.5 199 13 re\nS");
        }

        [Fact]
        public void SetQuadPoints_OutlinesEveryArea()
        {
            var doc = new HpdfDocument();
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "");

            redact.SetQuadPoints(Area, new HpdfRect(100, 680, 200, 694));
            doc.SaveToMemory();

            Numbers(redact.Dict["QuadPoints"]).Should().HaveCount(16);
            Numbers(redact.Dict["Rect"]).Should().Equal(99f, 679f, 301f, 715f);
            AppearanceContent(redact).Split('\n').Count(op => op == "h").Should().Be(2);
        }

        [Fact]
        public void InteriorColor_WritesIc()
        {
            var redact = new HpdfDocument().AddPage().CreateRedactAnnotation(Area, "");

            redact.SetInteriorGrayColor(0);

            Numbers(redact.Dict["IC"]).Should().Equal(0f);
        }

        [Fact]
        public void SetOverlayText_WritesTextAppearanceAlignmentAndRepeat()
        {
            var doc = new HpdfDocument();
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.HelveticaBold, "F2");
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "");

            redact.SetOverlayText("REDACTED", font, 10, HpdfTextAlignment.Center, repeat: true);
            redact.SetOverlayTextRgbColor(1, 1, 1);

            ((HpdfString)redact.Dict["OverlayText"]).GetText().Should().Be("REDACTED");
            ((HpdfString)redact.Dict["DA"]).GetText().Should().Be("/F2 10 Tf 1 1 1 rg");
            ((HpdfNumber)redact.Dict["Q"]).Value.Should().Be(1);
            ((HpdfBoolean)redact.Dict["Repeat"]).Value.Should().BeTrue();

            redact.RemoveOverlayText();
            redact.Dict.ContainsKey("OverlayText").Should().BeFalse();
            redact.Dict.ContainsKey("DA").Should().BeFalse();
        }

        [Fact]
        public void SetOverlayText_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1");
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "");

            redact.Invoking(r => r.SetOverlayText(null!, font, 10)).Should().Throw<HpdfException>();
            redact.Invoking(r => r.SetOverlayText("x", null!, 10)).Should().Throw<HpdfException>();
            redact.Invoking(r => r.SetOverlayText("x", font, 0)).Should().Throw<HpdfException>();
            redact.Invoking(r => r.SetOverlayText("x", font, 10, HpdfTextAlignment.Justify)).Should().Throw<HpdfException>();
            redact.Dict.ContainsKey("OverlayText").Should().BeFalse();
        }

        [Fact]
        public void OverlayFont_GoesToTheResourcesOfAnExistingForm()
        {
            var doc = new HpdfDocument();
            var form = doc.GetOrCreateAcroForm();
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.Courier, "F3");
            doc.AddPage().CreateRedactAnnotation(Area, "").SetOverlayText("X", font, 8);

            doc.SaveToMemory();

            ((HpdfDict)((HpdfDict)form.Dict["DR"])["Font"])["F3"].Should().BeSameAs(font.Dict);
        }

        [Fact]
        public void PdfA_DoesNotAllowRedactions()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().CreateRedactAnnotation(Area, "");

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var redact = doc.AddPage().CreateRedactAnnotation(Area, "");
            redact.SetQuadPoints(Area);
            redact.SetInteriorGrayColor(0);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedRedaction_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateRedactAnnotation(Area, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.AnnotationDictionary.Data["Subtype"].ToString().Should().Contain("Redact");
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
