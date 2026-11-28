using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfInsertPageAndZoomTests
    {
        private static void Label(HpdfPage page, HpdfFont font, string text)
        {
            page.SetFontAndSize(font, 12);
            page.TextOut(50, 700, text);
        }

        [Fact]
        public void InsertPage_PutsTheNewPageInFrontOfTheTarget()
        {
            var doc = new HpdfDocument();
            var font = new HpdfFont(doc.Xref, HpdfStandardFont.Helvetica, "F1");
            var first = doc.AddPage();
            var third = doc.AddPage();
            Label(first, font, "1");
            Label(third, font, "3");

            var second = doc.InsertPage(third);
            var zeroth = doc.InsertPage(first);
            Label(second, font, "2");
            Label(zeroth, font, "0");

            doc.Pages.Should().Equal(zeroth, first, second, third);
            doc.RootPages.Count.Should().Be(4);
            doc.CurrentPage.Should().BeSameAs(third, "inserting does not change the current page");
            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));
            Enumerable.Range(1, 4).Select(i => pdf.GetPage(i).Text).Should().Equal("0", "1", "2", "3");
        }

        [Fact]
        public void InsertPage_InAPagesNode()
        {
            var doc = new HpdfDocument();
            doc.SetPagesConfiguration(2);
            var pages = Enumerable.Range(0, 3).Select(_ => doc.AddPage()).ToList();

            var inserted = doc.InsertPage(pages[2]);

            inserted.Parent.Should().BeSameAs(pages[2].Parent);
            doc.Pages.Should().Equal(pages[0], pages[1], inserted, pages[2]);
            doc.RootPages.Count.Should().Be(4);
        }

        [Fact]
        public void InsertPage_FollowsTheCompressionMode()
        {
            var doc = new HpdfDocument();
            doc.SetCompressionMode(Haru.Types.HpdfCompressionMode.All);
            var target = doc.AddPage();

            var page = doc.InsertPage(target);

            page.Contents.Filter.Should().Be(Haru.Streams.HpdfStreamFilter.FlateDecode);
        }

        [Fact]
        public void InsertPage_InvalidTarget_Throws()
        {
            var doc = new HpdfDocument();
            var otherPage = new HpdfDocument().AddPage();

            doc.Invoking(d => d.InsertPage(null!)).Should().Throw<HpdfException>();
            doc.Invoking(d => d.InsertPage(otherPage)).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidPage);
            doc.Invoking(d => d.InsertPage(new HpdfPage(doc.Xref))).Should().Throw<HpdfException>("a page outside the tree");
        }

        [Fact]
        public void Zoom_WritesAndRemovesPZ()
        {
            var page = new HpdfDocument().AddPage();

            page.Zoom.Should().BeNull();
            page.Zoom = 1.5f;
            page.Zoom.Should().Be(1.5f);
            ((HpdfReal)page.Dict["PZ"]).Value.Should().Be(1.5f);

            page.Zoom = null;
            page.Dict.ContainsKey("PZ").Should().BeFalse();
            page.Invoking(p => p.Zoom = 0.05f).Should().Throw<HpdfException>();
            page.Invoking(p => p.Zoom = 33).Should().Throw<HpdfException>();
        }

        [Fact]
        public void LibraryVersion_IsTheAssemblyVersion()
        {
            HpdfDocument.LibraryVersion.Should().Be("1.2.0");
        }
    }
}
