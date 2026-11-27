using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Haru.Test.Font;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfFreeTextAnnotationTests
    {
        private static readonly HpdfRect TextBox = new HpdfRect(100, 600, 300, 660);

        private static HpdfFont Helvetica(HpdfDocument doc) => new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1");

        private static HpdfFreeTextAnnotation Create(HpdfDocument doc, string text = "Hello") =>
            doc.AddPage().CreateFreeTextAnnotation(TextBox, text, Helvetica(doc), 12);

        [Fact]
        public void Create_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc, "Free text");

            annot.Type.Should().Be(HpdfAnnotationType.FreeText);
            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be("FreeText");
            ((HpdfString)annot.Dict["Contents"]).GetText().Should().Be("Free text");
            ((HpdfString)annot.Dict["DA"]).GetText().Should().Be("/F1 12 Tf 0 g");
            annot.Dict.ContainsKey("C").Should().BeFalse("no background until a color is set");
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void Create_InvalidFont_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var font = Helvetica(doc);
            using var vertical = HpdfCIDFont.LoadFromTrueTypeFile(doc, "V", TestFonts.DemoResource(TestFonts.Japanese), 65001,
                HpdfWritingMode.Vertical);

            page.Invoking(p => p.CreateFreeTextAnnotation(TextBox, "x", null!, 12)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFreeTextAnnotation(TextBox, "x", font, 0)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFreeTextAnnotation(TextBox, "x", vertical.AsFont(), 12)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFreeTextAnnotation(TextBox, null!, font, 12)).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void SetFontAndTextColor_UpdateDefaultAppearance()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);
            var bold = new HpdfFont(doc.Xref, HpdfStandardFont.HelveticaBold, "F2");

            annot.SetFont(bold, 9.5f);
            annot.SetTextRgbColor(1, 0, 0.5f);

            ((HpdfString)annot.Dict["DA"]).GetText().Should().Be("/F2 9.5 Tf 1 0 0.5 rg");
        }

        [Fact]
        public void Save_DrawsBorderAndText()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc, "Hello");

            doc.SaveToMemory();

            Numbers(annot.Dict["Rect"]).Should().Equal(100f, 600f, 300f, 660f);
            var fonts = (HpdfDict)((HpdfDict)Appearance(annot)["Resources"])["Font"];
            fonts.ContainsKey("F1").Should().BeTrue();
            string content = AppearanceContent(annot);
            content.Should().Contain("0 G\n1 w\n100.5 600.5 199 59 re\nS");
            content.Should().Contain("/F1 12 Tf");
            // Inside the border (1) and the padding (2), below the ascent of Helvetica (718)
            content.Should().Contain("1 0 0 1 103 648.384 Tm\n(Hello) Tj");
        }

        [Fact]
        public void Background_IsTheAnnotationColor()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);

            annot.SetRgbColor(1, 1, 0);
            doc.SaveToMemory();

            AppearanceContent(annot).Should().Contain("1 1 0 rg\n100 600 200 60 re\nf");
        }

        [Fact]
        public void TypeWriter_HasNoBorder()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc, "Typed");

            annot.SetIntent(HpdfFreeTextIntent.TypeWriter);
            doc.SaveToMemory();

            ((HpdfName)annot.Dict["IT"]).Value.Should().Be("FreeTextTypeWriter");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
            string content = AppearanceContent(annot);
            content.Should().NotContain(" re\nS");
            content.Should().Contain("1 0 0 1 102 ");
        }

        [Theory]
        [InlineData(HpdfTextAlignment.Left, 0)]
        [InlineData(HpdfTextAlignment.Center, 1)]
        [InlineData(HpdfTextAlignment.Right, 2)]
        public void SetAlignment_WritesQuadding(HpdfTextAlignment alignment, int quadding)
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);

            annot.SetAlignment(alignment);

            ((HpdfNumber)annot.Dict["Q"]).Value.Should().Be(quadding);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void SetAlignment_Justify_Throws()
        {
            var annot = Create(new HpdfDocument());

            var act = () => annot.SetAlignment(HpdfTextAlignment.Justify);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void RightAlignment_PlacesLineAtTheRight()
        {
            var doc = new HpdfDocument();
            var font = Helvetica(doc);
            var annot = doc.AddPage().CreateFreeTextAnnotation(TextBox, "Hi", font, 10);

            annot.SetAlignment(HpdfTextAlignment.Right);
            doc.SaveToMemory();

            float x = 300 - 3 - font.MeasureText("Hi", 10);
            AppearanceContent(annot).Should().Contain($"1 0 0 1 {x.ToString(System.Globalization.CultureInfo.InvariantCulture)} ");
        }

        [Fact]
        public void LongText_IsWrappedToTheBox()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc, "one two three four five six seven eight nine ten eleven twelve thirteen");

            doc.SaveToMemory();

            AppearanceContent(annot).Split('\n').Count(op => op.EndsWith(" Tj")).Should().BeGreaterThan(1);
        }

        [Fact]
        public void SetDefaultStyle_WritesStringAndRequiresPdf15()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);

            annot.SetDefaultStyle("font: 12pt Helvetica; color: #FF0000");

            ((HpdfString)annot.Dict["DS"]).GetText().Should().Be("font: 12pt Helvetica; color: #FF0000");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void SetCallout_WritesLineIntentAndRectDifference()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);

            annot.SetCallout(new HpdfPoint(50, 500), new HpdfPoint(80, 630), new HpdfPoint(100, 630));
            annot.SetLineEndingStyle(HpdfLineEndingStyle.OpenArrow);
            doc.SaveToMemory();

            Numbers(annot.Dict["CL"]).Should().Equal(50f, 500f, 80f, 630f, 100f, 630f);
            ((HpdfName)annot.Dict["IT"]).Value.Should().Be("FreeTextCallout");
            ((HpdfName)annot.Dict["LE"]).Value.Should().Be("OpenArrow");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
            // The start point less the ending size (9) and the padding (2)
            Numbers(annot.Dict["Rect"]).Should().Equal(39f, 489f, 300f, 660f);
            Numbers(annot.Dict["RD"]).Should().Equal(61f, 0f, 0f, 111f);
            AppearanceContent(annot).Should().Contain("50 500 m\n80 630 l\n100 630 l\nS");
        }

        [Fact]
        public void ClosedCalloutEnding_IsFilledWithTextColor()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);
            annot.SetTextRgbColor(0, 0, 1);

            annot.SetCallout(new HpdfPoint(50, 500), new HpdfPoint(100, 630));
            annot.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow);
            doc.SaveToMemory();

            string content = AppearanceContent(annot);
            content.Should().Contain("0 0 1 rg");
            content.Split('\n').Should().Contain("B");
        }

        [Fact]
        public void RemoveCallout_RestoresTheTextBox()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);
            annot.SetCallout(new HpdfPoint(50, 500), new HpdfPoint(100, 630));
            annot.SetLineEndingStyle(HpdfLineEndingStyle.Circle);
            doc.SaveToMemory();

            annot.RemoveCallout();
            doc.SaveToMemory();

            annot.Dict.ContainsKey("CL").Should().BeFalse();
            annot.Dict.ContainsKey("LE").Should().BeFalse();
            annot.Dict.ContainsKey("RD").Should().BeFalse();
            ((HpdfName)annot.Dict["IT"]).Value.Should().Be("FreeText");
            Numbers(annot.Dict["Rect"]).Should().Equal(100f, 600f, 300f, 660f);
        }

        [Fact]
        public void SetCallout_NotFinite_Throws()
        {
            var annot = Create(new HpdfDocument());

            var act = () => annot.SetCallout(new HpdfPoint(float.NaN, 0), new HpdfPoint(1, 1));

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            annot.Dict.ContainsKey("CL").Should().BeFalse();
        }

        [Fact]
        public void CidFont_TextIsInTheFont()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "J", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            var annot = doc.AddPage().CreateFreeTextAnnotation(TextBox, "日本語のテキスト", cidFont.AsFont(), 12);

            doc.SaveToMemory();

            ((HpdfString)annot.Dict["DA"]).GetText().Should().Be("/J 12 Tf 0 g");
            AppearanceContent(annot).Should().MatchRegex("<[0-9A-F]+> Tj");
            var descendant = (HpdfDict)((HpdfArray)cidFont.Dict["DescendantFonts"])[0];
            ((HpdfArray)descendant["W"]).Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc, "Some text that is long enough to wrap into several lines of the box");
            annot.SetCallout(new HpdfPoint(50, 500), new HpdfPoint(80, 630), new HpdfPoint(100, 630));
            annot.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow);
            annot.SetRgbColor(0.9f, 0.9f, 1);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void SavedFreeText_IsReadBack()
        {
            var doc = new HpdfDocument();
            Create(doc, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.FreeText);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }

        [Fact]
        public void SetIntentOtherThanCallout_RemovesTheCallout()
        {
            var doc = new HpdfDocument();
            var annot = Create(doc);
            annot.SetCallout(new HpdfPoint(50, 500), new HpdfPoint(100, 630));
            annot.SetLineEndingStyle(HpdfLineEndingStyle.OpenArrow);

            annot.SetIntent(HpdfFreeTextIntent.TypeWriter);
            doc.SaveToMemory();

            annot.Dict.ContainsKey("CL").Should().BeFalse();
            annot.Dict.ContainsKey("LE").Should().BeFalse();
            annot.Dict.ContainsKey("RD").Should().BeFalse();
            Numbers(annot.Dict["Rect"]).Should().Equal(100f, 600f, 300f, 660f);
            AppearanceContent(annot).Should().NotContain("50 500 m");
        }
    }
}
