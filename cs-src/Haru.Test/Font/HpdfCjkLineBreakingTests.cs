using System;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;
using Xunit;

namespace Haru.Test.Font
{
    /// <summary>
    /// Tests of line breaking in text written without spaces: Chinese and Japanese text breaks between
    /// characters, but not before a character that must not begin a line and not after one that must
    /// not end a line; Latin and Korean words stay whole.
    /// </summary>
    public class HpdfCjkLineBreakingTests
    {
        // With the predefined fixed-pitch fonts at size 10 a full-width character is 10 wide and a
        // Latin letter, a digit or a space 5
        private const float Size = 10;

        // The opening of "I Am a Cat" by Natsume Soseki
        private const string Paragraph =
            "吾輩は猫である。名前はまだ無い。" +
            "どこで生れたかとんと見当がつかぬ。" +
            "何でも薄暗いじめじめした所で「ニャー" +
            "ニャー」泣いていた事だけは記憶している。";

        private const string NotAtLineStart = "。、」）ーャっ";
        private const string NotAtLineEnd = "「（";

        [Theory]
        // Between any two characters
        [InlineData("日本語のテキスト", 35, 3)]
        // A full stop must not begin a line: the character before it goes to the next line with it
        [InlineData("日本語。テキスト", 35, 2)]
        [InlineData("日本語。テキスト", 45, 4)]
        [InlineData("日本語、テキスト", 35, 2)]
        // A closing bracket must not begin a line, an opening one must not end it
        [InlineData("日本「語」。テスト", 45, 2)]
        [InlineData("東京（とうきょう）", 30, 2)]
        // Small kana and the prolonged sound mark stay with the character before them
        [InlineData("ニャーニャー", 35, 3)]
        [InlineData("ニャーニャー", 25, 0)]
        // A Latin word stays whole next to Japanese text
        [InlineData("ABC日本語", 20, 3)]
        [InlineData("ABC日本語", 30, 4)]
        [InlineData("日本語English", 42, 3)]
        [InlineData("English日本語", 30, 0)]
        // A number stays with its unit
        [InlineData("数字は100%です", 47, 3)]
        public void WordWrap_BreaksJapaneseTextWhereItsRulesAllow(string text, float width, int fits)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "MS-Mincho", "F1", "90ms-RKSJ-H");

            font.AsFont().MeasureText(text, Size, width, 0, 0, true, out _).Should().Be(fits);
        }

        [Fact]
        public void WordWrap_ReturnsTheWidthOfTheLine()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "MS-Mincho", "F1", "90ms-RKSJ-H");

            // Two characters are printed: the third one moves to the next line with the full stop
            font.AsFont().MeasureText("日本語。テキスト", Size, 35, 0, 0, true, out float realWidth);

            realWidth.Should().Be(20);
        }

        [Fact]
        public void WithoutWordWrap_ALineBreaksAnywhere()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "MS-Mincho", "F1", "90ms-RKSJ-H");

            font.AsFont().MeasureText("日本語。テキスト", Size, 35, 0, 0, false, out _).Should().Be(3);
        }

        [Theory]
        [InlineData("GBK-EUC-H", "SimSun", "中文排版。测试", 45, 3)]      // Chinese, the same rules
        [InlineData("GBK-EUC-H", "SimSun", "中文《测试》", 30, 2)]
        [InlineData("ETen-B5-H", "MingLiU", "中文排版，測試", 45, 3)]
        public void WordWrap_BreaksChineseTextWhereItsRulesAllow(string encoding, string fontName, string text, float width, int fits)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), fontName, "F1", encoding);

            font.AsFont().MeasureText(text, Size, width, 0, 0, true, out _).Should().Be(fits);
        }

        [Theory]
        // Korean separates words with spaces: a line breaks at them only
        [InlineData("한국어 세로쓰기", 45, 4)]
        [InlineData("한국어세로쓰기", 45, 0)]
        // ... and next to the punctuation it shares with Chinese and Japanese
        [InlineData("한국어、세로", 35, 0)]
        [InlineData("한국어、세로", 45, 4)]
        public void WordWrap_KeepsKoreanWordsWhole(string text, float width, int fits)
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "DotumChe", "F1", "KSCms-UHC-HW-H");

            font.AsFont().MeasureText(text, Size, width, 0, 0, true, out _).Should().Be(fits);
        }

        [Fact]
        public void WordWrap_LatinText_BreaksAtWhitespaceOnly()
        {
            var font = new HpdfDocument().GetFont("Courier");   // every character is 6 wide at size 10

            font.MeasureText("ab(cd), ef", Size, 50, 0, 0, true, out _).Should().Be(8);
            font.MeasureText("abcdefghijkl", Size, 50, 0, 0, true, out _).Should().Be(0);
        }

        [Fact]
        public void WordWrap_VerticalFont_FollowsTheSameRules()
        {
            using var font = HpdfCIDFontType0.Create(new HpdfDocument(), "MS-Mincho", "F1", "90ms-RKSJ-V");

            font.AsFont().MeasureText("日本語。テキスト", Size, 35, 0, 0, true, out _).Should().Be(2);
        }

        [Fact]
        public void WordWrap_CharacterOutsideTheBasicPlane_IsACharacterOfTheText()
        {
            // An ideograph of CJK Extension B: two code units of the string, one character
            const string text = "日本\U00020BB7野家";
            var doc = new HpdfDocument();
            using var font = HpdfCIDFont.LoadFromTrueTypeFile(doc, "F1", TestFonts.DemoResource(TestFonts.Japanese), 65001);

            font.AsFont().MeasureText(text, Size, 25, 0, 0, true, out _).Should().Be(2);
            font.AsFont().MeasureText(text, Size, 35, 0, 0, true, out _).Should().Be(4);
        }

        [Fact]
        public void TextRect_JapaneseParagraph_NoLineStartsOrEndsWithAForbiddenCharacter()
        {
            var doc = new HpdfDocument();
            var page = doc.AddPage();
            using var font = HpdfCIDFontType0.Create(doc, "MS-Mincho", "F1", "90ms-RKSJ-H");

            // Ten characters to a line
            page.BeginText();
            page.SetFontAndSize(font.AsFont(), Size);
            int printed = page.TextRect(100, 700, 200, 400, Paragraph, HpdfTextAlignment.Left);
            page.EndText();

            printed.Should().Be(Paragraph.Length);
            string[] lines = ShownLines(page);
            string.Concat(lines).Should().Be(Paragraph);
            lines.Length.Should().BeGreaterThan(5);
            foreach (string line in lines)
            {
                NotAtLineStart.Should().NotContain(line[0].ToString(), "line '{0}' must not begin with it", line);
                NotAtLineEnd.Should().NotContain(line[^1].ToString(), "line '{0}' must not end with it", line);
                (line.Length * Size).Should().BeLessThanOrEqualTo(100);
            }

            // The rules did move something: with ten characters to a line the second full stop
            // would begin the third line
            lines.Take(lines.Length - 1).Should().Contain(line => line.Length < 10);
        }

        /// <summary>
        /// Gets the strings shown with the ' operator, decoded from the Shift-JIS codes of the font.
        /// </summary>
        private static string[] ShownLines(HpdfPage page)
        {
            string content = System.Text.Encoding.ASCII.GetString(page.Contents.Stream.ToArray());
            var shiftJis = System.Text.Encoding.GetEncoding(932);
            return Regex.Matches(content, @"<([0-9A-Fa-f]*)> '")
                .Select(match => shiftJis.GetString(Convert.FromHexString(match.Groups[1].Value)))
                .ToArray();
        }
    }
}
