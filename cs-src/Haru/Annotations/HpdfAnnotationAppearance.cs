/* The generated normal appearance stream (/AP /N) of a markup annotation. */
using System;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// The normal appearance of an annotation: a Form XObject whose bounding box is the annotation
    /// rectangle, drawn in page coordinates (no /Matrix, so the box maps onto /Rect unscaled).
    /// The stream object is created once with the annotation and redrawn on every save, so that
    /// saving is repeatable and no object is created while the document is written.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "The appearance stream is registered in the cross-reference table, which owns every PDF object of the document; the annotation only keeps a reference to redraw it.")]
    internal sealed class HpdfAnnotationAppearance
    {
        private const string GraphicsStateName = "GS0";

        private readonly HpdfStreamObject _stream;
        private HpdfDict? _fonts;
        private HpdfDict? _xobjects;

        public HpdfAnnotationAppearance(HpdfXref xref)
        {
            _stream = new HpdfStreamObject();
            _stream.Add("Type", new HpdfName("XObject"));
            _stream.Add("Subtype", new HpdfName("Form"));
            xref.Add(_stream);
        }

        /// <summary>
        /// Gets the appearance stream.
        /// </summary>
        public HpdfStreamObject Stream => _stream;

        /// <summary>
        /// Redraws the appearance stream.
        /// </summary>
        /// <param name="context">The document's write settings (compression).</param>
        /// <param name="box">The bounding box: the annotation rectangle.</param>
        /// <param name="opacity">The constant opacity of the annotation (/CA); below 1 it goes into the stream.</param>
        /// <param name="multiply">Whether the content is painted with the Multiply blend mode (text highlights).</param>
        /// <param name="draw">Draws the content in page coordinates.</param>
        public void Draw(HpdfAnnotationWriteContext context, HpdfRect box, float opacity, bool multiply,
            Action<HpdfAppearanceCanvas> draw)
        {
            _stream.ClearStream();
            _stream.Filter = context.Compress ? HpdfStreamFilter.FlateDecode : HpdfStreamFilter.None;
            _stream["BBox"] = CreateBox(box);
            _fonts = null;
            _xobjects = null;

            var resources = new HpdfDict();
            var canvas = new HpdfAppearanceCanvas(_stream);

            // The viewer does not apply /CA to an appearance stream: the stream carries the opacity
            if (opacity < 1 || multiply)
            {
                var graphicsState = new HpdfDict();
                graphicsState.Add("Type", new HpdfName("ExtGState"));
                if (opacity < 1)
                {
                    graphicsState.Add("CA", new HpdfReal(opacity));
                    graphicsState.Add("ca", new HpdfReal(opacity));
                }
                if (multiply)
                    graphicsState.Add("BM", new HpdfName("Multiply"));

                var graphicsStates = new HpdfDict();
                graphicsStates.Add(GraphicsStateName, graphicsState);
                resources.Add("ExtGState", graphicsStates);

                canvas.Stream.WriteString("/" + GraphicsStateName + " gs\n");
            }

            draw(canvas);

            if (_fonts != null)
                resources.Add("Font", _fonts);
            if (_xobjects != null)
                resources.Add("XObject", _xobjects);
            _stream["Resources"] = resources;
        }

        /// <summary>
        /// Adds a font to the resources of the stream being drawn (by the font's local name).
        /// </summary>
        public void UseFont(HpdfFont font)
        {
            _fonts ??= new HpdfDict();
            _fonts[font.LocalName] = font.Dict;
        }

        /// <summary>
        /// Adds an XObject (an image) to the resources of the stream being drawn.
        /// </summary>
        public void UseXObject(string name, HpdfObject xobject)
        {
            _xobjects ??= new HpdfDict();
            _xobjects[name] = xobject;
        }

        /// <summary>
        /// Creates the appearance dictionary that refers to the stream.
        /// </summary>
        public HpdfDict CreateAppearanceDictionary()
        {
            var ap = new HpdfDict();
            ap.Add("N", _stream);
            return ap;
        }

        private static HpdfArray CreateBox(HpdfRect box)
        {
            var array = new HpdfArray();
            array.Add(new HpdfReal(box.Left));
            array.Add(new HpdfReal(box.Bottom));
            array.Add(new HpdfReal(box.Right));
            array.Add(new HpdfReal(box.Top));
            return array;
        }
    }
}
