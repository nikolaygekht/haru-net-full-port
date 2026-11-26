/* The fonts of an AcroForm: the default resources (/DR) and the fonts of generated field appearances. */
using System;
using System.Collections.Generic;
using Haru.Font;
using Haru.Objects;
using Haru.Xref;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// Creates the standard fonts that default appearance strings (/DA) of form fields refer to,
    /// once per document, and builds the font resources of the form (/DR) and of appearance streams.
    /// A font is found by the resource name of the /DA string: the names used by Acrobat
    /// (Helv, HeBo, TiRo, Cour, ZaDb, ...) and the PostScript names of the standard 14 fonts are known.
    /// </summary>
    internal sealed class HpdfFormFonts
    {
        private static readonly Dictionary<string, HpdfStandardFont> StandardFontNames = CreateStandardFontNames();

        private readonly HpdfXref _xref;
        private readonly Dictionary<string, HpdfFont> _fonts = new(StringComparer.Ordinal);

        /// <summary>
        /// Creates an empty font set.
        /// </summary>
        /// <param name="xref">The cross-reference table the font dictionaries are added to.</param>
        public HpdfFormFonts(HpdfXref xref)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
        }

        /// <summary>
        /// Gets whether no font has been created yet.
        /// </summary>
        public bool IsEmpty => _fonts.Count == 0;

        /// <summary>
        /// Gets the font for a resource name, creating it on first use.
        /// </summary>
        /// <param name="resourceName">The resource name without the leading slash (e.g., "Helv").</param>
        /// <returns>The font, or null if the name is not a known standard font name.</returns>
        public HpdfFont? Resolve(string resourceName)
        {
            if (_fonts.TryGetValue(resourceName, out var font))
                return font;

            if (!StandardFontNames.TryGetValue(resourceName, out var standardFont))
                return null;

            font = new HpdfFont(_xref, standardFont, resourceName);
            _fonts.Add(resourceName, font);
            return font;
        }

        /// <summary>
        /// Creates a resource dictionary with all fonts created so far, for the form's /DR entry.
        /// </summary>
        public HpdfDict CreateResources()
        {
            var fonts = new HpdfDict();
            foreach (var (name, font) in _fonts)
                fonts.Add(name, font.Dict);

            return WrapFonts(fonts);
        }

        /// <summary>
        /// Creates a resource dictionary with one font, for an appearance stream.
        /// </summary>
        /// <param name="font">The font of the appearance stream.</param>
        public static HpdfDict CreateResources(HpdfFont font)
        {
            ArgumentNullException.ThrowIfNull(font);
            var fonts = new HpdfDict();
            fonts.Add(font.LocalName, font.Dict);
            return WrapFonts(fonts);
        }

        private static HpdfDict WrapFonts(HpdfDict fonts)
        {
            var resources = new HpdfDict();
            resources.Add("Font", fonts);
            return resources;
        }

        private static Dictionary<string, HpdfStandardFont> CreateStandardFontNames()
        {
            var names = new Dictionary<string, HpdfStandardFont>(StringComparer.Ordinal)
            {
                ["Helv"] = HpdfStandardFont.Helvetica,
                ["HeBo"] = HpdfStandardFont.HelveticaBold,
                ["HeOb"] = HpdfStandardFont.HelveticaOblique,
                ["HeBO"] = HpdfStandardFont.HelveticaBoldOblique,
                ["TiRo"] = HpdfStandardFont.TimesRoman,
                ["TiBo"] = HpdfStandardFont.TimesBold,
                ["TiIt"] = HpdfStandardFont.TimesItalic,
                ["TiBI"] = HpdfStandardFont.TimesBoldItalic,
                ["Cour"] = HpdfStandardFont.Courier,
                ["CoBo"] = HpdfStandardFont.CourierBold,
                ["CoOb"] = HpdfStandardFont.CourierOblique,
                ["CoBO"] = HpdfStandardFont.CourierBoldOblique,
                ["Symb"] = HpdfStandardFont.Symbol,
                ["ZaDb"] = HpdfStandardFont.ZapfDingbats
            };

            foreach (HpdfStandardFont font in Enum.GetValues<HpdfStandardFont>())
                names.TryAdd(font.GetPostScriptName(), font);

            return names;
        }
    }
}
