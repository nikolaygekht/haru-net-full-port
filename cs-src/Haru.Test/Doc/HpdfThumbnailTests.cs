using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Test.Png;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfThumbnailTests
    {
        private static HpdfImage RgbImage(HpdfDocument doc) =>
            HpdfImage.LoadRawImageFromMem(doc.Xref, "Thumb", new byte[] { 255, 0, 0, 0, 0, 255 }, 2, 1,
                HpdfColorSpace.DeviceRgb, 8);

        [Fact]
        public void SetThumbnail_WritesThumbEntry()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = RgbImage(doc);

            page.SetThumbnail(image);
            var bytes = doc.SaveToMemory();

            page.Thumbnail.Should().BeSameAs(image);
            page.Dict["Thumb"].Should().BeSameAs(image.StreamObject);
            image.StreamObject.IsIndirect.Should().BeTrue();
            System.Text.Encoding.ASCII.GetString(bytes).Should().Contain("/Thumb ");
            doc.Version.Should().Be(HpdfVersion.Version12);
        }

        [Fact]
        public void SetThumbnail_Null_RemovesIt()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = RgbImage(doc);
            page.SetThumbnail(image);

            page.SetThumbnail(null);

            page.Thumbnail.Should().BeNull();
            page.Dict.ContainsKey("Thumb").Should().BeFalse();
        }

        [Fact]
        public void SetThumbnail_GrayAndOpaquePng_AreAccepted()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var gray = HpdfImage.LoadRawImageFromMem(doc.Xref, "Gray", new byte[] { 0, 255 }, 2, 1,
                HpdfColorSpace.DeviceGray, 8);
            using var png = doc.LoadPngImage(new MemoryStream(
                TestPngEncoder.EncodeRgba(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 }, 2, 1)));

            page.Invoking(p => p.SetThumbnail(gray)).Should().NotThrow();
            page.Invoking(p => p.SetThumbnail(png)).Should().NotThrow();
        }

        [Fact]
        public void SetThumbnail_UnsuitableImages_Throw()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var cmyk = HpdfImage.LoadRawImageFromMem(doc.Xref, "Cmyk", new byte[4], 1, 1,
                HpdfColorSpace.DeviceCmyk, 8);
            using var transparent = doc.LoadPngImage(new MemoryStream(
                TestPngEncoder.EncodeRgba(new byte[] { 255, 0, 0, 128, 0, 0, 255, 255 }, 2, 1)));
            var other = new HpdfDocument();
            using var foreign = RgbImage(other);

            page.Invoking(p => p.SetThumbnail(cmyk)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidColorSpace);
            page.Invoking(p => p.SetThumbnail(transparent)).Should().Throw<HpdfException>("it has a soft mask");
            page.Invoking(p => p.SetThumbnail(foreign)).Should().Throw<HpdfException>();
            page.Thumbnail.Should().BeNull();
            page.Dict.ContainsKey("Thumb").Should().BeFalse();
        }

        [Fact]
        public void Save_MaskAddedAfterSetThumbnail_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = RgbImage(doc);
            page.SetThumbnail(image);

            image.SetColorMask(0, 10, 0, 10, 0, 10);

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>();
        }
    }
}
