using System;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;
using Xunit;

namespace Haru.Test.Doc
{
    /// <summary>
    /// Tests for even-odd fill-and-stroke operators, flatness, page rotation,
    /// viewer preferences and the open action.
    /// </summary>
    public class HpdfPageAndCatalogOptionsTests
    {
        // ---------- path painting and flatness ----------

        [Fact]
        public void EoFillStroke_WritesOperator()
        {
            using var page = new HpdfPage(new HpdfXref(0));

            page.EoFillStroke();
            page.ClosePathEoFillStroke();

            Content(page).Should().Be("B*\nb*\n");
        }

        [Fact]
        public void SetFlat_WritesOperatorAndUpdatesGraphicsState()
        {
            using var page = new HpdfPage(new HpdfXref(0));

            page.SetFlat(5);

            Content(page).Should().Be("5 i\n");
            page.GraphicsState.Flatness.Should().Be(5);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void SetFlat_OutOfRange_Throws(float flatness)
        {
            using var page = new HpdfPage(new HpdfXref(0));

            Action act = () => page.SetFlat(flatness);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageOutOfRange);
        }

        // ---------- page rotation ----------

        [Fact]
        public void Rotate_NewPage_IsZeroAndNotWritten()
        {
            using var page = new HpdfPage(new HpdfXref(0));

            page.Rotate.Should().Be(0);
            page.Dict.ContainsKey("Rotate").Should().BeFalse();
        }

        [Theory]
        [InlineData(90, 90)]
        [InlineData(180, 180)]
        [InlineData(270, 270)]
        [InlineData(450, 90)]
        [InlineData(-90, 270)]
        public void Rotate_MultipleOf90_IsStoredNormalized(int angle, int expected)
        {
            using var page = new HpdfPage(new HpdfXref(0));

            page.Rotate = angle;

            page.Rotate.Should().Be(expected);
            ((HpdfNumber)page.Dict["Rotate"]).Value.Should().Be(expected);
        }

        [Fact]
        public void Rotate_SetBackToZero_RemovesEntry()
        {
            using var page = new HpdfPage(new HpdfXref(0));
            page.Rotate = 90;

            page.Rotate = 360;

            page.Dict.ContainsKey("Rotate").Should().BeFalse();
            page.Rotate.Should().Be(0);
        }

        [Fact]
        public void Rotate_NotMultipleOf90_Throws()
        {
            using var page = new HpdfPage(new HpdfXref(0));

            Action act = () => page.Rotate = 45;

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.PageInvalidRotateValue);
        }

        // ---------- viewer preferences ----------

        [Fact]
        public void ViewerPreferences_ByDefault_AreNotWritten()
        {
            var doc = new HpdfDocument();

            doc.Catalog.ViewerPreferences.Should().Be(HpdfViewerPreferences.None);
            doc.Catalog.Dict.ContainsKey("ViewerPreferences").Should().BeFalse();
        }

        [Fact]
        public void ViewerPreferences_Set_WritesBooleanEntriesAndReadsThemBack()
        {
            var doc = new HpdfDocument();
            var preferences = HpdfViewerPreferences.HideToolbar | HpdfViewerPreferences.HideMenubar |
                              HpdfViewerPreferences.HideWindowUI | HpdfViewerPreferences.FitWindow |
                              HpdfViewerPreferences.CenterWindow;

            doc.Catalog.ViewerPreferences = preferences;

            var dict = (HpdfDict)doc.Catalog.Dict["ViewerPreferences"];
            dict.Keys.Should().BeEquivalentTo("HideToolbar", "HideMenubar", "HideWindowUI", "FitWindow", "CenterWindow");
            ((HpdfBoolean)dict["FitWindow"]).Value.Should().BeTrue();
            doc.Catalog.ViewerPreferences.Should().Be(preferences);
            doc.Version.Should().Be(HpdfVersion.Version12);
        }

        [Fact]
        public void ViewerPreferences_PrintScalingNone_WritesNameAndRequiresPdf16()
        {
            var doc = new HpdfDocument();

            doc.Catalog.ViewerPreferences = HpdfViewerPreferences.PrintScalingNone;

            var dict = (HpdfDict)doc.Catalog.Dict["ViewerPreferences"];
            ((HpdfName)dict["PrintScaling"]).Value.Should().Be("None");
            doc.Catalog.ViewerPreferences.Should().Be(HpdfViewerPreferences.PrintScalingNone);
            doc.Version.Should().Be(HpdfVersion.Version16);
        }

