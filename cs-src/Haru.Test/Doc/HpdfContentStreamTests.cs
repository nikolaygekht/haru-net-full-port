using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfContentStreamTests
    {
        private static string Text(HpdfContentStream stream) =>
            System.Text.Encoding.ASCII.GetString(stream.StreamObject.Stream.ToArray());

        /// <summary>
        /// Draws a header into a stream of its own on the first page: font set inside, text, then the page goes on.
        /// </summary>
        private static (HpdfDocument Doc, HpdfPage First, HpdfContentStream Header) DocumentWithHeader()
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage();
            var header = first.NewContentStream();
            first.SetFontAndSize(new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1"), 14);
            first.TextOut(50, 800, "Shared header");
            first.NewContentStream();
            return (doc, first, header);
        }

        [Fact]
        public void NewContentStream_MakesContentsAnArray()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var initial = page.CurrentContentStream;
            page.SetRgbFill(1, 0, 0);

            var second = page.NewContentStream();
            page.SetRgbFill(0, 0, 1);

            page.ContentStreams.Should().Equal(initial, second);
            page.CurrentContentStream.Should().BeSameAs(second);
            page.Contents.Should().BeSameAs(second.StreamObject);
            initial.IsFinished.Should().BeTrue();
            second.IsFinished.Should().BeFalse();
            var contents = (HpdfArray)page.Dict["Contents"];
            contents.Should().Equal(initial.StreamObject, second.StreamObject);
            Text(initial).Should().Be("1 0 0 rg\n");
            Text(second).Should().Be("0 0 1 rg\n");
        }

        [Fact]
        public void SinglePage_KeepsContentsOneStream()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            page.Dict["Contents"].Should().BeSameAs(page.Contents);
        }

        [Fact]
        public void InsertSharedContentStream_AddsStreamAndResources()
        {
            var (doc, _, header) = DocumentWithHeader();
            var second = doc.AddPage();
            second.SetRgbFill(0, 0, 1);

            second.InsertSharedContentStream(header);
            second.Rectangle(50, 50, 10, 10);
            second.Fill();
            byte[] bytes = doc.SaveToMemory();

            header.IsShareable.Should().BeTrue();
            second.ContentStreams.Count.Should().Be(3);
            second.ContentStreams[1].Should().BeSameAs(header);
            Text(second.ContentStreams[0]).Should().EndWith("q\n");
            Text(second.ContentStreams[2]).Should().StartWith("Q\n");
            ((HpdfDict)second.GetResources()["Font"]).ContainsKey("F1").Should().BeTrue();
            System.Text.Encoding.ASCII.GetString(bytes).Split("(Shared header)").Length.Should().Be(2, "the header is written once");

            using var pdf = PdfDocument.Open(new MemoryStream(bytes));
            pdf.GetPage(1).Text.Should().Be("Shared header");
            pdf.GetPage(2).Text.Should().Be("Shared header");
        }

        [Fact]
        public void InsertSharedContentStream_NotFinished_Throws()
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage();
            var current = first.NewContentStream();
            var second = doc.AddPage();

            second.Invoking(p => p.InsertSharedContentStream(current)).Should().Throw<HpdfException>()
                .Which.Message.Should().Contain("not ended");
        }

        [Fact]
        public void StreamShowingTextWithAFontSetBefore_IsNotShareable()
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage();
            first.SetFontAndSize(new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1"), 14);
            var stream = first.NewContentStream();
            first.TextOut(50, 800, "Font from before");
            first.NewContentStream();

            stream.IsShareable.Should().BeFalse();
            doc.AddPage().Invoking(p => p.InsertSharedContentStream(stream)).Should().Throw<HpdfException>()
                .Which.Message.Should().Contain("font");
        }

        [Fact]
        public void StreamRestoringAStateSavedBeforeIt_IsNotShareable()
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage();
            first.GSave();
            var stream = first.NewContentStream();
            first.GRestore();
            first.GSave();
            first.NewContentStream();
            first.GRestore();

            stream.IsShareable.Should().BeFalse("its depth is balanced, but it pops a state pushed before it");
        }

        [Fact]
        public void UnbalancedOrOpenStreams_AreNotShareable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var unbalanced = page.NewContentStream();
            page.GSave();
            var openText = page.NewContentStream();
            page.GRestore();
            page.SetFontAndSize(doc.GetFont("Helvetica", null), 10);
            page.BeginText();
            page.NewContentStream();
            page.EndText();

            unbalanced.IsShareable.Should().BeFalse();
            openText.IsShareable.Should().BeFalse();
        }

        [Fact]
        public void InsertSharedContentStream_InvalidUse_Throws()
        {
            var (doc, _, header) = DocumentWithHeader();
            var second = doc.AddPage();
            var other = new HpdfDocument().AddPage();
            second.SetFontAndSize(doc.GetFont("Helvetica", null), 10);

            second.BeginText();
            second.Invoking(p => p.InsertSharedContentStream(header)).Should().Throw<HpdfException>("inside a text object");
            second.EndText();
            other.Invoking(p => p.InsertSharedContentStream(header)).Should().Throw<HpdfException>("another document");
            second.Invoking(p => p.InsertSharedContentStream(null!)).Should().Throw<HpdfException>();
            second.ContentStreams.Should().HaveCount(1);
        }

        [Fact]
        public void InsertSharedContentStream_ResourceNameTaken_ThrowsAndChangesNothing()
        {
            var (doc, _, header) = DocumentWithHeader();
            var second = doc.AddPage();
            // A different font object under the header's font name F1
            second.SetFontAndSize(new HpdfFont(doc.Xref, HpdfStandardFont.TimesRoman, "F1"), 12);
            string before = Text(second.CurrentContentStream);

            second.Invoking(p => p.InsertSharedContentStream(header)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.DuplicateRegistration);

            second.ContentStreams.Should().HaveCount(1);
            Text(second.CurrentContentStream).Should().Be(before);
            second.Dict["Contents"].Should().BeSameAs(second.Contents);
            doc.Invoking(d => d.SaveToMemory()).Should().NotThrow();
        }

        [Fact]
        public void Compression_AppliesToEveryStreamOfThePage_AndSavingIsRepeatable()
        {
            var (doc, first, header) = DocumentWithHeader();
            doc.SetCompressionMode(HpdfCompressionMode.All);
            doc.AddPage().InsertSharedContentStream(header);

            var once = doc.SaveToMemory();
            var twice = doc.SaveToMemory();

            twice.Should().Equal(once);
            first.ContentStreams.Should().OnlyContain(s => s.StreamObject.Filter == Haru.Streams.HpdfStreamFilter.FlateDecode);
        }
    }
}
