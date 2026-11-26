using Haru.Doc;
using Haru.Graphics;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// Generates PDF appearance streams for checkbox form fields.
    /// Uses PDF path drawing commands to create a checkmark (✓) for the checked state.
    /// </summary>
    public class HpdfCheckboxAppearance : IElementAppearance
    {
        /// <summary>
        /// The ZapfDingbats check mark.
        /// </summary>
        public string Caption => "4";

        /// <summary>
        /// Draws the square box of the checkbox.
        /// </summary>
        public void DrawFrame(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor)
        {
            HpdfWidgetFrame.DrawBox(drawable, width, height, borderColor, backgroundColor);
        }

        /// <summary>
        /// Draws the checked checkbox appearance (checkmark) using drawing primitives.
        /// The checkmark is drawn in normalized coordinates and then scaled to fit the widget size.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the annotation rectangle in points.</param>
        /// <param name="height">Height of the annotation rectangle in points.</param>
        public void GenerateOnStateAppearance(IDrawable drawable, float width, float height)
        {
            // Save graphics state
            drawable.GSave();

            // Set fill color to black (RGB: 0, 0, 0)
            drawable.SetRgbFill(0, 0, 0);

            // Apply transformation matrix to scale and position the checkmark
            // Scale to 80% of widget size and position it centered
            float scaleX = width * 0.8f;
            float scaleY = height * 0.8f;
            float translateX = width * 0.3f;
            float translateY = height * 0.5f;
            drawable.Concat(scaleX, 0, 0, scaleY, translateX, translateY);

            // Draw checkmark path in normalized coordinates (0-1 range)
            // This creates a stylized checkmark using line and Bezier curve commands
            drawable.MoveTo(0, 0);
            drawable.LineTo(0.066f, -0.026f);
            drawable.LineTo(0.137f, -0.15f);
            drawable.CurveTo(0.259f, 0.081f, 0.46f, 0.391f, 0.553f, 0.461f);
            drawable.LineTo(0.604f, 0.489f);
            drawable.LineTo(0.703f, 0.492f);
            drawable.CurveTo(0.543f, 0.312f, 0.255f, -0.205f, 0.154f, -0.439f);
            drawable.LineTo(0.069f, -0.399f);
            drawable.CurveTo(0.035f, -0.293f, -0.039f, -0.136f, -0.091f, -0.057f);
            drawable.ClosePath();
            drawable.Fill();

            // Restore graphics state
            drawable.GRestore();
        }

        /// <summary>
        /// Draws the unchecked checkbox appearance: nothing besides the frame drawn by <see cref="DrawFrame"/>.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the annotation rectangle in points.</param>
        /// <param name="height">Height of the annotation rectangle in points.</param>
        public void GenerateOffStateAppearance(IDrawable drawable, float width, float height)
        {
        }
    }
}
