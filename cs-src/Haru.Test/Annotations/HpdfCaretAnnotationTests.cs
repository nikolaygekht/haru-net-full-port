using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;
using static Haru.Test.Annotations.HpdfShapeAnnotationTests;

namespace Haru.Test.Annotations
{
    public class HpdfCaretAnnotationTests
    {
        private static readonly HpdfRect CaretRect = new HpdfRect(100, 100, 120, 120);

        [Fact]
        public void CreateCaretAnnotation_WritesDictionary()
        {
            var doc = new HpdfDocument();
            var caret = doc.AddPage().CreateCaretAnnotation(CaretRect, "inserted words");

            caret.Type.Should().Be(HpdfAnnotationType.Caret);
            ((HpdfName)caret.Dict["Subtype"]).Value.Should().Be("Caret");
            ((HpdfString)caret.Dict["Contents"]).GetText().Should().Be("inserted words");
            Numbers(caret.Dict["C"]).Should().Equal(0f, 0f, 1f);
            caret.Symbol.Should().Be(HpdfCaretSymbol.None);
            caret.Dict.ContainsKey("Sy").Should().BeFalse();
            caret.Flags.Should().Be(HpdfAnnotationFlags.Print);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void CreateCaretAnnotation_InvalidArguments_Throw()
        {
            var page = new HpdfDocument().AddPage();

            page.Invoking(p => p.CreateCaretAnnotation(CaretRect, null!)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateCaretAnnotation(new HpdfRect(0, 0, float.NaN, 1), "")).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Theory]
        [InlineData(HpdfCaretSymbol.None, "None")]
        [InlineData(HpdfCaretSymbol.Paragraph, "P")]
        public void SetSymbol_WritesName(HpdfCaretSymbol symbol, string name)
        {
            var caret = new HpdfDocument().AddPage().CreateCaretAnnotation(CaretRect, "");

            caret.SetSymbol(symbol);

            ((HpdfName)caret.Dict["Sy"]).Value.Should().Be(name);
            caret.Symbol.Should().Be(symbol);
            caret.Invoking(c => c.SetSymbol((HpdfCaretSymbol)4)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void Save_DrawsFilledCaret_AndPilcrowForParagraph()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var plain = page.CreateCaretAnnotation(CaretRect, "");
            var paragraph = page.CreateCaretAnnotation(CaretRect, "");
            paragraph.SetSymbol(HpdfCaretSymbol.Paragraph);

            doc.SaveToMemory();

            string content = AppearanceContent(plain);
            content.Should().Contain("0 0 1 rg");
            content.Should().Contain("100 100 m");
            content.Split('\n').Count(op => op == "f").Should().Be(1);
            AppearanceContent(paragraph).Split('\n').Count(op => op == "f").Should().Be(2);
            AppearanceContent(paragraph).Split('\n').Count(op => op.EndsWith(" re")).Should().Be(2, "the stems of the pilcrow");
        }

        [Fact]
        public void SetRectDifference_ShrinksTheCaret()
        {
            var doc = new HpdfDocument();
            var caret = doc.AddPage().CreateCaretAnnotation(CaretRect, "");

            caret.SetRectDifference(2, 3, 4, 5);
            doc.SaveToMemory();

            Numbers(caret.Dict["RD"]).Should().Equal(2f, 3f, 4f, 5f);
            AppearanceContent(caret).Should().Contain("102 105 m");
            caret.Invoking(c => c.SetRectDifference(10, 0, 10, 0)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void PdfA_DoesNotAllowCarets()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().CreateCaretAnnotation(CaretRect, "");

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void SavedCaret_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage().CreateCaretAnnotation(CaretRect, "Read me");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotation = pdf.GetPage(1).GetAnnotations().Single();

            annotation.Type.Should().Be(UglyToad.PdfPig.Annotations.AnnotationType.Caret);
            annotation.Content.Should().Be("Read me");
            annotation.HasNormalAppearance.Should().BeTrue();
        }
    }
}
