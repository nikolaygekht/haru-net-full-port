using System;
using System.Collections.Generic;
using Haru.Doc;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Line annotation (PDF 1.3): a straight line with optional line endings, leader lines and a caption
    /// (the text of the annotation shown on the line). Create it with <see cref="HpdfPage.CreateLineAnnotation"/>.
    /// The annotation rectangle follows from the geometry; the appearance stream is generated when the
    /// document is saved.
    /// </summary>
    public class HpdfLineAnnotation : HpdfGeometricAnnotation
    {
        /// <summary>
        /// Space between an inline caption and the ends of the broken line, and between a caption on top
        /// and the line.
        /// </summary>
        private const float CaptionGap = 2f;

        private readonly string _text;
        private HpdfPoint _start;
        private HpdfPoint _end;
        private HpdfLineEndingStyle _startStyle;
        private HpdfLineEndingStyle _endStyle;
        private float _leaderLength;
        private float _leaderExtension;
        private float _leaderOffset;
        private HpdfFont? _captionFont;
        private float _captionFontSize;
        private HpdfLineCaptionPosition _captionPosition;
        private float _captionHorizontalOffset;
        private float _captionVerticalOffset;

        /// <summary>
        /// Creates a line annotation (use <see cref="HpdfPage.CreateLineAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// <param name="text">The text of the annotation (/Contents), shown in its pop-up window and as the
        /// caption; may be empty.</param>
        internal HpdfLineAnnotation(HpdfXref xref, HpdfPoint start, HpdfPoint end, string text)
            : base(xref, HpdfAnnotationType.Line, new HpdfRect(start.X, start.Y, end.X, end.Y))
        {
            ArgumentNullException.ThrowIfNull(text);
            _text = text;
            _dict.Add("Contents", new HpdfString(text));
            SetPosition(start, end);
            SetRgbColor(1, 0, 0);
        }

        /// <summary>
        /// Sets the start and end points of the line (/L).
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        public void SetPosition(HpdfPoint start, HpdfPoint end)
        {
            if (!IsFinite(start.X) || !IsFinite(start.Y) || !IsFinite(end.X) || !IsFinite(end.Y))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Line points must be finite numbers");

            _start = start;
            _end = end;

            var points = new HpdfArray();
            points.Add(new HpdfReal(start.X));
            points.Add(new HpdfReal(start.Y));
            points.Add(new HpdfReal(end.X));
            points.Add(new HpdfReal(end.Y));
            _dict["L"] = points;
        }

        /// <summary>
        /// Sets the line endings at the start and at the end of the line (/LE, PDF 1.4).
        /// Closed endings (square, circle, diamond, closed arrows) are filled with the interior color.
        /// </summary>
        /// <param name="start">The ending at the start point.</param>
        /// <param name="end">The ending at the end point.</param>
        public void SetLineEndingStyle(HpdfLineEndingStyle start, HpdfLineEndingStyle end)
        {
            var styles = new HpdfArray();
            styles.Add(new HpdfName(HpdfLineEndings.GetName(start)));
            styles.Add(new HpdfName(HpdfLineEndings.GetName(end)));

            _startStyle = start;
            _endStyle = end;
            _dict["LE"] = styles;
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Moves the line away from its points and draws leader lines from the points to it, as dimension
        /// lines are drawn (/LL, /LLE: PDF 1.6, /LLO: PDF 1.7).
        /// </summary>
        /// <param name="length">The length of the leader lines: positive moves the line to the left of its
        /// direction (up for a line from left to right), negative to the right; 0 removes the leader lines
        /// (and the entries).</param>
        /// <param name="extension">How far the leader lines go beyond the line (non-negative).</param>
        /// <param name="offset">The gap between the points and the start of the leader lines (non-negative).</param>
        public void SetLeader(float length, float extension = 0, float offset = 0)
        {
            if (!IsFinite(length))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The leader line length must be a finite number");
            if (!(extension >= 0) || !IsFinite(extension) || !(offset >= 0) || !IsFinite(offset))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The leader line extension and offset must be non-negative numbers");

            _leaderLength = length;
            _leaderExtension = extension;
            _leaderOffset = offset;

            if (length == 0)
            {
                _dict.Remove("LL");
                _dict.Remove("LLE");
                _dict.Remove("LLO");
                return;
            }

            _dict["LL"] = new HpdfReal(length);
            SetOrRemove("LLE", extension);
            SetOrRemove("LLO", offset);
            _xref.RequireVersion(offset != 0 ? HpdfVersion.Version17 : HpdfVersion.Version16);
        }

        /// <summary>
        /// Shows the text of the annotation as a caption on the line (/Cap, PDF 1.6; /CP and /CO, PDF 1.7).
        /// The caption is one line of text in the annotation color, drawn with the given font.
        /// </summary>
        /// <param name="font">The font of the caption (any font of the document; not a vertical one).</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="position">Inside the line, which is broken around it, or on top of it.</param>
        /// <param name="horizontalOffset">Shift of the caption along the line from its center.</param>
        /// <param name="verticalOffset">Shift of the caption perpendicular to the line (positive: to the left
        /// of the line's direction).</param>
        public void SetCaption(HpdfFont font, float fontSize,
            HpdfLineCaptionPosition position = HpdfLineCaptionPosition.Inline,
            float horizontalOffset = 0, float verticalOffset = 0)
        {
            if (font is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Font cannot be null");
            if (font.WritingMode == HpdfWritingMode.Vertical)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The caption needs a font of horizontal writing");
            if (!(fontSize > 0) || !IsFinite(fontSize))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The font size must be a positive number");
            if (position is not (HpdfLineCaptionPosition.Inline or HpdfLineCaptionPosition.Top))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid caption position: {position}");
            if (!IsFinite(horizontalOffset) || !IsFinite(verticalOffset))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Caption offsets must be finite numbers");

            _captionFont = font;
            _captionFontSize = fontSize;
            _captionPosition = position;
            _captionHorizontalOffset = horizontalOffset;
            _captionVerticalOffset = verticalOffset;

            _dict["Cap"] = new HpdfBoolean(true);
            _xref.RequireVersion(HpdfVersion.Version16);

            if (position == HpdfLineCaptionPosition.Top)
            {
                _dict["CP"] = new HpdfName("Top");
                _xref.RequireVersion(HpdfVersion.Version17);
            }
            else
            {
                _dict.Remove("CP");
            }

            if (horizontalOffset != 0 || verticalOffset != 0)
            {
                var offsets = new HpdfArray();
                offsets.Add(new HpdfReal(horizontalOffset));
                offsets.Add(new HpdfReal(verticalOffset));
                _dict["CO"] = offsets;
                _xref.RequireVersion(HpdfVersion.Version17);
            }
            else
            {
                _dict.Remove("CO");
            }
        }

        /// <summary>
        /// Removes the caption.
        /// </summary>
        public void RemoveCaption()
        {
            _captionFont = null;
            _dict.Remove("Cap");
            _dict.Remove("CP");
            _dict.Remove("CO");
        }

        /// <summary>
        /// Sets what the line is for (/IT, PDF 1.6): an arrow or a dimension line.
        /// </summary>
        /// <param name="intent">The intent.</param>
        public void SetIntent(HpdfLineIntent intent)
        {
            string name = intent switch
            {
                HpdfLineIntent.Arrow => "LineArrow",
                HpdfLineIntent.Dimension => "LineDimension",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid line intent: {intent}")
            };

            _dict["IT"] = new HpdfName(name);
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        private protected override HpdfRect GetAppearanceRect()
        {
            var geometry = new Geometry(this);
            var points = new List<(double X, double Y)> { geometry.LineStart, geometry.LineEnd };

            if (geometry.HasLeaders)
            {
                points.Add(geometry.LeaderStart(geometry.Start));
                points.Add(geometry.LeaderEnd(geometry.Start));
                points.Add(geometry.LeaderStart(geometry.End));
                points.Add(geometry.LeaderEnd(geometry.End));
            }

            AddEndingExtent(points, _startStyle, geometry.LineStart, geometry.EndingSize);
            AddEndingExtent(points, _endStyle, geometry.LineEnd, geometry.EndingSize);

            if (geometry.Caption is { } caption)
            {
                foreach (var corner in caption.Corners(geometry))
                    points.Add(corner);
            }

            double left = double.MaxValue, bottom = double.MaxValue, right = double.MinValue, top = double.MinValue;
            foreach (var (x, y) in points)
            {
                left = Math.Min(left, x);
                bottom = Math.Min(bottom, y);
                right = Math.Max(right, x);
                top = Math.Max(top, y);
            }

            // Room for the stroke and its joins
            double pad = BorderWidth + 1;
            return new HpdfRect((float)(left - pad), (float)(bottom - pad), (float)(right + pad), (float)(top + pad));
        }

        private static void AddEndingExtent(List<(double X, double Y)> points, HpdfLineEndingStyle style,
            (double X, double Y) point, double size)
        {
            if (style == HpdfLineEndingStyle.None)
                return;

            points.Add((point.X - size, point.Y - size));
            points.Add((point.X + size, point.Y + size));
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            var geometry = new Geometry(this);
            var caption = geometry.Caption;

            canvas.GSave();
            SetPaint(canvas);

            if (HasStroke)
            {
                // The line, from edge to edge of closed endings, broken around an inline caption that fits
                double first = HpdfLineEndings.GetLineInset(_startStyle, geometry.EndingSize);
                double last = geometry.Length - HpdfLineEndings.GetLineInset(_endStyle, geometry.EndingSize);
                bool appended;
                if (caption is { Position: HpdfLineCaptionPosition.Inline } inline && inline.BreaksLine(geometry))
                {
                    appended = AppendSegment(canvas, geometry, first, inline.Center - inline.Width / 2 - CaptionGap);
                    appended |= AppendSegment(canvas, geometry, inline.Center + inline.Width / 2 + CaptionGap, last);
                }
                else
                {
                    appended = AppendSegment(canvas, geometry, first, last);
                }
                if (appended)
                    canvas.Stroke();

                if (geometry.HasLeaders)
                {
                    foreach (var point in new[] { geometry.Start, geometry.End })
                    {
                        var (fromX, fromY) = geometry.LeaderStart(point);
                        var (toX, toY) = geometry.LeaderEnd(point);
                        canvas.MoveTo((float)fromX, (float)fromY);
                        canvas.LineTo((float)toX, (float)toY);
                    }
                    canvas.Stroke();
                }
            }

            // Line endings are drawn solid, also on a dashed line
            if (HasStroke && BorderStyle == HpdfBorderStyle.Dashed)
                canvas.SetDash(null, 0);
            DrawEnding(canvas, _startStyle, geometry.LineStart, -geometry.DirX, -geometry.DirY, geometry.EndingSize);
            DrawEnding(canvas, _endStyle, geometry.LineEnd, geometry.DirX, geometry.DirY, geometry.EndingSize);

            if (caption != null)
                DrawCaption(canvas, geometry, caption);

            canvas.GRestore();
        }

        /// <summary>
        /// Appends the part of the line between two distances from its start; returns false, appending
        /// nothing, if it is empty.
        /// </summary>
        private static bool AppendSegment(IDrawable canvas, Geometry geometry, double from, double to)
        {
            if (to <= from)
                return false;

            var (x, y) = geometry.LineStart;
            canvas.MoveTo((float)(x + geometry.DirX * from), (float)(y + geometry.DirY * from));
            canvas.LineTo((float)(x + geometry.DirX * to), (float)(y + geometry.DirY * to));
            return true;
        }

        private void DrawEnding(IDrawable canvas, HpdfLineEndingStyle style, (double X, double Y) point,
            double dirX, double dirY, double size)
        {
            if (style == HpdfLineEndingStyle.None)
                return;

            bool closed = style is HpdfLineEndingStyle.Square or HpdfLineEndingStyle.Circle or HpdfLineEndingStyle.Diamond
                or HpdfLineEndingStyle.ClosedArrow or HpdfLineEndingStyle.RClosedArrow;
            if (!HasStroke && !(closed && HasFill))
                return;

            bool? appended = HpdfLineEndings.Append(canvas, style, point.X, point.Y, dirX, dirY, size);
            if (appended.HasValue)
                PaintPath(canvas, fill: appended.Value);
        }

        private void DrawCaption(HpdfAppearanceCanvas canvas, Geometry geometry, Caption caption)
        {
            var font = _captionFont!;
            UseAppearanceFont(font);

            var stream = canvas.Stream;
            stream.WriteString("BT\n");
            if (Color is { IsTransparent: false } color)
                color.SetFill(canvas);
            else
                canvas.SetGrayFill(0);

            stream.WriteEscapedName(font.LocalName);
            stream.WriteChar(' ');
            stream.WriteReal(_captionFontSize);
            stream.WriteString(" Tf\n");

            var (originX, originY) = caption.Origin(geometry);
            stream.WriteReal((float)geometry.DirX);
            stream.WriteChar(' ');
            stream.WriteReal((float)geometry.DirY);
            stream.WriteChar(' ');
            stream.WriteReal((float)-geometry.DirY);
            stream.WriteChar(' ');
            stream.WriteReal((float)geometry.DirX);
            stream.WriteChar(' ');
            stream.WriteReal((float)originX);
            stream.WriteChar(' ');
            stream.WriteReal((float)originY);
            stream.WriteString(" Tm\n");

            font.WriteTextOperand(stream, caption.Text);
            stream.WriteString(" Tj\nET\n");
        }

        private void SetOrRemove(string key, float value)
        {
            if (value != 0)
                _dict[key] = new HpdfReal(value);
            else
                _dict.Remove(key);
        }

        private static bool IsFinite(float value) => float.IsFinite(value);

        /// <summary>
        /// The geometry of the line in page coordinates: the points, the direction, the line moved by the
        /// leader length, and the caption.
        /// </summary>
        private sealed class Geometry
        {
            private readonly double _leaderLength;
            private readonly double _leaderExtension;
            private readonly double _leaderOffset;

            public Geometry(HpdfLineAnnotation line)
            {
                Start = (line._start.X, line._start.Y);
                End = (line._end.X, line._end.Y);

                double dx = End.X - Start.X;
                double dy = End.Y - Start.Y;
                Length = Math.Sqrt(dx * dx + dy * dy);
                (DirX, DirY) = Length > 0 ? (dx / Length, dy / Length) : (1.0, 0.0);

                // The leader lines go the other way for a negative length, also their extension and offset
                _leaderLength = line._leaderLength;
                double sign = _leaderLength < 0 ? -1 : 1;
                _leaderExtension = sign * line._leaderExtension;
                _leaderOffset = sign * line._leaderOffset;

                LineStart = Offset(Start, _leaderLength);
                LineEnd = Offset(End, _leaderLength);
                EndingSize = HpdfLineEndings.GetSize(line.BorderWidth);

                if (line._captionFont != null)
                    Caption = new Caption(line, this);
            }

            public (double X, double Y) Start { get; }
            public (double X, double Y) End { get; }
            public double Length { get; }
            public double DirX { get; }
            public double DirY { get; }
            public (double X, double Y) LineStart { get; }
            public (double X, double Y) LineEnd { get; }
            public double EndingSize { get; }
            public Caption? Caption { get; }
            public bool HasLeaders => _leaderLength != 0;

            /// <summary>
            /// A point moved perpendicular to the line, to the left of its direction.
            /// </summary>
            public (double X, double Y) Offset((double X, double Y) point, double distance) =>
                (point.X - DirY * distance, point.Y + DirX * distance);

            public (double X, double Y) LeaderStart((double X, double Y) point) => Offset(point, _leaderOffset);

            public (double X, double Y) LeaderEnd((double X, double Y) point) => Offset(point, _leaderLength + _leaderExtension);
        }

        /// <summary>
        /// The caption in the coordinates of the line: x along the line from its start, y to the left of it.
        /// </summary>
        private sealed class Caption
        {
            public Caption(HpdfLineAnnotation line, Geometry geometry)
            {
                var font = line._captionFont!;
                float size = line._captionFontSize;

                // One line of text
                Text = line._text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
                Position = line._captionPosition;
                Width = Text.Length > 0 ? font.MeasureText(Text, size) : 0;
                Ascent = font.GetAscent() / 1000.0 * size;
                Descent = font.GetDescent() / 1000.0 * size;
                Center = geometry.Length / 2 + line._captionHorizontalOffset;

                double baseline = Position == HpdfLineCaptionPosition.Inline
                    ? -(Ascent + Descent) / 2
                    : line.BorderWidth / 2 + CaptionGap - Descent;
                Baseline = baseline + line._captionVerticalOffset;
            }

            public string Text { get; }
            public HpdfLineCaptionPosition Position { get; }
            public double Width { get; }
            public double Ascent { get; }
            public double Descent { get; }
            public double Center { get; }
            public double Baseline { get; }

            /// <summary>
            /// Whether the line is broken around the caption: the caption has text and fits on the line.
            /// </summary>
            public bool BreaksLine(Geometry geometry) =>
                Width > 0 && Center - Width / 2 - CaptionGap > 0 && Center + Width / 2 + CaptionGap < geometry.Length;

            /// <summary>
            /// The start of the baseline in page coordinates.
            /// </summary>
            public (double X, double Y) Origin(Geometry geometry) => ToPage(geometry, Center - Width / 2, Baseline);

            public IEnumerable<(double X, double Y)> Corners(Geometry geometry)
            {
                double left = Center - Width / 2;
                double right = Center + Width / 2;
                yield return ToPage(geometry, left, Baseline + Descent);
                yield return ToPage(geometry, right, Baseline + Descent);
                yield return ToPage(geometry, left, Baseline + Ascent);
                yield return ToPage(geometry, right, Baseline + Ascent);
            }

            private static (double X, double Y) ToPage(Geometry geometry, double along, double across)
            {
                var (x, y) = geometry.LineStart;
                return (x + geometry.DirX * along - geometry.DirY * across, y + geometry.DirY * along + geometry.DirX * across);
            }
        }
    }
}
