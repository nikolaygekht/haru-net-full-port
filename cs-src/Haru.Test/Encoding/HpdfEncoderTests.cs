using AwesomeAssertions;
using Xunit;
using Haru.Encoding;
using Haru.Objects;

namespace Haru.Test.Encoding
{
    /// <summary>
    /// Tests for the PDF encoder system.
    /// </summary>
    public class HpdfEncoderTests
    {
        [Fact]
        public void WinAnsiEncoding_Should_Be_Available()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding");

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be("WinAnsiEncoding");
            encoder.BaseEncodingName.Should().Be("WinAnsiEncoding");
            encoder.HasDifferences.Should().BeFalse();
        }

        [Fact]
        public void WinAnsiEncoding_Should_Map_ASCII_Correctly()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")!;

            // Test basic ASCII characters
            encoder.GetUnicode(0x41).Should().Be((ushort)0x0041);  // 'A'
            encoder.GetUnicode(0x61).Should().Be((ushort)0x0061);  // 'a'
            encoder.GetUnicode(0x30).Should().Be((ushort)0x0030);  // '0'
            encoder.GetUnicode(0x20).Should().Be((ushort)0x0020);  // space

            // Test reverse mapping
            encoder.GetByteValue('A')!.Value.Should().Be((byte)0x41);
            encoder.GetByteValue('a')!.Value.Should().Be((byte)0x61);
            encoder.GetByteValue('0')!.Value.Should().Be((byte)0x30);
            encoder.GetByteValue(' ')!.Value.Should().Be((byte)0x20);
        }

        [Fact]
        public void WinAnsiEncoding_Should_Not_Create_Differences_Array()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")!;

            var differences = encoder.CreateDifferencesArray();
            differences.Should().BeNull();
        }

        [Theory]
        [InlineData("CP1250")]  // Central European
        [InlineData("CP1251")]  // Cyrillic
        [InlineData("CP1252")]  // Western European (same as WinAnsi)
        [InlineData("CP1253")]  // Greek
        [InlineData("CP1254")]  // Turkish
        [InlineData("CP1255")]  // Hebrew
        [InlineData("CP1256")]  // Arabic
        [InlineData("CP1257")]  // Baltic
        [InlineData("CP1258")]  // Vietnamese
        public void All_CP125x_Encoders_Should_Be_Available(string encodingName)
        {
            var encoder = HpdfEncoderFactory.GetEncoder(encodingName);

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be(encodingName);
        }

        [Theory]
        [InlineData("ISO8859-2")]   // Latin-2 (Central European)
        [InlineData("ISO8859-3")]   // Latin-3 (South European)
        [InlineData("ISO8859-4")]   // Latin-4 (North European)
        [InlineData("ISO8859-5")]   // Cyrillic
        [InlineData("ISO8859-6")]   // Arabic
        [InlineData("ISO8859-7")]   // Greek
        [InlineData("ISO8859-8")]   // Hebrew
        [InlineData("ISO8859-9")]   // Turkish
        [InlineData("ISO8859-10")]  // Nordic
        [InlineData("ISO8859-11")]  // Thai
        [InlineData("ISO8859-13")]  // Baltic Rim
        [InlineData("ISO8859-14")]  // Celtic
        [InlineData("ISO8859-15")]  // Western European with Euro
        [InlineData("ISO8859-16")]  // South-Eastern European
        public void All_ISO8859_Encoders_Should_Be_Available(string encodingName)
        {
            var encoder = HpdfEncoderFactory.GetEncoder(encodingName);

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be(encodingName);
        }

        [Fact]
        public void KOI8R_Encoder_Should_Be_Available()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("KOI8-R");

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be("KOI8-R");
        }

        [Fact]
        public void StandardEncoding_Should_Be_Available()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("StandardEncoding");

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be("StandardEncoding");
            encoder.BaseEncodingName.Should().Be("StandardEncoding");
        }

        [Fact]
        public void MacRomanEncoding_Should_Be_Available()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("MacRomanEncoding");

            encoder.Should().NotBeNull();
            encoder!.Name.Should().Be("MacRomanEncoding");
            encoder.BaseEncodingName.Should().Be("MacRomanEncoding");
        }

        [Fact]
        public void CP1251_Should_Map_Cyrillic_Characters_Correctly()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1251")!;

            // Test Cyrillic capital А (U+0410) should be at byte 0xC0 (192)
            encoder.GetUnicode(0xC0).Should().Be((ushort)0x0410);
            encoder.GetByteValue('А')!.Value.Should().Be((byte)0xC0);

            // Test Cyrillic lowercase а (U+0430) should be at byte 0xE0 (224)
            encoder.GetUnicode(0xE0).Should().Be((ushort)0x0430);
            encoder.GetByteValue('а')!.Value.Should().Be((byte)0xE0);

            // Test some more Cyrillic characters
            encoder.GetUnicode(0xC1).Should().Be((ushort)0x0411);  // Б
            encoder.GetUnicode(0xC2).Should().Be((ushort)0x0412);  // В
            encoder.GetUnicode(0xC3).Should().Be((ushort)0x0413);  // Г
        }

        [Fact]
        public void CP1251_Should_Have_Differences_From_WinAnsi()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1251")!;

            encoder.HasDifferences.Should().BeTrue();
            encoder.BaseEncodingName.Should().Be("WinAnsiEncoding");
        }

        [Fact]
        public void CP1251_Should_Create_Valid_Differences_Array()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1251")!;

            var differences = encoder.CreateDifferencesArray();
            differences.Should().NotBeNull();
            differences!.Count.Should().BePositive();

            // The differences array should contain HpdfNumber and HpdfName objects
            bool hasNumbers = false;
            bool hasNames = false;

            foreach (var item in differences)
            {
                if (item is HpdfNumber) hasNumbers = true;
                if (item is HpdfName) hasNames = true;
            }

            hasNumbers.Should().BeTrue("the differences array should contain HpdfNumber entries for byte positions");
            hasNames.Should().BeTrue("the differences array should contain HpdfName entries for glyph names");
        }

        [Fact]
        public void Encoder_Should_Encode_Text_Correctly()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")!;

            byte[] result = encoder.EncodeText("Hello");

            result.Should().HaveCount(5);
            result[0].Should().Be((byte)'H');
            result[1].Should().Be((byte)'e');
            result[2].Should().Be((byte)'l');
            result[3].Should().Be((byte)'l');
            result[4].Should().Be((byte)'o');
        }

        [Fact]
        public void Encoder_Should_Handle_Empty_Text()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")!;

            byte[] result1 = encoder.EncodeText("");
            byte[] result2 = encoder.EncodeText(null!);

            result1.Should().NotBeNull();
            result1.Should().BeEmpty();
            result2.Should().NotBeNull();
            result2.Should().BeEmpty();
        }

        [Fact]
        public void Encoder_Should_Handle_Unmappable_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("WinAnsiEncoding")!;

            // Try to encode Cyrillic characters with WinAnsi encoder
            // Should return '?' (0x3F) for unmappable characters
            byte[] result = encoder.EncodeText("Привет");  // Russian "Hello"

            result.Should().HaveCount(6);
            // All characters should be replaced with '?' since WinAnsi doesn't support Cyrillic
            result.Should().AllSatisfy(b => b.Should().Be((byte)'?'));
        }

        [Fact]
        public void CP1251_Should_Encode_Cyrillic_Text_Correctly()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1251")!;

            // Encode "Привет" (Russian "Hello")
            byte[] result = encoder.EncodeText("Привет");

            result.Should().HaveCount(6);
            // П=0xCF, р=0xF0, и=0xE8, в=0xE2, е=0xE5, т=0xF2
            result[0].Should().Be((byte)0xCF);
            result[1].Should().Be((byte)0xF0);
            result[2].Should().Be((byte)0xE8);
            result[3].Should().Be((byte)0xE2);
            result[4].Should().Be((byte)0xE5);
            result[5].Should().Be((byte)0xF2);
        }

        [Fact]
        public void CP1255_Should_Map_Hebrew_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1255")!;

            // Test Hebrew Alef (U+05D0) should be at byte 0xE0 (224)
            encoder.GetUnicode(0xE0).Should().Be((ushort)0x05D0);

            // Test Hebrew Bet (U+05D1) should be at byte 0xE1 (225)
            encoder.GetUnicode(0xE1).Should().Be((ushort)0x05D1);
        }

        [Fact]
        public void CP1256_Should_Map_Arabic_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1256")!;

            // Test Arabic Letter Hamza (U+0621) should be at byte 0xC1 (193)
            encoder.GetUnicode(0xC1).Should().Be((ushort)0x0621);
        }

        [Fact]
        public void CP1253_Should_Map_Greek_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("CP1253")!;

            // Test Greek Capital Alpha (U+0391) should be at byte 0xC1 (193)
            encoder.GetUnicode(0xC1).Should().Be((ushort)0x0391);

            // Test Greek Capital Beta (U+0392) should be at byte 0xC2 (194)
            encoder.GetUnicode(0xC2).Should().Be((ushort)0x0392);
        }

        [Fact]
        public void ISO8859_5_Should_Map_Cyrillic_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("ISO8859-5")!;

            // ISO8859-5 has Cyrillic at different byte positions than CP1251
            // Cyrillic capital А (U+0410) should be at byte 0xB0 (176)
            encoder.GetUnicode(0xB0).Should().Be((ushort)0x0410);
        }

        [Fact]
        public void KOI8R_Should_Map_Cyrillic_Characters()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("KOI8-R")!;

            // KOI8-R uses a different mapping for Cyrillic
            // Test that Cyrillic characters are mapped (exact positions vary)
            bool foundCyrillic = false;
            for (int i = 0x80; i < 0x100; i++)
            {
                ushort unicode = encoder.GetUnicode((byte)i);
                if (unicode >= 0x0410 && unicode <= 0x044F)
                {
                    foundCyrillic = true;
                    break;
                }
            }
            foundCyrillic.Should().BeTrue("KOI8-R should map Cyrillic characters in the upper byte range");
        }

        [Fact]
        public void GetEncoder_Should_Return_Default_For_Unknown_Encoding()
        {
            var encoder = HpdfEncoderFactory.GetEncoder("UnknownEncoding");

            encoder.Should().NotBeNull();
            // Should default to WinAnsiEncoding
            encoder!.Name.Should().Be("WinAnsiEncoding");
        }

        [Fact]
        public void GetEncoder_Should_Be_Case_Insensitive()
        {
            var encoder1 = HpdfEncoderFactory.GetEncoder("CP1251");
            var encoder2 = HpdfEncoderFactory.GetEncoder("cp1251");
            var encoder3 = HpdfEncoderFactory.GetEncoder("cP1251");

            encoder1.Should().NotBeNull();
            encoder2.Should().NotBeNull();
            encoder3.Should().NotBeNull();
            encoder1!.Name.Should().Be("CP1251");
            encoder2!.Name.Should().Be("CP1251");
            encoder3!.Name.Should().Be("CP1251");
        }
    }
}
