using System.Collections.Generic;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Polygon annotation (PDF 1.5, not in the C library): a closed shape through its vertices, stroked with
    /// the annotation color (red until changed) and filled with the interior color, optionally with a cloudy
    /// border. Create it with <see cref="HpdfPage.CreatePolygonAnnotation"/>.
    /// </summary>
    public class HpdfPolygonAnnotation : HpdfPolygonalAnnotation
    {
        private int _cloudIntensity;

        /// <summary>
        /// Creates a polygon annotation (use <see cref="HpdfPage.CreatePolygonAnnotation"/>).
        /// </summary>
        internal HpdfPolygonAnnotation(HpdfXref xref, HpdfPoint[] vertices, string text)
            : base(xref, HpdfAnnotationType.Polygon, vertices, text)
        {
        }

        /// <summary>
        /// A polygon has at least three vertices.
        /// </summary>
        internal const int MinimumVertexCount = 3;

        private protected override int MinimumVertices => MinimumVertexCount;

        /// <summary>
        /// Draws the border as a cloud (/BE, PDF 1.5). The bumps lie outside the polygon; the annotation
        /// rectangle grows by them.
        /// </summary>
        /// <param name="intensity">0 (no cloud), 1 or 2 (larger bumps).</param>
        public void SetCloudEffect(int intensity)
        {
            HpdfCloudyBorder.WriteEffect(_dict, intensity);
            _cloudIntensity = intensity;
        }

        /// <summary>
        /// Sets what the polygon is for (/IT, PDF 1.6): a cloud or a dimension polygon.
        /// </summary>
        /// <param name="intent">The intent.</param>
        public void SetIntent(HpdfPolygonIntent intent)
        {
            string name = intent switch
            {
                HpdfPolygonIntent.Cloud => "PolygonCloud",
                HpdfPolygonIntent.Dimension => "PolygonDimension",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid polygon intent: {intent}")
            };

            _dict["IT"] = new HpdfName(name);
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        private float CloudRadius => HpdfCloudyBorder.GetRadius(_cloudIntensity);

        private protected override HpdfRect GetAppearanceRect() =>
            GetBounds((_cloudIntensity > 0 ? CloudRadius : 0) + BorderWidth / 2 + 1);

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (!HasStroke && !HasFill)
                return;

            canvas.GSave();
            SetPaint(canvas);

            // Round joins stay within half the line width of the vertices, also at sharp corners
            canvas.SetLineJoin(HpdfLineJoin.RoundJoin);

            var points = Points;
            bool drawn;
            if (_cloudIntensity > 0)
            {
                var vertices = new List<(double X, double Y)>(points.Length);
                foreach (var point in points)
                    vertices.Add((point.X, point.Y));
                drawn = HpdfCloudyBorder.AppendPolygon(canvas, vertices, CloudRadius);
            }
            else
            {
                canvas.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Length; i++)
                    canvas.LineTo(points[i].X, points[i].Y);
                canvas.ClosePath();
                drawn = true;
            }

            if (drawn)
                PaintPath(canvas);
            canvas.GRestore();
        }
    }
}
