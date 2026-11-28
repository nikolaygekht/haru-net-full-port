using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Xunit;
using static Haru.Test.Doc.HpdfIccProfileTests;

namespace Haru.Test.Doc
{
    public class HpdfIccColorTests
    {
        private static string Content(HpdfPage page) =>
            System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());

        [Fact]
        public void SetIccFillAndStroke_WriteColorSpaceAndColor()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var proPhoto = doc.LoadIccProfile(ProPhoto);
            var grey = doc.LoadIccProfile(Grey);

            page.SetIccFill(proPhoto, 0.5f, 0.25f, 1f);
            page.SetIccStroke(grey, 0.75f);

            Content(page).Should().Contain("/HaruCS1 cs\n0.5 0.25 1 scn\n").And.Contain("/HaruCS2 CS\n0.75 SCN\n");
            var colorSpaces = (HpdfDict)page.GetResources()["ColorSpace"];
            colorSpaces["HaruCS1"].Should().BeSameAs(colorSpaces["HaruCS1"]);
            ((HpdfArray)colorSpaces["HaruCS1"])[1].Should().BeSameAs(proPhoto.StreamObject);
            ((HpdfArray)colorSpaces["HaruCS2"])[1].Should().BeSameAs(grey.StreamObject);
            page.GraphicsState.FillColorSpace.Should().Be(HpdfColorSpace.IccBased);
            page.GraphicsState.FillIccProfile.Should().BeSameAs(proPhoto);
            page.GraphicsState.FillIccComponents.Should().Equal(0.5f, 0.25f, 1f);
            page.GraphicsState.StrokeIccProfile.Should().BeSameAs(grey);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void GRestore_RestoresThePreviousColor()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetRgbFill(1, 0, 0);

            page.GSave();
            page.SetIccFill(doc.SRgbProfile, 0, 0, 1);
            page.GRestore();

            page.GraphicsState.FillColorSpace.Should().Be(HpdfColorSpace.DeviceRgb);
            page.GraphicsState.FillIccProfile.Should().BeNull();
        }

        [Fact]
        public void SameProfileTwice_IsOneResource()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            page.SetIccFill(doc.SRgbProfile, 1, 0, 0);
            page.SetIccStroke(doc.SRgbProfile, 0, 1, 0);

            ((HpdfDict)page.GetResources()["ColorSpace"]).Count.Should().Be(1);
        }

        [Fact]
        public void SetIccFill_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var other = new HpdfDocument();

            page.Invoking(p => p.SetIccFill(doc.SRgbProfile, 1, 0)).Should().Throw<HpdfException>("three components");
            page.Invoking(p => p.SetIccFill(doc.SRgbProfile, 1, 0, 2)).Should().Throw<HpdfException>("out of range");
            page.Invoking(p => p.SetIccFill(null!, 1)).Should().Throw<HpdfException>();
            page.Invoking(p => p.SetIccStroke(other.SRgbProfile, 1, 0, 0)).Should().Throw<HpdfException>("another document");
            Content(page).Should().NotContain(" cs").And.NotContain(" CS");
        }

        [Fact]
        public void PdfA1_ColorInAVersion4Profile_FailsSaving()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetIccFill(doc.LoadIccProfile(DisplayP3V4), 1, 0, 0);
            doc.SetPdfACompliance("1B");

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>().Which.Message.Should().Contain("version 2");
        }

        [Fact]
        public void PdfA1_ColorInAVersion2Profile_IsSaved()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetIccFill(doc.LoadIccProfile(ProPhoto), 1, 0, 0);
            doc.SetPdfACompliance("1B");

            doc.Invoking(d => d.SaveToMemory()).Should().NotThrow();
        }

        [Fact]
        public void DocumentWithIccColors_IsSavedAndReadBack()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = doc.GetFont("Helvetica", null);
            page.SetIccFill(doc.LoadIccProfile(ProPhoto), 0.2f, 0.6f, 0.3f);
            page.SetFontAndSize(font, 20);
            page.TextOut(50, 700, "ICC colored text");

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(new System.IO.MemoryStream(doc.SaveToMemory()));

            pdf.GetPage(1).Text.Should().Be("ICC colored text");
        }
    }
}
