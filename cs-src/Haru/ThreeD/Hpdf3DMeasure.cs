using System;
using Haru.Annotations;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.ThreeD
{
    /// <summary>
    /// A 3D measurement (/Type /3DMeasure, Adobe ExtensionLevel 3 and PDF 2.0): a comment note or a dimension
    /// placed in a 3D model, shown in the views it is added to (<see cref="Hpdf3DView.AddMeasure"/>) and, through
    /// a projection annotation, in the comments list (<see cref="Doc.HpdfPage.CreateProjectionAnnotation"/>).
    /// </summary>
    public abstract class Hpdf3DMeasure
    {
        private protected readonly HpdfDict _dict;

        private protected Hpdf3DMeasure(HpdfXref xref, string subtype)
        {
            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("3DMeasure"));
            _dict.Add("Subtype", new HpdfName(subtype));
            xref.Add(_dict);
            xref.RequireAdobeExtensionLevel(3);
        }

        /// <summary>
        /// Gets the measurement dictionary.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the projection annotation that shows the measurement in the comments list, or null.
        /// </summary>
        public HpdfProjectionAnnotation? ProjectionAnnotation { get; private set; }

        /// <summary>
        /// Sets the name of the measurement (/TRL), shown by viewers in their list of measurements.
        /// </summary>
        public void SetName(string name)
        {
            if (name is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Name cannot be null");
            _dict["TRL"] = new HpdfString(name);
        }

        /// <summary>
        /// Sets the color of the measurement (/C).
        /// </summary>
        public void SetColor(float r, float g, float b) => _dict["C"] = Hpdf3DView.ToColor(r, g, b);

        /// <summary>
        /// Sets the size of the measurement's text (/TS; 12 by default).
        /// </summary>
        public void SetTextSize(float size)
        {
            if (!(size > 0) || !float.IsFinite(size))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The text size must be a positive number");
            _dict["TS"] = new HpdfReal(size);
        }

        /// <summary>
        /// Connects the projection annotation that shows the measurement (/S).
        /// </summary>
        internal void AttachProjectionAnnotation(HpdfProjectionAnnotation annotation)
        {
            ProjectionAnnotation = annotation;
            _dict["S"] = annotation.Dict;
        }

        private protected static HpdfPoint3D Validate(HpdfPoint3D point, string what)
        {
            if (!Hpdf3DView.IsFinite(point))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"The {what} must have finite coordinates");
            return point;
        }
    }

    /// <summary>
    /// A comment note in a 3D model (/Subtype /3DC): a text with a leader line to an anchor point.
    /// Create it with <see cref="Hpdf3DModel.CreateCommentNote"/>.
    /// </summary>
    public class Hpdf3DCommentNote : Hpdf3DMeasure
    {
        internal Hpdf3DCommentNote(HpdfXref xref, HpdfPoint3D anchor, HpdfPoint3D textPosition)
            : base(xref, "3DC")
        {
            _dict.Add("A1", Hpdf3DView.ToArray(Validate(anchor, "anchor point")));
            _dict.Add("TP", Hpdf3DView.ToArray(Validate(textPosition, "text position")));
        }

        /// <summary>
        /// Sets the text of the note (/UT).
        /// </summary>
        public void SetText(string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            _dict["UT"] = new HpdfString(text);
        }

        /// <summary>
        /// Sets the size of the box of the text (/TB).
        /// </summary>
        public void SetTextBoxSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The text box size must be positive");

            var size = new HpdfArray();
            size.Add(new HpdfNumber(width));
            size.Add(new HpdfNumber(height));
            _dict["TB"] = size;
        }
    }

    /// <summary>
    /// A perpendicular dimension in a 3D model (/Subtype /PD3): the distance between two anchor points measured
    /// along a direction, with its value and units. Create it with <see cref="Hpdf3DModel.CreatePerpendicularMeasure"/>.
    /// </summary>
    public class Hpdf3DPerpendicularMeasure : Hpdf3DMeasure
    {
        internal Hpdf3DPerpendicularMeasure(HpdfXref xref, HpdfPoint3D annotationPlaneNormal, HpdfPoint3D firstAnchor,
            HpdfPoint3D secondAnchor, HpdfPoint3D leaderLinesDirection, HpdfPoint3D valuePosition,
            HpdfPoint3D textUpDirection, float value, string units)
            : base(xref, "PD3")
        {
            if (!float.IsFinite(value))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The value must be a finite number");
            ArgumentNullException.ThrowIfNull(units);

            _dict.Add("AP", Hpdf3DView.ToArray(Validate(annotationPlaneNormal, "annotation plane normal")));
            _dict.Add("A1", Hpdf3DView.ToArray(Validate(firstAnchor, "first anchor point")));
            _dict.Add("A2", Hpdf3DView.ToArray(Validate(secondAnchor, "second anchor point")));
            _dict.Add("D1", Hpdf3DView.ToArray(Validate(leaderLinesDirection, "leader line direction")));
            _dict.Add("TP", Hpdf3DView.ToArray(Validate(valuePosition, "value position")));
            _dict.Add("TY", Hpdf3DView.ToArray(Validate(textUpDirection, "text up direction")));
            _dict.Add("V", new HpdfReal(value));
            _dict.Add("U", new HpdfString(units));
        }
    }
}
