using System;
using System.Linq;
using AwesomeAssertions;
using Haru.Annotations;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Annotations
{
    public class HpdfMarkupAnnotationTests
    {
        private static readonly HpdfRect NoteRect = new HpdfRect { Left = 100, Bottom = 100, Right = 120, Top = 120 };
        private static readonly HpdfRect PopupRect = new HpdfRect { Left = 130, Bottom = 100, Right = 330, Top = 200 };

        private static float[] Components(HpdfDict dict, string key) =>
            ((HpdfArray)dict[key]).Select(item => ((HpdfReal)item).Value).ToArray();

        [Fact]
        public void SetGrayColor_WritesOneComponent()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            annot.SetGrayColor(0.25f);

            Components(annot.Dict, "C").Should().Equal(0.25f);
        }

        [Fact]
        public void SetNoColor_WritesEmptyArray()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");
            annot.SetRgbColor(1, 0, 0);

            annot.SetNoColor();

            ((HpdfArray)annot.Dict["C"]).Count.Should().Be(0);
        }

        [Fact]
        public void SetGrayColor_OutOfRange_Throws()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            var act = () => annot.SetGrayColor(1.5f);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void Flags_ByDefaultNoneForTextAnnotation()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            annot.Flags.Should().Be(HpdfAnnotationFlags.None);
            annot.Dict.ContainsKey("F").Should().BeFalse();
        }

        [Fact]
        public void Flags_WritesAndRemovesEntry()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            annot.Flags = HpdfAnnotationFlags.Print | HpdfAnnotationFlags.Locked;
            ((HpdfNumber)annot.Dict["F"]).Value.Should().Be(132);
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print | HpdfAnnotationFlags.Locked);

            annot.Flags = HpdfAnnotationFlags.None;
            annot.Dict.ContainsKey("F").Should().BeFalse();
        }

        [Fact]
        public void Flags_UnknownBits_Throw()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            var act = () => annot.Flags = (HpdfAnnotationFlags)1024;

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetTitle_WritesTextString()
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            annot.SetTitle("Иван");

            ((HpdfString)annot.Dict["T"]).GetText().Should().Be("Иван");
        }

        [Fact]
        public void SetSubject_RequiresPdf15()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateTextAnnotation(NoteRect, "Note");

            annot.SetSubject("Review");

            ((HpdfString)annot.Dict["Subj"]).GetText().Should().Be("Review");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void SetCreationDate_WritesPdfDateAndRequiresPdf15()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateTextAnnotation(NoteRect, "Note");

            annot.SetCreationDate(new DateTimeOffset(2026, 11, 27, 10, 30, 0, TimeSpan.FromHours(3)));

            ((HpdfString)annot.Dict["CreationDate"]).GetText().Should().Be("D:20261127103000+03'00'");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void SetTransparency_WritesCaAndRequiresPdf14()
        {
            var doc = new HpdfDocument();
            var annot = doc.AddPage().CreateTextAnnotation(NoteRect, "Note");

            annot.SetTransparency(0.5f);

            ((HpdfReal)annot.Dict["CA"]).Value.Should().Be(0.5f);
            annot.Opacity.Should().Be(0.5f);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version14);
        }

        [Theory]
        [InlineData(-0.1f)]
        [InlineData(1.1f)]
        [InlineData(float.NaN)]
        public void SetTransparency_OutOfRange_Throws(float opacity)
        {
            var page = new HpdfDocument().AddPage();
            var annot = page.CreateTextAnnotation(NoteRect, "Note");

            var act = () => annot.SetTransparency(opacity);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void CreatePopupAnnotation_LinksParentAndPopup()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var note = page.CreateTextAnnotation(NoteRect, "Note");

            var popup = page.CreatePopupAnnotation(PopupRect, note);
            popup.SetOpened(true);

            popup.Type.Should().Be(HpdfAnnotationType.Popup);
            popup.Parent.Should().BeSameAs(note);
            note.Popup.Should().BeSameAs(popup);
            popup.Dict["Parent"].Should().BeSameAs(note.Dict);
            note.Dict["Popup"].Should().BeSameAs(popup.Dict);
            ((HpdfBoolean)popup.Dict["Open"]).Value.Should().BeTrue();
            page.Annotations.Should().Equal(note, popup);
            ((HpdfArray)page.Dict["Annots"]).Count.Should().Be(2);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version13);
        }

        [Fact]
        public void CreatePopupAnnotation_SecondPopup_Throws()
        {
            var page = new HpdfDocument().AddPage();
            var note = page.CreateTextAnnotation(NoteRect, "Note");
            page.CreatePopupAnnotation(PopupRect, note);

            var act = () => page.CreatePopupAnnotation(PopupRect, note);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void CreatePopupAnnotation_ParentOnOtherPage_Throws()
        {
            var doc = new HpdfDocument();
            var note = doc.AddPage().CreateTextAnnotation(NoteRect, "Note");
            var otherPage = doc.AddPage();

            var act = () => otherPage.CreatePopupAnnotation(PopupRect, note);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            otherPage.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void SetInReplyTo_WritesIrtAndRequiresPdf15()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var note = page.CreateTextAnnotation(NoteRect, "Question");
            var reply = page.CreateTextAnnotation(NoteRect, "Answer");

            reply.SetInReplyTo(note);
            doc.SaveToMemory();

            reply.InReplyTo.Should().BeSameAs(note);
            reply.ReplyType.Should().Be(HpdfReplyType.Reply);
            reply.Dict["IRT"].Should().BeSameAs(note.Dict);
            reply.Dict.ContainsKey("RT").Should().BeFalse();
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
        }

        [Fact]
        public void SetInReplyTo_Group_WritesRtAndRequiresPdf16()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var square = page.CreateSquareAnnotation(PopupRect, "");
            var note = page.CreateTextAnnotation(NoteRect, "Grouped");

            note.SetInReplyTo(square, HpdfReplyType.Group);

            ((HpdfName)note.Dict["RT"]).Value.Should().Be("Group");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version16);
        }

        [Fact]
        public void SetInReplyTo_Invalid_Throws()
        {
            var page = new HpdfDocument().AddPage();
            var note = page.CreateTextAnnotation(NoteRect, "Note");

            note.Invoking(n => n.SetInReplyTo(null!)).Should().Throw<HpdfException>();
            note.Invoking(n => n.SetInReplyTo(note)).Should().Throw<HpdfException>();
            note.Invoking(n => n.SetInReplyTo(page.CreateTextAnnotation(NoteRect, "x"), (HpdfReplyType)5))
                .Should().Throw<HpdfException>();
            note.Dict.ContainsKey("IRT").Should().BeFalse();
        }

        [Fact]
        public void ReplyToAnAnnotationOfAnotherPage_FailsToSave()
        {
            var doc = new HpdfDocument();
            var note = doc.AddPage().CreateTextAnnotation(NoteRect, "Question");
            var reply = doc.AddPage().CreateTextAnnotation(NoteRect, "Answer");
            reply.SetInReplyTo(note);

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void SetRichText_WritesRcAndRequiresPdf15()
        {
            var doc = new HpdfDocument();
            var note = doc.AddPage().CreateTextAnnotation(NoteRect, "Bold text");
            const string xhtml = "<body xmlns=\"http://www.w3.org/1999/xhtml\"><p><b>Bold</b> text</p></body>";

            note.SetRichText(xhtml);

            ((HpdfString)note.Dict["RC"]).GetText().Should().Be(xhtml);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version15);
            note.Invoking(n => n.SetRichText(null!)).Should().Throw<HpdfException>();
        }

        [Fact]
        public void SavedPopup_IsReadBackWithParent()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var note = page.CreateTextAnnotation(NoteRect, "Comment text");
            note.SetTitle("Reviewer");
            page.CreatePopupAnnotation(PopupRect, note);

            using var pdf = PdfDocument.Open(doc.SaveToMemory());
            var annotations = pdf.GetPage(1).GetAnnotations().ToList();

            annotations.Select(a => a.Type).Should().Equal(
                UglyToad.PdfPig.Annotations.AnnotationType.Text, UglyToad.PdfPig.Annotations.AnnotationType.Popup);
            annotations[0].Content.Should().Be("Comment text");
            annotations[0].AnnotationDictionary.ContainsKey(UglyToad.PdfPig.Tokens.NameToken.Create("Popup")).Should().BeTrue();
            annotations[1].AnnotationDictionary.ContainsKey(UglyToad.PdfPig.Tokens.NameToken.Create("Parent")).Should().BeTrue();
        }
    }
}
