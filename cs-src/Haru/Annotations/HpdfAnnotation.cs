using Haru.Font;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Base class for all PDF annotations.
    /// </summary>
    public class HpdfAnnotation
    {
        private static readonly string[] AnnotationTypeNames = new[]
        {
            "Text",
            "Link",
            "Sound",
            "FreeText",
            "Stamp",
            "Square",
            "Circle",
            "StrikeOut",
            "Highlight",
            "Underline",
            "Ink",
            "FileAttachment",
            "Popup",
            "3D",
            "Squiggly",
            "Line",
            "Projection",
            "Widget"
        };

        protected readonly HpdfDict _dict;
        protected readonly HpdfXref _xref;
        protected readonly HpdfAnnotationType _type;

        /// <summary>
        /// Gets the underlying dictionary object.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the annotation type.
        /// </summary>
        public HpdfAnnotationType Type => _type;

        /// <summary>
        /// Creates a new annotation.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type">The annotation type.</param>
        /// <param name="rect">The annotation rectangle.</param>
        protected HpdfAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            _type = type;
            _dict = new HpdfDict();

            // Add to xref
            _xref.Add(_dict);

            _dict.Add("Type", new HpdfName("Annot"));
            _dict.Add("Subtype", new HpdfName(AnnotationTypeNames[(int)type]));
            SetRect(rect);
        }

        /// <summary>
        /// Gets the annotation rectangle (/Rect), normalized so that left &lt; right and bottom &lt; top.
        /// </summary>
        internal HpdfRect Rect { get; private set; }

        /// <summary>
        /// Sets the annotation rectangle (/Rect), normalized so that left &lt; right and bottom &lt; top.
        /// </summary>
        private protected void SetRect(HpdfRect rect)
        {
            if (rect.Top < rect.Bottom)
                (rect.Top, rect.Bottom) = (rect.Bottom, rect.Top);
            if (rect.Right < rect.Left)
                (rect.Right, rect.Left) = (rect.Left, rect.Right);

            Rect = rect;
            _dict["Rect"] = ToArray(rect);
        }

        /// <summary>
        /// Creates the array [left bottom right top] of a rectangle.
        /// </summary>
        private protected static HpdfArray ToArray(HpdfRect rect)
        {
            var array = new HpdfArray();
            array.Add(new HpdfReal(rect.Left));
            array.Add(new HpdfReal(rect.Bottom));
            array.Add(new HpdfReal(rect.Right));
            array.Add(new HpdfReal(rect.Top));
            return array;
        }

        /// <summary>
        /// Brings the annotation's objects up to date before the document is written, such as a generated
        /// appearance stream. Called by <see cref="Doc.HpdfDocument.Save(System.IO.Stream)"/> on every save,
        /// before the objects are written (and before the fonts write their subsets); it may run any number of
        /// times and must leave the same objects for an unchanged annotation.
        /// </summary>
        internal virtual void PrepareForWriting(HpdfAnnotationWriteContext context)
        {
        }

        /// <summary>
        /// Gets the font that the default appearance string (/DA) of the annotation names, or null; it belongs
        /// in the default resources (/DR) of the document's interactive form, if the document has one.
        /// </summary>
        internal virtual HpdfFont? DefaultAppearanceFont => null;

        /// <summary>
        /// Gets the border style set by <see cref="SetBorderStyle"/> (solid by default).
        /// </summary>
        internal HpdfBorderStyle BorderStyle { get; private set; } = HpdfBorderStyle.Solid;

        /// <summary>
        /// Gets the border width set by <see cref="SetBorderStyle"/> (1 by default, as /Border [0 0 1]).
        /// </summary>
        internal float BorderWidth { get; private set; } = 1f;

        /// <summary>
        /// Gets the dash and gap lengths of a dashed border.
        /// </summary>
        internal (ushort On, ushort Off) BorderDash { get; private set; } = (3, 3);

        /// <summary>
        /// Sets the border style for the annotation.
        /// </summary>
        /// <param name="style">The border style.</param>
        /// <param name="width">The border width (default: 1.0).</param>
        /// <param name="dashOn">The length of dashes for dashed border (default: 3).</param>
        /// <param name="dashOff">The length of gaps for dashed border (default: 3).</param>
        public void SetBorderStyle(HpdfBorderStyle style, float width = 1.0f, ushort dashOn = 3, ushort dashOff = 3)
        {
            if (!(width >= 0) || float.IsInfinity(width))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Border width must be a non-negative number");

            if (style == HpdfBorderStyle.Dashed && dashOn == 0 && dashOff == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Dash and gap lengths cannot both be zero");

            // Built completely before it is attached, so an invalid style leaves the annotation unchanged
            var bs = new HpdfDict();
            bs.Add("Type", new HpdfName("Border"));

            // Set style
            switch (style)
            {
                case HpdfBorderStyle.Solid:
                    bs.Add("S", new HpdfName("S"));
                    break;
                case HpdfBorderStyle.Dashed:
                    bs.Add("S", new HpdfName("D"));
                    var dashArray = new HpdfArray();
                    dashArray.Add(new HpdfNumber(dashOn));
                    dashArray.Add(new HpdfNumber(dashOff));
                    bs.Add("D", dashArray);
                    break;
                case HpdfBorderStyle.Beveled:
                    bs.Add("S", new HpdfName("B"));
                    break;
                case HpdfBorderStyle.Inset:
                    bs.Add("S", new HpdfName("I"));
                    break;
                case HpdfBorderStyle.Underlined:
                    bs.Add("S", new HpdfName("U"));
                    break;
                default:
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Invalid border style");
            }

            if (width != 1.0f)
                bs.Add("W", new HpdfReal(width));

            // Assigned rather than added: a later call replaces the border style
            _dict["BS"] = bs;
            BorderStyle = style;
            BorderWidth = width;
            BorderDash = (dashOn, dashOff);
        }

        /// <summary>
        /// Gets or sets the annotation flags (/F): whether the annotation is printed, hidden, locked, ...
        /// <see cref="HpdfAnnotationFlags.None"/> removes the entry.
        /// </summary>
        public HpdfAnnotationFlags Flags
        {
            get => _dict.TryGetValue("F", out var value) && value is HpdfNumber number
                ? (HpdfAnnotationFlags)number.Value
                : HpdfAnnotationFlags.None;
            set
            {
                if (((int)value & ~AllFlags) != 0)
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid annotation flags: {value}");

                if (value == HpdfAnnotationFlags.None)
                    _dict.Remove("F");
                else
                    _dict["F"] = new HpdfNumber((int)value);
            }
        }

        private const int AllFlags = 1023;

        /// <summary>
        /// Gets the color of the annotation (/C), or null when none was set.
        /// </summary>
        internal HpdfAnnotationColor? Color { get; private set; }

        /// <summary>
        /// Sets the RGB color for the annotation.
        /// </summary>
        /// <param name="r">Red component (0.0 - 1.0).</param>
        /// <param name="g">Green component (0.0 - 1.0).</param>
        /// <param name="b">Blue component (0.0 - 1.0).</param>
        public void SetRgbColor(float r, float g, float b) => SetColor(HpdfAnnotationColor.Rgb(r, g, b));

        /// <summary>
        /// Sets the CMYK color for the annotation.
        /// </summary>
        /// <param name="c">Cyan component (0.0 - 1.0).</param>
        /// <param name="m">Magenta component (0.0 - 1.0).</param>
        /// <param name="y">Yellow component (0.0 - 1.0).</param>
        /// <param name="k">Black component (0.0 - 1.0).</param>
        public void SetCmykColor(float c, float m, float y, float k) => SetColor(HpdfAnnotationColor.Cmyk(c, m, y, k));

        /// <summary>
        /// Sets a gray color for the annotation.
        /// </summary>
        /// <param name="gray">Gray level (0.0 = black, 1.0 = white).</param>
        public void SetGrayColor(float gray) => SetColor(HpdfAnnotationColor.Gray(gray));

        /// <summary>
        /// Makes the annotation's color transparent (an empty /C array): the elements painted with
        /// the color, such as the border of a square or a line, are not painted.
        /// </summary>
        public void SetNoColor() => SetColor(HpdfAnnotationColor.Transparent);

        /// <summary>
        /// Sets the color (/C); a later call replaces it.
        /// </summary>
        internal void SetColor(HpdfAnnotationColor color)
        {
            Color = color;
            _dict["C"] = color.ToArray();
        }
    }
}
