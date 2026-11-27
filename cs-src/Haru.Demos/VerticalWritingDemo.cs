/*
 * Vertical writing demo: Chinese, Japanese and Korean text written from top to bottom
 * with embedded CID fonts (Identity-V) and with predefined CJK fonts (the "-V" encodings).
 */

using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;

namespace BasicDemos
{
    /// <summary>
    /// Demonstrates vertical writing. Open the PDF in several viewers to compare: the page tells
    /// what has to be seen.
    /// </summary>
    public static class VerticalWritingDemo
    {
        private const string Japanese = "日本語の縦書き、「テスト」（ー）。ABC123";
        private const string SimplifiedChinese = "简体中文竖排、《测试》（一）。ABC123";
        private const string TraditionalChinese = "繁體中文直排、「測試」（一）。ABC123";
        private const string Korean = "한국어세로쓰기、「시험」（가）。ABC123";

        // The opening of "I Am a Cat" by Natsume Soseki (1905, public domain)
        private const string Paragraphs =
            "吾輩は猫である。名前はまだ無い。\n" +
            "どこで生れたかとんと見当がつかぬ。何でも薄暗いじめじめした所で「ニャーニャー」泣いていた事だけは記憶している。" +
            "吾輩はここで始めて人間というものを見た。";

        public static void Run()
        {
            try
            {
                Console.WriteLine("Creating Vertical Writing Demo...");

                var pdf = new HpdfDocument();
                var label = pdf.GetFont("Helvetica");

                // An embedded font is loaded for one writing mode: the same file once for each
                var japanese = Load(pdf, "JV", "demo/ttfont/noto-jp.ttf", 932, HpdfWritingMode.Vertical);
                var japaneseHorizontal = Load(pdf, "JH", "demo/ttfont/noto-jp.ttf", 932, HpdfWritingMode.Horizontal);
                var simplified = Load(pdf, "SV", "demo/ttfont/noto-chs.ttf", 936, HpdfWritingMode.Vertical);
                var simplifiedHorizontal = Load(pdf, "SH", "demo/ttfont/noto-chs.ttf", 936, HpdfWritingMode.Horizontal);
                var traditional = Load(pdf, "TV", "demo/ttfont/noto-cht.ttf", 950, HpdfWritingMode.Vertical);
                var traditionalHorizontal = Load(pdf, "TH", "demo/ttfont/noto-cht.ttf", 950, HpdfWritingMode.Horizontal);
                var korean = Load(pdf, "KV", "demo/ttfont/noto-kr.ttf", 949, HpdfWritingMode.Vertical);
                var koreanHorizontal = Load(pdf, "KH", "demo/ttfont/noto-kr.ttf", 949, HpdfWritingMode.Horizontal);

                // ---------- page 1: one column per language ----------
                var page = pdf.AddPage();
                Title(page, label, "Vertical Writing: Embedded Fonts",
                    "HpdfCIDFont.LoadFromTrueTypeFile(pdf, name, file, codePage, HpdfWritingMode.Vertical)");

                var samples = new (string Language, string Text, HpdfFont Vertical, HpdfFont Horizontal)[]
                {
                    ("Japanese", Japanese, japanese, japaneseHorizontal),
                    ("Simplified Chinese", SimplifiedChinese, simplified, simplifiedHorizontal),
                    ("Traditional Chinese", TraditionalChinese, traditional, traditionalHorizontal),
                    ("Korean", Korean, korean, koreanHorizontal),
                };

                float y = page.Height - 110;
                foreach (var sample in samples)
                {
                    page.BeginText();
                    page.SetFontAndSize(label, 9);
                    page.TextOut(50, y, sample.Language + ", horizontal:");
                    page.SetFontAndSize(sample.Horizontal, 14);
                    page.TextOut(200, y, sample.Text);
                    page.EndText();
                    y -= 22;
                }

                const float columnSize = 20;
                float columnTop = y - 30;
                for (int i = 0; i < samples.Length; i++)
                {
                    float x = 110 + i * 125;
                    Column(page, label, samples[i].Vertical, samples[i].Language, samples[i].Text, x, columnTop, columnSize);
                }

                Notes(page, label, 110,
                    "What to check: every column starts at the red cross, which is the text position: the top of the first",
                    "character, in the middle of its width. The gray line beside a column is as long as MeasureText says.",
                    "Brackets and the long vowel mark are rotated; commas and full stops sit in the upper right corner",
                    "(the Traditional Chinese font keeps them in the middle). Latin letters and digits stay upright.",
                    "Select and copy a column: the text comes out as it was written, in reading order.");

                // ---------- page 2: TextRect and character spacing ----------
                page = pdf.AddPage();
                Title(page, label, "Vertical Writing: TextRect and Character Spacing",
                    "Columns run from the top of the rectangle down, from its right edge to the left");

                var alignments = new[]
                {
                    HpdfTextAlignment.Left, HpdfTextAlignment.Center, HpdfTextAlignment.Right, HpdfTextAlignment.Justify,
                };
                const float boxTop = 700;
                const float boxBottom = 430;
                for (int i = 0; i < alignments.Length; i++)
                {
                    float left = 50 + i * 128;
                    float right = left + 112;

                    Frame(page, left, boxBottom, right, boxTop);

                    page.BeginText();
                    page.SetFontAndSize(japanese, 13);
                    page.SetTextLeading(19);  // the distance between the columns
                    int printed = page.TextRect(left, boxTop, right, boxBottom, Paragraphs, alignments[i]);
                    page.SetFontAndSize(label, 9);
                    page.TextOut(left, boxTop + 8, "HpdfTextAlignment." + alignments[i]);
                    page.TextOut(left, boxBottom - 12, $"printed {printed} of {Paragraphs.Length} characters");
                    page.EndText();
                }

                // Character spacing acts along the column; the spacing is added upwards, so a negative
                // value moves the characters apart
                float[] spacings = { 0, -6, 3 };
                const string spaced = "文字間隔のテスト。";
                for (int i = 0; i < spacings.Length; i++)
                {
                    float x = 90 + i * 150;
                    const float top = 370;

                    Cross(page, x, top);
                    page.BeginText();
                    page.SetFontAndSize(japanese, 16);
                    page.SetCharSpace(spacings[i]);
                    page.TextOut(x, top, spaced);
                    page.SetCharSpace(0);
                    page.SetFontAndSize(label, 9);
                    page.TextOut(x - 30, top + 12, $"SetCharSpace({spacings[i]})");
                    page.EndText();
                }

                Notes(page, label, 150,
                    "What to check: in every frame the first column is at the right edge and the columns follow to the left.",
                    "Left: columns start at the top. Right: columns end at the bottom. Center: columns are centered.",
                    "Justify: full columns are stretched over the height; the last column of a paragraph is not.",
                    "The line feed in the text starts a new column (the second one). No column leaves its frame.",
                    "Character spacing: 0 is the normal column, -6 moves the characters apart, 3 brings them closer.");

                // ---------- page 3: predefined fonts ----------
                page = pdf.AddPage();
                Title(page, label, "Vertical Writing: Predefined Fonts (Not Embedded)",
                    "HpdfCIDFontType0.Create(pdf, fontName, name, \"...-V\")");

                var predefined = new (string Font, string Encoding, string Text)[]
                {
                    ("MS-Mincho", "90ms-RKSJ-V", Japanese),
                    ("SimSun", "GBK-EUC-V", SimplifiedChinese),
                    ("MingLiU", "ETen-B5-V", TraditionalChinese),
                    ("BatangChe", "KSCms-UHC-HW-V", Korean),
                };

                // The font objects are used until the document is saved
                var predefinedFonts = new List<HpdfCIDFontType0>();
                try
                {
                    for (int i = 0; i < predefined.Length; i++)
                    {
                        var font = HpdfCIDFontType0.Create(pdf, predefined[i].Font, "P" + i, predefined[i].Encoding);
                        predefinedFonts.Add(font);

                        float x = 110 + i * 125;
                        Column(page, label, font.AsFont(), predefined[i].Font + " / " + predefined[i].Encoding,
                            predefined[i].Text, x, page.Height - 140, columnSize);
                    }

                    Notes(page, label, 110,
                        "These fonts are not in the file: the viewer supplies them (Adobe Acrobat may ask for its Asian font",
                        "pack; other viewers substitute a font they have). The columns have to look like those of page 1:",
                        "the same positions, rotated brackets, punctuation in the corner.");

                    pdf.SaveToFile("pdfs/VerticalWritingDemo.pdf");
                }
                finally
                {
                    foreach (HpdfCIDFontType0 font in predefinedFonts)
                        font.Dispose();
                }


                Console.WriteLine("Vertical Writing Demo PDF created: pdfs/VerticalWritingDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in VerticalWritingDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static HpdfFont Load(HpdfDocument pdf, string name, string file, int codePage, HpdfWritingMode writingMode)
        {
            return HpdfCIDFont.LoadFromTrueTypeFile(pdf, name, file, codePage, writingMode).AsFont();
        }

        /// <summary>
        /// Shows one column of vertical text with a cross at its text position, a line as long as
        /// the measured height of the text, and a caption.
        /// </summary>
        private static void Column(HpdfPage page, HpdfFont label, HpdfFont font, string caption, string text,
            float x, float top, float size)
        {
            float height = font.MeasureText(text, size);

            // The measured height, beside the column
            page.SetLineWidth(0.5f);
            page.SetRgbStroke(0.6f, 0.6f, 0.6f);
            page.MoveTo(x + size * 0.75f, top);
            page.LineTo(x + size * 0.75f, top - height);
            page.Stroke();

            Cross(page, x, top);

            page.BeginText();
            page.SetFontAndSize(font, size);
            page.TextOut(x, top, text);
            page.SetFontAndSize(label, 8);
            page.TextOut(x - 40, top + 14, caption);
            page.TextOut(x - 40, top - height - 12, $"{height:0} pt = {text.Length} x {size:0} pt");
            page.EndText();
        }

        private static void Cross(HpdfPage page, float x, float y)
        {
            page.SetLineWidth(0.5f);
            page.SetRgbStroke(1, 0, 0);
            page.MoveTo(x - 7, y);
            page.LineTo(x + 7, y);
            page.MoveTo(x, y - 7);
            page.LineTo(x, y + 7);
            page.Stroke();
        }

        private static void Frame(HpdfPage page, float left, float bottom, float right, float top)
        {
            page.SetLineWidth(0.5f);
            page.SetRgbStroke(1, 0, 0);
            page.Rectangle(left, bottom, right - left, top - bottom);
            page.Stroke();
        }

        private static void Title(HpdfPage page, HpdfFont label, string title, string subtitle)
        {
            page.BeginText();
            page.SetFontAndSize(label, 18);
            page.TextOut(50, page.Height - 60, title);
            page.SetFontAndSize(label, 10);
            page.TextOut(50, page.Height - 80, subtitle);
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
