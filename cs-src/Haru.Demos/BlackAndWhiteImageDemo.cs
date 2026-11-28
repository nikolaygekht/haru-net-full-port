/*
 * Demonstrates 1-bit (black and white) images stored with CCITT Group 4 compression: a generated Siemens star,
 * drawn from rows given top-down and bottom-up, and with both meanings of the bits (oneIsBlack)
 */

using Haru.Doc;
using Haru.Font;
using Haru.Types;

namespace BasicDemos
{
    public static class BlackAndWhiteImageDemo
    {
        private const int Size = 600;

        public static void Run()
        {
            try
            {
                var pdf = new HpdfDocument();
                pdf.SetCompressionMode(HpdfCompressionMode.All);
                var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
                var boldFont = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");

                var page = pdf.AddPage();
                float top = page.Height;
                page.SetFontAndSize(boldFont, 20);
                page.TextOut(50, top - 60, "Black and White Images (CCITT Group 4)");
                page.SetFontAndSize(font, 10);
                page.TextOut(50, top - 80, $"A {Size} x {Size} pixel image with one bit per pixel; the four copies must look the same, square top left.");

                // The star with 1 bits for the white pixels: the DeviceGray convention, 0 is black
                int stride = (Size + 7) / 8;
                byte[] zeroIsBlack = Star(stride, oneIsBlack: false);
                byte[] oneIsBlack = Star(stride, oneIsBlack: true);

                var images = new (string Caption, HpdfImage Image)[]
                {
                    ("0 bits black (oneIsBlack: false, default)",
                        HpdfImage.LoadRaw1BitImageFromMem(pdf.Xref, "Star1", zeroIsBlack, Size, Size, (uint)stride)),
                    ("1 bits black (oneIsBlack: true)",
                        HpdfImage.LoadRaw1BitImageFromMem(pdf.Xref, "Star2", oneIsBlack, Size, Size, (uint)stride,
                            oneIsBlack: true)),
                    ("rows given bottom-up (topIsFirst: false)",
                        HpdfImage.LoadRaw1BitImageFromMem(pdf.Xref, "Star3", FlipRows(zeroIsBlack, stride), Size, Size,
                            (uint)stride, topIsFirst: false)),
                    ("LoadRawImageFromMem, DeviceGray, 1 bit",
                        HpdfImage.LoadRawImageFromMem(pdf.Xref, "Star4", zeroIsBlack, Size, Size,
                            HpdfColorSpace.DeviceGray, 1)),
                };

                for (int i = 0; i < images.Length; i++)
                {
                    float x = 50 + (i % 2) * 260;
                    float y = top - 360 - (i / 2) * 300;
                    page.DrawImage(images[i].Image, x, y, 230, 230);
                    page.SetFontAndSize(font, 9);
                    page.TextOut(x, y - 15, images[i].Caption);
                    page.TextOut(x, y - 27, $"{images[i].Image.StreamObject.Stream.Size} bytes coded, {stride * Size} bytes raw");
                }

                pdf.SaveToFile("pdfs/BlackAndWhiteImageDemo.pdf");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error in BlackAndWhiteImageDemo: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// A Siemens star (36 black wedges) in a black ring with a square in the top left corner, one bit per pixel,
        /// rows from the top.
        /// </summary>
        private static byte[] Star(int stride, bool oneIsBlack)
        {
            var data = new byte[stride * Size];
            double center = (Size - 1) / 2.0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    double dx = x - center, dy = center - y;
                    double radius = Math.Sqrt(dx * dx + dy * dy);
                    double angle = Math.Atan2(dy, dx) + Math.PI;
                    bool black = (radius < 270 && (int)(angle / (Math.PI / 36)) % 2 == 0)
                        || (radius >= 280 && radius < 295)
                        || (x < 60 && y < 60);   // a square in the top left corner shows the orientation
                    if (black == oneIsBlack)
                        data[y * stride + x / 8] |= (byte)(0x80 >> (x % 8));
                }
            }
            return data;
        }

        private static byte[] FlipRows(byte[] data, int stride)
        {
            var flipped = new byte[data.Length];
            int rows = data.Length / stride;
            for (int y = 0; y < rows; y++)
                Array.Copy(data, y * stride, flipped, (rows - 1 - y) * stride, stride);
            return flipped;
        }
    }
}
