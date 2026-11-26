using System;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Objects;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of fonts by name: <see cref="HpdfDocument.RegisterFont"/> and
    /// <see cref="HpdfDocumentExtensions.GetFont"/> interpret encoding names in the same way, so a
    /// font registered under an encoding name is found under it.
    /// </summary>
    public class HpdfFontRegistryTests
    {
        // ---------- encoding names ----------

        [Theory]
        [InlineData(null, 1252)]
        [InlineData("", 1252)]
        [InlineData("WinAnsiEncoding", 1252)]
        [InlineData("StandardEncoding", 1252)]
        [InlineData("FontSpecific", 1252)]
        [InlineData("MacRomanEncoding", 10000)]
        [InlineData("CP1250", 1250)]
        [InlineData("CP1251", 1251)]
        [InlineData("CP1252", 1252)]
        [InlineData("CP1253", 1253)]
        [InlineData("CP1254", 1254)]
        [InlineData("CP1255", 1255)]
        [InlineData("CP1256", 1256)]
        [InlineData("CP1257", 1257)]
        [InlineData("CP1258", 1258)]
        [InlineData("1251", 1251)]
        [InlineData("ISO8859-2", 28592)]
        [InlineData("ISO8859-3", 28593)]
        [InlineData("ISO8859-4", 28594)]
        [InlineData("ISO8859-5", 28595)]
        [InlineData("ISO8859-6", 28596)]
        [InlineData("ISO8859-7", 28597)]
        [InlineData("ISO8859-8", 28598)]
        [InlineData("ISO8859-9", 28599)]
        [InlineData("ISO8859-10", 28600)]
        [InlineData("ISO8859-11", 28601)]
        [InlineData("ISO8859-13", 28603)]
        [InlineData("ISO8859-14", 28604)]
        [InlineData("ISO8859-15", 28605)]
        [InlineData("ISO8859-16", 28606)]
        [InlineData("ISO-8859-5", 28595)]
        [InlineData("KOI8-R", 20866)]
        [InlineData("windows-1251", 1251)]
        public void GetFont_EncodingName_SelectsItsCodePage(string? encodingName, int codePage)
        {
            var doc = new HpdfDocument();

            var font = doc.GetFont("Helvetica", encodingName);

            font.EncodingCodePage.Should().Be(codePage);
        }

        [Theory]
        [InlineData("CP1251", "cp1251")]
        [InlineData("WinAnsiEncoding", "winansiencoding")]
        [InlineData("ISO8859-2", "iso8859-2")]
        [InlineData("ISO8859-2", "ISO-8859-2")]
        [InlineData("KOI8-R", "koi8-r")]
        [InlineData("CP1252", null)]
        public void GetFont_NamesOfOneEncoding_ReturnOneFont(string? name, string? otherName)
        {
            var doc = new HpdfDocument();

            doc.GetFont("Times-Roman", otherName).Should().BeSameAs(doc.GetFont("Times-Roman", name));
        }

        [Theory]
        [InlineData("NoSuchEncoding")]
        [InlineData("CP")]
        [InlineData("ISO8859-12")]
        [InlineData("ISO8859-99")]
        [InlineData("CP12x")]
        public void GetFont_UnknownEncodingName_Throws(string encodingName)
        {
            var doc = new HpdfDocument();

            Action getFont = () => doc.GetFont("Helvetica", encodingName);

            getFont.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidEncodingName);
        }

        [Fact]
        public void GetFont_StandardFontWithMacRomanEncoding_IsWrittenAndEncodedAsMacRoman()
        {
            var font = new HpdfDocument().GetFont("Helvetica", "MacRomanEncoding");

            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be("MacRomanEncoding");
            // "é" is 0x8E in Mac OS Roman (0xE9 in WinAnsi)
            font.EncodeText("é").Should().Equal(0x8E);
            font.MeasureText("é", 1000).Should().Be(556);
        }

        [Fact]
        public void GetFont_UnknownFontName_Throws()
        {
            var doc = new HpdfDocument();

            Action getFont = () => doc.GetFont("NoSuchFont");

            getFont.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        // ---------- registered fonts ----------

        [Theory]
        [InlineData(1252, "CP1252")]
        [InlineData(1252, "WinAnsiEncoding")]
        [InlineData(1251, "CP1251")]
        [InlineData(1250, "cp1250")]
        [InlineData(28592, "ISO8859-2")]
        [InlineData(28595, "ISO-8859-5")]
        [InlineData(20866, "KOI8-R")]
        public void RegisterFont_WithoutEncoding_IsFoundByTheNameOfItsCodePage(int codePage, string encodingName)
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.DemoResource(TestFonts.Roboto), false, codePage);
            var font = ttf.AsFont();

            doc.RegisterFont(font);

            doc.GetFont(font.BaseFont, encodingName).Should().BeSameAs(font);
        }

        [Theory]
        [InlineData("ISO8859-2")]
        [InlineData("iso8859-2")]
        [InlineData("ISO-8859-2")]
        [InlineData("CP1251")]
        [InlineData("KOI8-R")]
        [InlineData("MacRomanEncoding")]
        public void RegisterFont_WithEncodingName_IsFoundByThatName(string encodingName)
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.DemoResource(TestFonts.Roboto), false);
            var font = ttf.AsFont();

            doc.RegisterFont(font, encodingName);

            doc.GetFont(font.BaseFont, encodingName).Should().BeSameAs(font);
        }

        [Fact]
        public void RegisterFont_UnknownEncodingName_Throws()
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.DemoResource(TestFonts.Roboto), false);
            var font = ttf.AsFont();

            Action register = () => doc.RegisterFont(font, "NoSuchEncoding");

            register.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidEncodingName);
        }

        [Theory]
        [InlineData(1251)]
        [InlineData(1252)]
        public void RegisteredFont_IsFoundByItsNameAlone(int codePage)
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.DemoResource(TestFonts.Roboto), false, codePage);
            var font = ttf.AsFont();
            doc.RegisterFont(font);

            doc.GetFont(font.BaseFont).Should().BeSameAs(font);
        }

        [Fact]
        public void RegisteredFont_AskedWithAnotherEncoding_IsNotFound()
        {
            var doc = new HpdfDocument();
            using var ttf = HpdfTrueTypeFont.LoadFromFile(doc.Xref, "F1", TestFonts.DemoResource(TestFonts.Roboto), false, 1251);
            var font = ttf.AsFont();
            doc.RegisterFont(font);

            Action getFont = () => doc.GetFont(font.BaseFont, "CP1250");

            getFont.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidParameter);
        }

        [Fact]
        public void RegisteredCidFont_IsFoundByUtf8AndByItsNameAlone()
        {
            var doc = new HpdfDocument();
            using var cidFont = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(TestFonts.Japanese), 65001);
            var font = cidFont.AsFont();

            doc.RegisterFont(font, "UTF-8");

            doc.GetFont(font.BaseFont, "UTF-8").Should().BeSameAs(font);
            doc.GetFont(font.BaseFont, "utf8").Should().BeSameAs(font);
            doc.GetFont(font.BaseFont).Should().BeSameAs(font);
        }

        [Fact]
        public void RegisteredPredefinedCjkFont_IsFoundByItsCMapName()
        {
            var doc = new HpdfDocument();
            using var simSun = HpdfCIDFontType0.Create(doc, "SimSun", "F1", 936, "GBK-EUC-H");
            var font = simSun.AsFont();

            doc.RegisterFont(font);

            doc.GetFont("SimSun", "GBK-EUC-H").Should().BeSameAs(font);
            doc.GetFont("SimSun", "CP936").Should().BeSameAs(font);
            doc.GetFont("SimSun").Should().BeSameAs(font);
        }

        [Fact]
        public void StandardFont_WithoutEncoding_IsWinAnsi_EvenIfAnotherEncodingWasUsedFirst()
        {
            var doc = new HpdfDocument();
            var cyrillic = doc.GetFont("Helvetica", "CP1251");

            var western = doc.GetFont("Helvetica");

            western.Should().NotBeSameAs(cyrillic);
            western.EncodingCodePage.Should().Be(1252);
        }
    }
}
