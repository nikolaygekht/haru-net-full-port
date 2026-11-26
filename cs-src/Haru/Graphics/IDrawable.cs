using Haru.Streams;
using Haru.Types;

namespace Haru.Graphics
{
    /// <summary>
    /// Interface for objects that support PDF drawing operations.
    /// This includes pages, appearance streams, patterns, and other drawable contexts.
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Gets the underlying stream to write PDF operators to.
        /// </summary>
        HpdfMemoryStream Stream { get; }

        /// <summary>
        /// Gets the current graphics state.
        /// </summary>
        HpdfGraphicsState GraphicsState { get; }

        /// <summary>
        /// Gets or sets the current position in the path.
        /// </summary>
        HpdfPoint CurrentPos { get; set; }

        /// <summary>
        /// Gets or sets whether a path is being built: set by path construction operators
        /// (m, l, c, re, ...), cleared by path painting operators (S, f, B, n, ...).
        /// An arc continues such a path with a line instead of starting a new subpath.
        /// </summary>
        bool PathInProgress { get; set; }

        /// <summary>
        /// Pushes the current graphics state onto the stack.
        /// Called internally by GSave operations.
        /// </summary>
        void PushGraphicsState();

        /// <summary>
        /// Pops the graphics state from the stack.
        /// Called internally by GRestore operations.
        /// </summary>
        void PopGraphicsState();
    }
}
