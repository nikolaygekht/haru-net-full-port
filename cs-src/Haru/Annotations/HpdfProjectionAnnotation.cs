using System;
using Haru.Doc;
using Haru.Objects;
using Haru.ThreeD;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Projection annotation (Adobe ExtensionLevel 3, PDF 2.0): a markup annotation that makes a 3D measurement
    /// part of the comments of the page; its external data (/ExData /Subtype /3DM) refers to the measurement, and
    /// the measurement refers back to it. Create it with <see cref="HpdfPage.CreateProjectionAnnotation"/>.
    /// It has no appearance of its own: the measurement is drawn by the 3D viewer. PDF/A-1 does not allow it.
    /// </summary>
    public class HpdfProjectionAnnotation : HpdfMarkupAnnotation
    {
        /// <summary>
        /// Creates a projection annotation (use <see cref="HpdfPage.CreateProjectionAnnotation"/>).
        /// </summary>
        internal HpdfProjectionAnnotation(HpdfXref xref, HpdfRect rect, string text, Hpdf3DMeasure measure)
            : base(xref, HpdfAnnotationType.Projection, rect)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(measure);
            Measure = measure;

            _dict.Add("Contents", new HpdfString(text));
            var exData = new HpdfDict();
            exData.Add("Type", new HpdfName("ExData"));
            exData.Add("Subtype", new HpdfName("3DM"));
            exData.Add("M3DREF", measure.Dict);
            _dict.Add("ExData", exData);

            measure.AttachProjectionAnnotation(this);
            xref.RequireAdobeExtensionLevel(3);
        }

        /// <summary>
        /// Gets the 3D measurement the annotation stands for.
        /// </summary>
        public Hpdf3DMeasure Measure { get; }
    }
}
