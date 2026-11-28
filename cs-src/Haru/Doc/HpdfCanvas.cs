/* What pages and form XObjects have in common: a content stream, the graphics and text state, and resources. */

using System.Collections.Generic;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// A surface that content is drawn on: a page (<see cref="HpdfPage"/>) or a form XObject. It holds the content stream the operators are written to, the graphics and
    /// text state tracked while drawing, and the resources (fonts, images, extended graphics states, color spaces,
    /// form XObjects) the content refers to by name. The drawing methods (<see cref="HpdfPageText"/>,
    /// <see cref="HpdfPageGraphics"/>, <see cref="HpdfPageExtensions"/>) work on any canvas.
    /// </summary>
    public abstract class HpdfCanvas : IDrawable
    {
        private readonly HpdfXref _xref;
        private HpdfStreamObject _contents;
        private HpdfGraphicsState _graphicsState = new HpdfGraphicsState();

        // The objects registered under each resource name, by category (Font, XObject, ...): a name refers to one
        // object, and the profiles of the color spaces are needed for the PDF/A checks
        private readonly Dictionary<string, Dictionary<string, HpdfObject>> _resources = new();
        private readonly List<HpdfIccProfile> _colorSpaceProfiles = new();

        private protected HpdfCanvas(HpdfXref xref, HpdfStreamObject contents)
        {
            _xref = xref;
            _contents = contents;
        }

        /// <summary>
        /// The cross-reference table of the document the canvas belongs to.
        /// </summary>
        internal HpdfXref Xref => _xref;

        /// <summary>
        /// Gets the content stream that drawing operators are written to.
        /// </summary>
        public HpdfStreamObject Contents => _contents;

        /// <summary>
        /// Gets the underlying stream to write PDF operators to (of <see cref="Contents"/>).
        /// </summary>
        public HpdfMemoryStream Stream => _contents.Stream;

        /// <summary>
        /// Gets the current graphics state.
        /// </summary>
        public HpdfGraphicsState GraphicsState => _graphicsState;

        /// <summary>
        /// Gets or sets the current position in the path.
        /// </summary>
        public HpdfPoint CurrentPos { get; set; } = new HpdfPoint(0, 0);

        /// <summary>
        /// Gets or sets whether a path is being built (see <see cref="IDrawable.PathInProgress"/>).
        /// </summary>
        public bool PathInProgress { get; set; }

        /// <summary>
        /// Gets whether a text object is open: <c>BeginText</c> was called and <c>EndText</c> not yet.
        /// </summary>
        public bool InTextObject { get; internal set; }

        /// <summary>
        /// Gets or sets the current text matrix.
        /// </summary>
        public HpdfTransMatrix TextMatrix { get; internal set; } = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);

        /// <summary>
        /// Gets or sets the current text line matrix.
        /// </summary>
        public HpdfTransMatrix TextLineMatrix { get; internal set; } = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);

        /// <summary>
        /// Gets the current font. The font is part of the graphics state and is saved and restored with
        /// GSave/GRestore.
        /// </summary>
        public HpdfFont? CurrentFont
        {
            get => _graphicsState.CurrentFont;
            internal set => _graphicsState.CurrentFont = value;
        }

        /// <summary>
        /// Pushes the current graphics state onto the stack (GSave).
        /// </summary>
        public void PushGraphicsState()
        {
            _graphicsState = _graphicsState.Clone();
            OnDepthChanged(_graphicsState.Depth);
        }

        /// <summary>
        /// Pops the graphics state from the stack (GRestore).
        /// </summary>
        public void PopGraphicsState()
        {
            if (_graphicsState.Previous != null)
                _graphicsState = _graphicsState.Previous;
            OnDepthChanged(_graphicsState.Depth);
        }

        /// <summary>
        /// The scope a font set now belongs to (a page's current content stream), or null.
        /// </summary>
        internal virtual object? CurrentFontScope => null;

        /// <summary>
        /// Called when text is shown with the current font.
        /// </summary>
        internal virtual void OnTextShown()
        {
        }

        /// <summary>
        /// Called when GSave or GRestore changed the depth of the graphics state stack.
        /// </summary>
        private protected virtual void OnDepthChanged(int depth)
        {
        }

        /// <summary>
        /// Gets the resource dictionary of the canvas, creating it if needed.
        /// </summary>
        /// <returns>The resources dictionary.</returns>
        public abstract HpdfDict GetResources();

        /// <summary>
        /// Makes another stream the one drawing operators are written to (a new content stream of a page).
        /// </summary>
        private protected void SetContents(HpdfStreamObject contents) => _contents = contents;

        /// <summary>
        /// Gets the ICC profiles of the color spaces the canvas draws with (<c>SetIccFill</c>, <c>SetIccStroke</c>).
        /// </summary>
        internal IEnumerable<HpdfIccProfile> ColorSpaceProfiles => _colorSpaceProfiles;

        /// <summary>
        /// Adds a font to the resources.
        /// </summary>
        internal void AddFontResource(HpdfFont font)
        {
            if (font != null)
                AddResource("Font", font.LocalName, font.Dict, font.Dict);
        }

        /// <summary>
        /// Adds an extended graphics state to the resources.
        /// </summary>
        internal void AddExtGStateResource(HpdfExtGState extGState)
        {
            if (extGState != null)
                AddResource("ExtGState", extGState.LocalName, extGState.Dict, extGState.Dict);
        }

        /// <summary>
        /// Adds an image to the resources (/XObject).
        /// </summary>
        internal void AddImageResource(HpdfImage image)
        {
            if (image != null)
                AddResource("XObject", image.LocalName, image.Dict, image.Dict);
        }

        /// <summary>
        /// Adds the ICC-based color space of a profile to the resources (/ColorSpace), under the profile's name.
        /// </summary>
        internal void AddColorSpaceResource(HpdfIccProfile profile)
        {
            if (AddResource("ColorSpace", profile.LocalName, profile.ColorSpace, profile.StreamObject))
                _colorSpaceProfiles.Add(profile);
        }

        /// <summary>
        /// Adds a form XObject to the resources (/XObject); a form remembers the forms it shows.
        /// </summary>
        internal void AddFormResource(HpdfFormXObject form)
        {
            AddResource("XObject", form.LocalName, form.StreamObject, form.StreamObject);
            if (this is HpdfFormXObject self)
                self.AddShownForm(form);
        }

        /// <summary>
        /// Registers an object under a resource name of a category; a name refers to one object (its identity):
        /// using a second, different object under a name already in use would silently draw with the first one.
        /// </summary>
        /// <returns>Whether the resource was new.</returns>
        private protected bool AddResource(string category, string name, HpdfObject value, HpdfObject identity)
        {
            if (!_resources.TryGetValue(category, out var names))
            {
                names = new Dictionary<string, HpdfObject>();
                _resources.Add(category, names);
            }

            if (names.TryGetValue(name, out var existing))
            {
                if (!ReferenceEquals(existing, identity))
                    throw new HpdfException(HpdfErrorCode.DuplicateRegistration,
                        $"The page already uses another resource named '{name}'; give each resource a unique local name");
                OnResourceUsed(category, name, value, identity);
                return false;
            }
            names.Add(name, identity);

            var resources = GetResources();
            if (!resources.TryGetValue(category, out var categoryObj) || categoryObj is not HpdfDict categoryDict)
            {
                categoryDict = new HpdfDict();
                resources[category] = categoryDict;
            }
            categoryDict[name] = value;
            OnResourceUsed(category, name, value, identity);
            return true;
        }

        /// <summary>
        /// Adds the resources a shared content stream uses (registered like the canvas's own).
        /// </summary>
        private protected void AddResources(IEnumerable<(string Category, string Name, HpdfObject Value, HpdfObject Identity)> resources)
        {
            foreach (var (category, name, value, identity) in resources)
                AddResource(category, name, value, identity);
        }

        /// <summary>
        /// Called whenever content uses a resource (the first time and again).
        /// </summary>
        private protected virtual void OnResourceUsed(string category, string name, HpdfObject value, HpdfObject identity)
        {
        }
    }
}
