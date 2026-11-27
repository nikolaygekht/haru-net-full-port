using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Actions;
using UglyToad.PdfPig.Outline;
using Xunit;

namespace Haru.Test.Doc
{
    public class HpdfNamedDestinationTests
    {
        private static readonly HpdfRect LinkRect = new HpdfRect(50, 700, 200, 720);

        private static HpdfDestination FitDestination(HpdfPage page)
        {
            var destination = page.CreateDestination();
            destination.SetFit();
            return destination;
        }

        private static string Text(HpdfObject value) => ((HpdfString)value).GetText();

        [Fact]
        public void CreateNamedDestination_TargetSetLater_WritesDestsTree()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();

            var intro = doc.CreateNamedDestination("intro");
            var destination = FitDestination(page);
            intro.SetDestination(destination);
            doc.SaveToMemory();

            intro.Name.Should().Be("intro");
            intro.Destination.Should().BeSameAs(destination);
            doc.NamedDestinations.Should().Equal(intro);
            var names = (HpdfArray)((HpdfDict)((HpdfDict)doc.Catalog.Dict["Names"])["Dests"])["Names"];
            names.Count.Should().Be(2);
            Text(names[0]).Should().Be("intro");
            names[1].Should().BeSameAs(destination.DestArray);
            doc.Xref.RequiredVersion.Should().Be(HpdfVersion.Version12, "the name dictionary is PDF 1.2");
        }

        [Fact]
        public void Save_WritesSortedDestsNextToEmbeddedFiles()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            doc.CreateNamedDestination("zeta", FitDestination(page));
            doc.CreateNamedDestination("alpha", FitDestination(page));
            doc.AttachFile("a.txt", new byte[] { 1 });

            doc.SaveToMemory();

            var namesDict = (HpdfDict)doc.Catalog.Dict["Names"];
            var names = (HpdfArray)((HpdfDict)namesDict["Dests"])["Names"];
            Text(names[0]).Should().Be("alpha");
            Text(names[2]).Should().Be("zeta");
            namesDict.ContainsKey("EmbeddedFiles").Should().BeTrue();
        }

        [Fact]
        public void LinkAndOutline_WriteTheNameAsDest()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var intro = doc.CreateNamedDestination("intro", FitDestination(page));

            var link = page.CreateLinkAnnotation(LinkRect, intro);
            var outline = doc.CreateOutline("Intro");
            outline.SetDestination(intro);

            Text(link.Dict["Dest"]).Should().Be("intro");
            Text(outline.Dict["Dest"]).Should().Be("intro");
        }

        [Fact]
        public void Save_WithoutTarget_Throws()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            doc.CreateNamedDestination("intro");

            doc.Invoking(d => d.SaveToMemory()).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDestination);
        }

        [Fact]
        public void InvalidArguments_Throw()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var intro = doc.CreateNamedDestination("intro");

            doc.Invoking(d => d.CreateNamedDestination("intro")).Should().Throw<HpdfException>("the name is taken");
            doc.Invoking(d => d.CreateNamedDestination("")).Should().Throw<HpdfException>();
            doc.Invoking(d => d.CreateNamedDestination(null!)).Should().Throw<HpdfException>();
            intro.Invoking(n => n.SetDestination(null!)).Should().Throw<HpdfException>();
            intro.Invoking(n => n.SetDestination(page.CreateDestination())).Should().Throw<HpdfException>()
                .Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDestination, "the destination has no view");
            page.Invoking(p => p.CreateLinkAnnotation(LinkRect, (HpdfNamedDestination)null!))
                .Should().Throw<HpdfException>();
            doc.NamedDestinations.Should().Equal(intro);
        }

        [Fact]
        public void NamedDestinationOfAnotherDocument_Throws()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var other = new HpdfDocument();
            var otherPage = other.AddPage();
            var foreign = other.CreateNamedDestination("foreign");
            var local = doc.CreateNamedDestination("local");

            page.Invoking(p => p.CreateLinkAnnotation(LinkRect, foreign)).Should().Throw<HpdfException>();
            doc.CreateOutline("Foreign").Invoking(o => o.SetDestination(foreign)).Should().Throw<HpdfException>();
            local.Invoking(n => n.SetDestination(FitDestination(otherPage))).Should().Throw<HpdfException>();
            page.Annotations.Should().BeEmpty();
        }

        // Encrypted files are not tested here: PdfPig 0.1.16 resolved neither the bookmark nor the link of this test
        // when encrypted (name "Глава 1", RC4-128); MuPDF resolves both to page 2
        [Theory]
        [InlineData("intro")]
        [InlineData("Глава 1")]
        public void SavedNamedDestinations_ResolveToTheirPages(string name)
        {
            var doc = new HpdfDocument();
            var first = doc.AddPage();
            var second = doc.AddPage();
            var chapter = doc.CreateNamedDestination(name);
            first.CreateLinkAnnotation(LinkRect, chapter);
            doc.CreateOutline("Chapter").SetDestination(chapter);
            chapter.SetDestination(FitDestination(second));

            using var pdf = PdfDocument.Open(new MemoryStream(doc.SaveToMemory()));

            pdf.TryGetBookmarks(out var bookmarks).Should().BeTrue();
            bookmarks!.Roots.Single().Should().BeOfType<DocumentBookmarkNode>().Which.PageNumber.Should().Be(2);
            var action = pdf.GetPage(1).GetAnnotations().Single().Action.Should().BeOfType<GoToAction>().Subject;
            action.Destination.PageNumber.Should().Be(2);
        }

        [Fact]
        public void Save_IsRepeatable()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var intro = doc.CreateNamedDestination("intro", FitDestination(page));
            page.CreateLinkAnnotation(LinkRect, intro);
            doc.AttachFile("a.txt", new byte[] { 1 });

            var first = doc.SaveToMemory();
            var second = doc.SaveToMemory();

            second.Should().Equal(first);
        }

        [Fact]
        public void Save_KeepsTheNameDictionaryOfTheUser()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            var names = new HpdfDict();
            names.Add("AP", new HpdfDict());
            doc.Catalog.SetNames(names);
            doc.CreateNamedDestination("intro", FitDestination(page));

            doc.SaveToMemory();

            doc.Catalog.Dict["Names"].Should().BeSameAs(names);
            names.ContainsKey("AP").Should().BeTrue();
            names.ContainsKey("Dests").Should().BeTrue();
        }
    }
}
