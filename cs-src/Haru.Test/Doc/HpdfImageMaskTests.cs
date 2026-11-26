using System;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Test.Png;
using Haru.Types;
using Haru.Xref;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for PNG color type detection and image masking (color mask, stencil mask, soft mask).
    /// </summary>
    public class HpdfImageMaskTests
    {
        // ---------- PNG color type detection ----------

        [Fact]
        public void LoadPngImage_GrayImageWithSingleColoredPixel_KeepsRgb()
        {
            // A pixel off any coarse sampling grid must still be detected
            var rgba = CreateRgba(100, 100, (_, _) => (128, 128, 128, 255));
            SetPixel(rgba, 100, 57, 33, (255, 0, 0, 255));

            using var image = LoadPng(rgba, 100, 100);

            ColorSpaceOf(image).Should().Be("DeviceRGB");
            image.Dict.ContainsKey("SMask").Should().BeFalse();
        }

        [Fact]
        public void LoadPngImage_OpaqueImageWithSingleTransparentPixel_CreatesSMask()
        {
            var rgba = CreateRgba(100, 100, (_, _) => (10, 200, 30, 255));
            SetPixel(rgba, 100, 57, 33, (10, 200, 30, 0));

            using var image = LoadPng(rgba, 100, 100);

            image.Dict.ContainsKey("SMask").Should().BeTrue();
            var smask = (HpdfStreamObject)image.Dict["SMask"];
            smask.Stream.ToArray()[33 * 100 + 57].Should().Be(0);
        }

        [Fact]
        public void LoadPngImage_GrayImage_StoresExactGrayValues()
        {
            // Luminance of an R=G=B pixel must equal the input (no rounding down of 255)
            byte[] values = { 0, 1, 127, 128, 254, 255 };
            var rgba = CreateRgba(values.Length, 1, (x, _) => (values[x], values[x], values[x], 255));

            using var image = LoadPng(rgba, values.Length, 1);

            ColorSpaceOf(image).Should().Be("DeviceGray");
            image.StreamObject.Stream.ToArray().Should().Equal(values);
        }

        [Fact]
        public void LoadPngImage_RgbaImage_SplitsColorAndAlpha()
        {
            var rgba = new byte[] { 1, 2, 3, 40, 5, 6, 7, 80 };

            using var image = LoadPng(rgba, 2, 1);

            image.StreamObject.Stream.ToArray().Should().Equal(1, 2, 3, 5, 6, 7);
            ((HpdfStreamObject)image.Dict["SMask"]).Stream.ToArray().Should().Equal(40, 80);
        }

        [Fact]
        public void LoadPngImage_GrayAlphaImage_SplitsGrayAndAlpha()
        {
            var rgba = new byte[] { 9, 9, 9, 0, 200, 200, 200, 255 };

            using var image = LoadPng(rgba, 2, 1);

            ColorSpaceOf(image).Should().Be("DeviceGray");
            image.StreamObject.Stream.ToArray().Should().Equal(9, 200);
            ((HpdfStreamObject)image.Dict["SMask"]).Stream.ToArray().Should().Equal(0, 255);
        }

        // ---------- SetColorMask ----------

        [Fact]
        public void SetColorMask_RgbImage_AddsMaskArray()
        {
            using var image = CreateRgbImage(new HpdfXref(0), "Im1");

            image.SetColorMask(0, 10, 20, 30, 40, 50);

            var mask = (HpdfArray)image.Dict["Mask"];
            mask.Cast<HpdfNumber>().Select(n => n.Value).Should().Equal(0, 10, 20, 30, 40, 50);
        }

        [Fact]
        public void SetColorMask_GrayImage_Throws()
        {
            using var image = CreateGrayImage(new HpdfXref(0), "Im1");

            AssertThrows(() => image.SetColorMask(0, 10, 0, 10, 0, 10), HpdfErrorCode.InvalidColorSpace);
        }

        [Fact]
        public void SetColorMask_ImageWithAlphaChannel_Throws()
        {
            using var image = LoadPng(new byte[] { 1, 2, 3, 40, 5, 6, 7, 80 }, 2, 1);

            AssertThrows(() => image.SetColorMask(0, 10, 0, 10, 0, 10), HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void SetColorMask_CalledTwice_Throws()
        {
            using var image = CreateRgbImage(new HpdfXref(0), "Im1");
            image.SetColorMask(0, 10, 0, 10, 0, 10);

            AssertThrows(() => image.SetColorMask(0, 10, 0, 10, 0, 10), HpdfErrorCode.InvalidOperation);
        }

        [Theory]
        [InlineData(20u, 10u, 0u, 10u, 0u, 10u)]
        [InlineData(0u, 256u, 0u, 10u, 0u, 10u)]
        [InlineData(0u, 10u, 0u, 10u, 0u, 300u)]
        public void SetColorMask_InvalidRange_Throws(uint rmin, uint rmax, uint gmin, uint gmax, uint bmin, uint bmax)
        {
            using var image = CreateRgbImage(new HpdfXref(0), "Im1");

            AssertThrows(() => image.SetColorMask(rmin, rmax, gmin, gmax, bmin, bmax), HpdfErrorCode.InvalidParameter);
        }

        // ---------- SetMaskImage (stencil mask) ----------

        [Fact]
        public void SetMaskImage_LightPixelsBecomeMaskedOutBits()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1", width: 9, height: 1);
            // Light (>= 128) => bit 1 => masked out; dark => bit 0 => painted
            using var mask = CreateGrayImage(xref, "Im2", new byte[] { 255, 0, 128, 127, 0, 0, 0, 0, 200 }, width: 9, height: 1);

            image.SetMaskImage(mask);

            mask.IsImageMask.Should().BeTrue();
            mask.BitsPerComponent.Should().Be(1);
            mask.Dict.ContainsKey("ColorSpace").Should().BeFalse();
            mask.StreamObject.Stream.ToArray().Should().Equal(0b1010_0000, 0b1000_0000);
            image.Dict["Mask"].Should().BeSameAs(mask.StreamObject);
        }

        [Fact]
        public void SetMaskImage_SameMaskForTwoImages_ConvertsOnce()
        {
            var xref = new HpdfXref(0);
            using var image1 = CreateRgbImage(xref, "Im1");
            using var image2 = CreateRgbImage(xref, "Im2");
            using var mask = CreateGrayImage(xref, "Im3", new byte[] { 255, 0, 0, 255 });

            image1.SetMaskImage(mask);
            var packed = mask.StreamObject.Stream.ToArray();
            image2.SetMaskImage(mask);

            mask.StreamObject.Stream.ToArray().Should().Equal(packed);
            image2.Dict["Mask"].Should().BeSameAs(mask.StreamObject);
        }

        [Fact]
        public void SetMaskImage_RgbMask_Throws()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1");
            using var mask = CreateRgbImage(xref, "Im2");

            AssertThrows(() => image.SetMaskImage(mask), HpdfErrorCode.InvalidColorSpace);
        }

        [Fact]
        public void SetMaskImage_ImageWithAlphaChannel_Throws()
        {
            var xref = new HpdfXref(0);
            using var image = HpdfImage.LoadPngImageFromMem(xref, "Im1", TestPngEncoder.EncodeRgba(new byte[] { 1, 2, 3, 40, 5, 6, 7, 80 }, 2, 1));
            using var mask = CreateGrayImage(xref, "Im2", new byte[] { 0, 255 }, width: 2, height: 1);

            AssertThrows(() => image.SetMaskImage(mask), HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void SetMaskImage_Self_Throws()
        {
            using var image = CreateGrayImage(new HpdfXref(0), "Im1");

            AssertThrows(() => image.SetMaskImage(image), HpdfErrorCode.InvalidParameter);
        }

        // ---------- SetSoftMask ----------

        [Fact]
        public void SetSoftMask_GrayMask_AddsSMaskAndKeepsMaskImageIntact()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1");
            byte[] maskData = { 0, 64, 128, 255 };
            using var mask = CreateGrayImage(xref, "Im2", maskData);

            image.SetSoftMask(mask);

            image.Dict["SMask"].Should().BeSameAs(mask.StreamObject);
            ColorSpaceOf(mask).Should().Be("DeviceGray");
            mask.BitsPerComponent.Should().Be(8);
            mask.IsImageMask.Should().BeFalse();
            mask.StreamObject.Stream.ToArray().Should().Equal(maskData);
        }

        [Fact]
        public void SetSoftMask_DifferentResolution_IsAllowed()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1", width: 4, height: 4);
            using var mask = CreateGrayImage(xref, "Im2", new byte[] { 0, 255 }, width: 2, height: 1);

            image.SetSoftMask(mask);

            image.Dict.ContainsKey("SMask").Should().BeTrue();
        }

        [Fact]
        public void SetSoftMask_ImageWithAlphaChannel_Throws()
        {
            var xref = new HpdfXref(0);
            using var image = HpdfImage.LoadPngImageFromMem(xref, "Im1", TestPngEncoder.EncodeRgba(new byte[] { 1, 2, 3, 40, 5, 6, 7, 80 }, 2, 1));
            using var mask = CreateGrayImage(xref, "Im2", new byte[] { 0, 255 }, width: 2, height: 1);

            AssertThrows(() => image.SetSoftMask(mask), HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void SetSoftMask_RgbMask_Throws()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1");
            using var mask = CreateRgbImage(xref, "Im2");

            AssertThrows(() => image.SetSoftMask(mask), HpdfErrorCode.InvalidColorSpace);
        }

        [Fact]
        public void SetSoftMask_StencilMask_Throws()
        {
            var xref = new HpdfXref(0);
            using var image1 = CreateRgbImage(xref, "Im1");
            using var image2 = CreateRgbImage(xref, "Im2");
            using var mask = CreateGrayImage(xref, "Im3");
            image1.SetMaskImage(mask);

            AssertThrows(() => image2.SetSoftMask(mask), HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetSoftMask_AfterColorMask_Throws()
        {
            var xref = new HpdfXref(0);
            using var image = CreateRgbImage(xref, "Im1");
            image.SetColorMask(0, 10, 0, 10, 0, 10);

            using var mask = CreateGrayImage(xref, "Im2");

            AssertThrows(() => image.SetSoftMask(mask), HpdfErrorCode.InvalidOperation);
        }

        // ---------- Helpers ----------

        private static void AssertThrows(Action action, HpdfErrorCode expected)
        {
            action.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(expected);
        }

        private static string? ColorSpaceOf(HpdfImage image) => (image.Dict["ColorSpace"] as HpdfName)?.Value;

        private static HpdfImage CreateRgbImage(HpdfXref xref, string name, int width = 2, int height = 2)
        {
            var data = Enumerable.Range(0, width * height * 3).Select(i => (byte)(i * 37)).ToArray();
            return HpdfImage.LoadRawImageFromMem(xref, name, data, (uint)width, (uint)height, HpdfColorSpace.DeviceRgb, 8);
        }

        private static HpdfImage CreateGrayImage(HpdfXref xref, string name, byte[]? data = null, int width = 2, int height = 2)
        {
            data ??= new byte[] { 0, 255, 255, 0 };
            return HpdfImage.LoadRawImageFromMem(xref, name, data, (uint)width, (uint)height, HpdfColorSpace.DeviceGray, 8);
        }

        private static HpdfImage LoadPng(byte[] rgba, int width, int height) =>
            HpdfImage.LoadPngImageFromMem(new HpdfXref(0), "Im1", TestPngEncoder.EncodeRgba(rgba, width, height));

        private static byte[] CreateRgba(int width, int height, Func<int, int, (byte R, byte G, byte B, byte A)> pixel)
        {
            var data = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    SetPixel(data, width, x, y, pixel(x, y));
            return data;
        }

        private static void SetPixel(byte[] rgba, int width, int x, int y, (byte R, byte G, byte B, byte A) color)
        {
            int i = (y * width + x) * 4;
            rgba[i] = color.R;
            rgba[i + 1] = color.G;
            rgba[i + 2] = color.B;
            rgba[i + 3] = color.A;
        }
    }
}