        [Fact]
        public void ViewerPreferences_SetToNone_RemovesDictionary()
        {
            var doc = new HpdfDocument();
            doc.Catalog.ViewerPreferences = HpdfViewerPreferences.FitWindow;

            doc.Catalog.ViewerPreferences = HpdfViewerPreferences.None;

            doc.Catalog.Dict.ContainsKey("ViewerPreferences").Should().BeFalse();
        }

        [Fact]
        public void ViewerPreferences_MatchConstantsOfCLibrary()
        {
            ((int)HpdfViewerPreferences.HideToolbar).Should().Be(HpdfConst.HideToolbar);
            ((int)HpdfViewerPreferences.HideMenubar).Should().Be(HpdfConst.HideMenubar);
            ((int)HpdfViewerPreferences.HideWindowUI).Should().Be(HpdfConst.HideWindowUi);
            ((int)HpdfViewerPreferences.FitWindow).Should().Be(HpdfConst.FitWindow);
            ((int)HpdfViewerPreferences.CenterWindow).Should().Be(HpdfConst.CenterWindow);
            ((int)HpdfViewerPreferences.PrintScalingNone).Should().Be(HpdfConst.PrintScalingNone);
        }

        // ---------- page layout and page mode versions ----------

        [Theory]
        [InlineData(HpdfPageLayout.TwoPageLeft, HpdfVersion.Version15)]
        [InlineData(HpdfPageLayout.TwoPageRight, HpdfVersion.Version15)]
        [InlineData(HpdfPageLayout.TwoColumnLeft, HpdfVersion.Version12)]
        public void PageLayout_RequiresVersionOfLayout(HpdfPageLayout layout, HpdfVersion expected)
        {
            var doc = new HpdfDocument();

            doc.Catalog.PageLayout = layout;

            doc.Version.Should().Be(expected);
        }

        [Theory]
        [InlineData(HpdfPageMode.UseOC, HpdfVersion.Version15)]
        [InlineData(HpdfPageMode.UseAttachments, HpdfVersion.Version16)]
        [InlineData(HpdfPageMode.UseOutlines, HpdfVersion.Version12)]
        public void PageMode_RequiresVersionOfMode(HpdfPageMode mode, HpdfVersion expected)
        {
            var doc = new HpdfDocument();

            doc.Catalog.PageMode = mode;

            doc.Version.Should().Be(expected);
        }

        // ---------- open action ----------

        [Fact]
        public void SetOpenAction_WritesDestinationArray()
        {
            var doc = new HpdfDocument();
            doc.AddPage();
            var second = doc.AddPage();
            var destination = second.CreateDestination();
            destination.SetXYZ(0, 500, 1.5f);

            doc.Catalog.SetOpenAction(destination);

            var openAction = doc.Catalog.GetOpenAction();
            openAction.Should().BeSameAs(destination.DestArray);
            openAction![0].Should().BeSameAs(second.Dict);
            ((HpdfName)openAction[1]).Value.Should().Be("XYZ");
            System.Text.Encoding.ASCII.GetString(doc.SaveToMemory()).Should().Contain("/OpenAction");
        }

        [Fact]
        public void SetOpenAction_Null_RemovesOpenAction()
        {
            var doc = new HpdfDocument();
            var destination = doc.AddPage().CreateDestination();
            destination.SetFit();
            doc.Catalog.SetOpenAction(destination);

            doc.Catalog.SetOpenAction(null);

            doc.Catalog.GetOpenAction().Should().BeNull();
            doc.Catalog.Dict.ContainsKey("OpenAction").Should().BeFalse();
        }

        [Fact]
        public void SetOpenAction_DestinationWithoutView_Throws()
        {
            var doc = new HpdfDocument();
            var destination = doc.AddPage().CreateDestination();

            Action act = () => doc.Catalog.SetOpenAction(destination);

            act.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidDestination);
        }

        private static string Content(HpdfPage page) =>
            System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());
    }
}
