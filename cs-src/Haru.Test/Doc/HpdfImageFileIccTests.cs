using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Test.Png;
using Xunit;
using static Haru.Test.Doc.HpdfIccProfileTests;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests that the ICC profile of a PNG (iCCP) or JPEG (APP2 ICC_PROFILE) file becomes the image's color space.
    /// </summary>
    public class HpdfImageFileIccTests
    {
        private static readonly byte[] ColorPixels = { 128, 200, 64, 255, 200, 80, 150, 255 };
        private static readonly byte[] GrayPixels = { 100, 100, 100, 255, 30, 30, 30, 255 };

        private static byte[] Png(byte[] pixels, byte[]? profile) =>
            TestPngEncoder.EncodeRgba(pixels, 2, 1, profile is null ? null : TestPngEncoder.CompressProfile(profile));

        private static byte[] ProPhotoJpeg => TestHelper.GetResourceBytes("Icc.prophoto.jpg");

        [Fact]
        public void Png_WithProfile_GetsItAsColorSpace()
        {
            var doc = new HpdfDocument();

            using var image = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, ProPhoto)));

            image.IccProfile.Should().NotBeNull();
            image.IccProfile!.StreamObject.Stream.ToArray().Should().Equal(ProPhoto);
            ((HpdfName)((HpdfArray)image.Dict["ColorSpace"])[0]).Value.Should().Be("ICCBased");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void Png_GrayPixelsWithRgbProfile_StayRgb()
        {
            var doc = new HpdfDocument();

            using var image = doc.LoadPngImage(new MemoryStream(Png(GrayPixels, ProPhoto)));

            image.IccProfile!.ComponentCount.Should().Be(3);
            image.StreamObject.Stream.ToArray().Should().Equal(100, 100, 100, 30, 30, 30);
        }

        [Fact]
        public void Png_ProfileThatDoesNotFit_IsIgnored()
        {
            var doc = new HpdfDocument();

            using var colorWithGrayProfile = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, Grey)));
            using var damaged = doc.LoadPngImage(new MemoryStream(
                TestPngEncoder.EncodeRgba(ColorPixels, 2, 1, new byte[] { 1, 2, 3, 4, 5 })));
            using var notAProfile = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, new byte[200])));

            colorWithGrayProfile.IccProfile.Should().BeNull();
            damaged.IccProfile.Should().BeNull();
            notAProfile.IccProfile.Should().BeNull();
            ((HpdfName)damaged.Dict["ColorSpace"]).Value.Should().Be("DeviceRGB");
            doc.IccProfiles.Should().BeEmpty();
        }

        [Fact]
        public void TwoImagesWithTheSameProfile_ShareIt()
        {
            var doc = new HpdfDocument();

            using var first = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, ProPhoto)));
            using var second = doc.LoadJpegImage(new MemoryStream(ProPhotoJpeg));

            second.IccProfile.Should().BeSameAs(first.IccProfile);
            doc.IccProfiles.Should().HaveCount(1);
        }

        [Fact]
        public void Jpeg_ProfileSplitOverSegments_IsJoined()
        {
            byte[] split = SplitIccSegment(ProPhotoJpeg, parts: 3, dropPart: 0);
            byte[] incomplete = SplitIccSegment(ProPhotoJpeg, parts: 3, dropPart: 2);
            var doc = new HpdfDocument();

            using var joined = doc.LoadJpegImage(new MemoryStream(split));
            using var missingPart = doc.LoadJpegImage(new MemoryStream(incomplete));

            joined.IccProfile!.StreamObject.Stream.ToArray().Should().Equal(ProPhoto);
            missingPart.IccProfile.Should().BeNull();
        }

        [Fact]
        public void PdfA1_Version4ProfileFromAFile_IsLeftOutOnSaving()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, DisplayP3V4)));
            page.DrawImage(image, 50, 50, 100, 50);
            doc.SetPdfACompliance("1B");

            doc.SaveToMemory();

            ((HpdfName)image.Dict["ColorSpace"]).Value.Should().Be("DeviceRGB");
            image.IccProfile.Should().NotBeNull("the image keeps its profile for documents that allow it");
        }

        [Fact]
        public void Version4ProfileFromAFile_IsKeptOutsidePdfA()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var image = doc.LoadPngImage(new MemoryStream(Png(ColorPixels, DisplayP3V4)));
            page.DrawImage(image, 50, 50, 100, 50);

            doc.SaveToMemory();

            image.Dict["ColorSpace"].Should().BeOfType<HpdfArray>();
        }

        /// <summary>
        /// Replaces the single APP2 ICC_PROFILE segment of a JPEG by <paramref name="parts"/> segments, leaving out
        /// the one numbered <paramref name="dropPart"/> (0: none).
        /// </summary>
        private static byte[] SplitIccSegment(byte[] jpeg, int parts, int dropPart)
        {
            byte[] marker = System.Text.Encoding.ASCII.GetBytes("ICC_PROFILE\0");
            int position = 2;
            while (true)
            {
                int length = (jpeg[position + 2] << 8) | jpeg[position + 3];
                if (jpeg[position + 1] == 0xE2 && jpeg.AsSpan(position + 4, marker.Length).SequenceEqual(marker))
                {
                    byte[] profile = jpeg.AsSpan(position + 4 + marker.Length + 2, length - 2 - marker.Length - 2).ToArray();
                    var output = new List<byte>(jpeg.AsSpan(0, position).ToArray());
                    int size = (profile.Length + parts - 1) / parts;
                    for (int i = 0; i < parts; i++)
                    {
                        if (i + 1 == dropPart)
                            continue;
                        byte[] piece = profile.Skip(i * size).Take(size).ToArray();
                        int segmentLength = 2 + marker.Length + 2 + piece.Length;
                        output.AddRange(new byte[] { 0xFF, 0xE2, (byte)(segmentLength >> 8), (byte)segmentLength });
                        output.AddRange(marker);
                        output.Add((byte)(i + 1));
                        output.Add((byte)parts);
                        output.AddRange(piece);
                    }
                    output.AddRange(jpeg.AsSpan(position + 2 + length).ToArray());
                    return output.ToArray();
                }
                position += 2 + length;
            }
        }
    }
}
