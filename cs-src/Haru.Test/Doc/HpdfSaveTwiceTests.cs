using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Forms;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests that a document can be saved more than once: each save gives the same, valid file
    /// unless the document was changed in between.
    /// </summary>
    public class HpdfSaveTwiceTests
    {
        [Theory]
        [InlineData("plain")]
        [InlineData("outlines")]
        [InlineData("pageLabels")]
        [InlineData("encryptionRc4")]
        [InlineData("pdfA")]
        [InlineData("pdfAEncrypted")]
        [InlineData("form")]
        public void Save_Twice_WritesIdenticalFiles(string feature)
        {
            var doc = CreateDocument(feature);

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void Save_TwiceWithAesEncryption_SecondFileIsReadable()
        {
            var doc = CreateDocument("plain");
            doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R4);

            doc.SaveToMemory();
            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()), new ParsingOptions { Password = "user" });

            pdf.GetPage(1).Text.Should().Contain("Hello");
        }

        [Fact]
        public void Save_TwiceWithOutlines_SecondFileHasTheOutlines()
        {
            var doc = CreateDocument("outlines");

            doc.SaveToMemory();
            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));

            pdf.TryGetBookmarks(out var bookmarks).Should().BeTrue();
            bookmarks!.Roots.Count.Should().Be(2);
        }

        [Fact]
        public void Save_PdfAAfterInfoChange_MetadataFollowsInfo()
        {
            var doc = CreateDocument("pdfA");
            doc.Info.Title = "First title";
            doc.SaveToMemory();
            int objectCount = doc.Xref.Entries.Count;

            doc.Info.Title = "Second title";
            string second = System.Text.Encoding.UTF8.GetString(doc.SaveToMemory());

            second.Should().Contain("Second title").And.NotContain("First title");
            doc.Xref.Entries.Count.Should().Be(objectCount, "the metadata stream is reused");
        }

        private static HpdfDocument CreateDocument(string feature)
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(doc.GetFont("Helvetica", null), 12);
            page.MoveTextPos(100, 700);
            page.ShowText("Hello");
            page.EndText();

            switch (feature)
            {
                case "outlines":
                    var destination = page.CreateDestination();
                    destination.SetFit();
                    var chapter = doc.CreateOutline("Chapter");
                    chapter.SetDestination(destination);
                    chapter.CreateChild("Section").SetDestination(destination);
                    doc.CreateOutline("Appendix").SetDestination(destination);
                    break;
                case "pageLabels":
                    doc.AddPageLabel(0, HpdfPageNumStyle.LowerRoman);
                    break;
                case "encryptionRc4":
                    doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R3);
                    break;
                case "pdfA":
                    doc.SetPdfACompliance();
                    break;
                case "pdfAEncrypted":
                    doc.SetPdfACompliance();
                    doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R3);
                    break;
                case "form":
                    var form = doc.GetOrCreateAcroForm();
                    var field = new HpdfTextField(doc.Xref, "Name");
                    field.SetValue("Jane");
                    page.AddWidgetAnnotation(field.CreateWidget(new HpdfRect(100, 600, 300, 620)));
                    form.AddField(field);
                    break;
            }

            return doc;
        }
    }
}
