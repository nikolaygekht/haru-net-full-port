using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Text annotation - creates sticky notes. The viewer draws the icon; the annotation has no
    /// appearance stream.
    /// </summary>
    public class HpdfTextAnnotation : HpdfMarkupAnnotation
    {
        private static readonly string[] IconNames = new[]
        {
            "Comment",
            "Key",
            "Note",
            "Help",
            "NewParagraph",
            "Paragraph",
            "Insert"
        };

        /// <summary>
        /// Checks the arguments before the base constructor registers the annotation object,
        /// so invalid arguments leave nothing behind in the document.
        /// </summary>
        private static HpdfRect ValidateArguments(HpdfRect rect, string text, HpdfAnnotationIcon icon)
        {
            if (string.IsNullOrEmpty(text))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null or empty");

            if ((int)icon < 0 || (int)icon >= IconNames.Length)
                throw new HpdfException(HpdfErrorCode.AnnotInvalidIcon, $"Invalid annotation icon: {icon}");

            return rect;
        }

        /// <summary>
        /// Creates a new text annotation (use <see cref="Doc.HpdfPage.CreateTextAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="text">The text content of the annotation.</param>
        /// <param name="icon">The icon to display (default: Note).</param>
        internal HpdfTextAnnotation(HpdfXref xref, HpdfRect rect, string text,
            HpdfAnnotationIcon icon = HpdfAnnotationIcon.Note)
            : base(xref, HpdfAnnotationType.Text, ValidateArguments(rect, text, icon))
        {
            _dict.Add("Contents", new HpdfString(text));
            _dict.Add("Name", new HpdfName(IconNames[(int)icon]));
            Icon = icon;
        }

        /// <summary>
        /// Sets whether the annotation is initially open.
        /// </summary>
        /// <param name="open">True to show the annotation open initially.</param>
        public void SetOpened(bool open)
        {
            _dict["Open"] = new HpdfBoolean(open);
        }

        /// <summary>
        /// Gets the icon the viewer shows for the note.
        /// </summary>
        public HpdfAnnotationIcon Icon { get; private set; }

        /// <summary>
        /// Sets the icon the viewer shows for the note (/Name, HPDF_TextAnnot_SetIcon).
        /// </summary>
        /// <param name="icon">The icon.</param>
        public void SetIcon(HpdfAnnotationIcon icon)
        {
            if ((int)icon < 0 || (int)icon >= IconNames.Length)
                throw new HpdfException(HpdfErrorCode.AnnotInvalidIcon, $"Invalid annotation icon: {icon}");
            _dict["Name"] = new HpdfName(IconNames[(int)icon]);
            Icon = icon;
        }
    }
}
