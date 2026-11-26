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
            Action create = () => HpdfCIDFontType0.Create(new HpdfDocument(), "SimSun", "F1", 936, "UniGB-UCS2-H").Dispose();

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
        [InlineData("GBK-EUC-H", "GB1", 2, 936)]
        [InlineData("GB-EUC-H", "GB1", 0, 20936)]
        [InlineData("ETen-B5-H", "CNS1", 0, 950)]
        [InlineData("90ms-RKSJ-H", "Japan1", 2, 932)]
        [InlineData("90msp-RKSJ-H", "Japan1", 2, 932)]
        [InlineData("EUC-H", "Japan1", 1, 20932)]
        [InlineData("KSCms-UHC-H", "Korea1", 1, 949)]
        [InlineData("KSCms-UHC-HW-H", "Korea1", 1, 949)]
        [InlineData("KSC-EUC-H", "Korea1", 0, 51949)]
        public void Encoder_IsCreatedByTheNameOfItsCMap(string name, string ordering, int supplement, int codePage)
        {
            var encoder = CIDEncoder.Create(name);

            encoder.Name.Should().Be(name);
            encoder.Registry.Should().Be("Adobe");
            encoder.Ordering.Should().Be(ordering);
            encoder.Supplement.Should().Be(supplement);
            encoder.CodePage.Should().Be(codePage);
            encoder.GetCID(0x41).Should().NotBe(0);

            // The name is found in any letter case; the encoder has the name of the CMap
            CIDEncoder.Create((name ?? string.Empty).ToUpperInvariant()).Name.Should().Be(name);
        }

        [Fact]
        public void EncoderClass_IsTheEncoderOfItsCMap()
        {
            var encoders = new CIDEncoder[]
            {
                new GBKEucHEncoder(), new ETenB5HEncoder(), new Ms90RKSJHEncoder(), new EucHEncoder(), new KSCmsUHCHEncoder(),
            };

            foreach (CIDEncoder encoder in encoders)
            {
                CIDEncoder byName = CIDEncoder.Create(encoder.Name);

                byName.CodePage.Should().Be(encoder.CodePage, encoder.Name);
                byName.Ordering.Should().Be(encoder.Ordering, encoder.Name);
                byName.Supplement.Should().Be(encoder.Supplement, encoder.Name);
                for (int code = 0; code <= 0xFFFF; code += 7)
                    byName.GetCID((ushort)code).Should().Be(encoder.GetCID((ushort)code));
            }
        }

        // The expected CIDs are those of the CMap arrays of libharu (c-src/hpdf_encoder_*.c)
        [Theory]
        // Japanese with proportional Latin characters: the Latin characters are CIDs 1-95,
        // in 90ms-RKSJ-H they are the half-width ones (231-325)
        [InlineData("90msp-RKSJ-H", "A", 34)]
        [InlineData("90msp-RKSJ-H", " ", 1)]
        [InlineData("90msp-RKSJ-H", "\u3042", 843)]
        [InlineData("90msp-RKSJ-H", "\uFF71", 343)]
        [InlineData("90ms-RKSJ-H", "A", 264)]
        // Korean with half-width Latin characters (in KSCms-UHC-H they are proportional, CIDs 1-95)
        [InlineData("KSCms-UHC-HW-H", "A", 8127)]
        [InlineData("KSCms-UHC-HW-H", " ", 8094)]
        [InlineData("KSCms-UHC-HW-H", "\uD55C", 3296)]
        [InlineData("KSCms-UHC-HW-H", "\uB620", 11319)]
        [InlineData("KSCms-UHC-H", "A", 34)]
        // Korean, KS X 1001 only
        [InlineData("KSC-EUC-H", "A", 8127)]
        [InlineData("KSC-EUC-H", "\uD55C", 3296)]
        [InlineData("KSC-EUC-H", "\u3001", 102)]
        // Simplified Chinese, GB 2312 only
        [InlineData("GB-EUC-H", "A", 846)]
        [InlineData("GB-EUC-H", " ", 7716)]
        [InlineData("GB-EUC-H", "\u4F60", 2886)]
        [InlineData("GB-EUC-H", "\u3042", 357)]
        public void Encoder_MapsACharacterToTheCidOfItsCMap(string name, string character, int cid)
        {
            CIDEncoder encoder = CIDEncoder.Create(name);
            byte[] encoded = encoder.EncodeText(character);

            int position = 0;
            encoder.ReadCID(encoded, ref position).Should().Be((ushort)cid);
            position.Should().Be(encoded.Length, "the character is one code");
        }

        [Theory]
        [InlineData("GB-EUC-H", "\u9555")]    // in GBK, not in GB 2312
        [InlineData("KSC-EUC-H", "\uB620")]   // in UHC, not in KS X 1001
        public void Encoder_OfANarrowCharacterSet_ReplacesACharacterOutsideIt(string name, string character)
        {
            // Not a code that the CMap could not read: the codes of the wider encoding are outside
            // its code space
            CIDEncoder.Create(name).EncodeText(character).Should().Equal((byte)'?');
        }

        [Theory]
        [InlineData("MS-PGothic", "90msp-RKSJ-H", 632 + 210)]
        [InlineData("MS-PMincho", "90msp-RKSJ-H", 664 + 242)]
        [InlineData("Dotum", "KSCms-UHC-HW-H", 666 + 228)]
        [InlineData("DotumChe", "KSC-EUC-H", 500 + 500)]
        [InlineData("SimSun", "GB-EUC-H", 500 + 500)]
        public void Font_IsCreatedWithEveryHorizontalCMap(string fontName, string encoding, int widthOfAi)
        {
            int codePage = CIDEncoder.Create(encoding).CodePage;
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), fontName, "F1", codePage, encoding);

            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be(encoding);
            font.MeasureText("Ai", 1000).Should().Be(widthOfAi);
        }

        [Fact]
        public void Font_WritesTheNameOfTheCMapAsTheCMapSpellsIt()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "SimSun", "F1", 936, "gbk-euc-h");

            ((HpdfName)font.Dict["Encoding"]).Value.Should().Be("GBK-EUC-H");
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
