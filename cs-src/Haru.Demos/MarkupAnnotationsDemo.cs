/*
 * Demonstrates markup annotations: squares, circles, lines, text markup, free text, stamps and pop-ups,
 * with the appearance streams the library generates
 */

using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;

namespace BasicDemos
{
    public static class MarkupAnnotationsDemo
    {
        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);

                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
                using var japanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "J1", "demo/ttfont/noto-jp.ttf", 65001);

                CreateShapesPage(pdf, font, boldFont);
                CreateTextMarkupPage(pdf, font, boldFont);
                CreateFreeTextAndStampsPage(pdf, font, boldFont, japanese.AsFont());

                pdf.SaveToFile("pdfs/MarkupAnnotationsDemo.pdf");
                Console.WriteLine("MarkupAnnotationsDemo completed successfully!");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in MarkupAnnotationsDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static void CreateShapesPage(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont)
        {
            var page = pdf.AddPage();
            float top = page.Height;

            DrawLabel(page, boldFont, 50, top - 50, "Markup Annotations - Shapes and Lines", 22);
            DrawLabel(page, font, 50, top - 72, "Squares, circles and lines with generated appearance streams", 11, 0.4f);

            // Squares and circles
            DrawLabel(page, boldFont, 50, top - 110, "Squares and circles", 13);
            float y = top - 220;

            page.CreateSquareAnnotation(new HpdfRect(50, y, 150, y + 80), "A plain square: red border of width 1");

            var filled = page.CreateSquareAnnotation(new HpdfRect(180, y, 280, y + 80), "Filled square, thick blue border");
            filled.SetRgbColor(0, 0.3f, 0.8f);
            filled.SetInteriorRgbColor(0.85f, 0.92f, 1);
            filled.SetBorderStyle(HpdfBorderStyle.Solid, 3);

            var dashed = page.CreateCircleAnnotation(new HpdfRect(310, y, 410, y + 80), "Dashed circle");
            dashed.SetBorderStyle(HpdfBorderStyle.Dashed, 2, 6, 3);

            var transparent = page.CreateCircleAnnotation(new HpdfRect(440, y, 540, y + 80), "Filled circle, 50% opaque");
            transparent.SetRgbColor(0, 0.5f, 0);
            transparent.SetInteriorRgbColor(0.5f, 0.9f, 0.5f);
            transparent.SetTransparency(0.5f);

            DrawSmallLabel(page, font, 100, y - 14, "plain");
            DrawSmallLabel(page, font, 230, y - 14, "interior color, width 3");
            DrawSmallLabel(page, font, 360, y - 14, "dashed");
            DrawSmallLabel(page, font, 490, y - 14, "transparency 0.5");

            // Cloudy borders
            y -= 130;
            var cloud1 = page.CreateSquareAnnotation(new HpdfRect(50, y, 270, y + 90), "Cloudy square, intensity 1");
            cloud1.SetCloudEffect(1);

            var cloud2 = page.CreateCircleAnnotation(new HpdfRect(320, y - 10, 540, y + 100), "Cloudy circle, intensity 2");
            cloud2.SetCloudEffect(2);
            cloud2.SetRgbColor(0.6f, 0, 0.6f);
            cloud2.SetInteriorRgbColor(1, 0.9f, 1);

            DrawSmallLabel(page, font, 160, y - 14, "cloud effect 1");
            DrawSmallLabel(page, font, 430, y - 24, "cloud effect 2, filled");

            // Lines with every ending style
            DrawLabel(page, boldFont, 50, y - 60, "Lines and line endings", 13);
            y -= 90;
            var styles = Enum.GetValues<HpdfLineEndingStyle>();
            for (int i = 0; i < styles.Length; i++)
            {
                float x = i < 5 ? 50 : 310;
                float lineY = y - (i % 5) * 28;
                var line = page.CreateLineAnnotation(new HpdfPoint(x + 90, lineY), new HpdfPoint(x + 230, lineY),
                    $"Line ending {styles[i]}");
                line.SetLineEndingStyle(styles[i], styles[i]);
                line.SetInteriorRgbColor(1, 0.8f, 0.8f);
                DrawLabel(page, font, x, lineY - 3, styles[i].ToString(), 9);
            }

            // A dimension line with leader lines and a caption
            y -= 190;
            DrawLabel(page, boldFont, 50, y + 40, "Dimension line: leader lines and caption", 13);
            page.SetRgbStroke(0.2f, 0.2f, 0.2f);
            page.SetLineWidth(1);
            page.Rectangle(100, y - 60, 300, 40);
            page.Stroke();

            var dimension = page.CreateLineAnnotation(new HpdfPoint(100, y - 20), new HpdfPoint(400, y - 20), "300 pt");
            dimension.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow, HpdfLineEndingStyle.ClosedArrow);
            dimension.SetInteriorRgbColor(1, 0, 0);
            dimension.SetLeader(20, 6, 3);
            dimension.SetCaption(font, 10);
            dimension.SetIntent(HpdfLineIntent.Dimension);

            var arrow = page.CreateLineAnnotation(new HpdfPoint(450, y - 70), new HpdfPoint(530, y + 10), "Caption on top");
            arrow.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
            arrow.SetRgbColor(0, 0, 0.7f);
            arrow.SetCaption(font, 9, HpdfLineCaptionPosition.Top);
            arrow.SetIntent(HpdfLineIntent.Arrow);
        }

        private static void CreateTextMarkupPage(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont)
        {
            var page = pdf.AddPage();
            float top = page.Height;

            DrawLabel(page, boldFont, 50, top - 50, "Markup Annotations - Text Markup", 22);
            DrawLabel(page, font, 50, top - 72, "Highlight, underline, squiggly and strikeout over lines of text, with pop-up windows", 11, 0.4f);

            string[] paragraph =
            {
                "Text markup annotations mark text that is already on the page. Each line of the",
                "marked text is one quadrilateral, so a single annotation can cover several lines.",
                "A highlight is painted with the Multiply blend mode and the text stays readable."
            };

            float y = top - 130;
            var kinds = new[] { "Highlight", "Underline", "Squiggly", "StrikeOut" };
            foreach (string kind in kinds)
            {
                DrawLabel(page, boldFont, 50, y, kind, 12);
                y -= 22;

                var lines = new HpdfRect[paragraph.Length];
                for (int i = 0; i < paragraph.Length; i++)
                {
                    DrawLabel(page, font, 60, y, paragraph[i], 12);
                    lines[i] = TextRect(font, 12, 60, y, paragraph[i]);
                    y -= 17;
                }

                var annot = kind switch
                {
                    "Highlight" => page.CreateHighlightAnnotation(lines[0], "Highlighted for the review"),
                    "Underline" => page.CreateUnderlineAnnotation(lines[0], "Important"),
                    "Squiggly" => page.CreateSquigglyAnnotation(lines[0], "Check the wording"),
                    _ => page.CreateStrikeOutAnnotation(lines[0], "Remove these lines")
                };
                annot.SetQuadPoints(lines);
                annot.SetTitle("Reviewer");
                annot.SetSubject(kind);
                annot.SetCreationDate(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero));
                if (kind == "Underline")
                    annot.SetRgbColor(0, 0.4f, 0.9f);
                else if (kind == "Squiggly")
                    annot.SetRgbColor(0, 0.6f, 0);

                y -= 25;
            }

            // A text note with a pop-up window that is open when the document is opened
            DrawLabel(page, boldFont, 50, y, "Pop-up window", 12);
            DrawLabel(page, font, 60, y - 20, "The note below has a pop-up window that viewers open with the document.", 11);
            var note = page.CreateTextAnnotation(new HpdfRect(60, y - 60, 80, y - 40),
                "Pop-up annotations show the text of a markup annotation in a window of their own.", HpdfAnnotationIcon.Comment);
            note.SetTitle("Reviewer");
            note.SetRgbColor(1, 0.85f, 0);
            var popup = page.CreatePopupAnnotation(new HpdfRect(100, y - 140, 330, y - 40), note);
            popup.SetOpened(true);
        }

        private static void CreateFreeTextAndStampsPage(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont, HpdfFont japanese)
        {
            var page = pdf.AddPage();
            float top = page.Height;

            DrawLabel(page, boldFont, 50, top - 50, "Markup Annotations - Free Text and Stamps", 22);
            DrawLabel(page, font, 50, top - 72, "Text boxes drawn with any font of the document, callouts and rubber stamps", 11, 0.4f);

            float y = top - 200;
            page.CreateFreeTextAnnotation(new HpdfRect(50, y, 270, y + 90),
                "A plain free text annotation. The text is wrapped to the width of the box and drawn with the font " +
                "given when the annotation is created.", font, 11);

            var styled = page.CreateFreeTextAnnotation(new HpdfRect(310, y, 540, y + 90),
                "Centered blue text on a yellow background, dashed border", font, 14);
            styled.SetTextRgbColor(0, 0, 0.7f);
            styled.SetRgbColor(1, 1, 0.75f);
            styled.SetAlignment(HpdfTextAlignment.Center);
            styled.SetBorderStyle(HpdfBorderStyle.Dashed, 1.5f, 4, 2);

            // A callout pointing at a square
            y -= 150;
            page.CreateSquareAnnotation(new HpdfRect(70, y - 30, 150, y + 20), "The area the callout points at");
            var callout = page.CreateFreeTextAnnotation(new HpdfRect(310, y + 20, 540, y + 70),
                "A callout: a text box with a line that points at an area of the page", font, 11);
            callout.SetTextRgbColor(0.8f, 0, 0);
            callout.SetCallout(new HpdfPoint(150, y), new HpdfPoint(230, y + 45), new HpdfPoint(310, y + 45));
            callout.SetLineEndingStyle(HpdfLineEndingStyle.ClosedArrow);

            // Typewriter text in Japanese, with an embedded CID font
            y -= 110;
            var typewriter = page.CreateFreeTextAnnotation(new HpdfRect(50, y, 540, y + 50),
                "タイプライター注釈：埋め込みフォントで日本語のテキストも表示できます。", japanese, 14);
            typewriter.SetIntent(HpdfFreeTextIntent.TypeWriter);

            // Rubber stamps
            DrawLabel(page, boldFont, 50, y - 40, "Rubber stamps", 13);
            var names = Enum.GetValues<HpdfStampName>();
            for (int i = 0; i < names.Length; i++)
            {
                float x = 50 + (i % 3) * 165;
                float stampY = y - 95 - (i / 3) * 52;
                page.CreateStampAnnotation(new HpdfRect(x, stampY, x + 150, stampY + 40), names[i],
                    $"Stamp {names[i]}", boldFont);
            }
        }

        /// <summary>
        /// Gets the rectangle of a line of text: from the descent to the ascent of the font.
        /// </summary>
        private static HpdfRect TextRect(HpdfFont font, float size, float x, float y, string text)
        {
            float width = font.MeasureText(text, size);
            return new HpdfRect(x, y + font.GetDescent() / 1000f * size, x + width, y + font.GetAscent() / 1000f * size);
        }

        private static void DrawLabel(HpdfPage page, HpdfFont font, float x, float y, string text,
            float size = 11, float gray = 0)
        {
            page.SetFontAndSize(font, size);
            page.SetGrayFill(gray);
            page.BeginText();
            page.MoveTextPos(x, y);
            page.ShowText(text);
            page.EndText();
        }

        private static void DrawSmallLabel(HpdfPage page, HpdfFont font, float x, float y, string text)
        {
            page.SetFontAndSize(font, 8);
            page.SetGrayFill(0.3f);
            page.BeginText();
            page.MoveTextPos(x - font.MeasureText(text, 8) / 2, y);
            page.ShowText(text);
            page.EndText();
        }
    }
}
