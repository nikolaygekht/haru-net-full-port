using Haru.Graphics;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// Interface for generating PDF appearance stream content for form field elements.
    /// Implementations draw using the IDrawable interface, providing consistent rendering
    /// for different form element types (checkboxes, radio buttons, etc.).
    /// </summary>
    public interface IElementAppearance
    {
        /// <summary>
        /// Gets the ZapfDingbats character that represents the "on" mark of this element.
        /// It is written as the caption (/CA) of the widget's appearance characteristics,
        /// which a viewer uses when it builds the appearance itself.
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Draws the frame (background and border) shared by the "on" and "off" states.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the annotation rectangle in points.</param>
        /// <param name="height">Height of the annotation rectangle in points.</param>
        /// <param name="borderColor">The border color (RGB, 0-1), or null for no border.</param>
        /// <param name="backgroundColor">The background color (RGB, 0-1), or null for no background.</param>
        void DrawFrame(IDrawable drawable, float width, float height, float[]? borderColor, float[]? backgroundColor);

        /// <summary>
        /// Draws the "on" or "checked" state appearance using drawing primitives.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the annotation rectangle in points.</param>
        /// <param name="height">Height of the annotation rectangle in points.</param>
        void GenerateOnStateAppearance(IDrawable drawable, float width, float height);

        /// <summary>
        /// Draws the "off" or "unchecked" state appearance using drawing primitives.
        /// </summary>
        /// <param name="drawable">The drawable canvas to draw on.</param>
        /// <param name="width">Width of the annotation rectangle in points.</param>
        /// <param name="height">Height of the annotation rectangle in points.</param>
        void GenerateOffStateAppearance(IDrawable drawable, float width, float height);
    }
}
