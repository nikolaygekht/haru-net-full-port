/*
 * Demonstrates form XObjects (a letterhead and a watermark drawn once, shown on every page) and shared content
 * streams (a footer drawn on the first page and inserted on the others)
 */

using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class FormXObjectsAndContentStreamsDemo
    {
        private const int PageCount = 3;

        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");

                var letterhead = CreateLetterhead(pdf, font, boldFont);
                var watermark = CreateWatermark(pdf, boldFont);
                HpdfContentStream? footer = null;

                for (int i = 1; i <= PageCount; i++)
                {
                    var page = pdf.AddPage();
                    float width = page.Width, height = page.Height;

                    // The watermark, scaled up and turned by 45 degrees around the page center
                    page.GSave();
                    page.Concat(0.7071f, 0.7071f, -0.7071f, 0.7071f, width / 2, height / 2);
                    page.DrawFormXObject(watermark, -watermark.Width, -watermark.Height / 2, watermark.Width * 2, watermark.Height);
                    page.GRestore();

                    // The letterhead at full size, and a small copy at the bottom
                    page.DrawFormXObject(letterhead, 40, height - 120);
                    page.DrawFormXObject(letterhead, width - 40 - letterhead.Width / 4, 60,
                        letterhead.Width / 4, letterhead.Height / 4);

                    page.SetFontAndSize(font, 12);
                    page.SetGrayFill(0);
                    page.TextOut(50, height - 170, $"Page {i}. The letterhead and the watermark are form XObjects:");
                    page.TextOut(50, height - 186, "drawn once, stored once in the file and shown on every page at any size.");
                    page.TextOut(50, height - 210, "The footer below is a content stream drawn on page 1 and inserted on the other pages.");

                    if (footer is null)
                    {
                        // Page 1 draws the footer into a stream of its own, then goes on in a new one
                        footer = page.NewContentStream();
                        DrawFooter(page, font);
                        page.NewContentStream();
                    }
                    else
                    {
                        page.InsertSharedContentStream(footer);
                    }
                }

                pdf.SaveToFile("pdfs/FormXObjectsAndContentStreamsDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in FormXObjectsAndContentStreamsDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static HpdfFormXObject CreateLetterhead(HpdfDocument pdf, HpdfFont font, HpdfFont boldFont)
        {
            var form = pdf.CreateFormXObject(515, 70);
            form.SetRgbFill(0.12f, 0.33f, 0.62f);
            form.Rectangle(0, 0, 515, 70);
            form.Fill();
            form.SetRgbFill(1, 1, 1);
            form.SetFontAndSize(boldFont, 26);
            form.TextOut(20, 32, "Haru.NET Example Inc.");
            form.SetFontAndSize(font, 11);
            form.TextOut(20, 14, "1 PDF Street, Port Document  -  form XObject letterhead");
            return form;
        }

        private static HpdfFormXObject CreateWatermark(HpdfDocument pdf, HpdfFont boldFont)
        {
            var form = pdf.CreateFormXObject(320, 60);
            form.SetGrayFill(0.88f);
            form.SetFontAndSize(boldFont, 44);
            form.TextOut(10, 12, "WATERMARK");
            return form;
        }

        private static void DrawFooter(HpdfPage page, HpdfFont font)
        {
            // Self-contained: the font is set and the state saved and restored inside the stream
            page.GSave();
            page.SetGrayStroke(0.5f);
            page.MoveTo(40, 45);
            page.LineTo(page.Width - 40, 45);
            page.Stroke();
            page.SetFontAndSize(font, 9);
            page.SetGrayFill(0.3f);
            page.TextOut(40, 30, "Shared footer: one content stream on every page - confidential example document");
            page.GRestore();
        }
    }
}
