/* The predefined strings and the standard encoding of the CFF format (Adobe Technical Note #5176,
   appendices A and B), generated from fontTools.cffLib */
namespace Haru.Font.Cff
{
    /// <summary>
    /// The tables of the CFF format that a font does not hold: the 391 standard strings (SIDs 0-390)
    /// and the standard encoding, by which an accented glyph (endchar with four arguments) names its
    /// components.
    /// </summary>
    internal static class CffStandardData
    {
        /// <summary>The number of standard strings: the SID of the first string of a font.</summary>
        public const int StandardStringCount = 391;

        /// <summary>The standard strings, by SID.</summary>
        public static readonly string[] StandardStrings =
        {
            ".notdef", "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand",
            "quoteright", "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period",
            "slash", "zero", "one", "two", "three", "four", "five", "six",
            "seven", "eight", "nine", "colon", "semicolon", "less", "equal", "greater",
            "question", "at", "A", "B", "C", "D", "E", "F",
            "G", "H", "I", "J", "K", "L", "M", "N",
            "O", "P", "Q", "R", "S", "T", "U", "V",
            "W", "X", "Y", "Z", "bracketleft", "backslash", "bracketright", "asciicircum",
            "underscore", "quoteleft", "a", "b", "c", "d", "e", "f",
            "g", "h", "i", "j", "k", "l", "m", "n",
            "o", "p", "q", "r", "s", "t", "u", "v",
            "w", "x", "y", "z", "braceleft", "bar", "braceright", "asciitilde",
            "exclamdown", "cent", "sterling", "fraction", "yen", "florin", "section", "currency",
            "quotesingle", "quotedblleft", "guillemotleft", "guilsinglleft", "guilsinglright", "fi", "fl", "endash",
            "dagger", "daggerdbl", "periodcentered", "paragraph", "bullet", "quotesinglbase", "quotedblbase", "quotedblright",
            "guillemotright", "ellipsis", "perthousand", "questiondown", "grave", "acute", "circumflex", "tilde",
            "macron", "breve", "dotaccent", "dieresis", "ring", "cedilla", "hungarumlaut", "ogonek",
            "caron", "emdash", "AE", "ordfeminine", "Lslash", "Oslash", "OE", "ordmasculine",
            "ae", "dotlessi", "lslash", "oslash", "oe", "germandbls", "onesuperior", "logicalnot",
            "mu", "trademark", "Eth", "onehalf", "plusminus", "Thorn", "onequarter", "divide",
            "brokenbar", "degree", "thorn", "threequarters", "twosuperior", "registered", "minus", "eth",
            "multiply", "threesuperior", "copyright", "Aacute", "Acircumflex", "Adieresis", "Agrave", "Aring",
            "Atilde", "Ccedilla", "Eacute", "Ecircumflex", "Edieresis", "Egrave", "Iacute", "Icircumflex",
            "Idieresis", "Igrave", "Ntilde", "Oacute", "Ocircumflex", "Odieresis", "Ograve", "Otilde",
            "Scaron", "Uacute", "Ucircumflex", "Udieresis", "Ugrave", "Yacute", "Ydieresis", "Zcaron",
            "aacute", "acircumflex", "adieresis", "agrave", "aring", "atilde", "ccedilla", "eacute",
            "ecircumflex", "edieresis", "egrave", "iacute", "icircumflex", "idieresis", "igrave", "ntilde",
            "oacute", "ocircumflex", "odieresis", "ograve", "otilde", "scaron", "uacute", "ucircumflex",
            "udieresis", "ugrave", "yacute", "ydieresis", "zcaron", "exclamsmall", "Hungarumlautsmall", "dollaroldstyle",
            "dollarsuperior", "ampersandsmall", "Acutesmall", "parenleftsuperior", "parenrightsuperior", "twodotenleader", "onedotenleader", "zerooldstyle",
            "oneoldstyle", "twooldstyle", "threeoldstyle", "fouroldstyle", "fiveoldstyle", "sixoldstyle", "sevenoldstyle", "eightoldstyle",
            "nineoldstyle", "commasuperior", "threequartersemdash", "periodsuperior", "questionsmall", "asuperior", "bsuperior", "centsuperior",
            "dsuperior", "esuperior", "isuperior", "lsuperior", "msuperior", "nsuperior", "osuperior", "rsuperior",
            "ssuperior", "tsuperior", "ff", "ffi", "ffl", "parenleftinferior", "parenrightinferior", "Circumflexsmall",
            "hyphensuperior", "Gravesmall", "Asmall", "Bsmall", "Csmall", "Dsmall", "Esmall", "Fsmall",
            "Gsmall", "Hsmall", "Ismall", "Jsmall", "Ksmall", "Lsmall", "Msmall", "Nsmall",
            "Osmall", "Psmall", "Qsmall", "Rsmall", "Ssmall", "Tsmall", "Usmall", "Vsmall",
            "Wsmall", "Xsmall", "Ysmall", "Zsmall", "colonmonetary", "onefitted", "rupiah", "Tildesmall",
            "exclamdownsmall", "centoldstyle", "Lslashsmall", "Scaronsmall", "Zcaronsmall", "Dieresissmall", "Brevesmall", "Caronsmall",
            "Dotaccentsmall", "Macronsmall", "figuredash", "hypheninferior", "Ogoneksmall", "Ringsmall", "Cedillasmall", "questiondownsmall",
            "oneeighth", "threeeighths", "fiveeighths", "seveneighths", "onethird", "twothirds", "zerosuperior", "foursuperior",
            "fivesuperior", "sixsuperior", "sevensuperior", "eightsuperior", "ninesuperior", "zeroinferior", "oneinferior", "twoinferior",
            "threeinferior", "fourinferior", "fiveinferior", "sixinferior", "seveninferior", "eightinferior", "nineinferior", "centinferior",
            "dollarinferior", "periodinferior", "commainferior", "Agravesmall", "Aacutesmall", "Acircumflexsmall", "Atildesmall", "Adieresissmall",
            "Aringsmall", "AEsmall", "Ccedillasmall", "Egravesmall", "Eacutesmall", "Ecircumflexsmall", "Edieresissmall", "Igravesmall",
            "Iacutesmall", "Icircumflexsmall", "Idieresissmall", "Ethsmall", "Ntildesmall", "Ogravesmall", "Oacutesmall", "Ocircumflexsmall",
            "Otildesmall", "Odieresissmall", "OEsmall", "Oslashsmall", "Ugravesmall", "Uacutesmall", "Ucircumflexsmall", "Udieresissmall",
            "Yacutesmall", "Thornsmall", "Ydieresissmall", "001.000", "001.001", "001.002", "001.003", "Black",
            "Bold", "Book", "Light", "Medium", "Regular", "Roman", "Semibold",
        };

