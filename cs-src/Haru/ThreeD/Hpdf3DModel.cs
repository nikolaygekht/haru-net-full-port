using System;
using System.Collections.Generic;
using System.IO;
using Haru.Doc;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.ThreeD
{
    /// <summary>
    /// A 3D model embedded in the document: a 3D stream (/Type /3D) with U3D or PRC data, its views (/VA), the
    /// default view (/DV) and a script run when the model is instantiated (/OnInstantiate). Load it with
    /// <see cref="HpdfDocument.Load3DModel(string)"/> and show it with <see cref="HpdfPage.Create3DAnnotation"/>.
    /// The library does not read the model; viewers that show 3D (Adobe Acrobat and Reader, Foxit) do.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "The stream is registered in the cross-reference table, which owns every PDF object of the document.")]
    public class Hpdf3DModel
    {
        private readonly HpdfXref _xref;
        private readonly HpdfStreamObject _stream;
        private readonly List<Hpdf3DView> _views = new List<Hpdf3DView>();
        private HpdfArray? _viewArray;
        private HpdfStreamObject? _script;

        /// <summary>
        /// Creates a model of U3D or PRC data (use <see cref="HpdfDocument.Load3DModel(byte[])"/>).
        /// </summary>
        internal Hpdf3DModel(HpdfXref xref, byte[] data)
        {
            Format = DetectFormat(data);
            _xref = xref;

            _stream = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
            _stream.Add("Type", new HpdfName("3D"));
            _stream.Add("Subtype", new HpdfName(Format == Hpdf3DFormat.U3D ? "U3D" : "PRC"));
            _stream.WriteToStream(data);
            xref.Add(_stream);

            xref.RequireVersion(HpdfVersion.Version16);
            if (Format == Hpdf3DFormat.PRC)
                xref.RequireAdobeExtensionLevel(3);
        }

        /// <summary>
        /// Gets the format from the signature of the data: "U3D" (the file header block of ECMA-363) or "PRC".
        /// </summary>
        internal static Hpdf3DFormat DetectFormat(byte[]? data)
        {
            if (data is null || data.Length < 4)
                throw new HpdfException(HpdfErrorCode.InvalidU3dData, "The data is not a U3D or PRC model");
            if (data[0] == 'U' && data[1] == '3' && data[2] == 'D' && data[3] == 0)
                return Hpdf3DFormat.U3D;
            if (data[0] == 'P' && data[1] == 'R' && data[2] == 'C')
                return Hpdf3DFormat.PRC;
            throw new HpdfException(HpdfErrorCode.InvalidU3dData, "The data is not a U3D or PRC model");
        }

        /// <summary>
        /// Reads a model file.
        /// </summary>
        internal static byte[] ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The path cannot be null or empty");
            if (!File.Exists(path))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"File not found: {path}");
            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Gets the format of the model.
        /// </summary>
        public Hpdf3DFormat Format { get; }

        /// <summary>
        /// Gets the 3D stream.
        /// </summary>
        public HpdfStreamObject Stream => _stream;

        /// <summary>
        /// Gets the views of the model, in the order of /VA.
        /// </summary>
        public IReadOnlyList<Hpdf3DView> Views => _views;

        /// <summary>
        /// Creates a view of the model (/VA). The first view is the default view until
        /// <see cref="SetDefaultView"/> chooses another.
        /// </summary>
        /// <param name="name">The name of the view that viewers show in their list of views.</param>
        /// <returns>The view.</returns>
        public Hpdf3DView CreateView(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The view name cannot be null or empty");

            var view = new Hpdf3DView(_xref, this, name);
            if (_viewArray is null)
            {
                _viewArray = new HpdfArray();
                _stream.Add("VA", _viewArray);
                _stream["DV"] = new HpdfNumber(0);
            }
            _viewArray.Add(view.Dict);
            _views.Add(view);
            return view;
        }

        /// <summary>
        /// Chooses the view the model is shown in first (/DV, as the index of the view).
        /// </summary>
        /// <param name="view">A view of this model.</param>
        public void SetDefaultView(Hpdf3DView view)
        {
            if (view is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "View cannot be null");
            int index = _views.IndexOf(view);
            if (index < 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The view is not a view of this model");

            _stream["DV"] = new HpdfNumber(index);
        }

        /// <summary>
        /// Sets a JavaScript program that the viewer runs when it instantiates the model (/OnInstantiate), for
        /// example to start an animation or to change the rendering mode; a later call replaces it.
        /// </summary>
        /// <param name="script">The JavaScript code.</param>
        public void SetOnInstantiate(string script)
        {
            if (script is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Script cannot be null");

            if (_script is null)
            {
                _script = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
                _xref.Add(_script);
                _stream.Add("OnInstantiate", _script);
            }

            // A text stream: PDFDocEncoding for ASCII, otherwise UTF-16BE with a byte order mark
            _script.ClearStream();
            _script.WriteToStream(new HpdfString(script).Value);
        }

        /// <summary>
        /// Creates a comment note in the model (a 3D measurement of subtype 3DC, Adobe ExtensionLevel 3): a text with
        /// a leader line to an anchor point. Add it to the views that show it (<see cref="Hpdf3DView.AddMeasure"/>).
        /// </summary>
        /// <param name="anchor">The point the leader line points at, in model coordinates.</param>
        /// <param name="textPosition">The position of the text, in model coordinates.</param>
        /// <returns>The comment note.</returns>
        public Hpdf3DCommentNote CreateCommentNote(HpdfPoint3D anchor, HpdfPoint3D textPosition)
        {
            if (!Hpdf3DView.IsFinite(anchor) || !Hpdf3DView.IsFinite(textPosition))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The points must have finite coordinates");
            return new Hpdf3DCommentNote(_xref, anchor, textPosition);
        }

        /// <summary>
        /// Creates a perpendicular dimension in the model (a 3D measurement of subtype PD3, Adobe ExtensionLevel 3).
        /// Add it to the views that show it (<see cref="Hpdf3DView.AddMeasure"/>).
        /// </summary>
        /// <param name="annotationPlaneNormal">The normal of the plane the dimension is drawn in.</param>
        /// <param name="firstAnchor">The first measured point.</param>
        /// <param name="secondAnchor">The second measured point.</param>
        /// <param name="leaderLinesDirection">The direction of the leader lines.</param>
        /// <param name="valuePosition">The position of the value text.</param>
        /// <param name="textUpDirection">The up direction of the text.</param>
        /// <param name="value">The measured value.</param>
        /// <param name="units">The units shown with the value, such as "mm".</param>
        /// <returns>The dimension.</returns>
        public Hpdf3DPerpendicularMeasure CreatePerpendicularMeasure(HpdfPoint3D annotationPlaneNormal, HpdfPoint3D firstAnchor,
            HpdfPoint3D secondAnchor, HpdfPoint3D leaderLinesDirection, HpdfPoint3D valuePosition, HpdfPoint3D textUpDirection,
            float value, string units)
        {
            foreach (var point in new[] { annotationPlaneNormal, firstAnchor, secondAnchor, leaderLinesDirection, valuePosition, textUpDirection })
            {
                if (!Hpdf3DView.IsFinite(point))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "The points must have finite coordinates");
            }
            if (!float.IsFinite(value))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The value must be a finite number");
            if (units is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Units cannot be null");

            return new Hpdf3DPerpendicularMeasure(_xref, annotationPlaneNormal, firstAnchor, secondAnchor,
                leaderLinesDirection, valuePosition, textUpDirection, value, units);
        }

        /// <summary>
        /// Whether a view belongs to this model.
        /// </summary>
        internal bool HasView(Hpdf3DView view) => _views.Contains(view);
    }
}
