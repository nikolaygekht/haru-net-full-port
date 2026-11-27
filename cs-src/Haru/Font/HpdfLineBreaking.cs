/* Line breaking of text that is written without spaces between words (Chinese, Japanese) */
using System.Collections.Generic;

namespace Haru.Font
{
    /// <summary>
    /// Tells where a line may be broken inside text that has no whitespace: Chinese and Japanese are
    /// written without spaces between words, so a line breaks between any two characters - except
    /// that some characters must not begin a line (closing brackets, full stops and commas, small
    /// kana, the prolonged sound mark) and some must not end one (opening brackets). These are the
    /// line breaking rules of Japanese typesetting (JIS X 4051, "kinsoku shori"); the characters
    /// libharu lists for the same purpose (JWW_LINE_HEAD_SJIS of hpdf_encoder_jp.c) are among them.
    /// Latin words and Korean words, which are separated by spaces, are never broken here.
    /// </summary>
    internal static class HpdfLineBreaking
    {
        // Characters that must not begin a line
        private static readonly HashSet<int> _notAtLineStart = ToSet(
            // Closing brackets and quotation marks
            "」』）］｝〕〉》】〗〙〛｠｣＞≫" +
            ")]}’”»" +
            // Full stops, commas and other punctuation that closes a phrase
            "、。，．・：；？！｡､･‼⁇⁈⁉" +
            "!,.:;?" +
            // The prolonged sound mark, iteration marks and dashes that continue what is before them
            "ーｰゝゞヽヾ々〻〜゠‐–" +
            // Small hiragana and katakana, also the half-width ones
            "ぁぃぅぇぉっゃゅょゎゕゖ" +
            "ァィゥェォッャュョヮヵヶ" +
            "ㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ" +
            "ｧｨｩｪｫｬｭｮｯ" +
            // Units and signs that follow a number
            "%％‰°′″℃");

        // Characters that must not end a line
        private static readonly HashSet<int> _notAtLineEnd = ToSet(
            // Opening brackets and quotation marks
            "「『（［｛〔〈《【〖〘〚｟｢＜≪" +
            "([{‘“«" +
            // Signs that precede a number
            "$£¥＄￡￥№");

        /// <summary>
        /// Gets whether a line may be broken between two characters that follow each other without
        /// whitespace.
        /// </summary>
        /// <param name="previous">The Unicode code point of the character that would end the line.</param>
        /// <param name="current">The Unicode code point of the character that would begin the next line.</param>
        public static bool IsBreakAllowed(int previous, int current)
        {
            // Words of scripts that separate them with spaces stay whole
            if (!IsWrittenWithoutSpaces(previous) && !IsWrittenWithoutSpaces(current))
                return false;

            return !_notAtLineEnd.Contains(previous) && !_notAtLineStart.Contains(current);
        }

        /// <summary>
        /// Gets whether a character belongs to text that is written without spaces between words, so
        /// that a line may break next to it: Chinese characters, Japanese kana, and the punctuation
        /// and full-width forms used with them. Korean (Hangul) is not among them: Korean separates
        /// words with spaces, and its words are kept whole.
        /// </summary>
        private static bool IsWrittenWithoutSpaces(int codePoint)
        {
            return (codePoint >= 0x2E80 && codePoint <= 0x2FDF)     // CJK and Kangxi radicals
                || (codePoint >= 0x3000 && codePoint <= 0x312F)     // CJK symbols and punctuation, hiragana, katakana, Bopomofo
                || (codePoint >= 0x31A0 && codePoint <= 0x31FF)     // Bopomofo extended, CJK strokes, katakana phonetic extensions
                || (codePoint >= 0x3200 && codePoint <= 0x4DBF)     // enclosed letters, compatibility, ideographs extension A
                || (codePoint >= 0x4E00 && codePoint <= 0x9FFF)     // CJK unified ideographs
                || (codePoint >= 0xF900 && codePoint <= 0xFAFF)     // compatibility ideographs
                || (codePoint >= 0xFE30 && codePoint <= 0xFE6F)     // vertical and small forms of punctuation
                || (codePoint >= 0xFF01 && codePoint <= 0xFF9F)     // full-width forms, half-width katakana
                || (codePoint >= 0xFFE0 && codePoint <= 0xFFE6)     // full-width signs
                || (codePoint >= 0x20000 && codePoint <= 0x3FFFF);  // ideographs outside the Basic Multilingual Plane
        }

        private static HashSet<int> ToSet(string characters)
        {
            var set = new HashSet<int>();
            foreach (char c in characters)
                set.Add(c);
            return set;
        }
    }
}
