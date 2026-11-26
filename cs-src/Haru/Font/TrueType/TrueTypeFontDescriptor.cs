/* The entries of a font descriptor that come from a TrueType font file */
using System;
using Haru.Objects;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// Writes the part of a font descriptor that is the same for every font made from a TrueType
    /// font file: the flags and the metrics of the font. The metrics are in 1000-unit glyph space,
    /// whatever the units of the font file are.
    /// </summary>
    internal static class TrueTypeFontDescriptor
    {
        private const int FixedPitch = 1;
        private const int Serif = 2;
        private const int Script = 8;
        private const int Nonsymbolic = 32;
        private const int Italic = 64;

        // OS/2 fsSelection and head macStyle
        private const int FsSelectionItalic = 0x01;
        private const int MacStyleItalic = 0x02;

        // The angle written for an italic font that does not tell its angle
        private const double DefaultItalicAngle = -12.0;

        /// <summary>
        /// Adds /Type, /FontName, /Flags, /FontBBox, /ItalicAngle, /Ascent, /Descent, /CapHeight,
        /// /StemV and /XHeight to a font descriptor.
        /// </summary>
        /// <param name="descriptor">The font descriptor dictionary, still empty.</param>
        /// <param name="file">The font file.</param>
        /// <param name="fontName">The name of the font in the document.</param>
        public static void AddFontMetrics(HpdfDict descriptor, TrueTypeFontFile file, string fontName)
        {
            TrueTypeHead head = file.Head;

            descriptor.Add("Type", new HpdfName("FontDescriptor"));
            descriptor.Add("FontName", new HpdfName(fontName));
            descriptor.Add("Flags", new HpdfNumber(GetFlags(file)));

            var bbox = new HpdfArray();
            bbox.Add(new HpdfNumber(file.Scale(head.XMin)));
            bbox.Add(new HpdfNumber(file.Scale(head.YMin)));
            bbox.Add(new HpdfNumber(file.Scale(head.XMax)));
            bbox.Add(new HpdfNumber(file.Scale(head.YMax)));
            descriptor.Add("FontBBox", bbox);

            descriptor.Add("ItalicAngle", new HpdfReal((float)GetItalicAngle(file)));
            descriptor.Add("Ascent", new HpdfNumber(file.Ascent));
            descriptor.Add("Descent", new HpdfNumber(file.Descent));
            descriptor.Add("CapHeight", new HpdfNumber(file.CapHeight));
            descriptor.Add("StemV", new HpdfNumber(GetStemV(file.OS2)));

            // Optional: left out if the font does not tell it
            if (file.XHeight != 0)
                descriptor.Add("XHeight", new HpdfNumber(file.XHeight));
        }

        private static int GetFlags(TrueTypeFontFile file)
        {
            // A text font is always nonsymbolic
            int flags = Nonsymbolic;

            TrueTypeOS2? os2 = file.OS2;
            if (os2 != null)
            {
                // PANOSE: family kind (2 - text with serifs, 3 - script) and proportion (9 - monospaced)
                if (os2.Panose.Length > 3 && os2.Panose[3] == 9)
                    flags |= FixedPitch;

                if (os2.Panose.Length > 0 && os2.Panose[0] == 2)
                    flags |= Serif;

                if (os2.Panose.Length > 0 && os2.Panose[0] == 3)
                    flags |= Script;

                if ((os2.FsSelection & FsSelectionItalic) != 0 || (file.Head.MacStyle & MacStyleItalic) != 0)
                    flags |= Italic;
            }

            return flags;
        }

        private static double GetItalicAngle(TrueTypeFontFile file)
        {
            // The 'post' table gives the angle as a 16.16 fixed-point number
            if (file.Post != null)
                return file.Post.ItalicAngle / 65536.0;

            if (file.OS2 != null && (file.OS2.FsSelection & FsSelectionItalic) != 0)
                return DefaultItalicAngle;

            if ((file.Head.MacStyle & MacStyleItalic) != 0)
                return DefaultItalicAngle;

            return 0.0;
        }

        /// <summary>
        /// Estimates the stem width from the weight class (100-900) of the font.
        /// </summary>
        private static int GetStemV(TrueTypeOS2? os2)
        {
            return os2 != null ? Math.Max(50, 50 + (os2.WeightClass - 400) / 5) : 80;
        }
    }
}
