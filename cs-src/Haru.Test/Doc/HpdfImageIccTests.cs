using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Xunit;
using static Haru.Test.Doc.HpdfIccProfileTests;

namespace Haru.Test.Doc
{
    public class HpdfImageIccTests
    {
        private static HpdfImage Rgb(HpdfDocument doc) =>
            HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 255, 0, 0, 0, 255, 0 }, 2, 1,
                HpdfColorSpace.DeviceRgb, 8);

        [Fact]
        public void SetIccProfile_WritesICCBasedColorSpace()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = Rgb(doc);
            var proPhoto = doc.LoadIccProfile(ProPhoto);

            image.SetIccProfile(proPhoto);
            page.DrawImage(image, 50, 50, 100, 50);
            doc.SaveToMemory();

            image.IccProfile.Should().BeSameAs(proPhoto);
            var colorSpace = (HpdfArray)image.Dict["ColorSpace"];
            ((HpdfName)colorSpace[0]).Value.Should().Be("ICCBased");
            colorSpace[1].Should().BeSameAs(proPhoto.StreamObject);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13, "ICC version 2.1 needs PDF 1.3");
        }

        [Fact]
        public void SetIccProfile_SharesOneColorSpaceBetweenImages()
        {
            var doc = new HpdfDocument();
            using var first = Rgb(doc);
            using var second = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im2", new byte[3], 1, 1, HpdfColorSpace.DeviceRgb, 8);

            first.SetIccProfile(doc.SRgbProfile);
            second.SetIccProfile(doc.SRgbProfile);

            second.Dict["ColorSpace"].Should().BeSameAs(first.Dict["ColorSpace"]);
        }

        [Fact]
        public void SetIccProfile_Null_ReturnsToTheDeviceColorSpace()
        {
            var doc = new HpdfDocument();
            using var image = Rgb(doc);
            image.SetIccProfile(doc.SRgbProfile);

            image.SetIccProfile(null);

            image.IccProfile.Should().BeNull();
            ((HpdfName)image.Dict["ColorSpace"]).Value.Should().Be("DeviceRGB");
        }

        [Fact]
        public void SetIccProfile_Version4_RaisesThePdfVersion()
        {
            var doc = new HpdfDocument();
            using var image = Rgb(doc);

            image.SetIccProfile(doc.LoadIccProfile(DisplayP3V4));

            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17, "ICC version 4.2 needs PDF 1.7");
        }

        [Fact]
        public void SetIccProfile_GrayAndCmykImages()
        {
            var doc = new HpdfDocument();
            using var gray = HpdfImage.LoadRawImageFromMem(doc.Xref, "G", new byte[] { 0, 255 }, 2, 1, HpdfColorSpace.DeviceGray, 8);
            using var cmyk = HpdfImage.LoadRawImageFromMem(doc.Xref, "C", new byte[4], 1, 1, HpdfColorSpace.DeviceCmyk, 8);

            gray.SetIccProfile(doc.LoadIccProfile(Grey));
            cmyk.SetIccProfile(doc.LoadIccProfile(CmykHeader()));

            gray.IccProfile!.ComponentCount.Should().Be(1);
            cmyk.IccProfile!.ComponentCount.Should().Be(4);
        }

        [Fact]
        public void SetIccProfile_InvalidUse_Throws()
        {
            var doc = new HpdfDocument();
            using var image = Rgb(doc);
            using var mask = HpdfImage.LoadRawImageFromMem(doc.Xref, "M", new byte[] { 0, 255 }, 2, 1, HpdfColorSpace.DeviceGray, 8);
            image.SetSoftMask(mask);
            var other = new HpdfDocument();

            image.Invoking(i => i.SetIccProfile(doc.LoadIccProfile(Grey))).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidIccComponentNum);
            image.Invoking(i => i.SetIccProfile(other.SRgbProfile)).Should().Throw<HpdfException>("another document");
            mask.Invoking(m => m.SetIccProfile(doc.LoadIccProfile(Grey))).Should().Throw<HpdfException>("a soft mask");
            image.IccProfile.Should().BeNull();
            ((HpdfName)image.Dict["ColorSpace"]).Value.Should().Be("DeviceRGB");
        }

        [Fact]
        public void ColorMask_WorksOnAnImageWithAProfile()
        {
            var doc = new HpdfDocument();
            using var image = Rgb(doc);
            image.SetIccProfile(doc.SRgbProfile);

            image.SetColorMask(250, 255, 0, 5, 0, 5);

            ((HpdfArray)image.Dict["Mask"]).Count.Should().Be(6);
        }

        [Fact]
        public void PdfA1_ProfileOfVersion4SetByHand_FailsSaving()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = Rgb(doc);
            image.SetIccProfile(doc.LoadIccProfile(DisplayP3V4));
            page.DrawImage(image, 50, 50, 100, 50);
            doc.SetPdfACompliance("1B");

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>()
                .Which.Message.Should().Contain("version 2");
        }

        [Fact]
        public void PdfA1_ProfileOfVersion2_IsKept()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = Rgb(doc);
            image.SetIccProfile(doc.LoadIccProfile(ProPhoto));
            page.DrawImage(image, 50, 50, 100, 50);
            doc.SetPdfACompliance("1B");

            doc.SaveToMemory();

            image.Dict["ColorSpace"].Should().BeOfType<HpdfArray>();
        }
    }
}
