using System;
using System.IO;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests the ports of HPDF_Image_GetColorSpace, HPDF_LoadRawImageFromFile, HPDF_TextAnnot_SetIcon and
    /// HPDF_Page_CreateWidgetAnnot_WhiteOnlyWhilePrint.
    /// </summary>
    public class HpdfImageAndAnnotationGapsTests
    {
        [Fact]
        public void Image_ColorSpace()
        {
            var doc = new HpdfDocument();
            using var gray = HpdfImage.LoadRawImageFromMem(doc.Xref, "G", new byte[] { 0 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            using var rgb = HpdfImage.LoadRawImageFromMem(doc.Xref, "R", new byte[3], 1, 1, HpdfColorSpace.DeviceRgb, 8);
            using var cmyk = HpdfImage.LoadRawImageFromMem(doc.Xref, "C", new byte[4], 1, 1, HpdfColorSpace.DeviceCmyk, 8);
            using var icc = HpdfImage.LoadRawImageFromMem(doc.Xref, "I", new byte[3], 1, 1, HpdfColorSpace.DeviceRgb, 8);
            using var stencil = HpdfImage.LoadRawImageFromMem(doc.Xref, "S", new byte[] { 255 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            icc.SetIccProfile(doc.SRgbProfile);
            using var masked = HpdfImage.LoadRawImageFromMem(doc.Xref, "M", new byte[] { 0 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            masked.SetMaskImage(stencil);

            gray.ColorSpace.Should().Be(HpdfColorSpace.DeviceGray);
            rgb.ColorSpace.Should().Be(HpdfColorSpace.DeviceRgb);
            cmyk.ColorSpace.Should().Be(HpdfColorSpace.DeviceCmyk);
            icc.ColorSpace.Should().Be(HpdfColorSpace.IccBased);
            stencil.ColorSpace.Should().BeNull("a stencil mask has no color space");
        }

        [Fact]
        public void LoadRawImageFromFile_ReadsThePixels()
        {
            string path = Path.Combine(Path.GetTempPath(), "haru-raw-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, new byte[] { 10, 20, 30, 40, 50, 60 });
            try
            {
                var doc = new HpdfDocument();
                using var image = HpdfImage.LoadRawImageFromFile(doc.Xref, "Raw1", path, 2, 1, HpdfColorSpace.DeviceRgb, 8);

                image.Width.Should().Be(2);
                image.StreamObject.Stream.ToArray().Should().Equal(10, 20, 30, 40, 50, 60);
                Action missing = () => HpdfImage.LoadRawImageFromFile(doc.Xref, "Raw2", path + ".none", 2, 1, HpdfColorSpace.DeviceRgb, 8);
                missing.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.FileNotFound);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void TextAnnotation_SetIcon()
        {
            var page = new HpdfDocument().AddPage();
            var note = page.CreateTextAnnotation(new HpdfRect(50, 50, 70, 70), "Note");

            note.Icon.Should().Be(HpdfAnnotationIcon.Note);
            note.SetIcon(HpdfAnnotationIcon.Key);

            note.Icon.Should().Be(HpdfAnnotationIcon.Key);
            ((HpdfName)note.Dict["Name"]).Value.Should().Be("Key");
            note.Invoking(n => n.SetIcon((HpdfAnnotationIcon)99)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.AnnotInvalidIcon);
        }

        [Fact]
        public void WhiteOnlyWhilePrint_WritesTheWidgetOfTheCLibrary()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            var blind = doc.CreateWhiteOnlyWhilePrintAnnotation(page, new HpdfRect(100, 100, 300, 150));
            doc.SaveToMemory();

            page.Annotations.Should().Equal(blind);
            blind.Type.Should().Be(HpdfAnnotationType.Widget);
            blind.Flags.Should().Be(HpdfAnnotationFlags.Print | HpdfAnnotationFlags.NoView);
            ((HpdfNumber)blind.Dict["F"]).Value.Should().Be(36);
            ((HpdfName)blind.Dict["FT"]).Value.Should().Be("Btn");
            ((HpdfString)blind.Dict["T"]).GetText().Should().Be("Blind");
            var normal = (HpdfStreamObject)((HpdfDict)blind.Dict["AP"])["N"];
            normal.Should().BeSameAs(doc.FormXObjects[0].StreamObject);
            System.Text.Encoding.ASCII.GetString(normal.Stream.ToArray()).Should().Be("1 g\n0 0 200 50 re\nf\n");
            ((HpdfArray)((HpdfDict)blind.Dict["MK"])["BG"]).Count.Should().Be(3);
        }

        [Fact]
        public void WhiteOnlyWhilePrint_InvalidUse_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var other = new HpdfDocument().AddPage();
            doc.CreateWhiteOnlyWhilePrintAnnotation(page, new HpdfRect(0, 0, 10, 10));
            doc.SetPdfACompliance("1B");

            doc.Invoking(d => d.CreateWhiteOnlyWhilePrintAnnotation(other, new HpdfRect(0, 0, 10, 10)))
                .Should().Throw<HpdfException>();
            doc.Invoking(d => d.CreateWhiteOnlyWhilePrintAnnotation(null!, new HpdfRect(0, 0, 10, 10)))
                .Should().Throw<HpdfException>();
            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>().Which.Message.Should().Contain("NoView");
        }
    }
}
