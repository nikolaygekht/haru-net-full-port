using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for outline counts, repeatable setters, compression mode, resource names, page sizes,
    /// image loading and masks, color components and culture-independent ToString.
    /// </summary>
    public class HpdfDocumentFixesTests
    {
        // ---------- outlines ----------

        [Fact]
        public void Outline_ClosedNestedItem_HidesItsChildrenFromTheCount()
        {
            var doc = new HpdfDocument();
            var chapter = doc.CreateOutline("Chapter");
            var section = chapter.CreateChild("Section");
            section.SetOpened(false);
            section.CreateChild("Part 1");
            section.CreateChild("Part 2");

            doc.SaveToMemory();

            Count(doc.GetOutlineRoot().Dict).Should().Be(2, "Chapter and Section are visible, the parts are hidden");
            Count(chapter.Dict).Should().Be(1);
            Count(section.Dict).Should().Be(-2, "a closed item has a negative count of its hidden descendants");
        }

        [Fact]
        public void Outline_SetDestinationTwice_ReplacesDestination()
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage().CreateDestination();
            first.SetFit();
            var second = doc.AddPage().CreateDestination();
            second.SetFit();
            var outline = doc.CreateOutline("Item");
            outline.SetDestination(first);

            outline.SetDestination(second);

            outline.Dict["Dest"].Should().BeSameAs(second.DestArray);
        }

        // ---------- pages ----------

        [Fact]
        public void SetSlideShow_Twice_ReplacesTransition()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetSlideShow(HpdfTransitionStyle.Dissolve, 5, 1);

            page.SetSlideShow(HpdfTransitionStyle.BoxIn, 0, 2);

            ((HpdfName)((HpdfDict)page.Dict["Trans"])["S"]).Value.Should().Be("Box");
            page.Dict.ContainsKey("Dur").Should().BeFalse("no display time means the viewer does not advance");
        }

        [Fact]
        public void CompressionMode_SetAfterPagesWereAdded_AppliesToThem()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            doc.CompressionMode = HpdfCompressionMode.Text;

            page.Contents.Filter.Should().Be(HpdfStreamFilter.FlateDecode);
        }

        [Fact]
        public void PageResource_DifferentObjectWithSameName_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.SetExtGState(new HpdfExtGState(doc.Xref, "GS1"));

            Action act = () => page.SetExtGState(new HpdfExtGState(doc.Xref, "GS1"));

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.DuplicateRegistration);
        }

        [Fact]
        public void PageResource_SameObjectTwice_IsAllowed()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var state = new HpdfExtGState(doc.Xref, "GS1");
            page.SetExtGState(state);

            page.SetExtGState(state);

            ((HpdfDict)page.GetResources()["ExtGState"]).Count.Should().Be(1);
        }

        [Fact]
        public void GetSize_UndefinedValues_Throw()
        {
            Action badSize = () => HpdfPageSizes.GetSize((HpdfPageSize)99, HpdfPageDirection.Portrait, out _, out _);
            Action badDirection = () => HpdfPageSizes.GetSize(HpdfPageSize.A4, (HpdfPageDirection)7, out _, out _);

            badSize.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageInvalidSize);
            badDirection.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageInvalidDirection);
        }

        [Fact]
        public void SetEncryption_UnsupportedMode_Throws()
        {
            var doc = new HpdfDocument();

            Action act = () => doc.SetEncryption("user", "owner", HpdfPermission.Print, (HpdfEncryptMode)7);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        // ---------- images ----------

        [Fact]
        public void LoadJpegImage_ReadsFromCurrentStreamPosition()
        {
            var doc = new HpdfDocument();
            byte[] jpeg = MinimalJpeg(width: 3, height: 2);
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }.Concat(jpeg).ToArray()) { Position = 5 };

            using var image = doc.LoadJpegImage(stream);

            image.Width.Should().Be(3);
            image.Height.Should().Be(2);
            image.StreamObject.Stream.ToArray().Should().Equal(jpeg);
        }

        [Fact]
        public void LoadJpegImage_TruncatedData_ThrowsHpdfExceptionWithoutAddingObjects()
        {
            var doc = new HpdfDocument();
            int objects = doc.Xref.Entries.Count;

            Action act = () => doc.LoadJpegImage(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidJpegData);
            doc.Xref.Entries.Count.Should().Be(objects);
        }

        [Fact]
        public void LoadRawImage_UnsupportedBitDepth_Throws()
        {
            var doc = new HpdfDocument();

            Action act = () => HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[8], 2, 2, HpdfColorSpace.DeviceGray, 3);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidBitPerComponent);
        }

        [Fact]
        public void LoadRawImage_TwoBits_RowsArePaddedToBytes()
        {
            var doc = new HpdfDocument();

            // 10 pixels per row at 2 bits = 3 bytes per row; 2 rows = 6 bytes, extra bytes are not part of the image
            // (1-bit gray images are coded with CCITT Group 4: HpdfImage1BitTests)
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 10, 2, HpdfColorSpace.DeviceGray, 2);
            Action tooShort = () => HpdfImage.LoadRawImageFromMem(doc.Xref, "Im2", new byte[5], 10, 2, HpdfColorSpace.DeviceGray, 2);

            image.StreamObject.Stream.ToArray().Should().Equal(1, 2, 3, 4, 5, 6);
            tooShort.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void ImageUsedAsSoftMask_CannotGetAMaskOfItsOwn()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 1, 2, 3 }, 1, 1, HpdfColorSpace.DeviceRgb, 8);
            using var softMask = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im2", new byte[] { 128 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            using var stencil = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im3", new byte[] { 255 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            image.SetSoftMask(softMask);

            Action colorMask = () => softMask.SetColorMask(0, 10, 0, 10, 0, 10);
            Action maskImage = () => softMask.SetMaskImage(stencil);

            colorMask.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
            maskImage.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        // ---------- value types ----------

        [Fact]
        public void ColorComponents_AreClampedBySettersLikeByConstructors()
        {
            var rgb = new HpdfRgbColor(0.5f, 0.5f, 0.5f) { R = 2, G = -1 };
            var cmyk = new HpdfCmykColor(0, 0, 0, 0) { K = 1.5f };

            rgb.R.Should().Be(1);
            rgb.G.Should().Be(0);
            cmyk.K.Should().Be(1);
        }

        [Fact]
        public void ToString_DoesNotDependOnCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                new HpdfRect(1.5f, 2, 3, 4).ToString().Should().Be("[L:1.5, B:2, R:3, T:4]");
                new HpdfPoint(0.25f, 1).ToString().Should().Be("(0.25, 1)");
                new HpdfRgbColor(0.5f, 0, 1).ToString().Should().Be("RGB(0.500, 0.000, 1.000)");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        // ---------- helpers ----------

        private static int Count(HpdfDict dict) => ((HpdfNumber)dict["Count"]).Value;

        /// <summary>
        /// A JPEG start-of-image marker followed by a baseline frame header (SOF0) and the end-of-image marker.
        /// </summary>
        private static byte[] MinimalJpeg(ushort width, ushort height) => new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xC0, 0x00, 0x11, 0x08,
            (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width,
            0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00,
            0xFF, 0xD9
        };
    }
}
