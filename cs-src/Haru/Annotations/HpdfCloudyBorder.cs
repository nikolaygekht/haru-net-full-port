/* The cloudy border effect (/BE /S /C) of generated annotation appearances. */
using System;
using System.Collections.Generic;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;

namespace Haru.Annotations
{
    /// <summary>
    /// Builds the path of a cloudy border: circles of one radius are placed along a closed outline, each
    /// overlapping its neighbors, and the path follows the outer arc of every circle from the intersection
    /// with the previous circle to the one with the next. The bumps lie outside the outline, at most one
    /// radius from it. Concave outlines are allowed: at every circle the arc that faces away from the
    /// outline is taken.
    /// </summary>
    internal static class HpdfCloudyBorder
    {
        /// <summary>
        /// Radius of the bumps per unit of intensity (/I of the border effect, 1 or 2).
        /// </summary>
        private const double RadiusPerIntensity = 4.75;

        /// <summary>
        /// Largest distance between neighboring centers, in radii: the circles overlap enough to
        /// give round bumps (a distance of 2 radii would make them touch only).
        /// </summary>
        private const double MaxSpacing = 1.85;

        /// <summary>
        /// Number of segments of the polygon that measures the perimeter of an ellipse.
        /// </summary>
        private const int EllipseSamples = 256;

        /// <summary>
        /// Gets the radius of the bumps of an intensity.
        /// </summary>
        public static float GetRadius(int intensity) => (float)(RadiusPerIntensity * intensity);

        /// <summary>
        /// Checks a cloud intensity and writes the border effect (/BE) for it: none for 0.
        /// </summary>
        /// <param name="dict">The annotation dictionary.</param>
        /// <param name="intensity">0 (no cloud), 1 or 2.</param>
        public static void WriteEffect(HpdfDict dict, int intensity)
        {
            ValidateIntensity(intensity);
            if (intensity == 0)
            {
                dict.Remove("BE");
                return;
            }

            var effect = new HpdfDict();
            effect.Add("S", new HpdfName("C"));
            effect.Add("I", new HpdfNumber(intensity));
            dict["BE"] = effect;
        }

        /// <summary>
        /// Checks a cloud intensity: 0, 1 or 2.
        /// </summary>
        public static void ValidateIntensity(int intensity)
        {
            if (intensity < 0 || intensity > 2)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The cloud intensity must be 0, 1 or 2");
        }

        /// <summary>
        /// Appends the cloud around a rectangle (bumps at the corners, evenly spaced on every side).
        /// Returns false, appending nothing, when the rectangle has no area.
        /// </summary>
        public static bool AppendRectangle(IDrawable canvas, double left, double bottom, double right, double top, double radius)
        {
            if (!(right > left && top > bottom))
                return false;

            return AppendPolygon(canvas, new (double X, double Y)[] { (left, bottom), (right, bottom), (right, top), (left, top) }, radius);
        }

        /// <summary>
        /// Appends the cloud around a closed polygon (bumps at the vertices, evenly spaced on every edge), in either
        /// orientation. Returns false, appending nothing, when the polygon has no area.
        /// </summary>
        public static bool AppendPolygon(IDrawable canvas, IReadOnlyList<(double X, double Y)> vertices, double radius)
        {
            if (!(radius > 0) || vertices.Count < 3)
                return false;

            // Counterclockwise, so that the outside is on the right of every edge
            double area = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Count];
                area += a.X * b.Y - b.X * a.Y;
            }
            if (Math.Abs(area) < 1e-9)
                return false;

            var corners = new List<(double X, double Y)>(vertices);
            if (area < 0)
                corners.Reverse();

            var centers = new List<(double X, double Y)>();
            var normals = new List<(double X, double Y)>();
            int n = corners.Count;
            for (int i = 0; i < n; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % n];
                double length = Distance(start, end);
                if (length == 0)
                    continue;

