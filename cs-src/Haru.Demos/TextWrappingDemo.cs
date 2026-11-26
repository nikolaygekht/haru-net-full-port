/*
 * Demonstrates text wrapping functionality with HpdfFont.MeasureText
 */

using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class TextWrappingDemo
    {
        public static void Run()
        {
            Console.WriteLine("Running TextWrappingDemo...");

            var document = new HpdfDocument();
            var page = document.AddPage();
            page.Width = 595;  // A4 width
            page.Height = 842; // A4 height

            var font = document.GetFont("Helvetica");
            float fontSize = 12f;
            float maxWidth = 400f;

            // Sample text that's too long to fit in maxWidth
            string longText = "This is a demonstration of the text wrapping functionality. " +
                            "The MeasureText method can now calculate how many characters fit " +
                            "within a specified width, with support for word wrapping to break " +
                            "at word boundaries rather than mid-word.";

            // Position for drawing
            float x = 50;
            float y = page.Height - 50;
            float lineHeight = fontSize * 1.5f;

            // Draw a box showing the maximum width
            page.SetRgbStroke(0.8f, 0.8f, 0.8f);
            page.SetLineWidth(1);
            page.Rectangle(x, y - 200, maxWidth, 200);
            page.Stroke();

            // Process text with word wrapping
            page.BeginText();
            page.SetFontAndSize(font, fontSize);
            page.MoveTextPos(x, y);

            int offset = 0;
            int lineCount = 0;
            string remainingText = longText;

            while (remainingText.Length > 0 && lineCount < 15)
            {
                // Measure how much text fits with word wrap
                int charCount = font.MeasureText(
                    remainingText,
                    fontSize,
                    maxWidth,
                    charSpace: 0f,
                    wordSpace: 0f,
                    wordWrap: true,
                    out float actualWidth);

                if (charCount == 0)
                    break;

                // Extract the line to display
                string line = remainingText.Substring(0, charCount).TrimEnd();

                // Display the line
                page.ShowText(line);
                page.MoveTextPos(0, -lineHeight);

                // Move to next line
                remainingText = remainingText.Substring(charCount).TrimStart();
                offset += charCount;
                lineCount++;
            }

            page.EndText();

            // Add title and info
            page.BeginText();
            var titleFont = document.GetFont("Helvetica-Bold");
            page.SetFontAndSize(titleFont, 18);
            page.MoveTextPos(50, page.Height - 30);
            page.ShowText("Text Wrapping Demo");
            page.EndText();

            page.BeginText();
            page.SetFontAndSize(font, 10);
            page.MoveTextPos(50, y - 220);
            page.ShowText($"Maximum width: {maxWidth} points");
            page.MoveTextPos(0, -15);
            page.ShowText($"Font: Helvetica, Size: {fontSize}pt");
            page.MoveTextPos(0, -15);
            page.ShowText($"Lines rendered: {lineCount}");
            page.EndText();

            // Compare with non-word-wrap mode
            y = y - 300;

            page.BeginText();
            page.SetFontAndSize(titleFont, 14);
            page.MoveTextPos(50, y);
            page.ShowText("Without Word Wrap (breaks anywhere):");
            page.EndText();

            y -= 30;
            page.SetRgbStroke(0.8f, 0.8f, 0.8f);
            page.Rectangle(x, y - 100, maxWidth, 100);
            page.Stroke();

            page.BeginText();
            page.SetFontAndSize(font, fontSize);
            page.MoveTextPos(x, y);

            remainingText = "Thisisaverylongwordthatcannotbebrokenatnormalboundaries";
            lineCount = 0;

            while (remainingText.Length > 0 && lineCount < 5)
            {
                int charCount = font.MeasureText(
                    remainingText,
                    fontSize,
                    maxWidth,
                    charSpace: 0f,
                    wordSpace: 0f,
                    wordWrap: false,  // No word wrap - break anywhere
                    out float actualWidth);

                if (charCount == 0)
                    break;

                string line = remainingText.Substring(0, charCount);
                page.ShowText(line);
                page.MoveTextPos(0, -lineHeight);

                remainingText = remainingText.Substring(charCount);
                lineCount++;
            }

            page.EndText();

            DrawTextRectPage(document, font, titleFont, longText);

            // Save the PDF
            string outputPath = "pdfs/TextWrappingDemo.pdf";
            document.SaveToFile(outputPath);

            Console.WriteLine($"TextWrappingDemo created: {outputPath}");
            Console.WriteLine("Demonstrates word wrapping and character-by-character breaking.");
            Console.WriteLine("TextWrappingDemo completed.");
        }

        /// <summary>
        /// Prints the same text with TextRect in four boxes, one per alignment.
        /// </summary>
        private static void DrawTextRectPage(HpdfDocument document, HpdfFont font, HpdfFont titleFont, string text)
        {
            var page = document.AddPage();
            page.Width = 595;
            page.Height = 842;

            page.BeginText();
            page.SetFontAndSize(titleFont, 18);
            page.MoveTextPos(50, page.Height - 40);
            page.ShowText("TextRect: text inside a rectangle");
            page.EndText();

            var alignments = new[]
            {
                (HpdfTextAlignment.Left, "Left"),
                (HpdfTextAlignment.Right, "Right"),
                (HpdfTextAlignment.Center, "Center"),
                (HpdfTextAlignment.Justify, "Justify")
            };

            float boxWidth = 230;
            float boxHeight = 130;
            for (int i = 0; i < alignments.Length; i++)
            {
                var (alignment, name) = alignments[i];
                float left = 50 + (i % 2) * (boxWidth + 35);
                float top = page.Height - 100 - (i / 2) * (boxHeight + 60);

                page.SetRgbStroke(0.8f, 0.8f, 0.8f);
                page.SetLineWidth(1);
                page.Rectangle(left, top - boxHeight, boxWidth, boxHeight);
                page.Stroke();

                page.BeginText();
                page.SetFontAndSize(titleFont, 11);
                page.MoveTextPos(left, top + 8);
                page.ShowText(name);
                page.EndText();

                page.BeginText();
                page.SetFontAndSize(font, 11);
                page.SetTextLeading(14);
                page.TextRect(left + 5, top - 5, left + boxWidth - 5, top - boxHeight + 5, text, alignment);
                page.EndText();
            }

            // A box that is too small: TextRect returns how many characters it printed
            float smallTop = page.Height - 500;
            page.Rectangle(50, smallTop - 50, boxWidth, 50);
            page.Stroke();

            page.BeginText();
            page.SetFontAndSize(font, 11);
            int printed = page.TextRect(55, smallTop - 5, 50 + boxWidth - 5, smallTop - 45, text, HpdfTextAlignment.Left);
            page.EndText();

            page.BeginText();
            page.SetFontAndSize(font, 10);
            page.MoveTextPos(50, smallTop - 70);
            page.ShowText($"Too small a box: {printed} of {text.Length} characters printed");
            page.EndText();

            // A page shown rotated by the viewer
            var rotated = document.AddPage();
            rotated.Rotate = 90;
            rotated.BeginText();
            rotated.SetFontAndSize(titleFont, 18);
            rotated.MoveTextPos(50, rotated.Height - 50);
            rotated.ShowText("This page has /Rotate 90");
            rotated.EndText();
        }
    }
}
