using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Polyline annotation (PDF 1.5, not in the C library): connected straight lines through the vertices, in the
    /// annotation color (red until changed), with optional line endings at the first and last vertex (closed
    /// endings are filled with the interior color). Create it with <see cref="HpdfPage.CreatePolyLineAnnotation"/>.
    /// </summary>
    public class HpdfPolyLineAnnotation : HpdfPolygonalAnnotation
    {
        private HpdfLineEndingStyle _startStyle;
        private HpdfLineEndingStyle _endStyle;

        /// <summary>
        /// Creates a polyline annotation (use <see cref="HpdfPage.CreatePolyLineAnnotation"/>).
        /// </summary>
        internal HpdfPolyLineAnnotation(HpdfXref xref, HpdfPoint[] vertices, string text)
            : base(xref, HpdfAnnotationType.PolyLine, vertices, text)
        {
        }

        /// <summary>
        /// A polyline has at least two vertices.
        /// </summary>
        internal const int MinimumVertexCount = 2;

        private protected override int MinimumVertices => MinimumVertexCount;

        /// <summary>
        /// Sets the line endings at the first and at the last vertex (/LE).
        /// </summary>
        /// <param name="start">The ending at the first vertex.</param>
        /// <param name="end">The ending at the last vertex.</param>
        public void SetLineEndingStyle(HpdfLineEndingStyle start, HpdfLineEndingStyle end)
        {
            var styles = new HpdfArray();
            styles.Add(new HpdfName(HpdfLineEndings.GetName(start)));
            styles.Add(new HpdfName(HpdfLineEndings.GetName(end)));

            _startStyle = start;
            _endStyle = end;
            _dict["LE"] = styles;
        }

        /// <summary>
        /// Sets what the polyline is for (/IT, PDF 1.6): a dimension polyline.
        /// </summary>
        /// <param name="intent">The intent.</param>
        public void SetIntent(HpdfPolyLineIntent intent)
        {
            if (intent != HpdfPolyLineIntent.Dimension)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid polyline intent: {intent}");

            _dict["IT"] = new HpdfName("PolyLineDimension");
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        private double EndingSize => HpdfLineEndings.GetSize(BorderWidth);

        private protected override HpdfRect GetAppearanceRect()
        {
            double endings = _startStyle != HpdfLineEndingStyle.None || _endStyle != HpdfLineEndingStyle.None ? EndingSize : 0;
            return GetBounds(endings + BorderWidth / 2 + 1);
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            var points = Points;
            canvas.GSave();
            SetPaint(canvas);
            canvas.SetLineJoin(HpdfLineJoin.RoundJoin);

            var (startDirX, startDirY) = Direction(points[1], points[0]);
            var (endDirX, endDirY) = Direction(points[^2], points[^1]);

            if (HasStroke)
            {
                // From edge to edge of closed endings
                double startInset = HpdfLineEndings.GetLineInset(_startStyle, EndingSize);
                double endInset = HpdfLineEndings.GetLineInset(_endStyle, EndingSize);
                canvas.MoveTo((float)(points[0].X - startDirX * startInset), (float)(points[0].Y - startDirY * startInset));
                for (int i = 1; i < points.Length - 1; i++)
                    canvas.LineTo(points[i].X, points[i].Y);
                canvas.LineTo((float)(points[^1].X - endDirX * endInset), (float)(points[^1].Y - endDirY * endInset));
                canvas.Stroke();

                // Line endings are drawn solid, also on a dashed line
                if (BorderStyle == HpdfBorderStyle.Dashed)
                    canvas.SetDash(null, 0);
            }

            DrawEnding(canvas, _startStyle, points[0], startDirX, startDirY);
            DrawEnding(canvas, _endStyle, points[^1], endDirX, endDirY);
            canvas.GRestore();
        }

        private void DrawEnding(IDrawable canvas, HpdfLineEndingStyle style, HpdfPoint point, double dirX, double dirY)
        {
            if (style == HpdfLineEndingStyle.None || (!HasStroke && !(HpdfLineEndings.IsClosed(style) && HasFill)))
                return;

            bool? closed = HpdfLineEndings.Append(canvas, style, point.X, point.Y, dirX, dirY, EndingSize);
            if (closed.HasValue)
                PaintPath(canvas, fill: closed.Value);
        }

        /// <summary>
        /// The unit direction from one point to another, pointing away from the line at the second; along x
        /// for equal points.
        /// </summary>
        private static (double X, double Y) Direction(HpdfPoint from, HpdfPoint to)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            return length > 0 ? (dx / length, dy / length) : (1, 0);
        }
    }
}
