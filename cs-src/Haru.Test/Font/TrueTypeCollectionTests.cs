using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using UglyToad.PdfPig;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of the fonts of a TrueType font collection (.ttc): a font is found by its index, its
    /// tables are read from its own table directory, and it is embedded as a font file of its own
    /// (a subset or the whole font), never as the collection.
    /// </summary>
    public class TrueTypeCollectionTests
    {
        private const string Text = "Wired 123";

        // [0] Noto Sans, [1] Space Mono; the values are those of the fonts (read with fontTools),
        // in 1000-unit glyph space; the width of W in font units
        [Theory]
        [InlineData(0, "NotoSans", 2048, 1069, -293, 1905)]
        [InlineData(1, "SpaceMono-Regular", 1000, 1120, -361, 612)]
        public void TrueTypeFont_IsTheFontOfTheIndex(int fontIndex, string name, int unitsPerEm, int ascent, int descent, int widthOfW)
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", TestFonts.Collection, fontIndex, true);
            ShowText(doc, font.AsFont(), Text);

            byte[] pdf = Save(doc);

            font.BaseFont.Should().Be(name);
            font.Ascent.Should().Be(ascent);
            font.Descent.Should().Be(descent);
            font.GetGlyphWidth(font.GetGlyphId('W')).Should().Be(widthOfW);

            byte[] program = FontProgram(font.Descriptor);
            ShouldBeAFontFile(program, unitsPerEm);
            program.Length.Should().Be(EmbeddedLength(font.Descriptor));
            ((HpdfName)font.Dict["BaseFont"]).Value.Should().MatchRegex(@"^[A-Z]{6}\+" + name + "$");
            ShouldShowTheText(pdf, Text);
        }

        [Theory]
        [InlineData(0, 2048)]
        [InlineData(1, 1000)]
        public void TrueTypeFont_EmbedSubsetOff_EmbedsTheWholeFontOfTheIndex(int fontIndex, int unitsPerEm)
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", TestFonts.Collection, fontIndex, true);
            font.EmbedSubset = false;
            ShowText(doc, font.AsFont(), Text);

            byte[] pdf = Save(doc);

            byte[] program = FontProgram(font.Descriptor);
            ShouldBeAFontFile(program, unitsPerEm);
            TableTags(program).Should().HaveCount(18, "the font has 18 tables in the collection");
            program.Length.Should().BeLessThan((int)new FileInfo(TestFonts.Collection).Length);
            ((HpdfName)font.Dict["BaseFont"]).Value.Should().Be(font.BaseFont);
            ShouldShowTheText(pdf, Text);
        }

        [Fact]
        public void TrueTypeFont_NotEmbedded_HasNoFontFile()
        {
            var doc = new HpdfDocument();
            using var font = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", TestFonts.Collection, 1, false);
            ShowText(doc, font.AsFont(), Text);

            Save(doc);

            font.Descriptor.ContainsKey("FontFile2").Should().BeFalse();
        }

        [Fact]
        public void TrueTypeFont_OfACollection_SavedTwice_IsTheSameFile()
        {
            var doc = new HpdfDocument();
            using var subset = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", TestFonts.Collection, 0, true);
            using var whole = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F2", TestFonts.Collection, 1, true);
            whole.EmbedSubset = false;
            ShowText(doc, subset.AsFont(), Text);
            ShowText(doc, whole.AsFont(), Text);

            Save(doc).Should().Equal(Save(doc));
        }

        [Theory]
        [InlineData(0, 2048, "Привет, Wired")]
        [InlineData(1, 1000, "Wired 123")]
        public void CidFont_IsASubsetOfTheFontOfTheIndex(int fontIndex, int unitsPerEm, string text)
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F1", TestFonts.Collection, fontIndex, 65001);
            ShowText(doc, font.AsFont(), text);

            byte[] pdf = Save(doc);

            ShouldBeAFontFile(FontProgram(CidDescriptor(font)), unitsPerEm);
            ((HpdfName)font.Dict["BaseFont"]).Value.Should().MatchRegex(@"^[A-Z]{6}\+" + font.BaseFont + "$");
            ShouldShowTheText(pdf, text);
        }

        [Fact]
        public void CidFont_EmbedSubsetOff_EmbedsTheWholeFontOfTheIndex()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F1", TestFonts.Collection, 1, 65001);
            font.EmbedSubset = false;
            ShowText(doc, font.AsFont(), Text);

            byte[] pdf = Save(doc);

            byte[] program = FontProgram(CidDescriptor(font));
            ShouldBeAFontFile(program, 1000);
            TableTags(program).Should().HaveCount(18);
            ShouldShowTheText(pdf, Text);
        }

        [Fact]
        public void CidFont_ForVerticalWriting_CanBeLoadedFromACollection()
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F1", TestFonts.Collection, 0, 65001, Haru.Types.HpdfWritingMode.Vertical);

            font.WritingMode.Should().Be(Haru.Types.HpdfWritingMode.Vertical);
            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be("Identity-V");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void FontsOfACollectionThatShareTables_AreReadFromTheirOwnDirectory(int fontIndex)
        {
            byte[] roboto = File.ReadAllBytes(TestFonts.DemoResource(TestFonts.Roboto));
            string path = TestFonts.WriteTempFile(TestFonts.SharedTableCollection(roboto, 3));
            try
            {
                var doc = new HpdfDocument();
                using var expected = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "E1", TestFonts.DemoResource(TestFonts.Roboto), false);
                using var subset = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", path, fontIndex, true);
                using var whole = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F2", path, fontIndex, 65001);
                whole.EmbedSubset = false;
                ShowText(doc, subset.AsFont(), Text);
                ShowText(doc, whole.AsFont(), Text);

                byte[] pdf = Save(doc);

                subset.BaseFont.Should().Be(expected.BaseFont);
                subset.FontBBox.Should().Be(expected.FontBBox);
                ShouldBeAFontFile(FontProgram(subset.Descriptor), 2048);
                FontProgram(CidDescriptor(whole)).Length.Should().BeCloseTo(roboto.Length, 64,
                    "the whole font is the font file again (tables padded to four bytes)");
                ShouldShowTheText(pdf, Text + Text);
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ---------- streams ----------

        [Fact]
        public void TrueTypeFont_FromAResourceStream_IsTheFontOfTheFile()
        {
            var doc = new HpdfDocument();
            using var fromFile = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "E1", TestFonts.Collection, 1, true);
            using var resource = TestFonts.OpenCollectionResource();
            using var font = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", resource, 1, true);
            ShowText(doc, fromFile.AsFont(), Text);
            ShowText(doc, font.AsFont(), Text);

            byte[] pdf = Save(doc);

            font.BaseFont.Should().Be("SpaceMono-Regular");
            font.FontBBox.Should().Be(fromFile.FontBBox);
            FontProgram(font.Descriptor).Should().Equal(FontProgram(fromFile.Descriptor));
            ShouldShowTheText(pdf, Text + Text);
        }

        [Fact]
        public void CidFont_FromAResourceStream_IsTheFontOfTheIndex()
        {
            var doc = new HpdfDocument();
            using var resource = TestFonts.OpenCollectionResource();
            using var font = HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F1", resource, 0, 65001);
            ShowText(doc, font.AsFont(), "Привет, Wired");

            byte[] pdf = Save(doc);

            font.BaseFont.Should().Be("NotoSans");
            ShouldBeAFontFile(FontProgram(CidDescriptor(font)), 2048);
            ShouldShowTheText(pdf, "Привет, Wired");
        }

        [Fact]
        public void Stream_IsReadFromItsPosition_AndStaysOpen()
        {
            byte[] collection = File.ReadAllBytes(TestFonts.Collection);
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 1, 2, 3 });
            stream.Write(collection);
            stream.Position = 3;
            var doc = new HpdfDocument();

            using var font = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", stream, 1, false);

            font.BaseFont.Should().Be("SpaceMono-Regular");
            stream.CanRead.Should().BeTrue("the caller owns the stream");
        }

        [Fact]
        public void Stream_OfAFontFile_Throws()
        {
            var doc = new HpdfDocument();
            using var roboto = File.OpenRead(TestFonts.DemoResource(TestFonts.Roboto));
            using var robotoAgain = File.OpenRead(TestFonts.DemoResource(TestFonts.Roboto));

            Action trueType = () => HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", roboto, 0, true);
            Action cid = () => HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F2", robotoAgain, 0, 65001);

            trueType.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            cid.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
        }

        // ---------- errors ----------

        [Fact]
        public void LoadingACollectionAsAFontFile_Throws()
        {
            var doc = new HpdfDocument();

            Action trueType = () => HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.Collection, true);
            Action cid = () => HpdfCIDFont.LoadFromTrueTypeFile(doc, "F2", TestFonts.Collection, 65001);

            trueType.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            cid.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
        }

        [Fact]
        public void LoadingAFontFileAsACollection_Throws()
        {
            var doc = new HpdfDocument();
            string roboto = TestFonts.DemoResource(TestFonts.Roboto);

            Action trueType = () => HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", roboto, 0, true);
            Action cid = () => HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F2", roboto, 0, 65001);

            trueType.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            cid.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(-1)]
        public void AnIndexOutsideTheCollection_Throws(int fontIndex)
        {
            var doc = new HpdfDocument();

            Action trueType = () => HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", TestFonts.Collection, fontIndex, true);
            Action cid = () => HpdfCIDFont.LoadFromTrueTypeCollection(doc, "F2", TestFonts.Collection, fontIndex, 65001);

            trueType.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcIndex);
            cid.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcIndex);
        }

        [Fact]
        public void ACollectionWithADamagedHeader_Throws()
        {
            byte[] collection = File.ReadAllBytes(TestFonts.Collection);
            BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(16), (uint)collection.Length);
            string path = TestFonts.WriteTempFile(collection);
            try
            {
                var doc = new HpdfDocument();

                Action load = () => HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", path, 1, true);

                load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AMissingFile_Throws()
        {
            var doc = new HpdfDocument();

            Action load = () => HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "F1", "no-such-file.ttc", 0, true);

            load.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.FileNotFound);
        }

        // ---------- font data pool ----------

        [Fact]
        public void Pool_FontsOfOneCollection_ShareTheBytesOfTheFile()
        {
            var noto = FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.Collection, 0);
            var spaceMono = FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.Collection, 1, 1251);
            var notoAgain = FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.Collection, 0);

            noto.ExtractedFontName.Should().Be("NotoSans");
            noto.FontIndex.Should().Be(0);
            spaceMono.ExtractedFontName.Should().Be("SpaceMono-Regular");
            spaceMono.FontIndex.Should().Be(1);
            spaceMono.CodePage.Should().Be(1251);
            spaceMono.FontData.Should().BeSameAs(noto.FontData, "the collection is read once");
            notoAgain.Should().BeSameAs(noto);
        }

        [Fact]
        public void Pool_FontOfACollection_CreatesTheFontOfTheIndex()
        {
            var doc = new HpdfDocument();
            using var expected = HpdfTrueTypeFont.LoadFromCollection(doc.Xref, "E1", TestFonts.Collection, 1, true);
            using var font = HpdfTrueTypeFont.LoadFromSharedData(
                doc.Xref, "F1", FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.Collection, 1), true);
            ShowText(doc, font.AsFont(), Text);

            Save(doc);

            font.BaseFont.Should().Be(expected.BaseFont);
            font.FontBBox.Should().Be(expected.FontBBox);
            ShouldBeAFontFile(FontProgram(font.Descriptor), 1000);
        }

        [Fact]
        public void Pool_CollectionAsAFontFile_AndFontFileAsACollection_Throw()
        {
            Action collection = () => FontDataPool.GetOrLoadTrueType(TestFonts.Collection);
            Action fontFile = () => FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.DemoResource(TestFonts.Roboto), 0);
            Action index = () => FontDataPool.GetOrLoadTrueTypeCollection(TestFonts.Collection, 5);

            collection.Should().Throw<InvalidOperationException>()
                .WithInnerException<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            fontFile.Should().Throw<InvalidOperationException>()
                .WithInnerException<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcFile);
            index.Should().Throw<InvalidOperationException>()
                .WithInnerException<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidTtcIndex);
        }

        // ---------- helpers ----------

        private static void ShowText(HpdfDocument doc, HpdfFont font, string text)
        {
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(font, 12);
            page.MoveTextPos(50, 700);
            page.ShowText(text);
            page.EndText();
        }

        private static byte[] Save(HpdfDocument doc)
        {
            using var stream = new MemoryStream();
            doc.Save(stream);
            return stream.ToArray();
        }

        private static HpdfDict CidDescriptor(HpdfCIDFont font)
        {
            var descendant = (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];
            return (HpdfDict)descendant["FontDescriptor"];
        }

        /// <summary>The embedded font program (FontFile2), not compressed.</summary>
        private static byte[] FontProgram(HpdfDict descriptor) => ((HpdfStreamObject)descriptor["FontFile2"]).Stream.ToArray();

        private static int EmbeddedLength(HpdfDict descriptor) => ((HpdfNumber)((HpdfDict)descriptor["FontFile2"])["Length1"]).Value;

        /// <summary>
        /// Checks that a font program is a TrueType font file of its own (not a collection) and
        /// that it is the expected font.
        /// </summary>
        private static void ShouldBeAFontFile(byte[] program, int unitsPerEm)
        {
            BinaryPrimitives.ReadUInt32BigEndian(program).Should().Be(0x00010000u, "a font program is a TrueType font file, not a collection");

            var tables = TableTags(program);
            tables.Should().Contain(new[] { "head", "glyf", "loca", "hmtx" });
            int head = TableOffset(program, "head");
            BinaryPrimitives.ReadUInt16BigEndian(program.AsSpan(head + 18)).Should().Be((ushort)unitsPerEm);
        }

        private static List<string> TableTags(byte[] font)
        {
            int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
            return Enumerable.Range(0, numTables)
                .Select(i => System.Text.Encoding.ASCII.GetString(font, 12 + i * 16, 4))
                .ToList();
        }

        private static int TableOffset(byte[] font, string tag)
        {
            int index = TableTags(font).IndexOf(tag);
            return (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(12 + index * 16 + 8));
        }

        /// <summary>
        /// Checks that a reader finds the text with the outlines of the embedded fonts.
        /// </summary>
        private static void ShouldShowTheText(byte[] pdf, string text)
        {
            using var read = PdfDocument.Open(pdf);
            var letters = read.GetPages().SelectMany(page => page.Letters).ToList();

            string.Concat(letters.Select(letter => letter.Value)).Should().Be(text);
            letters.Where(letter => letter.Value != " ")
                .Should().OnlyContain(letter => letter.GlyphRectangle.Width > 0 && letter.GlyphRectangle.Height > 0);
        }
    }
}
