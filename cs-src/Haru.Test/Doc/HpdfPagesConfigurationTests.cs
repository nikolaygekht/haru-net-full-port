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
    public class HpdfPagesConfigurationTests
    {
        private static HpdfArray Kids(HpdfDict pages) => (HpdfArray)pages["Kids"];

        [Fact]
        public void SetPagesConfiguration_GroupsPagesIntoNodesUnderTheRoot()
        {
            var doc = new HpdfDocument();
            doc.SetPagesConfiguration(2);

            var pages = Enumerable.Range(0, 5).Select(_ => doc.AddPage()).ToList();

            var nodes = Kids(doc.RootPages.Dict).Cast<HpdfDict>().ToList();
            nodes.Select(node => Kids(node).Count).Should().Equal(2, 2, 1);
            nodes.Should().OnlyContain(node => ((HpdfName)node["Type"]).Value == "Pages");
            doc.RootPages.Count.Should().Be(5);
            doc.Pages.Should().Equal(pages);
        }

        [Fact]
        public void SetPagesConfiguration_Zero_KeepsAllPagesInOneNode()
        {
            var doc = new HpdfDocument();
            doc.SetPagesConfiguration(0);

            for (int i = 0; i < 4; i++)
                doc.AddPage();

            var node = (HpdfDict)Kids(doc.RootPages.Dict).Single();
            Kids(node).Count.Should().Be(4);
        }

        [Fact]
        public void SavedDocument_HasThePagesInOrder()
        {
            var doc = new HpdfDocument();
            doc.SetPagesConfiguration(3);
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1");
            for (int i = 1; i <= 7; i++)
            {
                var page = doc.AddPage();
                page.SetFontAndSize(font, 12);
                page.TextOut(50, 700, $"Page {i}");
            }

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));

            pdf.NumberOfPages.Should().Be(7);
            for (int i = 1; i <= 7; i++)
                pdf.GetPage(i).Text.Should().Be($"Page {i}");
        }

        [Fact]
        public void SetPagesConfiguration_InvalidUse_Throws()
        {
            var doc = new HpdfDocument();
            doc.Invoking(d => d.SetPagesConfiguration(-1)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);

            doc.AddPage();

            doc.Invoking(d => d.SetPagesConfiguration(10)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDocumentState);
        }
    }
}
