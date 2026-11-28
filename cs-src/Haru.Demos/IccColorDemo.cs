/*
 * Demonstrates ICC color: the same numbers shown as device RGB, in the built-in sRGB profile and in a wide-gamut
 * ProPhoto profile - for images, a JPEG that embeds its profile, and fill / stroke colors of paths and text
 */

using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class IccColorDemo
    {
        private static readonly (float R, float G, float B)[] Swatches =
        {
            (0.50f, 0.78f, 0.25f), (0.78f, 0.31f, 0.59f), (0.24f, 0.35f, 0.78f), (0.90f, 0.60f, 0.20f), (0.70f, 0.70f, 0.70f)
        };

        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
                var proPhoto = pdf.LoadIccProfile("demo/icc/ProPhoto-v2-micro.icc");

                var page = pdf.AddPage();
                float top = page.Height;
                Label(page, boldFont, 50, top - 55, "ICC Color", 22);
                Label(page, font, 50, top - 75,
                    "The same color numbers, interpreted three ways: device RGB, the sRGB profile, the ProPhoto profile.", 10);

                // Fill colors: rows of swatches with the same components
                float y = top - 170;
                Label(page, boldFont, 50, y + 70, "Fill colors (SetRgbFill, SetIccFill)", 13);
                SwatchRow(page, font, y, "device RGB", (r, g, b) => page.SetRgbFill(r, g, b));
                SwatchRow(page, font, y - 60, "sRGB profile", (r, g, b) => page.SetIccFill(pdf.SRgbProfile, r, g, b));
                SwatchRow(page, font, y - 120, "ProPhoto profile", (r, g, b) => page.SetIccFill(proPhoto, r, g, b));

                // Stroke and text
                y -= 190;
                Label(page, boldFont, 50, y, "Stroke and text (SetIccStroke)", 13);
                page.SetIccStroke(proPhoto, 0.78f, 0.31f, 0.59f);
                page.SetLineWidth(6);
                page.Rectangle(50, y - 60, 200, 40);
                page.Stroke();
                page.SetIccFill(proPhoto, 0.24f, 0.35f, 0.78f);
                page.SetFontAndSize(boldFont, 20);
                page.TextOut(270, y - 47, "Text in ProPhoto blue");

                // Images: the same pixels with and without a profile
                y -= 110;
                Label(page, boldFont, 50, y, "Images (JPEG files; the second embeds the ProPhoto profile)", 13);
                using var plain = pdf.LoadJpegImageFromFile("demo/icc/hues-plain.jpg");
                using var withProfile = pdf.LoadJpegImageFromFile("demo/icc/hues-prophoto.jpg");
                page.DrawImage(plain, 50, y - 70, 360, 60);
                Label(page, font, 420, y - 45, "no profile (device RGB)", 9);
                page.DrawImage(withProfile, 50, y - 140, 360, 60);
                Label(page, font, 420, y - 115,
                    $"profile from the file: {(withProfile.IccProfile is null ? "none" : withProfile.IccProfile.ComponentCount + " components, ICC " + withProfile.IccProfile.ProfileVersion)}", 9);

                Label(page, font, 50, 60,
                    "A viewer with color management shows the ProPhoto rows more saturated; device RGB and sRGB look the same.", 9);

                pdf.SaveToFile("pdfs/IccColorDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in IccColorDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        private static void SwatchRow(HpdfPage page, HpdfFont font, float y, string caption, Action<float, float, float> setFill)
        {
            for (int i = 0; i < Swatches.Length; i++)
            {
                setFill(Swatches[i].R, Swatches[i].G, Swatches[i].B);
                page.Rectangle(50 + i * 70, y, 60, 45);
                page.Fill();
            }
            Label(page, font, 420, y + 18, caption, 10);
        }

        private static void Label(HpdfPage page, HpdfFont font, float x, float y, string text, float size)
        {
            page.SetGrayFill(0);
            page.SetFontAndSize(font, size);
            page.BeginText();
            page.MoveTextPos(x, y);
            page.ShowText(text);
            page.EndText();
        }
    }
}
