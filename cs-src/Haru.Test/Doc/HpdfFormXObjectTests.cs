using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Doc.HpdfIccProfileTests;

namespace Haru.Test.Doc
{
    public class HpdfFormXObjectTests
    {
        private static string Content(HpdfCanvas canvas) =>
            System.Text.Encoding.ASCII.GetString(canvas.Contents.Stream.ToArray());

        private static HpdfFormXObject Letterhead(HpdfDocument doc)
        {
            var form = doc.CreateFormXObject(200, 50);
            form.SetFontAndSize(new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1"), 20);
            form.TextOut(10, 15, "Letterhead");
            return form;
        }

        [Fact]
        public void CreateFormXObject_WritesFormDictionary()
        {
            var doc = new HpdfDocument();

            var form = doc.CreateFormXObject(200, 50);

            form.LocalName.Should().Be("HaruFm1");
            form.Width.Should().Be(200);
            form.Height.Should().Be(50);
            ((HpdfName)form.StreamObject["Type"]).Value.Should().Be("XObject");
            ((HpdfName)form.StreamObject["Subtype"]).Value.Should().Be("Form");
            ((HpdfArray)form.StreamObject["BBox"]).Count.Should().Be(4);
            ((object)form.GetResources()).Should().BeSameAs(form.StreamObject["Resources"]);
            form.StreamObject.IsIndirect.Should().BeTrue();
            doc.FormXObjects.Should().Equal(form);
        }

        [Fact]
        public void FormOnTwoPages_IsWrittenOnceAndReadBack()
        {
            var doc = new HpdfDocument();
            var form = Letterhead(doc);
            var first = doc.AddPage();
            var second = doc.AddPage();

            first.DrawFormXObject(form, 50, 750);
            second.DrawFormXObject(form, 50, 750, 400, 100);
            byte[] bytes = doc.SaveToMemory();

            ((HpdfDict)first.GetResources()["XObject"])["HaruFm1"].Should().BeSameAs(form.StreamObject);
            ((HpdfDict)second.GetResources()["XObject"])["HaruFm1"].Should().BeSameAs(form.StreamObject);
            ((HpdfDict)form.GetResources()["Font"]).ContainsKey("F1").Should().BeTrue();
            Content(first).Should().Contain("q\n1 0 0 1 50 750 cm\n/HaruFm1 Do\nQ\n");
            Content(second).Should().Contain("q\n2 0 0 2 50 750 cm\n/HaruFm1 Do\nQ\n");
            System.Text.Encoding.ASCII.GetString(bytes).Split("/Subtype /Form").Length.Should().Be(2, "one form object");

            using var pdf = PdfDocument.Open(new MemoryStream(bytes));
            pdf.GetPage(1).Text.Should().Be("Letterhead");
            pdf.GetPage(2).Text.Should().Be("Letterhead");
        }

        [Fact]
        public void ExecuteXObject_ShowsTheFormWithoutTransformation()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var form = doc.CreateFormXObject(10, 10);

            page.ExecuteXObject(form);

            Content(page).Should().Be("/HaruFm1 Do\n");
        }

        [Fact]
        public void FormInAForm_AndCycles()
        {
            var doc = new HpdfDocument();
            var inner = doc.CreateFormXObject(10, 10);
            var outer = doc.CreateFormXObject(20, 20);
            var outermost = doc.CreateFormXObject(30, 30);
            var other = new HpdfDocument().CreateFormXObject(10, 10);

            outer.DrawFormXObject(inner, 0, 0);
            outermost.DrawFormXObject(outer, 0, 0);

            outer.Invoking(f => f.DrawFormXObject(outer, 0, 0)).Should().Throw<HpdfException>("itself");
            inner.Invoking(f => f.DrawFormXObject(outermost, 0, 0)).Should().Throw<HpdfException>("a cycle");
            outer.Invoking(f => f.DrawFormXObject(other, 0, 0)).Should().Throw<HpdfException>("another document");
            ((HpdfDict)outer.GetResources()["XObject"])["HaruFm1"].Should().BeSameAs(inner.StreamObject);
        }

        [Fact]
        public void DrawFormXObject_InvalidUse_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var form = doc.CreateFormXObject(10, 10);
            page.SetFontAndSize(doc.GetFont("Helvetica", null), 10);

            page.BeginText();
            page.Invoking(p => p.DrawFormXObject(form, 0, 0)).Should().Throw<HpdfException>("inside a text object");
            page.EndText();
            page.Invoking(p => p.DrawFormXObject(form, 0, 0, 0, 10)).Should().Throw<HpdfException>();
            page.Invoking(p => p.DrawFormXObject(null!, 0, 0)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.CreateFormXObject(0, 10)).Should().Throw<HpdfException>();
            page.GetResources().ContainsKey("XObject").Should().BeFalse();
        }

        [Fact]
        public void Save_FormWithUnbalancedState_Throws()
        {
            var unbalanced = new HpdfDocument();
            unbalanced.AddPage();
            unbalanced.CreateFormXObject(10, 10).GSave();
            var openText = new HpdfDocument();
            openText.AddPage();
            openText.CreateFormXObject(10, 10).BeginText();

            unbalanced.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.PageCannotRestoreGState);
            openText.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>().Which.Message.Should().Contain("EndText");
        }

        [Fact]
        public void CreateFormXObjectFromImage_AndWhiteRect()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 255, 0, 0, 0, 255, 0 }, 2, 1,
                HpdfColorSpace.DeviceRgb, 8);

            var zoomed = doc.CreateFormXObjectFromImage(image, new HpdfRect(10, 10, 110, 60), zoom: true);
            var natural = doc.CreateFormXObjectFromImage(image, new HpdfRect(10, 60, 110, 10), zoom: false);
            var white = doc.CreateWhiteRectFormXObject(new HpdfRect(0, 0, 30, 20));

            zoomed.Width.Should().Be(100);
            natural.Height.Should().Be(50, "reversed edges are swapped");
            Content(zoomed).Should().Contain("100 0 0 50 0 0 cm\n/Im1 Do");
            Content(natural).Should().Contain("2 0 0 1 0 0 cm\n/Im1 Do");
            Content(white).Should().Be("1 g\n0 0 30 20 re\nf\n");
        }

        [Fact]
        public void Save_WithForms_IsRepeatable()
        {
            var doc = new HpdfDocument();
            doc.SetCompressionMode(HpdfCompressionMode.All);
            var form = Letterhead(doc);
            doc.AddPage().DrawFormXObject(form, 50, 750);

            var first = doc.SaveToMemory();
            var second = doc.SaveToMemory();

            second.Should().Equal(first);
            form.StreamObject.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
        }

        [Fact]
        public void PdfA1_FormWithAVersion4Color_FailsSaving()
        {
            var doc = new HpdfDocument();
            var form = doc.CreateFormXObject(10, 10);
            form.SetIccFill(doc.LoadIccProfile(DisplayP3V4), 1, 0, 0);
            doc.AddPage().DrawFormXObject(form, 0, 0);
            doc.SetPdfACompliance("1B");

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>().Which.Message.Should().Contain("version 2");
        }
    }
}
