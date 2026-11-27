using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// File attachment annotation (PDF 1.3, not in the C library): an icon on the page that opens an embedded file
    /// (/FS). Create it with <see cref="HpdfPage.CreateFileAttachmentAnnotation(HpdfRect, HpdfEmbeddedFile, string, HpdfFileAttachmentIcon)"/>.
    /// The appearance stream is generated when the document is saved: the icon drawn in the annotation color
    /// (blue until changed), as large as fits into the annotation rectangle. PDF/A-1 does not allow it.
    /// </summary>
    public class HpdfFileAttachmentAnnotation : HpdfMarkupAnnotation
    {
        private static readonly string[] IconNames = { "PushPin", "Graph", "Paperclip", "Tag" };

        private HpdfFileAttachmentIcon _icon;

        /// <summary>
        /// Creates a file attachment annotation (use <see cref="HpdfPage.CreateFileAttachmentAnnotation(HpdfRect, HpdfEmbeddedFile, string, HpdfFileAttachmentIcon)"/>).
        /// </summary>
        internal HpdfFileAttachmentAnnotation(HpdfXref xref, HpdfRect rect, HpdfEmbeddedFile file, string text,
            HpdfFileAttachmentIcon icon)
            : base(xref, HpdfAnnotationType.FileAttachment, rect, HpdfVersion.Version13)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(text);
            File = file;
            _dict.Add("FS", file.Dict);
            _dict.Add("Contents", new HpdfString(text));
            SetIcon(icon);
            SetRgbColor(0.2f, 0.3f, 0.8f);
        }

        /// <summary>
        /// Gets the embedded file the annotation opens.
        /// </summary>
        public HpdfEmbeddedFile File { get; }

        /// <summary>
        /// Gets the icon.
        /// </summary>
        public HpdfFileAttachmentIcon Icon => _icon;

        /// <summary>
        /// Sets the icon (/Name).
        /// </summary>
        /// <param name="icon">The icon.</param>
        public void SetIcon(HpdfFileAttachmentIcon icon)
        {
            ValidateIcon(icon);
            _icon = icon;
            _dict["Name"] = new HpdfName(IconNames[(int)icon]);
        }

        /// <summary>
        /// Checks an icon value (before an annotation object is created).
        /// </summary>
        internal static void ValidateIcon(HpdfFileAttachmentIcon icon)
        {
            if ((int)icon < 0 || (int)icon >= IconNames.Length)
                throw new HpdfException(HpdfErrorCode.AnnotInvalidIcon, $"Invalid file attachment icon: {icon}");
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color)
                return;

            // The icons are designed on a square of 20 units, centered in the rectangle
            float width = Rect.Right - Rect.Left;
            float height = Rect.Top - Rect.Bottom;
            float size = Math.Min(width, height);
            if (size <= 0)
                return;

            float scale = size / 20;
            canvas.GSave();
            canvas.Concat(scale, 0, 0, scale, Rect.Left + (width - size) / 2, Rect.Bottom + (height - size) / 2);
            color.SetStroke(canvas);
            color.SetFill(canvas);
            canvas.SetLineWidth(1.5f);
            canvas.SetLineCap(HpdfLineCap.RoundEnd);
            canvas.SetLineJoin(HpdfLineJoin.RoundJoin);

            switch (_icon)
            {
                case HpdfFileAttachmentIcon.PushPin:
                    DrawPushPin(canvas);
                    break;
                case HpdfFileAttachmentIcon.Graph:
                    DrawGraph(canvas);
                    break;
                case HpdfFileAttachmentIcon.Paperclip:
                    DrawPaperclip(canvas);
                    break;
                default:
                    DrawTag(canvas);
                    break;
            }

            canvas.GRestore();
        }

        private static void DrawPushPin(IDrawable canvas)
        {
            // The needle, slanted down to the left
            canvas.MoveTo(9, 9);
            canvas.LineTo(2.5f, 2.5f);
            canvas.Stroke();

            // The head: a disk on a collar
            canvas.MoveTo(7, 9);
            canvas.LineTo(11, 13);
            canvas.LineTo(13.5f, 12.5f);
            canvas.LineTo(7.5f, 6.5f);
            canvas.ClosePath();
            canvas.FillStroke();
            canvas.Circle(14.5f, 14.5f, 4);
            canvas.FillStroke();
        }

        private static void DrawGraph(IDrawable canvas)
        {
            // Axes
            canvas.MoveTo(2.5f, 17.5f);
            canvas.LineTo(2.5f, 2.5f);
            canvas.LineTo(17.5f, 2.5f);
            canvas.Stroke();

            // Bars
            canvas.Rectangle(5, 2.5f, 2.5f, 6);
            canvas.Rectangle(9.5f, 2.5f, 2.5f, 11);
            canvas.Rectangle(14, 2.5f, 2.5f, 8);
            canvas.Fill();
        }

        private static void DrawPaperclip(IDrawable canvas)
        {
            // Nested loops of a wire
            canvas.MoveTo(12, 6);
            canvas.LineTo(12, 15);
            canvas.CurveTo(12, 17.5f, 8, 17.5f, 8, 15);
            canvas.LineTo(8, 4.5f);
            canvas.CurveTo(8, 1, 14.5f, 1, 14.5f, 4.5f);
            canvas.LineTo(14.5f, 15.5f);
            canvas.CurveTo(14.5f, 20, 5.5f, 20, 5.5f, 15.5f);
            canvas.LineTo(5.5f, 7);
            canvas.Stroke();
        }

        private static void DrawTag(IDrawable canvas)
        {
            // A label with a cut corner and a hole, turned 45 degrees
            canvas.MoveTo(2.5f, 10);
            canvas.LineTo(10, 2.5f);
            canvas.LineTo(17.5f, 10);
            canvas.LineTo(17.5f, 17.5f);
            canvas.LineTo(10, 17.5f);
            canvas.ClosePath();
            canvas.Stroke();
            canvas.Circle(14, 14, 1.5f);
            canvas.Fill();
        }
    }
}
