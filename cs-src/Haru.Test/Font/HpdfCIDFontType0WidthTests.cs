using System;
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
    /// Tests of the predefined (not embedded) CJK fonts: the character codes of the text are mapped
    /// to CIDs by the font's CMap, and the widths, which the font definition gives by CID, are taken
    /// for these CIDs - as a viewer does with the /W array of the font.
    /// </summary>
    public class HpdfCIDFontType0WidthTests
    {
        // ---------- code -> CID -> width ----------

        [Theory]
        // Simplified Chinese: half-width Latin, full-width ideographs, Latin and punctuation
        [InlineData("SimSun", 936, "GBK-EUC-H", "A", 500)]
        [InlineData("SimSun", 936, "GBK-EUC-H", " ", 500)]
        [InlineData("SimSun", 936, "GBK-EUC-H", "你", 1000)]
        [InlineData("SimSun", 936, "GBK-EUC-H", "ａ", 1000)]
        [InlineData("SimSun", 936, "GBK-EUC-H", "、", 1000)]
        [InlineData("SimHei", 936, "GBK-EUC-H", "A", 500)]
        [InlineData("SimHei", 936, "GBK-EUC-H", "你", 1000)]
        // Traditional Chinese
        [InlineData("MingLiU", 950, "ETen-B5-H", "A", 500)]
        [InlineData("MingLiU", 950, "ETen-B5-H", " ", 500)]
        [InlineData("MingLiU", 950, "ETen-B5-H", "你", 1000)]
        // Japanese, fixed pitch: half-width Latin and katakana
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H", "A", 500)]
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H", " ", 500)]
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H", "あ", 1000)]
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H", "ｱ", 500)]
        [InlineData("MS-Mincho", 932, "90ms-RKSJ-H", "A", 500)]
        [InlineData("MS-Mincho", 932, "90ms-RKSJ-H", "あ", 1000)]
        // Japanese, proportional
        [InlineData("MS-PGothic", 932, "90ms-RKSJ-H", "A", 632)]
        [InlineData("MS-PGothic", 932, "90ms-RKSJ-H", "i", 210)]
        [InlineData("MS-PGothic", 932, "90ms-RKSJ-H", " ", 304)]
        [InlineData("MS-PGothic", 932, "90ms-RKSJ-H", "あ", 941)]
        [InlineData("MS-PMincho", 932, "90ms-RKSJ-H", "A", 664)]
        [InlineData("MS-PMincho", 932, "90ms-RKSJ-H", "i", 242)]
        // Korean, fixed pitch and proportional
        [InlineData("DotumChe", 949, "KSCms-UHC-H", "A", 500)]
        [InlineData("DotumChe", 949, "KSCms-UHC-H", " ", 500)]
        [InlineData("DotumChe", 949, "KSCms-UHC-H", "한", 1000)]
        [InlineData("BatangChe", 949, "KSCms-UHC-H", "A", 500)]
        [InlineData("Dotum", 949, "KSCms-UHC-H", "A", 666)]
        [InlineData("Dotum", 949, "KSCms-UHC-H", "i", 228)]
        [InlineData("Dotum", 949, "KSCms-UHC-H", " ", 333)]
        [InlineData("Dotum", 949, "KSCms-UHC-H", "한", 1000)]
        [InlineData("Batang", 949, "KSCms-UHC-H", "A", 687)]
        [InlineData("Batang", 949, "KSCms-UHC-H", "i", 469)]
        public void MeasureText_TakesTheWidthOfTheCidOfTheCharacter(string fontName, int codePage, string encoding, string text, int width)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), fontName, "F1", codePage, encoding);

            font.MeasureText(text, 1000).Should().Be(width);
        }

        [Fact]
        public void MeasureText_SumsMixedText()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "SimSun", "F1", 936, "GBK-EUC-H");

            // Two ideographs and five half-width characters
            font.MeasureText("你好, abc", 12).Should().BeApproximately((2 * 1000 + 5 * 500) * 12 / 1000f, 0.001f);
        }

        [Fact]
        public void GetCharWidth_MapsTheByteThroughTheCMap()
        {
            using var proportional = HpdfCIDFontType0.Create(new HpdfDocument(), "Dotum", "F1", 949, "KSCms-UHC-H");

            proportional.GetCharWidth((byte)'A').Should().Be(666);
            proportional.GetCharWidth((byte)'i').Should().Be(228);
        }

        [Fact]
        public void Create_WithoutEncoder_UsesTheEncoderOfTheCMap()
        {
            var doc = new HpdfDocument();
            using var withEncoder = HpdfCIDFontType0.Create(doc, "MS-PGothic", "F1", 932, "90ms-RKSJ-H", new Ms90RKSJHEncoder());
            using var withoutEncoder = HpdfCIDFontType0.Create(doc, "MS-PGothic", "F2", 932, "90ms-RKSJ-H");

            const string text = "Hello こんにちは";
            withoutEncoder.MeasureText(text, 1000).Should().Be(withEncoder.MeasureText(text, 1000));
            withoutEncoder.ConvertTextToBytes(text).Should().Equal(withEncoder.ConvertTextToBytes(text));
        }

        [Fact]
        public void Create_UnknownCMapWithoutEncoder_Throws()
        {
            Action create = () => HpdfCIDFontType0.Create(new HpdfDocument(), "SimSun", "F1", 936, "GB-EUC-H").Dispose();

            create.Should().Throw<HpdfException>().Which.ErrorCode.Should().Be(HpdfErrorCode.InvalidEncodingName);
        }

        // ---------- the encoders ----------

        [Fact]
        public void Encoder_MapsCodesToCids()
        {
            var gbk = new GBKEucHEncoder();

            gbk.GetCID(0x20).Should().Be(7716);
            gbk.GetCID(0x41).Should().Be(846);
            gbk.GetCID(0xC4E3).Should().Be(2886);
            gbk.GetCID(0x00).Should().Be(0, "control codes have no CID");
            gbk.GetCID(0xFFFF).Should().Be(0);
        }

        [Fact]
        public void Encoder_ReadsOneAndTwoByteCodes()
        {
            var sjis = new Ms90RKSJHEncoder();
            byte[] encoded = sjis.EncodeText("Aあｱ");
            encoded.Should().HaveCount(4);

            var cids = new List<ushort>();
            int position = 0;
            while (position < encoded.Length)
                cids.Add(sjis.ReadCID(encoded, ref position));

            cids.Should().Equal(264, 843, 343);
        }

        [Theory]
        [InlineData("GBK-EUC-H", typeof(GBKEucHEncoder))]
        [InlineData("ETen-B5-H", typeof(ETenB5HEncoder))]
        [InlineData("90ms-RKSJ-H", typeof(Ms90RKSJHEncoder))]
        [InlineData("EUC-H", typeof(EucHEncoder))]
        [InlineData("KSCms-UHC-H", typeof(KSCmsUHCHEncoder))]
        public void Encoder_IsCreatedByTheNameOfItsCMap(string name, Type type)
        {
            var encoder = CIDEncoder.Create(name);

            encoder.Should().BeOfType(type);
            encoder.Name.Should().Be(name);
            encoder.GetCID(0x41).Should().NotBe(0);
        }

        // ---------- the font objects ----------

        [Theory]
        [InlineData("SimSun", 936, "GBK-EUC-H")]
        [InlineData("SimHei", 936, "GBK-EUC-H")]
        [InlineData("MingLiU", 950, "ETen-B5-H")]
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H")]
        [InlineData("MS-Mincho", 932, "90ms-RKSJ-H")]
        [InlineData("MS-PGothic", 932, "90ms-RKSJ-H")]
        [InlineData("MS-PMincho", 932, "90ms-RKSJ-H")]
        [InlineData("DotumChe", 949, "KSCms-UHC-H")]
        [InlineData("BatangChe", 949, "KSCms-UHC-H")]
        [InlineData("Dotum", 949, "KSCms-UHC-H")]
        [InlineData("Batang", 949, "KSCms-UHC-H")]
        public void WidthArray_GivesEveryCidOneWidth(string fontName, int codePage, string encoding)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), fontName, "F1", codePage, encoding);
            var descendant = (HpdfDict)((HpdfArray)font.Dict["DescendantFonts"])[0];
            var w = (HpdfArray)descendant["W"];

            // The array is a list of "first last width" entries
            (w.Count % 3).Should().Be(0);
            int previousLast = -1;
            for (int i = 0; i < w.Count; i += 3)
            {
                int first = ((HpdfNumber)w[i]).Value;
                int last = ((HpdfNumber)w[i + 1]).Value;
                first.Should().BeGreaterThan(previousLast, "a CID must not get two widths");
                last.Should().BeGreaterThanOrEqualTo(first);
                font.GetCIDWidth((ushort)first).Should().Be((short)((HpdfNumber)w[i + 2]).Value);
                previousLast = last;
            }
        }

        [Fact]
        public void Font_HasNoToUnicodeMap()
        {
            // The encoding is a predefined CMap of an Adobe character collection: a reader maps
            // its codes to Unicode itself
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "SimSun", "F1", 936, "GBK-EUC-H");

            font.Dict.ContainsKey("ToUnicode").Should().BeFalse();
            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be("GBK-EUC-H");
        }

        [Theory]
        [InlineData("SimSun", 936, "GBK-EUC-H", "你好世界")]
        [InlineData("MingLiU", 950, "ETen-B5-H", "繁體中文")]
        [InlineData("MS-Gothic", 932, "90ms-RKSJ-H", "こんにちは")]
        [InlineData("DotumChe", 949, "KSCms-UHC-H", "안녕하세요")]
        public void ShownText_IsExtractedThroughThePredefinedCMap(string fontName, int codePage, string encoding, string text)
        {
            var doc = new HpdfDocument();
            using var font = HpdfCIDFontType0.Create(doc, fontName, "F1", codePage, encoding);
            var page = doc.AddPage();
            page.BeginText();
            page.SetFontAndSize(font.AsFont(), 12);
            page.MoveTextPos(50, 700);
            page.ShowText(text);
            page.EndText();

            using var stream = new MemoryStream();
            doc.Save(stream);
            using var read = PdfDocument.Open(stream.ToArray());

            string.Concat(read.GetPage(1).Letters.Select(letter => letter.Value)).Should().Be(text);
        }
    }
}
