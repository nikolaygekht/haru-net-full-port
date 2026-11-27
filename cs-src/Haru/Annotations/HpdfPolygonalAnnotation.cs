using System;
using Haru.Doc;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Base class of the polygon and polyline annotations (PDF 1.5, not in the C library): a shape given by
    /// its vertices (/Vertices), drawn with the annotation color and the border width and dash, with an
    /// interior color. The annotation rectangle follows from the vertices; the appearance stream is generated
    /// when the document is saved.
    /// </summary>
    public abstract class HpdfPolygonalAnnotation : HpdfGeometricAnnotation
    {
        private HpdfPoint[] _vertices = Array.Empty<HpdfPoint>();

        private protected HpdfPolygonalAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfPoint[] vertices, string text)
            : base(xref, type, BoundingBox(vertices), HpdfVersion.Version15)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            SetVertices(vertices);
            SetRgbColor(1, 0, 0);
        }

        /// <summary>
        /// Gets the smallest number of vertices of the shape.
        /// </summary>
        private protected abstract int MinimumVertices { get; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        public HpdfPoint[] Vertices => (HpdfPoint[])_vertices.Clone();

        /// <summary>
        /// Sets the vertices (/Vertices), in page coordinates.
        /// </summary>
        /// <param name="vertices">The vertices: at least 3 for a polygon, 2 for a polyline.</param>
        public void SetVertices(params HpdfPoint[] vertices)
        {
            ValidateVertices(vertices, MinimumVertices);

            var array = new HpdfArray();
            foreach (var vertex in vertices)
            {
                array.Add(new HpdfReal(vertex.X));
                array.Add(new HpdfReal(vertex.Y));
            }

            _vertices = (HpdfPoint[])vertices.Clone();
            _dict["Vertices"] = array;
            SetRect(GetAppearanceRect());
        }

        /// <summary>
        /// Checks vertices: enough of them, all finite (before an annotation object is created).
        /// </summary>
        internal static void ValidateVertices(HpdfPoint[]? vertices, int minimum)
        {
            if (vertices is null || vertices.Length < minimum)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"At least {minimum} vertices are required");
            foreach (var vertex in vertices)
            {
                if (!float.IsFinite(vertex.X) || !float.IsFinite(vertex.Y))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Vertices must be finite numbers");
            }
        }

        /// <summary>
        /// Gets the vertices as the drawing code uses them.
        /// </summary>
        private protected HpdfPoint[] Points => _vertices;

        /// <summary>
        /// Gets the bounding box of the vertices grown by a margin on every side.
        /// </summary>
        private protected HpdfRect GetBounds(double margin)
        {
            var box = BoundingBox(_vertices);
            return new HpdfRect((float)(box.Left - margin), (float)(box.Bottom - margin),
                (float)(box.Right + margin), (float)(box.Top + margin));
        }

        private static HpdfRect BoundingBox(HpdfPoint[]? vertices)
        {
            if (vertices is null || vertices.Length == 0)
                return default;

            float left = float.MaxValue, bottom = float.MaxValue, right = float.MinValue, top = float.MinValue;
            foreach (var vertex in vertices)
            {
                left = Math.Min(left, vertex.X);
                bottom = Math.Min(bottom, vertex.Y);
                right = Math.Max(right, vertex.X);
                top = Math.Max(top, vertex.Y);
            }
            return new HpdfRect(left, bottom, right, top);
        }
    }
}
