using System;
using Haru.Doc;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Base class of the markup annotations (comments): text notes, free text, lines, squares and
    /// circles, text markup and stamps. A markup annotation has an author (title), a subject,
    /// a creation date, an opacity and an optional pop-up window that shows its text.
    /// </summary>
    public abstract class HpdfMarkupAnnotation : HpdfAnnotation
    {
        /// <summary>
        /// Creates a markup annotation.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type">The annotation type.</param>
        /// <param name="rect">The annotation rectangle.</param>
        private protected HpdfMarkupAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect)
            : base(xref, type, rect)
        {
        }

        /// <summary>
        /// Creates a markup annotation that draws its own appearance stream: it is printed by default
        /// (/F 4), as viewers create comments.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type">The annotation type.</param>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="minimumVersion">The PDF version that introduced the annotation type.</param>
        private protected HpdfMarkupAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect, HpdfVersion minimumVersion)
            : base(xref, type, rect)
        {
            _appearance = new HpdfAnnotationAppearance(xref);
            Flags = HpdfAnnotationFlags.Print;
            _xref.RequireVersion(minimumVersion);
        }

        private readonly HpdfAnnotationAppearance? _appearance;

        /// <summary>
        /// Gets the constant opacity of the annotation (/CA), 1 unless <see cref="SetTransparency"/> was called.
        /// </summary>
        public float Opacity { get; private set; } = 1f;

        /// <summary>
        /// Gets the pop-up annotation that shows the text of this annotation, or null
        /// (see <see cref="HpdfPage.CreatePopupAnnotation"/>).
        /// </summary>
        public HpdfPopupAnnotation? Popup { get; private set; }

        /// <summary>
        /// Gets the annotation this annotation is in reply to (or grouped with), or null
        /// (see <see cref="SetInReplyTo"/>).
        /// </summary>
        public HpdfMarkupAnnotation? InReplyTo { get; private set; }

        /// <summary>
        /// Gets how this annotation relates to <see cref="InReplyTo"/>.
        /// </summary>
        public HpdfReplyType ReplyType { get; private set; }

        /// <summary>
        /// Makes this annotation a reply to another markup annotation of the same page, or groups the two
        /// (/IRT, PDF 1.5; /RT for a group, PDF 1.6). Viewers show replies in the thread of comments of the
        /// annotation they answer. Saving fails if the two annotations are not on the same page.
        /// </summary>
        /// <param name="annotation">The annotation this one answers or is grouped with.</param>
        /// <param name="type">A reply (default) or a group.</param>
        public void SetInReplyTo(HpdfMarkupAnnotation annotation, HpdfReplyType type = HpdfReplyType.Reply)
        {
            if (annotation is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The annotation cannot be null");
            if (ReferenceEquals(annotation, this))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "An annotation cannot reply to itself");
            if (type is not (HpdfReplyType.Reply or HpdfReplyType.Group))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid reply type: {type}");

            InReplyTo = annotation;
            ReplyType = type;
            _dict["IRT"] = annotation.Dict;
            _xref.RequireVersion(HpdfVersion.Version15);

            // R (a reply) is the default
            if (type == HpdfReplyType.Group)
            {
                _dict["RT"] = new HpdfName("Group");
                _xref.RequireVersion(HpdfVersion.Version16);
            }
            else
            {
                _dict.Remove("RT");
            }
        }

        /// <summary>
        /// Sets the text of the annotation as rich text (/RC, PDF 1.5): an XHTML body with the text and its
        /// formatting, which viewers show in the pop-up window instead of the plain text (/Contents), for example
        /// <c>&lt;body xmlns="http://www.w3.org/1999/xhtml"&gt;&lt;p&gt;&lt;b&gt;Bold&lt;/b&gt; text&lt;/p&gt;&lt;/body&gt;</c>.
        /// The string is written as it is; a generated appearance stream does not use it.
        /// </summary>
        /// <param name="xhtml">The rich text.</param>
        public void SetRichText(string xhtml)
        {
            if (xhtml is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Rich text cannot be null");

            _dict["RC"] = new HpdfString(xhtml);
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Sets the title of the pop-up window (/T), by convention the name of the author.
        /// </summary>
        /// <param name="title">The title.</param>
        public void SetTitle(string title)
        {
            if (title is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Title cannot be null");

            _dict["T"] = new HpdfString(title);
        }

        /// <summary>
        /// Sets the subject of the annotation (/Subj, PDF 1.5): a short description that viewers show
        /// in the list of comments.
        /// </summary>
        /// <param name="subject">The subject.</param>
        public void SetSubject(string subject)
        {
            if (subject is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Subject cannot be null");

            _dict["Subj"] = new HpdfString(subject);
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Sets the date the annotation was created (/CreationDate, PDF 1.5). A <see cref="DateTimeKind.Utc"/>
        /// value is written as UTC (Z); a local or unspecified value with the offset of the local time zone.
        /// </summary>
        /// <param name="date">The creation date.</param>
        public void SetCreationDate(DateTime date) => SetCreationDate(HpdfDateFormat.Create(date));

        /// <summary>
        /// Sets the date the annotation was created (/CreationDate, PDF 1.5) with an explicit offset from UTC.
        /// </summary>
        /// <param name="date">The creation date.</param>
        public void SetCreationDate(DateTimeOffset date) => SetCreationDate(HpdfDateFormat.Create(date));

        private void SetCreationDate(HpdfString date)
        {
            _dict["CreationDate"] = date;
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Sets the constant opacity of the annotation (/CA, PDF 1.4), applied to everything the annotation
        /// shows on the page but not to its pop-up window. A generated appearance stream is drawn with this
        /// opacity. PDF/A-1 does not allow an opacity below 1: saving such a document fails.
        /// </summary>
        /// <param name="opacity">The opacity, from 0 (invisible) to 1 (opaque).</param>
        public void SetTransparency(float opacity)
        {
            if (!(opacity >= 0 && opacity <= 1))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Opacity must be between 0 and 1");

            Opacity = opacity;
            _dict["CA"] = new HpdfReal(opacity);
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Checks the PDF/A rules and redraws the generated appearance stream (/AP) from the annotation's
        /// current properties; the annotation rectangle may change with them (<see cref="GetAppearanceRect"/>).
        /// </summary>
        internal override void PrepareForWriting(HpdfAnnotationWriteContext context)
        {
            if (context.PdfA && Opacity < 1)
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "PDF/A-1 does not allow transparent annotations: the opacity of an annotation must be 1");

            if (_appearance is null)
                return;

            var box = GetAppearanceRect();
            SetRect(box);
            _appearance.Draw(context, Rect, Opacity, UsesMultiply(context), canvas => DrawAppearance(canvas, context));
            _dict["AP"] = _appearance.CreateAppearanceDictionary();
        }

        /// <summary>
        /// Gets the annotation rectangle that encloses the appearance: by default the rectangle the
        /// annotation was created with; annotations whose extent follows from their geometry (lines,
        /// callouts) compute it here.
        /// </summary>
        private protected virtual HpdfRect GetAppearanceRect() => Rect;

        /// <summary>
        /// Draws the appearance stream in page coordinates, inside <see cref="HpdfAnnotation.Rect"/>.
        /// </summary>
        private protected virtual void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
        }

        /// <summary>
        /// Whether the appearance is painted with the Multiply blend mode (text highlights).
        /// </summary>
        private protected virtual bool UsesMultiply(HpdfAnnotationWriteContext context) => false;

        /// <summary>
        /// Adds a font to the resources of the appearance stream being drawn.
        /// </summary>
        private protected void UseAppearanceFont(HpdfFont font) => _appearance?.UseFont(font);

        /// <summary>
        /// Connects the pop-up annotation that shows the text of this annotation (/Popup).
        /// </summary>
        internal void AttachPopup(HpdfPopupAnnotation popup)
        {
            Popup = popup;
            _dict["Popup"] = popup.Dict;
        }
    }
}
