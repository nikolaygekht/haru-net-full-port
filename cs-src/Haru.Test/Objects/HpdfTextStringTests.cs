using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Forms;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Objects
{
    /// <summary>
    /// Tests that text strings are written as PDF text strings (ASCII or UTF-16BE with a byte order mark)
    /// and that viewers read non-ASCII text back.
    /// </summary>
    public class HpdfTextStringTests
    {
        private const string Cyrillic = "Привет, мир";

        // ---------- encoding ----------

        [Fact]
        public void TextString_Ascii_IsStoredAsIs()
        {
            new HpdfString("Hello (World)\t1").Value.Should().Equal(System.Text.Encoding.ASCII.GetBytes("Hello (World)\t1"));
        }

        [Theory]
        [InlineData("Привет")]
        [InlineData("Café")]
        [InlineData("日本語")]
        [InlineData("€ 100")]
        public void TextString_NonAscii_IsUtf16BigEndianWithByteOrderMark(string text)
        {
            var value = new HpdfString(text).Value;

            value.Take(2).Should().Equal((byte)0xFE, (byte)0xFF);
            value.Skip(2).Should().Equal(System.Text.Encoding.BigEndianUnicode.GetBytes(text));
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("Привет")]
        [InlineData("Café 日本語")]
        [InlineData("")]
        public void GetText_ReadsBackWhatWasWritten(string text)
        {
            new HpdfString(text).GetText().Should().Be(text);
        }

        [Fact]
        public void GetText_Utf8WithByteOrderMark_IsDecodedAsUtf8()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(System.Text.Encoding.UTF8.GetBytes(Cyrillic)).ToArray();

            new HpdfString(bytes).GetText().Should().Be(Cyrillic);
        }

        [Fact]
        public void GetText_SingleBytes_AreDecodedAsLatin1()
        {
            new HpdfString(new byte[] { 0x43, 0x61, 0x66, 0xE9 }).GetText().Should().Be("Café");
        }

        [Fact]
        public void InfoTitle_NonAscii_ReadsBack()
        {
            var doc = new HpdfDocument();

            doc.Info.Title = Cyrillic;

            doc.Info.Title.Should().Be(Cyrillic);
        }

        // ---------- read back by a viewer ----------

        [Fact]
        public void Save_NonAsciiInfoOutlineAndFieldValues_AreReadByPdfPig()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            doc.Info.Title = Cyrillic;
            doc.Info.Author = "José Müller";
            var outline = doc.CreateOutline("Глава 1");
            var destination = page.CreateDestination();
            destination.SetFit();
            outline.SetDestination(destination);

            var form = doc.GetOrCreateAcroForm();
            var field = new HpdfTextField(doc.Xref, "Имя");
            field.SetValue(Cyrillic);
            page.AddWidgetAnnotation(field.CreateWidget(new HpdfRect(100, 600, 300, 620)));
            form.AddField(field);

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));

            pdf.Information.Title.Should().Be(Cyrillic);
            pdf.Information.Author.Should().Be("José Müller");
            pdf.TryGetBookmarks(out var bookmarks).Should().BeTrue();
            bookmarks!.Roots.Single().Title.Should().Be("Глава 1");
            pdf.TryGetForm(out var acroForm).Should().BeTrue();
            var textField = acroForm!.Fields.OfType<UglyToad.PdfPig.AcroForms.Fields.AcroTextField>().Single();
            textField.Information.PartialName.Should().Be("Имя");
            textField.Value.Should().Be(Cyrillic);
        }

        [Fact]
        public void Save_EncryptedNonAsciiTitle_IsReadWithPassword()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.Info.Title = Cyrillic;
            doc.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R3);

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()), new ParsingOptions { Password = "user" });

            pdf.Information.Title.Should().Be(Cyrillic);
        }

        [Theory]
        [InlineData(HpdfEncryptMode.R2)]
        [InlineData(HpdfEncryptMode.R3)]
        [InlineData(HpdfEncryptMode.R4)]
        public void Save_EncryptedObjectWithSeveralStrings_EveryStringIsReadable(HpdfEncryptMode mode)
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.Info.Title = "Title";
            doc.Info.Author = "Author";
            doc.Info.Subject = Cyrillic;
            doc.Info.Keywords = "Keywords";
            doc.SetEncryption("user", "owner", HpdfPermission.Print, mode);

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()), new ParsingOptions { Password = "user" });

            pdf.Information.Title.Should().Be("Title");
            pdf.Information.Author.Should().Be("Author");
            pdf.Information.Subject.Should().Be(Cyrillic);
            pdf.Information.Keywords.Should().Be("Keywords");
        }

        // ---------- URI ----------

        [Fact]
        public void LinkAnnotation_NonAsciiUri_IsPercentEncodedUtf8()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            var link = page.CreateLinkAnnotation(new HpdfRect(0, 0, 10, 10), "https://example.com/путь?q=ä");

            var uri = (HpdfString)((HpdfDict)link.Dict["A"])["URI"];
            System.Text.Encoding.ASCII.GetString(uri.Value)
                .Should().Be("https://example.com/%D0%BF%D1%83%D1%82%D1%8C?q=%C3%A4");
        }

        [Fact]
        public void LinkAnnotation_AsciiUri_IsUnchanged()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            var link = page.CreateLinkAnnotation(new HpdfRect(0, 0, 10, 10), "https://example.com/a?b=c%20d");

            var uri = (HpdfString)((HpdfDict)link.Dict["A"])["URI"];
            System.Text.Encoding.ASCII.GetString(uri.Value).Should().Be("https://example.com/a?b=c%20d");
        }
    }
}
