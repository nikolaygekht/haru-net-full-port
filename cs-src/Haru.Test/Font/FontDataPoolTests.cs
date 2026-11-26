using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Objects;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of <see cref="FontDataPool"/>: a font file is cached once, and the code page belongs to
    /// the fonts created from the cached data, not to the cache entry.
    /// </summary>
    public class FontDataPoolTests
    {
        private static string RobotoPath => TestFonts.DemoResource(TestFonts.Roboto);

        [Fact]
        public void GetOrLoadTrueType_DefaultCodePage_IsWinAnsi()
        {
            var data = FontDataPool.GetOrLoadTrueType(RobotoPath);

            data.CodePage.Should().Be(1252);
            FontDataPool.DefaultCodePage.Should().Be(1252);
        }

        [Fact]
        public void GetOrLoadTrueType_SameFileWithTwoCodePages_GivesEachItsCodePage()
        {
            var western = FontDataPool.GetOrLoadTrueType(RobotoPath, 1252);
            var cyrillic = FontDataPool.GetOrLoadTrueType(RobotoPath, 1251);
            var westernAgain = FontDataPool.GetOrLoadTrueType(RobotoPath, 1252);

            western.CodePage.Should().Be(1252);
            cyrillic.CodePage.Should().Be(1251, "the second load must not return the data of the first one");
            westernAgain.CodePage.Should().Be(1252);

            // The font file itself is cached once and shared
            cyrillic.FontData.Should().BeSameAs(western.FontData);
            cyrillic.ExtractedFontName.Should().Be(western.ExtractedFontName);
            cyrillic.FilePath.Should().Be(western.FilePath);
            cyrillic.LoadedAt.Should().Be(western.LoadedAt);
        }

        [Fact]
        public void LoadFromSharedData_SameFileWithTwoCodePages_CreatesCorrectlyEncodedFonts()
        {
            var doc = new HpdfDocument();
            using var expectedWestern = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "E1", RobotoPath, false, 1252);
            using var expectedCyrillic = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "E2", RobotoPath, false, 1251);

            using var western = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F1", FontDataPool.GetOrLoadTrueType(RobotoPath, 1252), false);
            using var cyrillic = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F2", FontDataPool.GetOrLoadTrueType(RobotoPath, 1251), false);

            western.CodePage.Should().Be(1252);
            cyrillic.CodePage.Should().Be(1251);

            // 0xDF is "ß" in code page 1252 and "Я" in code page 1251
            western.EncodeText("ß").Should().Equal(0xDF);
            cyrillic.EncodeText("Я").Should().Equal(0xDF);
            western.GetCharWidth(0xDF).Should().Be(expectedWestern.GetCharWidth(0xDF));
            cyrillic.GetCharWidth(0xDF).Should().Be(expectedCyrillic.GetCharWidth(0xDF));
            cyrillic.GetCharWidth(0xDF).Should().NotBe(western.GetCharWidth(0xDF));
            cyrillic.MeasureText("Яблоко", 1000)
                .Should().Be(expectedCyrillic.MeasureText("Яблоко", 1000));
        }

        [Fact]
        public void LoadFromSharedData_WithCodePage_UsesIt()
        {
            var doc = new HpdfDocument();
            var data = FontDataPool.GetOrLoadTrueType(RobotoPath);

            using var cyrillic = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F1", data, false, 1251);

            cyrillic.CodePage.Should().Be(1251);
            cyrillic.EncodeText("Я").Should().Equal(0xDF);
        }

        [Fact]
        public void LoadFromSharedData_NamesTheFontAsLoadFromFileDoes()
        {
            var doc = new HpdfDocument();
            using var fromFile = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", RobotoPath, false);
            using var shared = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F2", FontDataPool.GetOrLoadTrueType(RobotoPath), false);

            shared.BaseFont.Should().Be(fromFile.BaseFont);
            ((HpdfName)shared.Dict["BaseFont"]).Value.Should().Be(shared.BaseFont);
        }

        [Fact]
        public void Type1_LoadFromSharedData_WithCodePage_UsesIt()
        {
            var doc = new HpdfDocument();
            var data = FontDataPool.GetOrLoadType1(
                TestFonts.DemoResource("demo/type1/a010013l.afm"), TestFonts.DemoResource("demo/type1/a010013l.pfb"));

            using var western = HpdfType1Font.LoadFromSharedData(doc.Xref, "F1", data, false);
            using var centralEuropean = HpdfType1Font.LoadFromSharedData(doc.Xref, "F2", data, false, 1250);

            western.CodePage.Should().Be(1252);
            centralEuropean.CodePage.Should().Be(1250);
            // "ł" (lslash) is 300 units wide in this font and is not in code page 1252
            centralEuropean.MeasureText("ł", 1000).Should().Be(300);
        }
    }
}
