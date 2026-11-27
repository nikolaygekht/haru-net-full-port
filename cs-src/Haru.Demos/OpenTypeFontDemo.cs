/*
 * OpenType fonts with CFF outlines (.otf, and a collection of them, .ttc): a simple font of a
 * Latin font with glyph names, and CID fonts of CJK fonts, horizontal and vertical.
 */

using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;

namespace BasicDemos
{
    /// <summary>
    /// Demonstrates fonts made from OpenType files with CFF (PostScript) outlines. The page tells
    /// what has to be seen; open it in several viewers to compare.
    /// </summary>
    public static class OpenTypeFontDemo
    {
        private const string Latin = "Grüße aus Köln! Æsop's façade, naïve “quotes” — 0123456789";
        private const string Japanese = "日本語のテキスト、「OpenType」フォント。ABC 123";

        // The fonts of the collection NotoSansCJK-Regular.ttc: one CFF program shared by all of them
        private static readonly (int Index, string Language, int CodePage, string Text)[] CollectionFonts =
        {
            (0, "Japanese", 932, "こんにちは世界、縦書きと横書き。"),
            (1, "Korean", 949, "안녕하세요, 세계. 한국어 글꼴."),
            (2, "Simplified Chinese", 936, "你好，世界。简体中文字体。"),
            (3, "Traditional Chinese", 950, "你好，世界。繁體中文字型。"),
        };

        public static void Run()
        {
            try
            {
                Console.WriteLine("Creating OpenType (CFF) Font Demo...");

                var pdf = new HpdfDocument();
                var label = pdf.GetFont("Helvetica");
                var page = pdf.AddPage();

                Title(page, label, "OpenType Fonts with CFF Outlines",
                    "The fonts are embedded as CFF programs (FontFile3): subsets of the glyphs in use, without subroutines");

                // ---------- a simple font: a Latin font with glyph names ----------
                float y = page.Height - 120;
                var puritan = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "Puritan", "demo/otfont/Puritan2.otf", true, 1252);
                Section(page, label, y, "Simple font (Type1 font, Type1C program): HpdfTrueTypeFont.LoadFromFile(xref, name, \"Puritan2.otf\", true, 1252)");
                Line(page, puritan.AsFont(), 16, y - 22, Latin);

                var cyrillic = HpdfTrueTypeFont.LoadFromFile(pdf.Xref, "PuritanCyr", "demo/otfont/Puritan2.otf", true, 1251);
                Section(page, label, y - 45, "The same font with code page 1251: Puritan has no Cyrillic glyphs, so the letters show its .notdef glyph");
                Line(page, cyrillic.AsFont(), 16, y - 67, "Latin text, then: Привет");

                // ---------- a CID font of a CJK OpenType file ----------
                y -= 110;
                var japanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "NotoJP", "demo/otfont/NotoSansJP-Regular.otf", 932);
                Section(page, label, y, "CID font (CIDFontType0, CIDFontType0C program): HpdfCIDFont.LoadFromTrueTypeFile(pdf, name, \"NotoSansJP-Regular.otf\", 932)");
                Line(page, japanese.AsFont(), 18, y - 24, Japanese);

                var unicode = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "PuritanU", "demo/otfont/Puritan2.otf", 65001);
                Section(page, label, y - 48, "A Latin font as a CID font (code page 65001): its program is written CID-keyed");
                Line(page, unicode.AsFont(), 16, y - 70, Latin);

                // ---------- the fonts of a collection ----------
                y -= 115;
                Section(page, label, y, "Fonts of a collection with one CFF program: HpdfCIDFont.LoadFromTrueTypeCollection(pdf, name, \"NotoSansCJK-Regular.ttc\", index, codePage)");
                y -= 24;
                foreach (var (index, language, codePage, text) in CollectionFonts)
                {
                    var font = HpdfCIDFont.LoadFromTrueTypeCollection(pdf, "CJK" + index, "demo/otfont/NotoSansCJK-Regular.ttc", index, codePage);
                    page.BeginText();
                    page.SetFontAndSize(label, 9);
                    page.TextOut(50, y + 2, $"[{index}] {language}");
                    page.SetFontAndSize(font.AsFont(), 16);
                    page.TextOut(170, y, text);
                    page.EndText();
                    y -= 26;
                }

                // ---------- vertical writing ----------
                y -= 20;
                Section(page, label, y, "Vertical writing: the vertical forms of the 'vert' feature, the origins of the VORG table");
                var verticalJapanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "NotoJPV", "demo/otfont/NotoSansJP-Regular.otf", 932, HpdfWritingMode.Vertical);
                var verticalKorean = HpdfCIDFont.LoadFromTrueTypeCollection(pdf, "CJKKRV", "demo/otfont/NotoSansCJK-Regular.ttc", 1, 949, HpdfWritingMode.Vertical);
                var verticalChinese = HpdfCIDFont.LoadFromTrueTypeCollection(pdf, "CJKSCV", "demo/otfont/NotoSansCJK-Regular.ttc", 2, 936, HpdfWritingMode.Vertical);

                float top = y - 30;
                Column(page, label, verticalJapanese.AsFont(), "Noto Sans JP (.otf)", "縦書き「テスト」。℃", 110, top);
                Column(page, label, verticalKorean.AsFont(), "Noto Sans CJK KR (.ttc)", "세로쓰기「시험」。", 260, top);
                Column(page, label, verticalChinese.AsFont(), "Noto Sans CJK SC (.ttc)", "竖排《测试》。", 410, top);

                Notes(page, label, 70,
                    "Check: every line shows its text (none shows boxes or wrong glyphs, except the Cyrillic line), the vertical columns",
                    "show rotated brackets and dashes, and the text can be copied. A viewer that cannot read the font programs shows",
                    "nothing or a substitute font: try Adobe Acrobat, a browser, MuPDF and poppler.");

                pdf.SaveToFile("pdfs/OpenTypeFontDemo.pdf");
                Console.WriteLine("OpenType font demo created: pdfs/OpenTypeFontDemo.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OpenTypeFontDemo: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private static void Title(HpdfPage page, HpdfFont label, string title, string subtitle)
        {
            page.BeginText();
            page.SetFontAndSize(label, 18);
            page.TextOut(50, page.Height - 60, title);
            page.SetFontAndSize(label, 9);
            page.TextOut(50, page.Height - 80, subtitle);
            page.EndText();
        }

        private static void Section(HpdfPage page, HpdfFont label, float y, string text)
        {
            page.BeginText();
            page.SetFontAndSize(label, 8);
            page.TextOut(50, y, text);
            page.EndText();
        }

        private static void Line(HpdfPage page, HpdfFont font, float size, float y, string text)
        {
            page.BeginText();
            page.SetFontAndSize(font, size);
            page.TextOut(50, y, text);
            page.EndText();
        }

        private static void Column(HpdfPage page, HpdfFont label, HpdfFont font, string caption, string text, float x, float top)
        {
            const float size = 20;
            page.BeginText();
            page.SetFontAndSize(label, 8);
            page.TextOut(x - 40, top + 14, caption);
            page.SetFontAndSize(font, size);
            page.TextOut(x, top, text);
            page.EndText();
        }

        private static void Notes(HpdfPage page, HpdfFont label, float top, params string[] lines)
        {
            page.BeginText();
            page.SetFontAndSize(label, 9);
            float y = top;
            foreach (string line in lines)
            {
                page.TextOut(50, y, line);
                y -= 13;
            }
            page.EndText();
        }
    }
}
