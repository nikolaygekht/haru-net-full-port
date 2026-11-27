using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Pop-up annotation (PDF 1.3): the window in which a viewer shows the text of a markup annotation,
    /// its parent. Create it with <see cref="HpdfPage.CreatePopupAnnotation"/>. The viewer draws the window
    /// itself; it has no appearance stream.
    /// </summary>
    public class HpdfPopupAnnotation : HpdfAnnotation
    {
        /// <summary>
        /// Creates a pop-up annotation for a markup annotation (use <see cref="HpdfPage.CreatePopupAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The rectangle of the pop-up window.</param>
        /// <param name="parent">The markup annotation whose text the window shows.</param>
        internal HpdfPopupAnnotation(HpdfXref xref, HpdfRect rect, HpdfMarkupAnnotation parent)
            : base(xref, HpdfAnnotationType.Popup, rect)
        {
            Parent = parent;
            _dict.Add("Parent", parent.Dict);
            _xref.RequireVersion(HpdfVersion.Version13);
        }

        /// <summary>
        /// Gets the markup annotation whose text the pop-up window shows.
        /// </summary>
        public HpdfMarkupAnnotation Parent { get; }

        /// <summary>
        /// Sets whether the pop-up window is initially open (/Open).
        /// </summary>
        /// <param name="open">True to show the window open initially.</param>
        public void SetOpened(bool open)
        {
            _dict["Open"] = new HpdfBoolean(open);
        }
    }
}
