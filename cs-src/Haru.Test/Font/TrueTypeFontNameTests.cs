using System.IO;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of the names a font file gets in a document: the simple TrueType font, the CID font and
    /// the font data pool read the 'name' table of a font file in the same way.
    /// </summary>
    public class TrueTypeFontNameTests
    {
        [Theory]
        [InlineData(TestFonts.Roboto, "Roboto-Regular")]
        [InlineData(TestFonts.Japanese, "NotoSansJP-Regular")]
        public void EveryFontKind_NamesAFontFileByItsPostScriptName(string fontFile, string postScriptName)
        {
            string path = TestFonts.DemoResource(fontFile);
            var doc = new HpdfDocument();

            using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, false);
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);
            var pooled = FontDataPool.GetOrLoadTrueType(path);
            using var shared = HpdfTrueTypeFont.LoadFromSharedData(doc.Xref, "F3", pooled, false);

            trueType.BaseFont.Should().Be(postScriptName);
            cid.BaseFont.Should().Be(postScriptName);
            pooled.ExtractedFontName.Should().Be(postScriptName);
            shared.BaseFont.Should().Be(postScriptName);
        }

        [Fact]
        public void CidFont_WritesTheFamilyNameOfTheFontFile()
        {
            var doc = new HpdfDocument();
            using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(TestFonts.Japanese), 932);

            var descendant = (HpdfDict)((HpdfArray)cid.Dict["DescendantFonts"])[0];
            var descriptor = (HpdfDict)descendant["FontDescriptor"];

            ((HpdfString)descriptor["FontFamily"]).GetText().Should().Be("Noto Sans JP");
        }

        [Fact]
        public void FontWithoutNameTable_GetsASyntheticName()
        {
            byte[] fontData = TestFonts.WithRenamedTable(File.ReadAllBytes(TestFonts.DemoResource(TestFonts.Roboto)), "name", "namx");
            string path = TestFonts.WriteTempFile(fontData);
            try
            {
                var doc = new HpdfDocument();

                using var trueType = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", path, false);
                using var cid = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", path, 65001);
                var pooled = FontDataPool.GetOrLoadTrueType(path);

                trueType.BaseFont.Should().Be("CustomTTFont-F1");
                cid.BaseFont.Should().Be("CIDFont-F2");
                pooled.ExtractedFontName.Should().Be("CustomTTFont-" + Path.GetFileNameWithoutExtension(path));
                // The same file gives the same name on every load
                FontDataPool.GetOrLoadTrueType(path, 1251).ExtractedFontName.Should().Be(pooled.ExtractedFontName);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
