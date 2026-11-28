/*
 * Demonstrates page thumbnails (/Thumb): every page carries a thumbnail that does not look like the page (a colored
 * checkerboard), so the page panel of a viewer shows whether it uses embedded thumbnails or draws its own
 */

using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class ThumbnailDemo
    {
        private const int ThumbWidth = 106;
        private const int ThumbHeight = 150;

        private static readonly (string Name, byte R, byte G, byte B)[] Colors =
        {
            ("red", 220, 40, 40), ("green", 40, 160, 60), ("blue", 40, 80, 220), ("orange", 240, 150, 20)
        };

        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                pdf.Catalog.SetPageMode(HpdfPageMode.UseThumbs);   // open the page panel

                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");

                for (int i = 0; i < Colors.Length; i++)
                {
                    var page = pdf.AddPage();
                    float top = page.Height;

                    page.SetFontAndSize(boldFont, 48);
                    page.TextOut(50, top - 120, $"Page {i + 1}");

                    page.SetFontAndSize(font, 12);
                    page.TextOut(50, top - 170, $"The embedded thumbnail of this page is a {Colors[i].Name} checkerboard.");
                    page.TextOut(50, top - 190, "If the page panel of the viewer shows checkerboards, the viewer uses the");
                    page.TextOut(50, top - 205, "thumbnails embedded in the file (/Thumb); if it shows this text, it draws");
                    page.TextOut(50, top - 220, "its own thumbnails and ignores the embedded ones.");

                    var thumbnail = HpdfImage.LoadRawImageFromMem(pdf.Xref, $"Thumb{i + 1}",
                        Checkerboard(Colors[i].R, Colors[i].G, Colors[i].B), ThumbWidth, ThumbHeight,
                        HpdfColorSpace.DeviceRgb, 8);
                    page.SetThumbnail(thumbnail);
                }

                pdf.SaveToFile("pdfs/ThumbnailDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in ThumbnailDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// RGB pixels of a checkerboard of the color and white, 8 x 11 squares.
        /// </summary>
        private static byte[] Checkerboard(byte r, byte g, byte b)
        {
            var pixels = new byte[ThumbWidth * ThumbHeight * 3];
            for (int y = 0; y < ThumbHeight; y++)
            {
                for (int x = 0; x < ThumbWidth; x++)
                {
                    bool colored = ((x * 8 / ThumbWidth) + (y * 11 / ThumbHeight)) % 2 == 0;
                    int offset = (y * ThumbWidth + x) * 3;
                    pixels[offset] = colored ? r : (byte)255;
                    pixels[offset + 1] = colored ? g : (byte)255;
                    pixels[offset + 2] = colored ? b : (byte)255;
                }
            }
            return pixels;
        }
    }
}
