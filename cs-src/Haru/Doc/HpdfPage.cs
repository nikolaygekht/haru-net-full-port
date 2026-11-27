using System;
using System.Collections.Generic;
using Haru.Objects;
using Haru.Xref;
using Haru.Graphics;
using Haru.Types;
using Haru.Font;
using Haru.Annotations;
using Haru.Forms;
using Haru.Streams;

namespace Haru.Doc
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public class HpdfPage : IDrawable, IDisposable
    {
        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;
        private readonly HpdfStreamObject _contents;
        private HpdfCompressionMode _compressionMode;
        private HpdfGraphicsState _graphicsState = null!;
        private bool _disposed = false;
        private HpdfPoint _currentPos;
        private HpdfTransMatrix _textMatrix;
        private HpdfTransMatrix _textLineMatrix;
        // Note: _currentFont removed - now stored in GraphicsState.CurrentFont for proper save/restore
        private readonly Dictionary<string, HpdfFont> _fontResources;
        private readonly Dictionary<string, HpdfExtGState> _extGStateResources;
        private readonly Dictionary<string, HpdfImage> _imageResources;
        private readonly List<HpdfAnnotation> _annotations = new List<HpdfAnnotation>();

        /// <summary>
        /// Default page width in points (595.276 = A4 width).
        /// </summary>
        public const float DefaultWidth = 595.276f;

        /// <summary>
        /// Default page height in points (841.89 = A4 height).
        /// </summary>
        public const float DefaultHeight = 841.89f;

        /// <summary>
        /// Gets the underlying dictionary object for this page.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// The cross-reference table of the document the page belongs to.
        /// </summary>
        internal HpdfXref Xref => _xref;

        /// <summary>
        /// Gets the parent pages object.
        /// </summary>
        public HpdfPages? Parent { get; internal set; }

        /// <summary>
        /// Gets the annotations created on or added to this page, in the order of /Annots.
        /// </summary>
        public IReadOnlyList<HpdfAnnotation> Annotations => _annotations;

        /// <summary>
        /// Whether an annotation is on this page.
        /// </summary>
        internal bool HasAnnotation(HpdfAnnotation annotation) => _annotations.Contains(annotation);

        /// <summary>
        /// Gets the contents stream for this page.
        /// </summary>
        public HpdfStreamObject Contents => _contents;

        /// <summary>
        /// Gets the underlying stream to write PDF operators to.
        /// Implementation of IDrawable interface.
        /// </summary>
        public HpdfMemoryStream Stream => _contents.Stream;

        /// <summary>
        /// Gets the current graphics state.
        /// </summary>
        public HpdfGraphicsState GraphicsState => _graphicsState;

        /// <summary>
        /// Gets or sets the current position in the path.
        /// </summary>
        public HpdfPoint CurrentPos
        {
            get => _currentPos;
            set => _currentPos = value;
        }

        /// <summary>
        /// Gets or sets whether a path is being built (see <see cref="IDrawable.PathInProgress"/>).
        /// </summary>
        public bool PathInProgress { get; set; }

        /// <summary>
        /// Gets or sets the current text matrix.
        /// </summary>
        public HpdfTransMatrix TextMatrix
        {
            get => _textMatrix;
            internal set => _textMatrix = value;
        }

        /// <summary>
        /// Gets or sets the current text line matrix.
        /// </summary>
        public HpdfTransMatrix TextLineMatrix
        {
            get => _textLineMatrix;
            internal set => _textLineMatrix = value;
        }

        /// <summary>
        /// Gets or sets the current font.
        /// Font is part of graphics state and is saved/restored with GSave/GRestore.
        /// </summary>
        public HpdfFont? CurrentFont
        {
            get => _graphicsState.CurrentFont;
            internal set => _graphicsState.CurrentFont = value;
        }

        /// <summary>
        /// Gets or sets the page width in points.
        /// </summary>
        public float Width
        {
            get
            {
                var mediaBox = GetMediaBox();
                return mediaBox[2] - mediaBox[0];
            }
            set
            {
                // PDF spec typically limits page dimensions to 14,400 points (200 inches)
                // Use a generous limit of 100,000 points to reject clearly invalid values
                if (value <= 0 || value > 100000 || float.IsNaN(value) || float.IsInfinity(value))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid page width: {value}");

                var mediaBox = GetMediaBox();
                SetMediaBox(mediaBox[0], mediaBox[1], mediaBox[0] + value, mediaBox[3]);
            }
        }

        /// <summary>
        /// Gets or sets the page height in points.
        /// </summary>
        public float Height
        {
            get
            {
                var mediaBox = GetMediaBox();
                return mediaBox[3] - mediaBox[1];
            }
            set
            {
                // PDF spec typically limits page dimensions to 14,400 points (200 inches)
                // Use a generous limit of 100,000 points to reject clearly invalid values
                if (value <= 0 || value > 100000 || float.IsNaN(value) || float.IsInfinity(value))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid page height: {value}");

                var mediaBox = GetMediaBox();
                SetMediaBox(mediaBox[0], mediaBox[1], mediaBox[2], mediaBox[1] + value);
            }
        }

        /// <summary>
        /// Gets or sets the clockwise rotation of the page when it is displayed or printed (/Rotate),
        /// in degrees. The value must be a multiple of 90 and is stored as 0, 90, 180 or 270
        /// (for example, -90 is stored as 270). The page content and its coordinate system are not changed.
        /// </summary>
        public int Rotate
        {
            get => _dict.TryGetValue("Rotate", out var rotate) && rotate is HpdfNumber number ? number.Value : 0;
            set
            {
                if (value % 90 != 0)
                    throw new HpdfException(HpdfErrorCode.PageInvalidRotateValue,
                        $"Page rotation must be a multiple of 90 degrees: {value}");

                int normalized = ((value % 360) + 360) % 360;
                if (normalized == 0)
                    _dict.Remove("Rotate");
                else
                    _dict["Rotate"] = new HpdfNumber(normalized);
            }
        }

        /// <summary>
        /// Creates a new page with default size.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        public HpdfPage(HpdfXref xref)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");

            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("Page"));

            // Create default MediaBox (0, 0, 595.276, 841.89) - A4 size
            var mediaBox = new HpdfArray();
            mediaBox.Add(new HpdfNumber(0));
            mediaBox.Add(new HpdfNumber(0));
            mediaBox.Add(new HpdfReal(DefaultWidth));
            mediaBox.Add(new HpdfReal(DefaultHeight));
            _dict.Add("MediaBox", mediaBox);

            // Create contents stream
            _contents = new HpdfStreamObject();
            xref.Add(_contents);
            _dict.Add("Contents", _contents);

            // Default compression mode - will be set by document when page is added
            _compressionMode = HpdfCompressionMode.None;

            // Create empty Resources dictionary
            var resources = new HpdfDict();
            _dict.Add("Resources", resources);

            // Initialize graphics state
            _graphicsState = new HpdfGraphicsState();
            _currentPos = new HpdfPoint(0, 0);

            // Initialize text state
            _textMatrix = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);
            _textLineMatrix = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);
            _fontResources = new Dictionary<string, HpdfFont>();
            _extGStateResources = new Dictionary<string, HpdfExtGState>();
            _imageResources = new Dictionary<string, HpdfImage>();

            // Add page to xref
            xref.Add(_dict);
        }

        /// <summary>
        /// Pushes the current graphics state onto the stack.
        /// Implementation of IDrawable interface.
        /// </summary>
        public void PushGraphicsState()
        {
            _graphicsState = _graphicsState.Clone();
        }

        /// <summary>
        /// Pops the graphics state from the stack.
        /// Implementation of IDrawable interface.
        /// </summary>
        public void PopGraphicsState()
        {
            if (_graphicsState.Previous != null)
            {
                _graphicsState = _graphicsState.Previous;
            }
        }

        /// <summary>
        /// Gets the MediaBox array [llx, lly, urx, ury].
        /// </summary>
        /// <returns>Array of four floats representing the media box.</returns>
        private float[] GetMediaBox()
        {
            if (!_dict.TryGetValue("MediaBox", out var mediaBoxObjRaw))
                return new float[] { 0, 0, DefaultWidth, DefaultHeight };
            var mediaBoxObj = mediaBoxObjRaw as HpdfArray;
            if (mediaBoxObj == null || mediaBoxObj.Count != 4)
            {
                // Return default
                return new float[] { 0, 0, DefaultWidth, DefaultHeight };
            }

            var result = new float[4];
            for (int i = 0; i < 4; i++)
            {
                var item = mediaBoxObj[i];
                if (item is HpdfNumber num)
                    result[i] = num.Value;
                else if (item is HpdfReal real)
                    result[i] = real.Value;
                else
                    result[i] = 0;
            }
            return result;
        }

        /// <summary>
        /// Sets the MediaBox for this page.
        /// </summary>
        /// <param name="llx">Lower-left x coordinate.</param>
        /// <param name="lly">Lower-left y coordinate.</param>
        /// <param name="urx">Upper-right x coordinate.</param>
        /// <param name="ury">Upper-right y coordinate.</param>
        public void SetMediaBox(float llx, float lly, float urx, float ury)
        {
            var mediaBox = new HpdfArray();
            mediaBox.Add(new HpdfReal(llx));
            mediaBox.Add(new HpdfReal(lly));
            mediaBox.Add(new HpdfReal(urx));
            mediaBox.Add(new HpdfReal(ury));
            _dict["MediaBox"] = mediaBox;
        }

        /// <summary>
        /// Sets the page size using a predefined size and orientation.
        /// </summary>
        /// <param name="size">The page size.</param>
        /// <param name="direction">The page orientation.</param>
        public void SetSize(HpdfPageSize size, HpdfPageDirection direction)
        {
            HpdfPageSizes.GetSize(size, direction, out float width, out float height);
            SetMediaBox(0, 0, width, height);
        }

        /// <summary>
        /// Sets the page size using custom width and height.
        /// </summary>
        /// <param name="width">The page width in points.</param>
        /// <param name="height">The page height in points.</param>
        public void SetSize(float width, float height)
        {
            SetMediaBox(0, 0, width, height);
        }

        /// <summary>
        /// Gets the Resources dictionary for this page.
        /// </summary>
        /// <returns>The resources dictionary.</returns>
        public HpdfDict GetResources()
        {
            HpdfDict? resources = null;
            if (_dict.TryGetValue("Resources", out var resourcesObj))
                resources = resourcesObj as HpdfDict;
            if (resources is null)
            {
                resources = new HpdfDict();
                _dict.Add("Resources", resources);
            }
            return resources;
        }

        /// <summary>
        /// Adds a font to the page resources.
        /// </summary>
        /// <param name="font">The font to add.</param>
        internal void AddFontResource(HpdfFont font)
        {
            if (font is null)
                return;

            // Check if already added
            if (_fontResources.TryGetValue(font.LocalName, out var existingFont))
            {
                EnsureSameResource(existingFont.Dict, font.Dict, font.LocalName);
                return;
            }

            _fontResources[font.LocalName] = font;

            // Add to Resources/Font dictionary
            var resources = GetResources();
            HpdfDict? fontDict;
            if (resources.TryGetValue("Font", out var fontObj))
            {
                fontDict = fontObj as HpdfDict;
            }
            else
            {
                fontDict = new HpdfDict();
                resources["Font"] = fontDict;
            }

            if (fontDict != null)
            {
                fontDict[font.LocalName] = font.Dict;
            }
            else
            {
                fontDict = new HpdfDict();
                fontDict[font.LocalName] = font.Dict;
                resources["Font"] = fontDict;
            }
        }

        /// <summary>
        /// A page resource name refers to one object: using a second, different object under a name
        /// already in use would silently draw with the first one.
        /// </summary>
        private static void EnsureSameResource(HpdfDict existing, HpdfDict added, string localName)
        {
            if (!ReferenceEquals(existing, added))
                throw new HpdfException(HpdfErrorCode.DuplicateRegistration,
                    $"The page already uses another resource named '{localName}'; give each resource a unique local name");
        }

        /// <summary>
        /// Adds an extended graphics state resource to the page.
        /// </summary>
        /// <param name="extGState">The extended graphics state to add.</param>
        internal void AddExtGStateResource(HpdfExtGState extGState)
        {
            if (extGState is null)
                return;

            // Check if already added
            if (_extGStateResources.TryGetValue(extGState.LocalName, out var existingState))
            {
                EnsureSameResource(existingState.Dict, extGState.Dict, extGState.LocalName);
                return;
            }

            _extGStateResources[extGState.LocalName] = extGState;

            // Add to Resources/ExtGState dictionary
            var resources = GetResources();
            HpdfDict? extGStateDict;
            if (resources.TryGetValue("ExtGState", out var extGStateObj))
            {
                extGStateDict = extGStateObj as HpdfDict;
            }
            else
            {
                extGStateDict = new HpdfDict();
                resources["ExtGState"] = extGStateDict;
            }

            if (extGStateDict != null)
            {
                extGStateDict[extGState.LocalName] = extGState.Dict;
            }
            else
            {
                extGStateDict = new HpdfDict();
                extGStateDict[extGState.LocalName] = extGState.Dict;
                resources["ExtGState"] = extGStateDict;
            }
        }

        /// <summary>
        /// Adds an image resource to the page.
        /// </summary>
        /// <param name="image">The image to add.</param>
        internal void AddImageResource(HpdfImage image)
        {
            if (image is null)
                return;

            // Check if already added
            if (_imageResources.TryGetValue(image.LocalName, out var existingImage))
            {
                EnsureSameResource(existingImage.Dict, image.Dict, image.LocalName);
                return;
            }

            _imageResources[image.LocalName] = image;

            // Add to Resources/XObject dictionary
            var resources = GetResources();
            HpdfDict? xobjectDict;
            if (resources.TryGetValue("XObject", out var xobjectObj))
            {
                xobjectDict = xobjectObj as HpdfDict;
            }
            else
            {
                xobjectDict = new HpdfDict();
                resources["XObject"] = xobjectDict;
            }

            if (xobjectDict != null)
            {
                xobjectDict[image.LocalName] = image.Dict;
            }
            else
            {
                xobjectDict = new HpdfDict();
                xobjectDict[image.LocalName] = image.Dict;
                resources["XObject"] = xobjectDict;
            }
        }

        /// <summary>
        /// Creates a new text annotation (sticky note) on this page.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="text">The text content of the annotation.</param>
        /// <param name="icon">The icon to display (default: Note).</param>
        /// <returns>The created text annotation.</returns>
        public HpdfTextAnnotation CreateTextAnnotation(HpdfRect rect, string text,
            HpdfAnnotationIcon icon = HpdfAnnotationIcon.Note)
        {
            var annot = new HpdfTextAnnotation(_xref, rect, text, icon);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a new URI link annotation on this page.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="uri">The URI to link to.</param>
        /// <returns>The created link annotation.</returns>
        public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, string uri)
        {
            var annot = new HpdfLinkAnnotation(_xref, rect, uri);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a new internal link annotation (GoTo) on this page.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="destination">The destination array.</param>
        /// <returns>The created link annotation.</returns>
        public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfArray destination)
        {
            var annot = new HpdfLinkAnnotation(_xref, rect, destination);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a new internal link annotation (GoTo) on this page.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="destination">The destination, with its view set (SetXYZ, SetFit, ...).</param>
        /// <returns>The created link annotation.</returns>
        public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfDestination destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");

            return CreateLinkAnnotation(rect, destination.DestArray);
        }

        /// <summary>
        /// Creates a new internal link annotation (GoTo) to a named destination on this page; the target of the
        /// named destination may be set later, before the document is saved.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="destination">A named destination of this document.</param>
        /// <returns>The created link annotation.</returns>
        public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfNamedDestination destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");

            var annot = new HpdfLinkAnnotation(_xref, rect, destination);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a square annotation on this page: a rectangle drawn inside <paramref name="rect"/> with the
        /// annotation color (red until changed) and filled with the interior color, if one is set.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created square annotation.</returns>
        public HpdfShapeAnnotation CreateSquareAnnotation(HpdfRect rect, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");

            ValidateRect(rect);
            var annot = new HpdfShapeAnnotation(_xref, HpdfAnnotationType.Square, rect, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a circle annotation on this page: an ellipse inscribed in <paramref name="rect"/>, drawn with
        /// the annotation color (red until changed) and filled with the interior color, if one is set.
        /// </summary>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created circle annotation.</returns>
        public HpdfShapeAnnotation CreateCircleAnnotation(HpdfRect rect, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");

            ValidateRect(rect);
            var annot = new HpdfShapeAnnotation(_xref, HpdfAnnotationType.Circle, rect, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a line annotation on this page: a line from <paramref name="start"/> to <paramref name="end"/>
        /// in the annotation color (red until changed), with optional line endings, leader lines and caption.
        /// The annotation rectangle is computed from the line when the document is saved.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window and as the caption; may be empty.</param>
        /// <returns>The created line annotation.</returns>
        public HpdfLineAnnotation CreateLineAnnotation(HpdfPoint start, HpdfPoint end, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            if (!float.IsFinite(start.X) || !float.IsFinite(start.Y) || !float.IsFinite(end.X) || !float.IsFinite(end.Y))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Line points must be finite numbers");

            var annot = new HpdfLineAnnotation(_xref, start, end, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a highlight annotation on this page: the marked text is painted over in the annotation
        /// color (yellow until changed) with the Multiply blend mode, so that it stays readable.
        /// </summary>
        /// <param name="rect">The rectangle of the marked text; for several lines call
        /// <see cref="HpdfTextMarkupAnnotation.SetQuadPoints(HpdfRect[])"/>.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created highlight annotation.</returns>
        public HpdfTextMarkupAnnotation CreateHighlightAnnotation(HpdfRect rect, string text) =>
            CreateTextMarkupAnnotation(HpdfAnnotationType.Highlight, rect, text);

        /// <summary>
        /// Creates an underline annotation on this page, drawn in the annotation color (red until changed).
        /// </summary>
        /// <param name="rect">The rectangle of the marked text; for several lines call
        /// <see cref="HpdfTextMarkupAnnotation.SetQuadPoints(HpdfRect[])"/>.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created underline annotation.</returns>
        public HpdfTextMarkupAnnotation CreateUnderlineAnnotation(HpdfRect rect, string text) =>
            CreateTextMarkupAnnotation(HpdfAnnotationType.Underline, rect, text);

        /// <summary>
        /// Creates a squiggly underline annotation on this page (PDF 1.4), drawn in the annotation color
        /// (red until changed).
        /// </summary>
        /// <param name="rect">The rectangle of the marked text; for several lines call
        /// <see cref="HpdfTextMarkupAnnotation.SetQuadPoints(HpdfRect[])"/>.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created squiggly annotation.</returns>
        public HpdfTextMarkupAnnotation CreateSquigglyAnnotation(HpdfRect rect, string text) =>
            CreateTextMarkupAnnotation(HpdfAnnotationType.Squiggly, rect, text);

        /// <summary>
        /// Creates a strikeout annotation on this page, drawn in the annotation color (red until changed).
        /// </summary>
        /// <param name="rect">The rectangle of the marked text; for several lines call
        /// <see cref="HpdfTextMarkupAnnotation.SetQuadPoints(HpdfRect[])"/>.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created strikeout annotation.</returns>
        public HpdfTextMarkupAnnotation CreateStrikeOutAnnotation(HpdfRect rect, string text) =>
            CreateTextMarkupAnnotation(HpdfAnnotationType.StrikeOut, rect, text);

        /// <summary>
        /// Checks that an annotation rectangle has finite coordinates (before the annotation is created).
        /// </summary>
        private static void ValidateRect(HpdfRect rect)
        {
            if (!float.IsFinite(rect.Left) || !float.IsFinite(rect.Bottom) || !float.IsFinite(rect.Right) || !float.IsFinite(rect.Top))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Rectangle coordinates must be finite numbers");
        }

        private HpdfTextMarkupAnnotation CreateTextMarkupAnnotation(HpdfAnnotationType type, HpdfRect rect, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);

            var annot = new HpdfTextMarkupAnnotation(_xref, type, rect, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a free text annotation on this page: text shown in a box, wrapped to its width, drawn with
        /// the given font in the text color (black until changed), with a border of the border width (1 until
        /// changed by <see cref="HpdfAnnotation.SetBorderStyle"/>) and no background until a color is set.
        /// </summary>
        /// <param name="rect">The text box.</param>
        /// <param name="text">The text; may be empty.</param>
        /// <param name="font">The font of the text (any font of the document; not a vertical one).</param>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The created free text annotation.</returns>
        public HpdfFreeTextAnnotation CreateFreeTextAnnotation(HpdfRect rect, string text, HpdfFont font, float fontSize)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfFreeTextAnnotation.ValidateFont(font, fontSize);

            ValidateRect(rect);
            var annot = new HpdfFreeTextAnnotation(_xref, rect, text, font, fontSize);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a rubber stamp annotation on this page: a standard stamp (Approved, Draft, ...) drawn as a
        /// frame with rounded corners and the stamp's label in capitals, filling <paramref name="rect"/>.
        /// </summary>
        /// <param name="rect">The box of the stamp.</param>
        /// <param name="name">The standard stamp.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <param name="font">The font of the label (any font of the document; not a vertical one).</param>
        /// <returns>The created stamp annotation.</returns>
        public HpdfStampAnnotation CreateStampAnnotation(HpdfRect rect, HpdfStampName name, string text, HpdfFont font)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfStampAnnotation.ValidateArguments(name, font);

            ValidateRect(rect);
            var annot = new HpdfStampAnnotation(_xref, rect, name, text, font);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a polygon annotation on this page (PDF 1.5): a closed shape through the vertices, drawn in the
        /// annotation color (red until changed) and filled with the interior color, if one is set. The annotation
        /// rectangle is computed from the vertices when the document is saved.
        /// </summary>
        /// <param name="vertices">The vertices, at least three, in page coordinates.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created polygon annotation.</returns>
        public HpdfPolygonAnnotation CreatePolygonAnnotation(HpdfPoint[] vertices, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfPolygonalAnnotation.ValidateVertices(vertices, HpdfPolygonAnnotation.MinimumVertexCount);

            var annot = new HpdfPolygonAnnotation(_xref, vertices, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a polyline annotation on this page (PDF 1.5): connected lines through the vertices in the
        /// annotation color (red until changed), with optional line endings. The annotation rectangle is computed
        /// from the vertices when the document is saved.
        /// </summary>
        /// <param name="vertices">The vertices, at least two, in page coordinates.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created polyline annotation.</returns>
        public HpdfPolyLineAnnotation CreatePolyLineAnnotation(HpdfPoint[] vertices, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfPolygonalAnnotation.ValidateVertices(vertices, HpdfPolyLineAnnotation.MinimumVertexCount);

            var annot = new HpdfPolyLineAnnotation(_xref, vertices, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates an ink annotation on this page: freehand strokes drawn in the annotation color (red until
        /// changed) with the border width. The annotation rectangle is computed from the points.
        /// </summary>
        /// <param name="strokes">The strokes, each a path of points in page coordinates.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created ink annotation.</returns>
        public HpdfInkAnnotation CreateInkAnnotation(HpdfPoint[][] strokes, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfInkAnnotation.ValidateStrokes(strokes);

            var annot = new HpdfInkAnnotation(_xref, strokes, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a caret annotation on this page (PDF 1.5): a caret that marks where text is to be inserted,
        /// filled with the annotation color (blue until changed).
        /// </summary>
        /// <param name="rect">The annotation rectangle; the caret fills it.</param>
        /// <param name="text">The text of the annotation (what to insert), shown in its pop-up window; may be empty.</param>
        /// <returns>The created caret annotation.</returns>
        public HpdfCaretAnnotation CreateCaretAnnotation(HpdfRect rect, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);

            var annot = new HpdfCaretAnnotation(_xref, rect, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a file attachment annotation on this page: an icon that opens an embedded file, such as a file
        /// attached to the document (<see cref="HpdfDocument.AttachFile(string, byte[], string?)"/>), which then
        /// is embedded once.
        /// </summary>
        /// <param name="rect">The annotation rectangle; the icon fills it (about 20 x 20 points is usual).</param>
        /// <param name="file">The embedded file.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <param name="icon">The icon (a push pin by default).</param>
        /// <returns>The created file attachment annotation.</returns>
        public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, HpdfEmbeddedFile file, string text,
            HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)
        {
            if (file is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "File cannot be null");
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);
            HpdfFileAttachmentAnnotation.ValidateIcon(icon);

            var annot = new HpdfFileAttachmentAnnotation(_xref, rect, file, text, icon);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a file attachment annotation on this page with a file of its own (not listed among the
        /// document's attachments).
        /// </summary>
        /// <param name="rect">The annotation rectangle; the icon fills it (about 20 x 20 points is usual).</param>
        /// <param name="fileName">The file name, without a directory.</param>
        /// <param name="data">The file's contents (copied).</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <param name="icon">The icon (a push pin by default).</param>
        /// <returns>The created file attachment annotation.</returns>
        public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, string fileName, byte[] data, string text,
            HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);
            HpdfFileAttachmentAnnotation.ValidateIcon(icon);
            HpdfEmbeddedFile.ValidateArguments(fileName, data, null);

            return CreateFileAttachmentAnnotation(rect, new HpdfEmbeddedFile(_xref, fileName, data, null), text, icon);
        }

        /// <summary>
        /// Creates a redaction annotation on this page (PDF 1.7): it marks content that a viewer which applies
        /// redactions removes; the library itself removes nothing. The area is shown as an outline in the
        /// annotation color (red until changed).
        /// </summary>
        /// <param name="rect">The area to redact; for several areas call
        /// <see cref="HpdfRedactAnnotation.SetQuadPoints(HpdfRect[])"/>.</param>
        /// <param name="text">The text of the annotation, shown in its pop-up window; may be empty.</param>
        /// <returns>The created redaction annotation.</returns>
        public HpdfRedactAnnotation CreateRedactAnnotation(HpdfRect rect, string text)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);

            var annot = new HpdfRedactAnnotation(_xref, rect, text);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a 3D annotation on this page (PDF 1.6, HPDF_Page_Create3DAnnot): the model is shown in the
        /// rectangle when the page opens, in viewers that show 3D (Adobe Acrobat and Reader, Foxit). Other viewers
        /// show the poster image, or a gray box with a cube when no image is given.
        /// </summary>
        /// <param name="rect">The area of the page that shows the model.</param>
        /// <param name="model">The model (<see cref="HpdfDocument.Load3DModel(string)"/>).</param>
        /// <param name="text">The text of the annotation, a description of the model.</param>
        /// <param name="poster">The image shown before the model is activated, or null.</param>
        /// <returns>The created 3D annotation.</returns>
        public Hpdf3DAnnotation Create3DAnnotation(HpdfRect rect, Haru.ThreeD.Hpdf3DModel model, string text = "3D Model",
            HpdfImage? poster = null)
        {
            if (model is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Model cannot be null");
            if (!ReferenceEquals(model.Xref, _xref))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The model belongs to another document");
            if (poster != null && !ReferenceEquals(poster.Xref, _xref))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The poster image belongs to another document");
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            ValidateRect(rect);

            var annot = new Hpdf3DAnnotation(_xref, rect, model, text, poster);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a projection annotation on this page (Adobe ExtensionLevel 3): it makes a 3D measurement part of
        /// the comments of the page. It has no appearance and no area of its own (/Rect [0 0 0 0]): the 3D viewer
        /// draws the measurement, and Adobe Acrobat fails on a projection annotation with an area.
        /// </summary>
        /// <param name="text">The text of the annotation, shown in the comments list.</param>
        /// <param name="measure">The measurement; one projection annotation per measurement.</param>
        /// <returns>The created projection annotation.</returns>
        public HpdfProjectionAnnotation CreateProjectionAnnotation(string text, Haru.ThreeD.Hpdf3DMeasure measure)
        {
            if (measure is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Measure cannot be null");
            if (measure.ProjectionAnnotation != null)
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "The measurement already has a projection annotation");
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");

            var annot = new HpdfProjectionAnnotation(_xref, text, measure);
            AddAnnotation(annot);
            return annot;
        }

        /// <summary>
        /// Creates a pop-up annotation that shows the text of a markup annotation of this page, and
        /// connects the two (/Parent of the pop-up, /Popup of the markup annotation).
        /// </summary>
        /// <param name="rect">The rectangle of the pop-up window.</param>
        /// <param name="parent">The markup annotation; it must be on this page and have no pop-up yet.</param>
        /// <returns>The created pop-up annotation.</returns>
        public HpdfPopupAnnotation CreatePopupAnnotation(HpdfRect rect, HpdfMarkupAnnotation parent)
        {
            if (parent is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Parent annotation cannot be null");
            if (!HasAnnotation(parent))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The parent annotation is not on this page");
            if (parent.Popup != null)
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "The annotation already has a pop-up");

            ValidateRect(rect);
            var popup = new HpdfPopupAnnotation(_xref, rect, parent);
            parent.AttachPopup(popup);
            AddAnnotation(popup);
            return popup;
        }

        /// <summary>
        /// Adds a widget annotation (form field) to this page.
        /// This method is used to place form field widgets on the page.
        /// </summary>
        /// <param name="widget">The widget annotation to add.</param>
        public void AddWidgetAnnotation(HpdfWidgetAnnotation widget)
        {
            if (widget is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Widget cannot be null");

            // A widget appears on exactly one page
            if (widget.Dict.TryGetValue("P", out var page) && !ReferenceEquals(page, _dict))
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "The widget is already placed on another page");

            // Set page reference in widget
            widget.SetPage(_dict);

            // Add to page's Annots array
            AddAnnotation(widget);
        }

        /// <summary>
        /// Adds an annotation to the page's Annots array.
        /// </summary>
        /// <param name="annot">The annotation to add.</param>
        private void AddAnnotation(HpdfAnnotation annot)
        {
            // Find or create the Annots array
            HpdfArray? annotsArray;
            if (_dict.TryGetValue("Annots", out var annotsObj))
            {
                annotsArray = annotsObj as HpdfArray;
                if (annotsArray is null)
                    throw new HpdfException(HpdfErrorCode.InvalidObject, "Annots is not an array");
            }
            else
            {
                annotsArray = new HpdfArray();
                _dict.Add("Annots", annotsArray);
            }

            // Adding the same annotation again changes nothing
            for (int i = 0; i < annotsArray.Count; i++)
            {
                if (ReferenceEquals(annotsArray[i], annot.Dict))
                    return;
            }

            annotsArray.Add(annot.Dict);
            _annotations.Add(annot);
        }

        /// <summary>
        /// Sets the CropBox for this page.
        /// The CropBox defines the region to which the contents of the page shall be clipped when displayed or printed.
        /// </summary>
        /// <param name="left">Lower-left x coordinate.</param>
        /// <param name="bottom">Lower-left y coordinate.</param>
        /// <param name="right">Upper-right x coordinate.</param>
        /// <param name="top">Upper-right y coordinate.</param>
        public void SetCropBox(float left, float bottom, float right, float top)
        {
            var cropBox = new HpdfArray();
            cropBox.Add(new HpdfReal(left));
            cropBox.Add(new HpdfReal(bottom));
            cropBox.Add(new HpdfReal(right));
            cropBox.Add(new HpdfReal(top));
            _dict["CropBox"] = cropBox;
        }

        /// <summary>
        /// Sets the CropBox for this page using an HpdfBox structure.
        /// </summary>
        /// <param name="box">The crop box.</param>
        public void SetCropBox(HpdfBox box)
        {
            SetCropBox(box.Left, box.Bottom, box.Right, box.Top);
        }

        /// <summary>
        /// Gets the CropBox for this page, if set.
        /// </summary>
        /// <returns>The crop box, or null if not set.</returns>
        public HpdfBox? GetCropBox()
        {
            if (!_dict.TryGetValue("CropBox", out var cropBoxObj))
                return null;

            if (cropBoxObj is HpdfArray arr && arr.Count == 4)
            {
                float[] values = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    if (arr[i] is HpdfNumber num)
                        values[i] = num.Value;
                    else if (arr[i] is HpdfReal real)
                        values[i] = real.Value;
                }
                return new HpdfBox(values[0], values[1], values[2], values[3]);
            }
            return null;
        }

        /// <summary>
        /// Sets the BleedBox for this page.
        /// The BleedBox defines the region to which the contents of the page shall be clipped
        /// when output in a production environment (e.g., for printing with bleed).
        /// </summary>
        /// <param name="left">Lower-left x coordinate.</param>
        /// <param name="bottom">Lower-left y coordinate.</param>
        /// <param name="right">Upper-right x coordinate.</param>
        /// <param name="top">Upper-right y coordinate.</param>
        public void SetBleedBox(float left, float bottom, float right, float top)
        {
            var bleedBox = new HpdfArray();
            bleedBox.Add(new HpdfReal(left));
            bleedBox.Add(new HpdfReal(bottom));
            bleedBox.Add(new HpdfReal(right));
            bleedBox.Add(new HpdfReal(top));
            _dict["BleedBox"] = bleedBox;
        }

        /// <summary>
        /// Sets the BleedBox for this page using an HpdfBox structure.
        /// </summary>
        /// <param name="box">The bleed box.</param>
        public void SetBleedBox(HpdfBox box)
        {
            SetBleedBox(box.Left, box.Bottom, box.Right, box.Top);
        }

        /// <summary>
        /// Gets the BleedBox for this page, if set.
        /// </summary>
        /// <returns>The bleed box, or null if not set.</returns>
        public HpdfBox? GetBleedBox()
        {
            if (!_dict.TryGetValue("BleedBox", out var bleedBoxObj))
                return null;

            if (bleedBoxObj is HpdfArray arr && arr.Count == 4)
            {
                float[] values = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    if (arr[i] is HpdfNumber num)
                        values[i] = num.Value;
                    else if (arr[i] is HpdfReal real)
                        values[i] = real.Value;
                }
                return new HpdfBox(values[0], values[1], values[2], values[3]);
            }
            return null;
        }

        /// <summary>
        /// Sets the TrimBox for this page.
        /// The TrimBox defines the intended dimensions of the finished page after trimming.
        /// </summary>
        /// <param name="left">Lower-left x coordinate.</param>
        /// <param name="bottom">Lower-left y coordinate.</param>
        /// <param name="right">Upper-right x coordinate.</param>
        /// <param name="top">Upper-right y coordinate.</param>
        public void SetTrimBox(float left, float bottom, float right, float top)
        {
            var trimBox = new HpdfArray();
            trimBox.Add(new HpdfReal(left));
            trimBox.Add(new HpdfReal(bottom));
            trimBox.Add(new HpdfReal(right));
            trimBox.Add(new HpdfReal(top));
            _dict["TrimBox"] = trimBox;
        }

        /// <summary>
        /// Sets the TrimBox for this page using an HpdfBox structure.
        /// </summary>
        /// <param name="box">The trim box.</param>
        public void SetTrimBox(HpdfBox box)
        {
            SetTrimBox(box.Left, box.Bottom, box.Right, box.Top);
        }

        /// <summary>
        /// Gets the TrimBox for this page, if set.
        /// </summary>
        /// <returns>The trim box, or null if not set.</returns>
        public HpdfBox? GetTrimBox()
        {
            if (!_dict.TryGetValue("TrimBox", out var trimBoxObj))
                return null;

            if (trimBoxObj is HpdfArray arr && arr.Count == 4)
            {
                float[] values = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    if (arr[i] is HpdfNumber num)
                        values[i] = num.Value;
                    else if (arr[i] is HpdfReal real)
                        values[i] = real.Value;
                }
                return new HpdfBox(values[0], values[1], values[2], values[3]);
            }
            return null;
        }

        /// <summary>
        /// Sets the ArtBox for this page.
        /// The ArtBox defines the extent of the page's meaningful content
        /// (including potential white space) as intended by the page's creator.
        /// </summary>
        /// <param name="left">Lower-left x coordinate.</param>
        /// <param name="bottom">Lower-left y coordinate.</param>
        /// <param name="right">Upper-right x coordinate.</param>
        /// <param name="top">Upper-right y coordinate.</param>
        public void SetArtBox(float left, float bottom, float right, float top)
        {
            var artBox = new HpdfArray();
            artBox.Add(new HpdfReal(left));
            artBox.Add(new HpdfReal(bottom));
            artBox.Add(new HpdfReal(right));
            artBox.Add(new HpdfReal(top));
            _dict["ArtBox"] = artBox;
        }

        /// <summary>
        /// Sets the ArtBox for this page using an HpdfBox structure.
        /// </summary>
        /// <param name="box">The art box.</param>
        public void SetArtBox(HpdfBox box)
        {
            SetArtBox(box.Left, box.Bottom, box.Right, box.Top);
        }

        /// <summary>
        /// Gets the ArtBox for this page, if set.
        /// </summary>
        /// <returns>The art box, or null if not set.</returns>
        public HpdfBox? GetArtBox()
        {
            if (!_dict.TryGetValue("ArtBox", out var artBoxObj))
                return null;

            if (artBoxObj is HpdfArray arr && arr.Count == 4)
            {
                float[] values = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    if (arr[i] is HpdfNumber num)
                        values[i] = num.Value;
                    else if (arr[i] is HpdfReal real)
                        values[i] = real.Value;
                }
                return new HpdfBox(values[0], values[1], values[2], values[3]);
            }
            return null;
        }

        /// <summary>
        /// Sets the compression mode for this page's content stream.
        /// </summary>
        /// <param name="mode">The compression mode to apply.</param>
        internal void SetCompressionMode(HpdfCompressionMode mode)
        {
            _compressionMode = mode;

            // Apply compression to content stream based on mode
            if ((mode & HpdfCompressionMode.Text) != 0)
            {
                // Text compression applies to page content streams
                _contents.Filter = HpdfStreamFilter.FlateDecode;
            }
            else
            {
                _contents.Filter = HpdfStreamFilter.None;
            }
        }

        /// <summary>
        /// Sets a transition effect (slide show) for this page.
        /// </summary>
        /// <param name="type">The transition style.</param>
        /// <param name="displayTime">Display time in seconds before transitioning.</param>
        /// <param name="transTime">Transition duration in seconds.</param>
        public void SetSlideShow(HpdfTransitionStyle type, float displayTime, float transTime)
        {
            // Create Trans dictionary for page transition
            var transDict = new HpdfDict();
            transDict.Add("Type", new HpdfName("Trans"));

            // Map transition style to PDF name
            string styleName = type switch
            {
                HpdfTransitionStyle.WipeRight => "Wipe",
                HpdfTransitionStyle.WipeUp => "Wipe",
                HpdfTransitionStyle.WipeLeft => "Wipe",
                HpdfTransitionStyle.WipeDown => "Wipe",
                HpdfTransitionStyle.BarnDoorsHorizontalOut => "Split",
                HpdfTransitionStyle.BarnDoorsHorizontalIn => "Split",
                HpdfTransitionStyle.BarnDoorsVerticalOut => "Split",
                HpdfTransitionStyle.BarnDoorsVerticalIn => "Split",
                HpdfTransitionStyle.BoxOut => "Box",
                HpdfTransitionStyle.BoxIn => "Box",
                HpdfTransitionStyle.BlindsHorizontal => "Blinds",
                HpdfTransitionStyle.BlindsVertical => "Blinds",
                HpdfTransitionStyle.Dissolve => "Dissolve",
                HpdfTransitionStyle.GlitterRight => "Glitter",
                HpdfTransitionStyle.GlitterDown => "Glitter",
                HpdfTransitionStyle.GlitterTopLeftToBottomRight => "Glitter",
                HpdfTransitionStyle.Replace => "R",
                _ => "R"
            };

            transDict.Add("S", new HpdfName(styleName));
            transDict.Add("D", new HpdfReal(transTime));

            // Add direction/dimension based on transition type
            if (type == HpdfTransitionStyle.WipeRight)
                transDict.Add("Di", new HpdfNumber(0));
            else if (type == HpdfTransitionStyle.WipeDown)
                transDict.Add("Di", new HpdfNumber(270));
            else if (type == HpdfTransitionStyle.WipeLeft)
                transDict.Add("Di", new HpdfNumber(180));
            else if (type == HpdfTransitionStyle.WipeUp)
                transDict.Add("Di", new HpdfNumber(90));
            else if (type == HpdfTransitionStyle.BarnDoorsHorizontalOut)
            {
                transDict.Add("Dm", new HpdfName("H"));
                transDict.Add("M", new HpdfName("O"));
            }
            else if (type == HpdfTransitionStyle.BarnDoorsHorizontalIn)
            {
                transDict.Add("Dm", new HpdfName("H"));
                transDict.Add("M", new HpdfName("I"));
            }
            else if (type == HpdfTransitionStyle.BarnDoorsVerticalOut)
            {
                transDict.Add("Dm", new HpdfName("V"));
                transDict.Add("M", new HpdfName("O"));
            }
            else if (type == HpdfTransitionStyle.BarnDoorsVerticalIn)
            {
                transDict.Add("Dm", new HpdfName("V"));
                transDict.Add("M", new HpdfName("I"));
            }
            else if (type == HpdfTransitionStyle.BoxOut)
                transDict.Add("M", new HpdfName("O"));
            else if (type == HpdfTransitionStyle.BoxIn)
                transDict.Add("M", new HpdfName("I"));
            else if (type == HpdfTransitionStyle.BlindsHorizontal)
                transDict.Add("Dm", new HpdfName("H"));
            else if (type == HpdfTransitionStyle.BlindsVertical)
                transDict.Add("Dm", new HpdfName("V"));
            else if (type == HpdfTransitionStyle.GlitterRight)
                transDict.Add("Di", new HpdfNumber(0));
            else if (type == HpdfTransitionStyle.GlitterDown)
                transDict.Add("Di", new HpdfNumber(270));
            else if (type == HpdfTransitionStyle.GlitterTopLeftToBottomRight)
                transDict.Add("Di", new HpdfNumber(315));

            // Assigned rather than added: a later call replaces the transition
            _dict["Trans"] = transDict;

            // Set page duration (display time); no duration means the viewer does not advance by itself
            if (displayTime > 0)
                _dict["Dur"] = new HpdfReal(displayTime);
            else
                _dict.Remove("Dur");
        }

        /// <summary>
        /// Disposes the page and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the page
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _contents?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