                var outward = RightNormal(start, end);
                var previousOutward = PreviousEdgeNormal(corners, i);
                int count = Math.Max(1, (int)Math.Ceiling(length / (MaxSpacing * radius)));
                for (int j = 0; j < count; j++)
                {
                    double t = (double)j / count;
                    centers.Add((start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t));

                    // At a vertex the bump faces between the two edges
                    normals.Add(j == 0 ? Normalize((outward.X + previousOutward.X, outward.Y + previousOutward.Y), outward) : outward);
                }
            }

            if (centers.Count < 3)
                return false;

            AppendPath(canvas, centers, normals, radius);
            return true;
        }

        /// <summary>
        /// Appends the cloud around an ellipse (bumps evenly spaced along its perimeter).
        /// Returns false, appending nothing, when the ellipse has no area.
        /// </summary>
        public static bool AppendEllipse(IDrawable canvas, double centerX, double centerY, double radiusX, double radiusY, double radius)
        {
            if (!(radiusX > 0 && radiusY > 0 && radius > 0))
                return false;

            // The perimeter, measured on a fine polygon: cumulative lengths at the sample points
            var samples = new (double X, double Y)[EllipseSamples + 1];
            var lengths = new double[EllipseSamples + 1];
            for (int i = 0; i <= EllipseSamples; i++)
            {
                double angle = 2 * Math.PI * i / EllipseSamples;
                samples[i] = (centerX + radiusX * Math.Cos(angle), centerY + radiusY * Math.Sin(angle));
                if (i > 0)
                    lengths[i] = lengths[i - 1] + Distance(samples[i - 1], samples[i]);
            }

            double perimeter = lengths[EllipseSamples];
            int count = Math.Max(3, (int)Math.Ceiling(perimeter / (MaxSpacing * radius)));
            var centers = new List<(double X, double Y)>(count);
            var normals = new List<(double X, double Y)>(count);
            int segment = 0;
            for (int k = 0; k < count; k++)
            {
                double target = perimeter * k / count;
                while (lengths[segment + 1] < target)
                    segment++;

                double span = lengths[segment + 1] - lengths[segment];
                double t = span > 0 ? (target - lengths[segment]) / span : 0;
                var a = samples[segment];
                var b = samples[segment + 1];
                var center = (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                centers.Add(center);

                // The normal of an ellipse points along the gradient of (x/rx)^2 + (y/ry)^2
                normals.Add(Normalize(((center.Item1 - centerX) / (radiusX * radiusX), (center.Item2 - centerY) / (radiusY * radiusY)), (1, 0)));
            }

            AppendPath(canvas, centers, normals, radius);
            return true;
        }

        /// <summary>
        /// Appends the closed path of the outer arcs of circles around the centers of a counterclockwise outline.
        /// </summary>
        private static void AppendPath(IDrawable canvas, List<(double X, double Y)> centers,
            List<(double X, double Y)> normals, double radius)
        {
            int count = centers.Count;

            // Outer intersection of every circle with the next one
            var joints = new (double X, double Y)[count];
            for (int i = 0; i < count; i++)
                joints[i] = OuterIntersection(centers[i], centers[(i + 1) % count], radius);

            var startJoint = joints[count - 1];
            canvas.MoveTo((float)startJoint.X, (float)startJoint.Y);
            for (int i = 0; i < count; i++)
            {
                var center = centers[i];
                var from = joints[(i + count - 1) % count];
                var to = joints[i];
                double startAngle = Math.Atan2(from.Y - center.Y, from.X - center.X);
                double endAngle = Math.Atan2(to.Y - center.Y, to.X - center.X);
                double ccwSpan = Positive(endAngle - startAngle);

                // The counterclockwise arc, unless the outward direction lies on the clockwise one
                double outward = Math.Atan2(normals[i].Y, normals[i].X);
                bool counterclockwise = Positive(outward - startAngle) <= ccwSpan;
                double sweep = counterclockwise ? ccwSpan : ccwSpan - 2 * Math.PI;

                AppendArc(canvas, center, radius, startAngle, sweep);
            }
            canvas.ClosePath();
        }

        /// <summary>
        /// Gets an angle in [0, 2 pi).
        /// </summary>
        private static double Positive(double angle)
        {
            angle %= 2 * Math.PI;
            return angle < 0 ? angle + 2 * Math.PI : angle;
        }

        /// <summary>
        /// Gets the intersection of two circles of the same radius on the right of the direction from the
        /// first center to the second: outside a counterclockwise outline.
        /// </summary>
        private static (double X, double Y) OuterIntersection((double X, double Y) a, (double X, double Y) b, double radius)
        {
            double distance = Distance(a, b);
            double midX = (a.X + b.X) / 2;
            double midY = (a.Y + b.Y) / 2;
            if (distance == 0)
                return (midX, midY - radius);

            double half = Math.Min(distance / 2, radius);
            double height = Math.Sqrt(radius * radius - half * half);
            double dirX = (b.X - a.X) / distance;
            double dirY = (b.Y - a.Y) / distance;
            return (midX + height * dirY, midY - height * dirX);
        }

        /// <summary>
        /// Appends an arc as Bezier curves of at most 90 degrees each, counterclockwise for a positive sweep;
        /// the current point is the start of the arc.
        /// </summary>
        private static void AppendArc(IDrawable canvas, (double X, double Y) center, double radius, double startAngle, double sweep)
        {
            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 2) - 1e-9));
            double step = sweep / segments;
            double k = 4.0 / 3.0 * Math.Tan(step / 4) * radius;

            double angle = startAngle;
            for (int i = 0; i < segments; i++)
            {
                double next = angle + step;
                double cos0 = Math.Cos(angle), sin0 = Math.Sin(angle);
                double cos1 = Math.Cos(next), sin1 = Math.Sin(next);
                canvas.CurveTo(
                    (float)(center.X + radius * cos0 - k * sin0), (float)(center.Y + radius * sin0 + k * cos0),
                    (float)(center.X + radius * cos1 + k * sin1), (float)(center.Y + radius * sin1 - k * cos1),
                    (float)(center.X + radius * cos1), (float)(center.Y + radius * sin1));
                angle = next;
            }
        }

        /// <summary>
        /// The unit normal on the right of the direction from a to b.
        /// </summary>
        private static (double X, double Y) RightNormal((double X, double Y) a, (double X, double Y) b)
        {
            double length = Distance(a, b);
            return ((b.Y - a.Y) / length, -(b.X - a.X) / length);
        }

        /// <summary>
        /// The right normal of the last edge of non-zero length that ends at a vertex.
        /// </summary>
        private static (double X, double Y) PreviousEdgeNormal(List<(double X, double Y)> corners, int vertex)
        {
            int n = corners.Count;
            for (int k = 1; k < n; k++)
            {
                var start = corners[(vertex - k + n) % n];
                var end = corners[(vertex - k + 1 + n) % n];
                if (Distance(start, end) > 0)
                    return RightNormal(start, end);
            }
            return (0, -1);
        }

        private static (double X, double Y) Normalize((double X, double Y) vector, (double X, double Y) fallback)
        {
            double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            return length > 1e-12 ? (vector.X / length, vector.Y / length) : fallback;
        }

        private static double Distance((double X, double Y) a, (double X, double Y) b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
