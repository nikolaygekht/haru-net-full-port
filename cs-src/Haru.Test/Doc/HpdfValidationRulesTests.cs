using System;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Forms;
using Haru.Objects;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for password rules, destinations with a view, the outline root count, page label numbers,
    /// annotation argument checks, PDF/A print flags and choice field selections after flag changes.
    /// </summary>
    public class HpdfValidationRulesTests
    {
        private static readonly HpdfRect Box = new HpdfRect(100, 100, 200, 120);

        // ---------- passwords ----------

        [Theory]
        [InlineData("user", "")]
        [InlineData("user", null)]
        [InlineData("same", "same")]
        public void SetEncryption_WeakOwnerPassword_Throws(string user, string? owner)
        {
            var doc = new HpdfDocument();

            Action act = () => doc.SetEncryption(user, owner!, HpdfPermission.Print, HpdfEncryptMode.R3);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.EncryptInvalidPassword);
            doc.IsEncrypted.Should().BeFalse();
        }

        [Fact]
        public void SetEncryption_PasswordsEqualInTheirFirst32Characters_Throws()
        {
            var doc = new HpdfDocument();
            string common = new string('x', 32);

            Action act = () => doc.SetEncryption(common + "user", common + "owner", HpdfPermission.Print, HpdfEncryptMode.R3);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.EncryptInvalidPassword);
        }

        [Fact]
        public void SetEncryption_NonLatin1Password_Throws()
        {
            var doc = new HpdfDocument();

            Action act = () => doc.SetEncryption("пароль", "owner", HpdfPermission.Print, HpdfEncryptMode.R3);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.EncryptInvalidPassword);
        }

        [Fact]
        public void SetEncryption_EmptyUserPassword_IsAllowed()
        {
            var doc = new HpdfDocument();

            doc.SetEncryption("", "owner", HpdfPermission.Print, HpdfEncryptMode.R3);

            doc.IsEncrypted.Should().BeTrue();
        }

        // ---------- destinations, outlines, page labels ----------

        [Fact]
        public void OutlineSetDestination_WithoutView_Throws()
        {
            var doc = new HpdfDocument();
            var destination = doc.AddPage().CreateDestination();
            var outline = doc.CreateOutline("Chapter");

            Action act = () => outline.SetDestination(destination);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDestination);
        }

        [Fact]
        public void ClosedOutlineRoot_StillHasPositiveCount()
        {
            var doc = new HpdfDocument();
            doc.CreateOutline("A");
            doc.CreateOutline("B");
            doc.GetOutlineRoot().SetOpened(false);

            doc.SaveToMemory();

            ((HpdfNumber)doc.GetOutlineRoot().Dict["Count"]).Value.Should().Be(2);
        }

        [Fact]
        public void AddPageLabel_FirstPageBelowOne_Throws()
        {
            var doc = new HpdfDocument();

            Action act = () => doc.AddPageLabel(0, HpdfPageNumStyle.Decimal, 0);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        // ---------- annotations ----------

        [Fact]
        public void TextAnnotation_InvalidIcon_ThrowsWithoutLeavingObjects()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            int objects = doc.Xref.Entries.Count;

            Action act = () => page.CreateTextAnnotation(Box, "Note", (HpdfAnnotationIcon)99);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.AnnotInvalidIcon);
            doc.Xref.Entries.Count.Should().Be(objects);
        }

        [Fact]
        public void LinkAnnotation_EmptyUri_ThrowsWithoutLeavingObjects()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            int objects = doc.Xref.Entries.Count;

            Action act = () => page.CreateLinkAnnotation(Box, string.Empty);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            doc.Xref.Entries.Count.Should().Be(objects);
        }

        [Fact]
        public void LinkAnnotation_DestinationWithoutView_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            Action act = () => page.CreateLinkAnnotation(Box, doc.AddPage().CreateDestination());

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDestination);
        }

        [Fact]
        public void LinkAnnotation_WithDestinationObject_WritesItsArray()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var destination = doc.AddPage().CreateDestination();
            destination.SetFit();

            var link = page.CreateLinkAnnotation(Box, destination);

            link.Dict["Dest"].Should().BeSameAs(destination.DestArray);
        }

        [Fact]
        public void Annotation_ReversedRectangle_IsNormalized()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            var note = page.CreateTextAnnotation(new HpdfRect(200, 120, 100, 100), "Note");

            var rect = (HpdfArray)note.Dict["Rect"];
            ((HpdfReal)rect[0]).Value.Should().Be(100);
            ((HpdfReal)rect[2]).Value.Should().Be(200);
        }

        [Fact]
        public void SetBorderStyle_ZeroDashAndGap_Throws()
        {
            var doc = new HpdfDocument();
            var link = doc.AddPage().CreateLinkAnnotation(Box, "https://example.com");

            Action act = () => link.SetBorderStyle(HpdfBorderStyle.Dashed, 1, 0, 0);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetRgbColor_NaN_Throws()
        {
            var doc = new HpdfDocument();
            var link = doc.AddPage().CreateLinkAnnotation(Box, "https://example.com");

            Action act = () => link.SetRgbColor(float.NaN, 0, 0);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void PdfA_SetsPrintFlagOnEveryAnnotation()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var link = page.CreateLinkAnnotation(Box, "https://example.com");
            var note = page.CreateTextAnnotation(Box, "Note");
            doc.SetPdfACompliance();

            doc.SaveToMemory();

            ((HpdfNumber)link.Dict["F"]).Value.Should().Be(4);
            ((HpdfNumber)note.Dict["F"]).Value.Should().Be(4);
        }

        [Fact]
        public void WidgetSetHighlightMode_InvalidValue_Throws()
        {
            var widget = new HpdfWidgetAnnotation(new HpdfDocument().Xref, Box);

            Action act = () => widget.SetHighlightMode((HpdfHighlightMode)42);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        // ---------- images ----------

        [Fact]
        public void LoadPngImage_ForwardOnlyStream_IsBuffered()
        {
            var doc = new HpdfDocument();

            using var stream = new ForwardOnlyStream(TestHelper.GetResourceBytes("test_rgba_2x2.png"));
            using var image = doc.LoadPngImage(stream);

            image.Width.Should().Be(2);
            image.Height.Should().Be(2);
        }

        /// <summary>
        /// A stream that can only be read forwards, like a network stream.
        /// </summary>
        private sealed class ForwardOnlyStream : System.IO.MemoryStream
        {
            public ForwardOnlyStream(byte[] data) : base(data) { }
            public override bool CanSeek => false;
            public override long Seek(long offset, System.IO.SeekOrigin loc) => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
        }

        // ---------- choice fields after flag changes ----------

        [Fact]
        public void ComboBox_EditableTurnedOff_DropsTypedValue()
        {
            var combo = new HpdfChoice(new HpdfDocument().Xref, "Country");
            combo.AddOptions("Canada", "USA");
            combo.SetEditable(true);
            combo.SetValue("Mexico");

            combo.SetEditable(false);
            combo.ClearOptions();

            combo.Dict.ContainsKey("V").Should().BeFalse();
            combo.Options.Should().BeEmpty();
        }

        [Fact]
        public void ListBox_MultiSelectTurnedOff_KeepsFirstSelection()
        {
            var list = new HpdfChoice(new HpdfDocument().Xref, "Colors", isComboBox: false);
            list.SetMultiSelect(true);
            list.AddOptions("Red", "Green", "Blue");
            list.SetSelectedValues("Green", "Blue");

            list.SetMultiSelect(false);

            ((HpdfString)list.Dict["V"]).GetText().Should().Be("Green");
        }
    }
}
