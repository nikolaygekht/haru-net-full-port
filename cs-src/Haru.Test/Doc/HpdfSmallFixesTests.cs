using System;
using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Forms;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for the page tree, unbalanced graphics states, document dates, catalog names, extended
    /// graphics states, destinations, TextOut, generated resource names, page labels and radio options.
    /// </summary>
    public class HpdfSmallFixesTests
    {
        // ---------- page tree ----------

        [Fact]
        public void Pages_FollowThePageTreeOrder()
        {
            var doc = new HpdfDocument();
            var a = doc.AddPage();
            var node = doc.InsertPagesNode();    // root: a, node; node becomes the current pages node
            doc.CurrentPages = doc.RootPages;
            var c = doc.AddPage();                // root: a, node, c
            doc.CurrentPages = node;
            var b = doc.AddPage();                // node: b -> tree order a, b, c

            // Compared by reference: a failure message would try to print the cyclic page objects
            doc.Pages.Select((page, i) => ReferenceEquals(page, new[] { a, b, c }[i])).Should().Equal(true, true, true);
            doc.PageCount.Should().Be(3);
        }

        [Fact]
        public void Pages_AddedWithAddKid_AreDocumentPagesAndFollowCompression()
        {
            var doc = new HpdfDocument();
            doc.CompressionMode = HpdfCompressionMode.Text;
            using var page = new HpdfPage(doc.Xref);

            doc.RootPages.AddKid(page);
            doc.SaveToMemory();

            doc.Pages.Any(p => ReferenceEquals(p, page)).Should().BeTrue();
            page.Contents.Filter.Should().Be(HpdfStreamFilter.FlateDecode);
        }

        [Fact]
        public void AddKid_SameKidTwice_AddsItOnce()
        {
            var doc = new HpdfDocument();
            using var page = new HpdfPage(doc.Xref);
            doc.RootPages.AddKid(page);

            doc.RootPages.AddKid(page);

            doc.RootPages.GetChildren().Should().HaveCount(1);
            doc.PageCount.Should().Be(1);
        }

        [Fact]
        public void AddKid_Cycle_Throws()
        {
            var doc = new HpdfDocument();
            var child = doc.InsertPagesNode();
            var grandchild = doc.InsertPagesNode(child);

            Action self = () => child.AddKid(child);
            Action ancestor = () => grandchild.AddKid(doc.RootPages);

            self.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageCannotSetParent);
            ancestor.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageCannotSetParent);
        }

        [Fact]
        public void Save_GSaveWithoutGRestore_Throws()
        {
            var doc = new HpdfDocument();
            doc.AddPage().GSave();

            Action act = () => doc.SaveToMemory();

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageCannotRestoreGState);
        }

        // ---------- document information dates ----------

        [Fact]
        public void SetCreationDate_Utc_IsWrittenAsZulu()
        {
            var doc = new HpdfDocument();

            doc.Info.SetCreationDate(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

            doc.Info.GetCreationDate().Should().Be("D:20260102030405Z");
        }

        [Fact]
        public void SetCreationDate_DateTimeOffset_KeepsItsOffset()
        {
            var doc = new HpdfDocument();

            doc.Info.SetModificationDate(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(330)));

            doc.Info.GetModificationDate().Should().Be("D:20260102030405+05'30'");
        }

        [Fact]
        public void SetCreationDate_DoesNotDependOnCultureCalendar()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("th-TH");
                var doc = new HpdfDocument();

                doc.Info.SetCreationDate(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

                doc.Info.GetCreationDate().Should().StartWith("D:2026");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        // ---------- catalog, extended graphics state, destinations ----------

        [Fact]
        public void SetNames_Twice_ReplacesNames()
        {
            var doc = new HpdfDocument();
            doc.Catalog.SetNames(new HpdfDict());
            var second = new HpdfDict();

            doc.Catalog.SetNames(second);

            doc.Catalog.GetNames().Should().BeSameAs(second);
        }

        [Theory]
        [InlineData(1.5f)]
        [InlineData(-0.1f)]
        [InlineData(float.NaN)]
        public void ExtGState_AlphaOutOfRange_ThrowsExtGStateOutOfRange(float alpha)
        {
            var state = new HpdfExtGState(new HpdfDocument().Xref, "GS1");

            Action stroke = () => state.SetAlphaStroke(alpha);
            Action fill = () => state.SetAlphaFill(alpha);

            stroke.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.ExtGStateOutOfRange);
            fill.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.ExtGStateOutOfRange);
        }

        [Fact]
        public void ExtGState_UndefinedBlendMode_Throws()
        {
            var state = new HpdfExtGState(new HpdfDocument().Xref, "GS1");

            Action act = () => state.SetBlendMode((HpdfBlendMode)99);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.ExtGStateOutOfRange);
        }

        [Theory]
        [InlineData(-1f, 0f, 1f)]
        [InlineData(0f, -1f, 1f)]
        [InlineData(0f, 0f, 0.05f)]
        [InlineData(0f, 0f, 33f)]
        public void SetXYZ_InvalidValues_Throw(float left, float top, float zoom)
        {
            var destination = new HpdfDocument().AddPage().CreateDestination();

            Action act = () => destination.SetXYZ(left, top, zoom);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void SetXYZ_ZeroZoom_KeepsCurrentZoom()
        {
            var destination = new HpdfDocument().AddPage().CreateDestination();

            destination.SetXYZ(0, 800, 0);

            destination.DestArray.Count.Should().Be(5);
        }

        // ---------- text, names ----------

        [Fact]
        public void TextOut_WithoutFont_ThrowsBeforeWriting()
        {
            var page = new HpdfDocument().AddPage();

            Action act = () => page.TextOut(10, 10, "Hello");

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageFontNotFound);
            page.Contents.Stream.Size.Should().Be(0);
        }

        [Fact]
        public void GeneratedResourceNames_UseReservedPrefix()
        {
            var doc = new HpdfDocument();

            var font = doc.GetFont("Helvetica", null);
            using var image = doc.LoadPngImage(new System.IO.MemoryStream(TestHelper.GetResourceBytes("test_rgb_2x2.png")));

            font.LocalName.Should().StartWith(HpdfDocument.GeneratedNamePrefix + "F");
            image.LocalName.Should().StartWith(HpdfDocument.GeneratedNamePrefix + "Im");
        }

        // ---------- page labels ----------

        [Fact]
        public void PageLabels_WithoutLabelForFirstPage_GetDecimalEntryForIndexZero()
        {
            var doc = new HpdfDocument();
            for (int i = 0; i < 4; i++)
                doc.AddPage();
            doc.AddPageLabel(2, HpdfPageNumStyle.LowerRoman);

            doc.SaveToMemory();

            var nums = (HpdfArray)doc.Catalog.GetPageLabels()!["Nums"];
            ((HpdfNumber)nums[0]).Value.Should().Be(0);
            ((HpdfName)((HpdfDict)nums[1])["S"]).Value.Should().Be("D");
            ((HpdfNumber)nums[2]).Value.Should().Be(2);
        }

        [Fact]
        public void PageLabel_InvalidValues_Throw()
        {
            var label = new HpdfPageLabel();

            Action firstPage = () => label.FirstPage = 0;
            Action style = () => label.Style = HpdfPageNumStyle.Eof;

            firstPage.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            style.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageNumStyleOutOfRange);
        }

        // ---------- radio options ----------

        [Fact]
        public void AddOption_WidgetWithoutMatchingState_Throws()
        {
            var doc = new HpdfDocument();
            var radio = new HpdfRadioButton(doc.Xref, "Color");
            var widget = new HpdfWidgetAnnotation(doc.Xref, new HpdfRect(0, 0, 10, 10));
            widget.SetButtonAppearances(ButtonElementType.RadioButton, "Blue", "Off");

            Action act = () => radio.AddOption("Red", widget);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
            radio.Widgets.Should().BeEmpty();
        }
    }
}
