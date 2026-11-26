/* Draws the border and background of form field widgets into their appearance streams. */
using System;
using Haru.Doc;
using Haru.Graphics;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// Draws widget frames (background and border) with the colors of the widget's
    /// appearance characteristics (/MK). The border is one point wide, the default
    /// width of an annotation border, and lies entirely inside the widget rectangle.
    /// </summary>
    public static class HpdfWidgetFrame
    {
        /// <summary>
        /// The border width in points.
        /// </summary>
        public const float BorderWidth = 1f;

        /// <summary>
        /// Draws a rectangular frame filling the widget.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the widget in points.</param>
        /// <param name="height">Height of the widget in points.</param>
        /// <param name="borderColor">The border color (RGB, 0-1), or null for no border.</param>
        /// <param name="backgroundColor">The background color (RGB, 0-1), or null for no background.</param>
        public static void DrawBox(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
        {
            Draw(drawable, borderColor, backgroundColor, inset =>
                drawable.Rectangle(inset, inset, width - 2 * inset, height - 2 * inset));
        }

        /// <summary>
        /// Draws a circular frame centered in the widget.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the widget in points.</param>
        /// <param name="height">Height of the widget in points.</param>
        /// <param name="borderColor">The border color (RGB, 0-1), or null for no border.</param>
        /// <param name="backgroundColor">The background color (RGB, 0-1), or null for no background.</param>
        public static void DrawCircle(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
        {
            float radius = Math.Min(width, height) / 2f;
            Draw(drawable, borderColor, backgroundColor, inset =>
                drawable.Circle(width / 2f, height / 2f, radius - inset));
        }

        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="borderColor">The border color, or null.</param>
        /// <param name="backgroundColor">The background color, or null.</param>
        /// <param name="buildPath">Builds the frame path shrunk by the given inset on every side.</param>
        private static void Draw(IDrawable drawable, float[]? borderColor, float[]? backgroundColor, Action<float> buildPath)
        {
            if (borderColor is null && backgroundColor is null)
                return;

            drawable.GSave();

            // The path runs along the middle of the border line, so the line stays inside the widget
            float inset = borderColor is null ? 0 : BorderWidth / 2f;

            if (backgroundColor != null)
                drawable.SetRgbFill(backgroundColor[0], backgroundColor[1], backgroundColor[2]);

            if (borderColor != null)
            {
                drawable.SetRgbStroke(borderColor[0], borderColor[1], borderColor[2]);
                drawable.SetLineWidth(BorderWidth);
            }

            buildPath(inset);

            if (borderColor != null && backgroundColor != null)
                drawable.FillStroke();
            else if (borderColor != null)
                drawable.Stroke();
            else
                drawable.Fill();

            drawable.GRestore();
        }
    }
}
