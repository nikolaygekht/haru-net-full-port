/* Parses the default appearance string (/DA) of variable text fields. */
using System;
using System.Globalization;
using Haru.Streams;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// A parsed default appearance string (/DA), such as "/Helv 12 Tf 0 0 0 rg": the font resource name,
    /// the font size (0 means auto size) and the other operators (text color) that are copied as they are.
    /// </summary>
    internal sealed class HpdfDefaultAppearance
    {
        private readonly string[] _tokens;
        private readonly int _sizeIndex;

        /// <summary>
        /// Gets the font resource name without the leading slash (e.g., "Helv").
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// Gets the font size; 0 means that the size is chosen to fit the field.
        /// </summary>
        public float FontSize { get; }

        private HpdfDefaultAppearance(string[] tokens, int sizeIndex, string fontName, float fontSize)
        {
            _tokens = tokens;
            _sizeIndex = sizeIndex;
            FontName = fontName;
            FontSize = fontSize;
        }

        /// <summary>
        /// Parses a default appearance string.
        /// </summary>
        /// <param name="appearance">The /DA string.</param>
        /// <returns>The parsed appearance, or null if the string is empty or has no valid Tf operator.</returns>
        public static HpdfDefaultAppearance? Parse(string? appearance)
        {
            if (string.IsNullOrWhiteSpace(appearance))
                return null;

            string[] tokens = appearance.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            int tf = Array.LastIndexOf(tokens, "Tf");
            if (tf < 2)
                return null;

            string name = tokens[tf - 2];
            if (name.Length < 2 || name[0] != '/')
                return null;

            if (!float.TryParse(tokens[tf - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float size) || size < 0)
                return null;

            return new HpdfDefaultAppearance(tokens, tf - 1, name.Substring(1), size);
        }

        /// <summary>
        /// Writes the appearance operators to a content stream, with the given font size.
        /// </summary>
        /// <param name="stream">The content stream.</param>
        /// <param name="fontSize">The font size to write in place of the size of the string.</param>
        public void Write(HpdfStream stream, float fontSize)
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                if (i > 0)
                    stream.WriteChar(' ');

                if (i == _sizeIndex)
                    stream.WriteReal(fontSize);
                else
                    stream.WriteString(_tokens[i]);
            }

            stream.WriteChar('\n');
        }
    }
}