        /// <summary>The glyph names of the standard encoding by character code; null for a code
        /// without a glyph.</summary>
        public static readonly string?[] StandardEncoding =
        {
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            "space", "exclam", "quotedbl", "numbersign", "dollar", "percent", "ampersand", "quoteright",
            "parenleft", "parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
            "zero", "one", "two", "three", "four", "five", "six", "seven",
            "eight", "nine", "colon", "semicolon", "less", "equal", "greater", "question",
            "at", "A", "B", "C", "D", "E", "F", "G",
            "H", "I", "J", "K", "L", "M", "N", "O",
            "P", "Q", "R", "S", "T", "U", "V", "W",
            "X", "Y", "Z", "bracketleft", "backslash", "bracketright", "asciicircum", "underscore",
            "quoteleft", "a", "b", "c", "d", "e", "f", "g",
            "h", "i", "j", "k", "l", "m", "n", "o",
            "p", "q", "r", "s", "t", "u", "v", "w",
            "x", "y", "z", "braceleft", "bar", "braceright", "asciitilde", null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, "exclamdown", "cent", "sterling", "fraction", "yen", "florin", "section",
            "currency", "quotesingle", "quotedblleft", "guillemotleft", "guilsinglleft", "guilsinglright", "fi", "fl",
            null, "endash", "dagger", "daggerdbl", "periodcentered", null, "paragraph", "bullet",
            "quotesinglbase", "quotedblbase", "quotedblright", "guillemotright", "ellipsis", "perthousand", null, "questiondown",
            null, "grave", "acute", "circumflex", "tilde", "macron", "breve", "dotaccent",
            "dieresis", null, "ring", "cedilla", null, "hungarumlaut", "ogonek", "caron",
            "emdash", null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, "AE", null, "ordfeminine", null, null, null, null,
            "Lslash", "Oslash", "OE", "ordmasculine", null, null, null, null,
            null, "ae", null, null, null, "dotlessi", null, null,
            "lslash", "oslash", "oe", "germandbls", null, null, null, null,
        };
    }
}
