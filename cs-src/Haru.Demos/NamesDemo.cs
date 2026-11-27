/*
 * Demonstrates the name dictionary: named destinations (links, outline items and URLs go to a chapter by name),
 * document-level JavaScript and links that run JavaScript
 */

using Haru.Annotations;
using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class NamesDemo
    {
        private static readonly string[] Chapters = { "Introduction", "Named destinations", "JavaScript" };

        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                pdf.Catalog.SetPageMode(HpdfPageMode.UseOutlines);

                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");

                // The named destinations are created first: the contents page links to the chapters before
                // their pages exist, the target of each name is set when its page is made
                var contents = pdf.CreateNamedDestination("contents");
                var chapters = new HpdfNamedDestination[Chapters.Length];
                for (int i = 0; i < Chapters.Length; i++)
                    chapters[i] = pdf.CreateNamedDestination($"chapter{i + 1}");

                // Document-level JavaScript: run by a viewer that runs JavaScript when the document is opened
                pdf.AddJavaScript("helpers",
                    "function greet() { app.alert('Hello from the document script of NamesDemo.pdf'); }");

                CreateContentsPage(pdf, font, boldFont, contents, chapters);
                for (int i = 0; i < Chapters.Length; i++)
                    CreateChapterPage(pdf, font, boldFont, i, chapters[i], contents);

                var root = pdf.GetOutlineRoot();
                root.SetOpened(true);
                root.CreateChild("Contents").SetDestination(contents);
                for (int i = 0; i < Chapters.Length; i++)
                    root.CreateChild($"{i + 1}. {Chapters[i]}").SetDestination(chapters[i]);

                pdf.SaveToFile("pdfs/NamesDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in NamesDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static void CreateContentsPage(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont,
            HpdfNamedDestination contents, HpdfNamedDestination[] chapters)
        {
            var page = pdf.AddPage();
            float top = page.Height;
            contents.SetDestination(FitDestination(page));

            DrawLabel(page, boldFont, 50, top - 60, "Name Dictionary Demo", 24);
            DrawLabel(page, font, 50, top - 85,
                "Named destinations, document JavaScript and JavaScript links", 12, 0.3f);

            DrawLabel(page, boldFont, 50, top - 130, "Contents (links to named destinations)", 14);
            float y = top - 160;
            for (int i = 0; i < Chapters.Length; i++, y -= 25)
                DrawLink(page.CreateLinkAnnotation(LinkRect(page, font, 70, y, $"{i + 1}. {Chapters[i]}"), chapters[i]));

            DrawLabel(page, font, 50, y - 10,
                "The links and the bookmarks write only the name; the name tree maps it to the page.", 10, 0.3f);
            DrawLabel(page, font, 50, y - 25,
                "A URL can open the file at a name too: NamesDemo.pdf#nameddest=chapter2", 10, 0.3f);

            y -= 70;
            DrawLabel(page, boldFont, 50, y, "JavaScript links", 14);
            DrawLabel(page, font, 50, y - 18,
                "Only viewers that run JavaScript (Adobe Acrobat / Reader) react; others ignore these links.", 10, 0.6f);
            y -= 50;
            DrawLink(page.CreateJavaScriptLinkAnnotation(
                LinkRect(page, font, 70, y, "Say hello (calls greet() of the document script)"), "greet();"));
            y -= 25;
            DrawLink(page.CreateJavaScriptLinkAnnotation(
                LinkRect(page, font, 70, y, "Go to chapter 3 by script (this.gotoNamedDest)"),
                "this.gotoNamedDest('chapter3');"));
            y -= 25;
            DrawLink(page.CreateJavaScriptLinkAnnotation(
                LinkRect(page, font, 70, y, "Show the number of pages"),
                "app.alert('This document has ' + this.numPages + ' pages.');"));
        }

        private static void CreateChapterPage(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont, int index,
            HpdfNamedDestination chapter, HpdfNamedDestination contents)
        {
            var page = pdf.AddPage();
            float top = page.Height;
            chapter.SetDestination(FitDestination(page));

            DrawLabel(page, boldFont, 50, top - 60, $"{index + 1}. {Chapters[index]}", 24);
            DrawLabel(page, font, 50, top - 90, $"This page is the target of the named destination \"{chapter.Name}\".", 12);
            DrawLink(page.CreateLinkAnnotation(LinkRect(page, font, 50, top - 130, "Back to the contents"), contents));
        }

        private static HpdfDestination FitDestination(HpdfPage page)
        {
            var destination = page.CreateDestination();
            destination.SetFit();
            return destination;
        }

        /// <summary>
        /// Draws the text of a link in blue and returns the rectangle around it.
        /// </summary>
        private static HpdfRect LinkRect(HpdfPage page, HpdfFont font, float x, float y, string text)
        {
            const float size = 12;
            DrawLabel(page, font, x, y, text, size, 0, 0, 0.8f);
            float width = font.MeasureText(text, size);
            return new HpdfRect(x - 3, y - 4, x + width + 3, y + size);
        }

        private static void DrawLink(HpdfLinkAnnotation link)
        {
            link.SetBorderStyle(HpdfBorderStyle.Solid, 0);
            link.SetHighlightMode(HpdfHighlightMode.InvertBox);
        }

        private static void DrawLabel(HpdfPage page, HpdfFont font, float x, float y, string text,
            float size = 11, float r = 0, float g = -1, float b = -1)
        {
            page.SetFontAndSize(font, size);
            page.SetRgbFill(r, g < 0 ? r : g, b < 0 ? r : b);
            page.BeginText();
            page.MoveTextPos(x, y);
            page.ShowText(text);
            page.EndText();
        }
    }
}
