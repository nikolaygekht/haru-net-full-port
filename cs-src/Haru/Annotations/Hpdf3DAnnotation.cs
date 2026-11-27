using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Streams;
using Haru.ThreeD;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// 3D annotation (PDF 1.6): shows a 3D model in an area of the page (/3DD), activated when the page opens
    /// unless set otherwise (/3DA). Create it with <see cref="HpdfPage.Create3DAnnotation"/>. Viewers that do not
    /// show 3D, and every viewer before the model is activated, show the appearance stream: a poster image, if one
    /// is given, else a gray box with a cube. PDF/A-1 does not allow 3D annotations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "The appearance stream is registered in the cross-reference table, which owns every PDF object of the document.")]
    public class Hpdf3DAnnotation : HpdfAnnotation
    {
        private readonly HpdfAnnotationAppearance _appearance;
        private readonly HpdfDict _activation;
        private readonly HpdfImage? _poster;

        /// <summary>
        /// Creates a 3D annotation (use <see cref="HpdfPage.Create3DAnnotation"/>).
        /// </summary>
        internal Hpdf3DAnnotation(HpdfXref xref, HpdfRect rect, Hpdf3DModel model, string text, HpdfImage? poster)
            : base(xref, HpdfAnnotationType.ThreeD, rect)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(text);
            Model = model;
            _poster = poster;

            _dict.Add("Contents", new HpdfString(text));
            _dict.Add("3DD", model.Stream);

            // Activated when the page opens, live; uninstantiated again when the page closes (the defaults are
            // written, as the C library does)
            _activation = new HpdfDict();
            _activation.Add("A", new HpdfName("PO"));
            _activation.Add("AIS", new HpdfName("L"));
            _activation.Add("D", new HpdfName("PC"));
            _activation.Add("DIS", new HpdfName("U"));
            _dict.Add("3DA", _activation);

            _appearance = new HpdfAnnotationAppearance(xref);
            Flags = HpdfAnnotationFlags.Print;
            xref.RequireVersion(HpdfVersion.Version16);
        }

        /// <summary>
        /// Gets the model the annotation shows.
        /// </summary>
        public Hpdf3DModel Model { get; }

        /// <summary>
        /// Sets when the annotation is activated and what state the model is in then (/A, /AIS).
        /// </summary>
        /// <param name="when">When it is activated.</param>
        /// <param name="state">Instantiated or live (the default); not uninstantiated.</param>
        public void SetActivation(Hpdf3DActivation when, Hpdf3DArtworkState state = Hpdf3DArtworkState.Live)
        {
            string name = when switch
            {
                Hpdf3DActivation.PageOpen => "PO",
                Hpdf3DActivation.PageVisible => "PV",
                Hpdf3DActivation.Explicit => "XA",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid activation: {when}")
            };
            if (state is not (Hpdf3DArtworkState.Instantiated or Hpdf3DArtworkState.Live))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "An activated model is instantiated or live");

            _activation["A"] = new HpdfName(name);
            _activation["AIS"] = new HpdfName(StateName(state));
        }

        /// <summary>
        /// Sets when the annotation is deactivated and what state the model is in then (/D, /DIS).
        /// </summary>
        /// <param name="when">When it is deactivated.</param>
        /// <param name="state">The state after deactivation (uninstantiated by default).</param>
        public void SetDeactivation(Hpdf3DDeactivation when, Hpdf3DArtworkState state = Hpdf3DArtworkState.Uninstantiated)
        {
            string name = when switch
            {
                Hpdf3DDeactivation.PageClose => "PC",
                Hpdf3DDeactivation.PageInvisible => "PI",
                Hpdf3DDeactivation.Explicit => "XD",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid deactivation: {when}")
            };

            _activation["D"] = new HpdfName(name);
            _activation["DIS"] = new HpdfName(StateName(state));
        }

        private static string StateName(Hpdf3DArtworkState state) => state switch
        {
            Hpdf3DArtworkState.Uninstantiated => "U",
            Hpdf3DArtworkState.Instantiated => "I",
            Hpdf3DArtworkState.Live => "L",
            _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid artwork state: {state}")
        };

        /// <summary>
        /// Sets whether the viewer shows its 3D toolbar (/TB, PDF 1.7; shown by default) and the model tree
        /// (/NP, PDF 1.7; hidden by default) when the annotation is activated.
        /// </summary>
        public void SetUserInterface(bool showToolbar, bool showNavigationPanel)
        {
            _activation["TB"] = new HpdfBoolean(showToolbar);
            _activation["NP"] = new HpdfBoolean(showNavigationPanel);
            _xref.RequireVersion(HpdfVersion.Version17);
        }

        /// <summary>
        /// Sets the view the annotation shows first (/3DV); without it, the default view of the model.
        /// </summary>
        /// <param name="view">A view of the annotation's model.</param>
        public void SetDefaultView(Hpdf3DView view)
        {
            if (view is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "View cannot be null");
            if (!Model.HasView(view))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The view is not a view of the annotation's model");

            _dict["3DV"] = view.Dict;
        }

        internal override void PrepareForWriting(HpdfAnnotationWriteContext context)
        {
            _appearance.Draw(context, Rect, 1, false, DrawPoster);
            _dict["AP"] = _appearance.CreateAppearanceDictionary();
        }

        /// <summary>
        /// Draws the poster image scaled to the rectangle, or a placeholder: a light gray box with a cube.
        /// </summary>
        private void DrawPoster(HpdfAppearanceCanvas canvas)
        {
            float width = Rect.Right - Rect.Left;
            float height = Rect.Top - Rect.Bottom;
            if (width <= 0 || height <= 0)
                return;

            canvas.GSave();
            if (_poster != null)
            {
                _appearance.UseXObject(_poster.LocalName, _poster.Dict);
                canvas.Concat(width, 0, 0, height, Rect.Left, Rect.Bottom);
                canvas.Stream.WriteString("/" + _poster.LocalName + " Do\n");
            }
            else
            {
                canvas.SetGrayFill(0.92f);
                canvas.SetGrayStroke(0.6f);
                canvas.SetLineWidth(1);
                canvas.Rectangle(Rect.Left + 0.5f, Rect.Bottom + 0.5f, width - 1, height - 1);
                canvas.FillStroke();

                // A cube in a cabinet projection, a third of the smaller side
                float size = Math.Min(width, height) / 3;
                float depth = size / 2;
                float x = Rect.Left + (width - size - depth) / 2;
                float y = Rect.Bottom + (height - size - depth) / 2;
                canvas.SetGrayStroke(0.45f);
                canvas.SetLineWidth(Math.Max(1, size / 30));
                canvas.SetLineJoin(HpdfLineJoin.RoundJoin);
                canvas.Rectangle(x, y, size, size);
                canvas.MoveTo(x, y + size);
                canvas.LineTo(x + depth, y + size + depth);
                canvas.LineTo(x + size + depth, y + size + depth);
                canvas.LineTo(x + size, y + size);
                canvas.MoveTo(x + size + depth, y + size + depth);
                canvas.LineTo(x + size + depth, y + depth);
                canvas.LineTo(x + size, y);
                canvas.Stroke();
            }
            canvas.GRestore();
        }
    }
}
