namespace Haru.Annotations
{
    /// <summary>
    /// Types of PDF annotations.
    /// </summary>
    public enum HpdfAnnotationType
    {
        /// <summary>
        /// Text annotation (sticky note).
        /// </summary>
        Text = 0,

        /// <summary>
        /// Link annotation (hyperlink or internal link).
        /// </summary>
        Link,

        /// <summary>
        /// Sound annotation.
        /// </summary>
        Sound,

        /// <summary>
        /// Free text annotation.
        /// </summary>
        FreeText,

        /// <summary>
        /// Stamp annotation.
        /// </summary>
        Stamp,

        /// <summary>
        /// Square annotation.
        /// </summary>
        Square,

        /// <summary>
        /// Circle annotation.
        /// </summary>
        Circle,

        /// <summary>
        /// Strike-out annotation.
        /// </summary>
        StrikeOut,

        /// <summary>
        /// Highlight annotation.
        /// </summary>
        Highlight,

        /// <summary>
        /// Underline annotation.
        /// </summary>
        Underline,

        /// <summary>
        /// Ink annotation.
        /// </summary>
        Ink,

        /// <summary>
        /// File attachment annotation.
        /// </summary>
        FileAttachment,

        /// <summary>
        /// Popup annotation.
        /// </summary>
        Popup,

        /// <summary>
        /// 3D annotation.
        /// </summary>
        ThreeD,

        /// <summary>
        /// Squiggly underline annotation.
        /// </summary>
        Squiggly,

        /// <summary>
        /// Line annotation.
        /// </summary>
        Line,

        /// <summary>
        /// Projection annotation.
        /// </summary>
        Projection,

        /// <summary>
        /// Widget annotation (form field).
        /// </summary>
        Widget
    }

    /// <summary>
    /// Annotation icon types for text annotations.
    /// </summary>
    public enum HpdfAnnotationIcon
    {
        /// <summary>
        /// Comment icon.
        /// </summary>
        Comment = 0,

        /// <summary>
        /// Key icon.
        /// </summary>
        Key,

        /// <summary>
        /// Note icon.
        /// </summary>
        Note,

        /// <summary>
        /// Help icon.
        /// </summary>
        Help,

        /// <summary>
        /// New paragraph icon.
        /// </summary>
        NewParagraph,

        /// <summary>
        /// Paragraph icon.
        /// </summary>
        Paragraph,

        /// <summary>
        /// Insert icon.
        /// </summary>
        Insert
    }

    /// <summary>
    /// Border styles for annotations.
    /// </summary>
    public enum HpdfBorderStyle
    {
        /// <summary>
        /// Solid border.
        /// </summary>
        Solid = 0,

        /// <summary>
        /// Dashed border.
        /// </summary>
        Dashed,

        /// <summary>
        /// Beveled border.
        /// </summary>
        Beveled,

        /// <summary>
        /// Inset border.
        /// </summary>
        Inset,

        /// <summary>
        /// Underlined border.
        /// </summary>
        Underlined
    }

    /// <summary>
    /// Highlight modes (/H): how a link or a form field widget looks while it is clicked.
    /// </summary>
    public enum HpdfHighlightMode
    {
        /// <summary>
        /// No highlighting.
        /// </summary>
        NoHighlight = 0,

        /// <summary>
        /// Invert the annotation rectangle.
        /// </summary>
        InvertBox,

        /// <summary>
        /// Invert the annotation border.
        /// </summary>
        InvertBorder,

        /// <summary>
        /// Display the annotation's down appearance.
        /// </summary>
        DownAppearance
    }

    /// <summary>
    /// Line ending styles (/LE) of line annotations and free text callouts, in the order of the C library
    /// (HPDF_LineAnnotEndingStyle).
    /// </summary>
    public enum HpdfLineEndingStyle
    {
        /// <summary>
        /// No line ending.
        /// </summary>
        None = 0,

        /// <summary>
        /// A square centered on the endpoint, filled with the interior color.
        /// </summary>
        Square,

        /// <summary>
        /// A circle centered on the endpoint, filled with the interior color.
        /// </summary>
        Circle,

        /// <summary>
        /// A diamond centered on the endpoint, filled with the interior color.
        /// </summary>
        Diamond,

        /// <summary>
        /// Two short lines that form an open arrowhead pointing away from the line.
        /// </summary>
        OpenArrow,

        /// <summary>
        /// A closed arrowhead pointing away from the line, filled with the interior color.
        /// </summary>
        ClosedArrow,

        /// <summary>
        /// A short line perpendicular to the line.
        /// </summary>
        Butt,

        /// <summary>
        /// An open arrowhead pointing back along the line (reverse).
        /// </summary>
        ROpenArrow,

        /// <summary>
        /// A closed arrowhead pointing back along the line (reverse), filled with the interior color.
        /// </summary>
        RClosedArrow,

        /// <summary>
        /// A short line at 30 degrees clockwise from the perpendicular to the line.
        /// </summary>
        Slash
    }

    /// <summary>
    /// Position of the caption of a line annotation (/CP).
    /// </summary>
    public enum HpdfLineCaptionPosition
    {
        /// <summary>
        /// The caption is centered inside the line, which is broken around it.
        /// </summary>
        Inline = 0,

        /// <summary>
        /// The caption is on top of the line.
        /// </summary>
        Top
    }

    /// <summary>
    /// Intent of a line annotation (/IT): what the line is for.
    /// </summary>
    public enum HpdfLineIntent
    {
        /// <summary>
        /// An arrow (LineArrow).
        /// </summary>
        Arrow = 0,

        /// <summary>
        /// A dimension line (LineDimension).
        /// </summary>
        Dimension
    }
}
