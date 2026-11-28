using System;
using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfIccProfileTests
    {
        internal static byte[] ProPhoto => TestHelper.GetResourceBytes("Icc.ProPhoto-v2-micro.icc");
        internal static byte[] DisplayP3V4 => TestHelper.GetResourceBytes("Icc.DisplayP3-v4.icc");
        internal static byte[] Grey => TestHelper.GetResourceBytes("Icc.sGrey-v2-micro.icc");

        /// <summary>
        /// A profile header (and an empty tag table) of a CMYK printer profile, version 2.4: enough for the header
        /// checks of the library.
        /// </summary>
        internal static byte[] CmykHeader(string colorSpace = "CMYK", string signature = "acsp", int size = 132)
        {
            var data = new byte[132];
            data[0] = (byte)(size >> 24);
            data[1] = (byte)(size >> 16);
            data[2] = (byte)(size >> 8);
            data[3] = (byte)size;
            data[8] = 2;
            data[9] = 0x40;
            System.Text.Encoding.ASCII.GetBytes("prtr").CopyTo(data, 12);
            System.Text.Encoding.ASCII.GetBytes(colorSpace).CopyTo(data, 16);
            System.Text.Encoding.ASCII.GetBytes("Lab ").CopyTo(data, 20);
            System.Text.Encoding.ASCII.GetBytes(signature).CopyTo(data, 36);
            return data;
        }

        [Fact]
        public void SRgbProfile_IsTheBuiltInV2Profile()
        {
            var doc = new HpdfDocument();

            var srgb = doc.SRgbProfile;

            srgb.ComponentCount.Should().Be(3);
            srgb.AlternateColorSpace.Should().Be(HpdfColorSpace.DeviceRgb);
            srgb.DeviceClass.Should().Be("mntr");
            srgb.ProfileVersion.Should().Be(new Version(2, 1));
            srgb.StreamObject.Stream.Size.Should().Be(456);
            ((HpdfNumber)srgb.StreamObject["N"]).Value.Should().Be(3);
            ((HpdfName)srgb.StreamObject["Alternate"]).Value.Should().Be("DeviceRGB");
            srgb.StreamObject.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
            srgb.StreamObject.IsIndirect.Should().BeTrue();
            doc.SRgbProfile.Should().BeSameAs(srgb);
            doc.IccProfiles.Should().Equal(srgb);
        }

        [Fact]
        public void LoadIccProfile_SameDataTwice_IsEmbeddedOnce()
        {
            var doc = new HpdfDocument();

            var first = doc.LoadIccProfile(ProPhoto);
            var second = doc.LoadIccProfile(ProPhoto);
            var grey = doc.LoadIccProfile(Grey);

            second.Should().BeSameAs(first);
            first.ComponentCount.Should().Be(3);
            first.LocalName.Should().Be("HaruCS1");
            grey.ComponentCount.Should().Be(1);
            grey.AlternateColorSpace.Should().Be(HpdfColorSpace.DeviceGray);
            grey.LocalName.Should().Be("HaruCS2");
            first.StreamObject.Stream.ToArray().Should().Equal(ProPhoto);
            doc.IccProfiles.Should().Equal(first, grey);
        }

        [Fact]
        public void LoadIccProfile_ReadsVersionAndClass()
        {
            var doc = new HpdfDocument();

            var p3 = doc.LoadIccProfile(DisplayP3V4);
            var cmyk = doc.LoadIccProfile(CmykHeader());

            p3.ProfileVersion.Should().Be(new Version(4, 2));
            cmyk.ComponentCount.Should().Be(4);
            cmyk.AlternateColorSpace.Should().Be(HpdfColorSpace.DeviceCmyk);
            cmyk.DeviceClass.Should().Be("prtr");
            cmyk.ProfileVersion.Should().Be(new Version(2, 4));
        }

        [Fact]
        public void LoadIccProfile_FromFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "haru-icc-" + Guid.NewGuid().ToString("N") + ".icc");
            File.WriteAllBytes(path, Grey);
            try
            {
                new HpdfDocument().LoadIccProfile(path).ComponentCount.Should().Be(1);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadIccProfile_InvalidData_ThrowsAndEmbedsNothing()
        {
            var doc = new HpdfDocument();
            int objects = doc.Xref.Entries.Count;

            doc.Invoking(d => d.LoadIccProfile(new byte[100])).Should().Throw<HpdfException>("shorter than a header");
            doc.Invoking(d => d.LoadIccProfile(CmykHeader(signature: "xxxx"))).Should().Throw<HpdfException>();
            doc.Invoking(d => d.LoadIccProfile(CmykHeader(size: 5000))).Should().Throw<HpdfException>();
            doc.Invoking(d => d.LoadIccProfile(CmykHeader(colorSpace: "Lab "))).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidIccComponentNum);
            doc.Invoking(d => d.LoadIccProfile((byte[])null!)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.LoadIccProfile("missing-" + Guid.NewGuid().ToString("N") + ".icc"))
                .Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.FileNotFound);

            doc.IccProfiles.Should().BeEmpty();
            doc.Xref.Entries.Count.Should().Be(objects);
        }

        [Fact]
        public void ProfilesOfDifferentDocuments_AreSeparate()
        {
            var first = new HpdfDocument();
            var second = new HpdfDocument();

            first.SRgbProfile.Should().NotBeSameAs(second.SRgbProfile);
            second.IccProfiles.Should().HaveCount(1);
        }
    }
}
