using Haru.Objects;
using Haru.Xref;
using Haru.Types;

namespace Haru.Doc
{
    /// <summary>
    /// Represents an extended graphics state dictionary.
    /// Extended graphics states control advanced rendering parameters like transparency and blend modes.
    /// </summary>
    public class HpdfExtGState
    {
        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;
        private readonly string _localName;

        /// <summary>
        /// Creates a new extended graphics state.
        /// </summary>
        public HpdfExtGState(HpdfXref xref, string localName)
        {
            if (xref is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            if (string.IsNullOrEmpty(localName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Local name cannot be null or empty");

            _xref = xref;
            _localName = localName;
            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("ExtGState"));

            xref.Add(_dict);
        }

        /// <summary>
        /// Gets the underlying dictionary.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the local resource name (e.g., "GS1").
        /// </summary>
        public string LocalName => _localName;

        /// <summary>
        /// Sets the alpha value for stroking operations (CA entry).
        /// </summary>
        /// <param name="alpha">Alpha value (0.0 = transparent, 1.0 = opaque).</param>
        public void SetAlphaStroke(float alpha)
        {
            if (!(alpha >= 0 && alpha <= 1))
                throw new HpdfException(HpdfErrorCode.ExtGStateOutOfRange, "Alpha must be 0-1");

            _dict["CA"] = new HpdfReal(alpha);
            _xref.RequireVersion(HpdfVersion.Version14); // transparency was introduced in PDF 1.4
        }

        /// <summary>
        /// Sets the alpha value for non-stroking operations (ca entry).
        /// </summary>
        /// <param name="alpha">Alpha value (0.0 = transparent, 1.0 = opaque).</param>
        public void SetAlphaFill(float alpha)
        {
            if (!(alpha >= 0 && alpha <= 1))
                throw new HpdfException(HpdfErrorCode.ExtGStateOutOfRange, "Alpha must be 0-1");

            _dict["ca"] = new HpdfReal(alpha);
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Sets the blend mode (BM entry).
        /// </summary>
        /// <param name="mode">The blend mode.</param>
        public void SetBlendMode(HpdfBlendMode mode)
        {
            if (mode < HpdfBlendMode.Normal || mode >= HpdfBlendMode.Eof)
                throw new HpdfException(HpdfErrorCode.ExtGStateOutOfRange, $"Invalid blend mode: {mode}");

            string modeName = mode.ToString();
            _dict["BM"] = new HpdfName(modeName);
            _xref.RequireVersion(HpdfVersion.Version14);
        }
    }
}
