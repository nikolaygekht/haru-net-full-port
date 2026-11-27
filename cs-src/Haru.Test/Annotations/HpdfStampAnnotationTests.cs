using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfStampAnnotationTests
    {
        private static readonly HpdfRect StampRect = new HpdfRect(100, 600, 300, 660);

        private static HpdfFont Bold(HpdfDocument doc) => new HpdfFont(doc.Xref, HpdfStandardFont.HelveticaBold, "F2");

        [Theory]
        [InlineData(HpdfStampName.Approved, "Approved")]
        [InlineData(HpdfStampName.NotForPublicRelease, "NotForPublicRelease")]
        [InlineData(HpdfStampName.ForPublicRelease, "ForPublicRelease")]
        public void Create_WritesNameAndContents(HpdfStampName name, string pdfName)
        {
            var doc = new HpdfDocument();
            var stamp = doc.AddPage().CreateStampAnnotation(StampRect, name, "Stamp text", Bold(doc));

            stamp.Type.Should().Be(HpdfAnnotationType.Stamp);
            stamp.Name.Should().Be(name);
            ((HpdfName)stamp.Dict["Subtype"]).Value.Should().Be("Stamp");
            ((HpdfName)stamp.Dict["Name"]).Value.Should().Be(pdfName);
            ((HpdfString)stamp.Dict["Contents"]).GetText().Should().Be("Stamp text");
            stamp.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void DefaultColor_FollowsTheMeaning()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = Bold(doc);

            Numbers(page.CreateStampAnnotation(StampRect, HpdfStampName.Approved, "", font).Dict["C"])
                .Should().Equal(0.1f, 0.5f, 0.1f);
            Numbers(page.CreateStampAnnotation(StampRect, HpdfStampName.TopSecret, "", font).Dict["C"])
                .Should().Equal(0.75f, 0.1f, 0.1f);
            Numbers(page.CreateStampAnnotation(StampRect, HpdfStampName.Draft, "", font).Dict["C"])
                .Should().Equal(0.1f, 0.2f, 0.6f);
        }

        [Fact]
        public void Create_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = Bold(doc);

            page.Invoking(p => p.CreateStampAnnotation(StampRect, (HpdfStampName)14, "", font)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateStampAnnotation(StampRect, HpdfStampName.Draft, "", null!)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateStampAnnotation(StampRect, HpdfStampName.Draft, null!, font)).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void Save_DrawsFrameAndLabel()
        {
            var doc = new HpdfDocument();
            var font = Bold(doc);
            var stamp = doc.AddPage().CreateStampAnnotation(StampRect, HpdfStampName.NotApproved, "", font);

            doc.SaveToMemory();

            var fonts = (HpdfDict)((HpdfDict)Appearance(stamp)["Resources"])["Font"];
            fonts["F2"].Should().BeSameAs(font.Dict);
            string content = AppearanceContent(stamp);
            content.Should().Contain("0.75 0.1 0.1 RG");
            content.Should().Contain("5 w");
            content.Split('\n').Count(op => op.EndsWith(" c")).Should().Be(4, "the corners are rounded");
            content.Should().Contain("(NOT APPROVED) Tj");
        }

        [Fact]
        public void SetFont_ChangesTheLabelFont()
        {
            var doc = new HpdfDocument();
            var stamp = doc.AddPage().CreateStampAnnotation(StampRect, HpdfStampName.Final, "", Bold(doc));
            var times = new HpdfFont(doc.Xref, HpdfStandardFont.TimesBold, "F3");

            stamp.SetFont(times);
            doc.SaveToMemory();

            AppearanceContent(stamp).Should().Contain("/F3 ");
        }

        [Fact]
        public void NoColor_DrawsNothing()
        {
            var doc = new HpdfDocument();
            var stamp = doc.AddPage().CreateStampAnnotation(StampRect, HpdfStampName.Draft, "", Bold(doc));

            stamp.SetNoColor();
            doc.SaveToMemory();

            AppearanceContent(stamp).Should().BeEmpty();
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateStampAnnotation(StampRect, HpdfStampName.Confidential, "", Bold(doc)).SetTransparency(0.6f);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedStamp_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateStampAnnotation(StampRect, HpdfStampName.Approved, "Read me", Bold(doc));

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Stamp);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
