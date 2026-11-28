/*
 * Extension methods for HpdfPage for API compatibility
 */

using System;
using Haru.Font;
using Haru.Streams;
using Haru.Types;

namespace Haru.Doc
{
    /// <summary>
    /// Extension methods for HpdfPage providing convenience methods for API compatibility.
    /// </summary>
    public static class HpdfPageExtensions
    {
        /// <summary>
        /// Creates a destination for the current page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns>A new destination targeting this page.</returns>
        public static HpdfDestination CreateDestination(this HpdfPage page)
        {
            return new HpdfDestination(page);
        }

        /// <summary>
        /// Outputs text at the specified position (convenience method).
        /// This is equivalent to: BeginText() + MoveTextPos() + ShowText() + EndText()
        /// </summary>
        public static void TextOut(this HpdfCanvas canvas, float x, float y, string text)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            // Checked before BT is written, so a missing font leaves no open text object behind
            if (canvas.CurrentFont is null)
                throw new HpdfException(HpdfErrorCode.PageFontNotFound, "No font has been set: call SetFontAndSize first");

            canvas.BeginText();
            canvas.MoveTextPos(x, y);
            canvas.ShowText(text);
            canvas.EndText();
        }

        /// <summary>
        /// Measures the width of text in the current font and size.
        /// </summary>
        public static float TextWidth(this HpdfCanvas canvas, string text)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            if (canvas.CurrentFont is null)
                throw new HpdfException(HpdfErrorCode.PageFontNotFound, "No font has been set");

            return canvas.CurrentFont.MeasureText(text, canvas.GraphicsState.FontSize);
        }

        /// <summary>
        /// Gets the current font size.
        /// </summary>
        public static float GetCurrentFontSize(this HpdfCanvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            return canvas.GraphicsState.FontSize;
        }

        /// <summary>
        /// Gets the current RGB fill color.
        /// </summary>
        public static HpdfRgbColor GetRGBFill(this HpdfCanvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            return canvas.GraphicsState.RgbFill;
        }

        /// <summary>
        /// Gets the current RGB stroke color.
        /// </summary>
        public static HpdfRgbColor GetRGBStroke(this HpdfCanvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            return canvas.GraphicsState.RgbStroke;
        }

        /// <summary>
        /// Gets the current font.
        /// </summary>
        public static HpdfFont? GetCurrentFont(this HpdfCanvas canvas)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            return canvas.CurrentFont;
        }

        /// <summary>
        /// Sets the text matrix (convenience overload accepting 6 parameters).
        /// </summary>
        public static void SetTextMatrix(this HpdfCanvas canvas, float a, float b, float c, float d, float x, float y)
        {
            var matrix = new HpdfTransMatrix(a, b, c, d, x, y);
            canvas.SetTextMatrix(matrix);
        }

        /// <summary>
        /// Executes an XObject (typically an image) directly on the canvas.
        /// Note: You should use GSave/GRestore and set up transformation matrix before calling this.
        /// </summary>
        public static void ExecuteXObject(this HpdfCanvas canvas, HpdfImage image)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            if (image is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image cannot be null");

            // Add image to page resources
            canvas.AddImageResource(image);

            // Execute the XObject (Do operator)
            var stream = canvas.Contents.Stream;
            stream.WriteEscapedName(image.LocalName);
            stream.WriteString(" Do\n");
        }

        /// <summary>
        /// Sets the page height (convenience method for API compatibility).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="height">The height in points.</param>
        public static void SetHeight(this HpdfPage page, float height)
        {
            ArgumentNullException.ThrowIfNull(page);
            page.Height = height;
        }

        /// <summary>
        /// Sets the page width (convenience method for API compatibility).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="width">The width in points.</param>
        public static void SetWidth(this HpdfPage page, float width)
        {
            ArgumentNullException.ThrowIfNull(page);
            page.Width = width;
        }
    }
}
