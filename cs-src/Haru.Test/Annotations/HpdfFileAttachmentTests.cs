using System;
using System.IO;
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
    public class HpdfFileAttachmentTests
    {
        private static readonly HpdfRect IconRect = new HpdfRect(100, 100, 120, 120);
        private static readonly byte[] Data = System.Text.Encoding.ASCII.GetBytes("Hello, attachment");

        private static HpdfStreamObject Stream(HpdfEmbeddedFile file) =>
            (HpdfStreamObject)((HpdfDict)file.Dict["EF"])["F"];

        [Fact]
        public void AttachFile_WritesFileSpecificationAndStream()
        {
            var doc = new HpdfDocument();

            var file = doc.AttachFile("notes.txt", Data, "text/plain");

            file.Name.Should().Be("notes.txt");
            file.Size.Should().Be(Data.Length);
            ((HpdfName)file.Dict["Type"]).Value.Should().Be("Filespec");
            System.Text.Encoding.ASCII.GetString(((HpdfString)file.Dict["F"]).Value).Should().Be("notes.txt");
            file.Dict.ContainsKey("UF").Should().BeFalse("an ASCII name fits into /F");
            var stream = Stream(file);
            ((HpdfName)stream["Type"]).Value.Should().Be("EmbeddedFile");
            ((HpdfName)stream["Subtype"]).Value.Should().Be("text/plain");
            var parameters = (HpdfDict)stream["Params"];
            ((HpdfNumber)parameters["Size"]).Value.Should().Be(Data.Length);
            ((HpdfString)parameters["CheckSum"]).Value.Should().Equal(System.Security.Cryptography.MD5.HashData(Data));
            stream.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
            doc.AttachedFiles.Should().Equal(file);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version14);
        }

        [Fact]
        public void AttachFile_NonAsciiName_GoesToUnicodeFileName()
        {
            var doc = new HpdfDocument();

            var file = doc.AttachFile("данные.csv", Data);

            ((HpdfString)file.Dict["UF"]).GetText().Should().Be("данные.csv");
            System.Text.Encoding.ASCII.GetString(((HpdfString)file.Dict["F"]).Value).Should().Be("______.csv");
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version17);
        }

        [Fact]
        public void AttachFile_FromPath_UsesTheFileName()
        {
            string path = Path.Combine(Path.GetTempPath(), "haru-attach-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllBytes(path, Data);
            try
            {
                var file = new HpdfDocument().AttachFile(path);

                file.Name.Should().Be(Path.GetFileName(path));
                file.Size.Should().Be(Data.Length);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AttachFile_InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            doc.AttachFile("a.txt", Data);

            doc.Invoking(d => d.AttachFile("a.txt", Data)).Should().Throw<HpdfException>("the name is taken");
            doc.Invoking(d => d.AttachFile("", Data)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AttachFile("dir/b.txt", Data)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AttachFile("b.txt", null!)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AttachFile("b.txt", Data, "plain")).Should().Throw<HpdfException>();
            doc.Invoking(d => d.AttachFile("missing-file-" + Guid.NewGuid().ToString("N"))).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.FileNotFound);
            doc.AttachedFiles.Should().HaveCount(1);
        }

        [Fact]
        public void Save_WritesSortedEmbeddedFilesNameTree()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            var zeta = doc.AttachFile("zeta.txt", Data);
            var alpha = doc.AttachFile("alpha.txt", Data);

            doc.SaveToMemory();

            var names = (HpdfArray)((HpdfDict)((HpdfDict)doc.Catalog.Dict["Names"])["EmbeddedFiles"])["Names"];
            ((HpdfString)names[0]).GetText().Should().Be("alpha.txt");
            names[1].Should().BeSameAs(alpha.Dict);
            ((HpdfString)names[2]).GetText().Should().Be("zeta.txt");
            names[3].Should().BeSameAs(zeta.Dict);
        }

        [Fact]
        public void Save_KeepsTheNameDictionaryOfTheUser()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            var names = new HpdfDict();
            names.Add("Custom", new HpdfNumber(1));
            doc.Catalog.SetNames(names);
            doc.AttachFile("a.txt", Data);

            doc.SaveToMemory();

            doc.Catalog.Dict["Names"].Should().BeSameAs(names);
            names.ContainsKey("Custom").Should().BeTrue();
            names.ContainsKey("EmbeddedFiles").Should().BeTrue();
        }

        [Fact]
        public void SavedAttachment_IsReadBack()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.AttachFile("notes.txt", Data, "text/plain").SetDescription("Notes");

            using var pdf = PdfDocument.Open(doc.SaveToMemory());

            pdf.Advanced.TryGetEmbeddedFiles(out var files).Should().BeTrue();
            files.Should().HaveCount(1);
            files[0].Name.Should().Be("notes.txt");
            files[0].Bytes.ToArray().Should().Equal(Data);
        }

        [Fact]
        public void PdfA_DoesNotAllowAttachedFiles()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage();
            doc.AttachFile("a.txt", Data);

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void FileAttachmentAnnotation_WritesDictionaryAndIcon()
        {
            var doc = new HpdfDocument();
            var file = doc.AttachFile("chart.csv", Data, "text/csv");

            var annot = doc.AddPage().CreateFileAttachmentAnnotation(IconRect, file, "The data", HpdfFileAttachmentIcon.Graph);
            doc.SaveToMemory();

            annot.Type.Should().Be(HpdfAnnotationType.FileAttachment);
            ((HpdfName)annot.Dict["Subtype"]).Value.Should().Be("FileAttachment");
            annot.Dict["FS"].Should().BeSameAs(file.Dict);
            annot.File.Should().BeSameAs(file);
            ((HpdfName)annot.Dict["Name"]).Value.Should().Be("Graph");
            ((HpdfString)annot.Dict["Contents"]).GetText().Should().Be("The data");
            annot.Flags.Should().Be(HpdfAnnotationFlags.Print);
            AppearanceContent(annot).Should().Contain("1 0 0 1 100 100 cm");
        }

        [Theory]
        [InlineData(HpdfFileAttachmentIcon.PushPin, "PushPin")]
        [InlineData(HpdfFileAttachmentIcon.Paperclip, "Paperclip")]
        [InlineData(HpdfFileAttachmentIcon.Tag, "Tag")]
        public void SetIcon_WritesName(HpdfFileAttachmentIcon icon, string name)
        {
            var annot = new HpdfDocument().AddPage().CreateFileAttachmentAnnotation(IconRect, "a.bin", Data, "");

            annot.SetIcon(icon);

            ((HpdfName)annot.Dict["Name"]).Value.Should().Be(name);
            annot.Icon.Should().Be(icon);
            annot.Invoking(a => a.SetIcon((HpdfFileAttachmentIcon)9)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.AnnotInvalidIcon);
        }

        [Fact]
        public void FileAttachmentWithOwnFile_IsNotADocumentAttachment()
        {
            var doc = new HpdfDocument();

            var annot = doc.AddPage().CreateFileAttachmentAnnotation(IconRect, "own.bin", Data, "");
            doc.SaveToMemory();

            annot.File.Name.Should().Be("own.bin");
            doc.AttachedFiles.Should().BeEmpty();
            doc.Catalog.Dict.ContainsKey("Names").Should().BeFalse();
        }

        [Fact]
        public void FileAttachment_InvalidArguments_Throw()
        {
            var page = new HpdfDocument().AddPage();

            page.Invoking(p => p.CreateFileAttachmentAnnotation(IconRect, (HpdfEmbeddedFile)null!, "")).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFileAttachmentAnnotation(IconRect, "a.bin", Data, null!)).Should().Throw<HpdfException>();
            page.Invoking(p => p.CreateFileAttachmentAnnotation(IconRect, "a.bin", Data, "", (HpdfFileAttachmentIcon)7))
                .Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        [Fact]
        public void PdfA_DoesNotAllowFileAttachmentAnnotations()
        {
            var doc = new HpdfDocument();
            doc.SetPdfACompliance();
            doc.AddPage().CreateFileAttachmentAnnotation(IconRect, "a.bin", Data, "");

            var act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidOperation);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var file = doc.AttachFile("a.txt", Data);
            doc.AddPage().CreateFileAttachmentAnnotation(IconRect, file, "");

            byte[] first = doc.SaveToMemory();
            byte[] second = doc.SaveToMemory();

            second.Should().Equal(first);
        }
    }
}
