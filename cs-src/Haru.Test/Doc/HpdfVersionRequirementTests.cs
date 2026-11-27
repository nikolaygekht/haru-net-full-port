using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Test.Png;
using Haru.Types;
using Haru.Xref;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests that the document's PDF version is raised to what its content requires
    /// and never lowered below that.
    /// </summary>
    public class HpdfVersionRequirementTests
    {
        [Fact]
        public void RequireVersion_NeverLowersRequiredVersion()
        {
            var xref = new HpdfXref(0);

            xref.RequireVersion(HpdfVersion.Version16);
            xref.RequireVersion(HpdfVersion.Version14);

            xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
        }

        [Fact]
        public void NewDocument_IsVersion12()
        {
            var doc = new HpdfDocument();

            doc.Version.Should().Be(HpdfVersion.Version12);
            Header(doc).Should().Be("%PDF-1.2");
        }

        [Fact]
        public void PngWithAlpha_RaisesVersionTo14()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            using var image = doc.LoadPngImage(new MemoryStream(TestPng.Rgba2x1));

            doc.Version.Should().Be(HpdfVersion.Version14);
            Header(doc).Should().Be("%PDF-1.4");
        }

        [Fact]
        public void DocumentJavaScript_RaisesVersionTo13()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            doc.AddJavaScript("init", "var x;");

            Header(doc).Should().Be("%PDF-1.3");
        }

        [Fact]
        public void NamedDestination_KeepsVersion12()
        {
            var doc = new HpdfDocument();
            var destination = doc.AddPage().CreateDestination();
            destination.SetFit();

            doc.CreateNamedDestination("intro", destination);

            Header(doc).Should().Be("%PDF-1.2");
        }

        [Fact]
        public void PngWithoutAlpha_KeepsVersion12()
        {
            var doc = new HpdfDocument();

            using var image = doc.LoadPngImage(new MemoryStream(TestPng.Opaque2x1));

            doc.Version.Should().Be(HpdfVersion.Version12);
        }

        [Fact]
        public void SetSoftMask_RaisesVersionTo14()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 1, 2, 3 }, 1, 1, HpdfColorSpace.DeviceRgb, 8);
            using var mask = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im2", new byte[] { 128 }, 1, 1, HpdfColorSpace.DeviceGray, 8);
            doc.Version.Should().Be(HpdfVersion.Version12);

            image.SetSoftMask(mask);

            doc.Version.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void SetMaskImage_RaisesVersionTo13()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 1, 2, 3 }, 1, 1, HpdfColorSpace.DeviceRgb, 8);
            using var mask = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im2", new byte[] { 255 }, 1, 1, HpdfColorSpace.DeviceGray, 8);

            image.SetMaskImage(mask);

            // Explicit (stencil) masking was introduced in PDF 1.3
            doc.Version.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void SetColorMask_RaisesVersionTo13()
        {
            var doc = new HpdfDocument();
            using var image = HpdfImage.LoadRawImageFromMem(doc.Xref, "Im1", new byte[] { 1, 2, 3 }, 1, 1, HpdfColorSpace.DeviceRgb, 8);

            image.SetColorMask(0, 10, 0, 10, 0, 10);

            doc.Version.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void PageLabel_RaisesVersionTo13()
        {
            var doc = new HpdfDocument();
            doc.AddPage();

            doc.AddPageLabel(0, HpdfPageNumStyle.LowerRoman);

            doc.Version.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void EncryptionR2_KeepsVersion12()
        {
            var doc = new HpdfDocument();

            doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R2);

            doc.Version.Should().Be(HpdfVersion.Version12);
        }

        [Fact]
        public void ExtGStateWithoutTransparency_KeepsVersion12()
        {
            var doc = new HpdfDocument();

            _ = new HpdfExtGState(doc.Xref, "GS1");

            doc.Version.Should().Be(HpdfVersion.Version12);
        }

        [Fact]
        public void ExtGStateAlphaFill_RaisesVersionTo14()
        {
            var doc = new HpdfDocument();

            new HpdfExtGState(doc.Xref, "GS1").SetAlphaFill(0.5f);

            doc.Version.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void ExtGStateAlphaStroke_RaisesVersionTo14()
        {
            var doc = new HpdfDocument();

            new HpdfExtGState(doc.Xref, "GS1").SetAlphaStroke(0.5f);

            doc.Version.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void ExtGStateBlendMode_RaisesVersionTo14()
        {
            var doc = new HpdfDocument();

            new HpdfExtGState(doc.Xref, "GS1").SetBlendMode(HpdfBlendMode.Multiply);

            doc.Version.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void ExplicitHigherVersion_IsKept()
        {
            var doc = new HpdfDocument { Version = HpdfVersion.Version17 };

            new HpdfExtGState(doc.Xref, "GS1").SetAlphaFill(0.5f);

            doc.Version.Should().Be(HpdfVersion.Version17);
            Header(doc).Should().Be("%PDF-1.7");
        }

        [Fact]
        public void ExplicitLowerVersion_CannotGoBelowContentRequirement()
        {
            var doc = new HpdfDocument();
            new HpdfExtGState(doc.Xref, "GS1").SetAlphaFill(0.5f);

            doc.Version = HpdfVersion.Version13;

            doc.Version.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void PdfACompliance_DoesNotLowerVersionRequiredByAesEncryption()
        {
            var doc = new HpdfDocument();
            doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R4);

            doc.SetPdfACompliance("1B");

            doc.Version.Should().Be(HpdfVersion.Version16);
        }

        private static string Header(HpdfDocument doc)
        {
            var bytes = doc.SaveToMemory();
            return System.Text.Encoding.ASCII.GetString(bytes, 0, 8);
        }

        /// <summary>
        /// 2x1 RGBA PNG files: one with a half-transparent pixel, one fully opaque.
        /// </summary>
        private static class TestPng
        {
            public static readonly byte[] Rgba2x1 = TestPngEncoder.EncodeRgba(new byte[] { 255, 0, 0, 128, 0, 0, 255, 255 }, 2, 1);

            public static readonly byte[] Opaque2x1 = TestPngEncoder.EncodeRgba(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 }, 2, 1);
        }
    }
}
