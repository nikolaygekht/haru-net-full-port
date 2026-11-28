/* A form XObject: a piece of content drawn once and shown on any number of pages, at any size. */

using System.Collections.Generic;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// A form XObject (ISO 32000-1, 8.10): content drawn once - text, paths, images, other forms - that pages and
    /// other forms show with <see cref="HpdfPageExtensions.DrawFormXObject(HpdfCanvas, HpdfFormXObject, float, float)"/>,
    /// such as a letterhead, a watermark or a logo; the file holds it once however often it is shown. Draw into it
    /// with the same methods as on a page: it is an <see cref="HpdfCanvas"/>. Create it with
    /// <see cref="HpdfDocument.CreateFormXObject"/>.
    /// </summary>
    public sealed class HpdfFormXObject : HpdfCanvas
    {
        private readonly HashSet<HpdfFormXObject> _forms = new();

        /// <summary>
        /// Gets the resource name of the form on the canvases that show it.
        /// </summary>
        public string LocalName { get; }

        /// <summary>
        /// Gets the width of the form's box (/BBox) in its own units, points when drawn at full size.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height of the form's box (/BBox).
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the form XObject stream (/Type /XObject /Subtype /Form, /BBox, /Resources and the content).
        /// </summary>
        public HpdfStreamObject StreamObject => Contents;

        internal HpdfFormXObject(HpdfXref xref, string localName, float width, float height)
            : base(xref, new HpdfStreamObject())
        {
            LocalName = localName;
            Width = width;
            Height = height;

            var stream = Contents;
            stream.Add("Type", new HpdfName("XObject"));
            stream.Add("Subtype", new HpdfName("Form"));
            stream.Add("FormType", new HpdfNumber(1));
            stream.Add("BBox", new HpdfArray
            {
                new HpdfNumber(0), new HpdfNumber(0), new HpdfReal(width), new HpdfReal(height)
            });
            stream.Add("Resources", new HpdfDict());
            xref.Add(stream);
        }

        /// <inheritdoc/>
        public override HpdfDict GetResources()
        {
            if (Contents.TryGetValue("Resources", out var resources) && resources is HpdfDict dict)
                return dict;
            dict = new HpdfDict();
            Contents["Resources"] = dict;
            return dict;
        }

        /// <summary>
        /// Whether this form shows <paramref name="form"/>, directly or through other forms.
        /// </summary>
        internal bool Shows(HpdfFormXObject form)
        {
            foreach (var shown in _forms)
            {
                if (shown == form || shown.Shows(form))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remembers a form shown in this form, for the cycle check of <see cref="Shows"/>.
        /// </summary>
        internal void AddShownForm(HpdfFormXObject form) => _forms.Add(form);

        /// <summary>
        /// Compresses the content as the document's compression mode says (<see cref="HpdfCompressionMode.Text"/>).
        /// </summary>
        internal void ApplyCompressionMode(HpdfCompressionMode mode) =>
            Contents.Filter = (mode & HpdfCompressionMode.Text) != 0 ? HpdfStreamFilter.FlateDecode : HpdfStreamFilter.None;
    }
}
