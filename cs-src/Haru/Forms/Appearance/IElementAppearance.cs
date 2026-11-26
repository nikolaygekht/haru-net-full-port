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
