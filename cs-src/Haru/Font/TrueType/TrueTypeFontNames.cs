/* Names of a TrueType font, read from its 'name' table */
using System.Text;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// The names of a TrueType font that a PDF document needs, read from the font's 'name' table.
    /// This is the one place where the table is interpreted; the names are a part of the parsed font
    /// file (<see cref="TrueTypeFontFile.Names"/>), so the simple TrueType font, the CID font and the
    /// font data pool all name a font file in the same way.
    /// </summary>
    internal sealed class TrueTypeFontNames
    {
        private const ushort FamilyNameId = 1;
        private const ushort SubfamilyNameId = 2;
        private const ushort PostScriptNameId = 6;

        private const ushort MacintoshPlatform = 1;
        private const ushort MicrosoftPlatform = 3;
        private const ushort UnicodeBmpEncoding = 1;
        private const ushort EnglishUnitedStates = 0x0409;

        /// <summary>
        /// A font without names: no 'name' table, or no usable records in it.
        /// </summary>
        public static readonly TrueTypeFontNames None = new TrueTypeFontNames(null, null);

        private TrueTypeFontNames(string? postScriptName, string? familyName)
        {
            PostScriptName = postScriptName;
            FamilyName = familyName;
        }

        /// <summary>
        /// Gets the name of the font for /BaseFont and /FontName: the PostScript name of the font
        /// (name ID 6), or else its family name with the subfamily ("Arial-Bold"; "Regular" is left
        /// out), reduced to the characters a PDF name can hold. Null if the font has neither.
        /// </summary>
        public string? PostScriptName { get; }

        /// <summary>
        /// Gets the family name of the font (name ID 1) as it is written in the font, for /FontFamily.
        /// Null if the font has none.
        /// </summary>
        public string? FamilyName { get; }

        /// <summary>
        /// Reads the names of the font a parser is open on.
        /// </summary>
        /// <param name="parser">The parser of the font file; its position is changed.</param>
        /// <param name="offsetTable">The table directory of the font.</param>
        public static TrueTypeFontNames Read(TrueTypeParser parser, TrueTypeOffsetTable offsetTable)
        {
            TrueTypeTable? table = parser.FindTable(offsetTable, "name");
            if (table is null)
                return None;

            TrueTypeNameTable nameTable = parser.ParseName(table);
            if (nameTable?.NameRecords is null)
                return None;

            string? family = ReadName(parser, nameTable, table, FamilyNameId);

            string? postScriptName = ToPdfName(ReadName(parser, nameTable, table, PostScriptNameId));
            if (postScriptName is null && family != null)
            {
                string? subfamily = ReadName(parser, nameTable, table, SubfamilyNameId);
                postScriptName = ToPdfName(subfamily is null || subfamily == "Regular"
                    ? family
                    : $"{family}-{subfamily.Replace(" ", string.Empty)}");
            }

            return new TrueTypeFontNames(postScriptName, family?.Trim());
        }

        /// <summary>
        /// Reads the string of a name ID, in the order of preference: the Microsoft Unicode record in
        /// US English, any Microsoft record, a Macintosh record, any record.
        /// </summary>
        /// <returns>The string; null if the font has no such name or the name is blank.</returns>
        private static string? ReadName(TrueTypeParser parser, TrueTypeNameTable nameTable, TrueTypeTable table, ushort nameId)
        {
            TrueTypeNameRecord? record =
                FindRecord(nameTable.NameRecords, nameId, MicrosoftPlatform, UnicodeBmpEncoding, EnglishUnitedStates)
                ?? FindRecord(nameTable.NameRecords, nameId, MicrosoftPlatform)
                ?? FindRecord(nameTable.NameRecords, nameId, MacintoshPlatform)
                ?? FindRecord(nameTable.NameRecords, nameId);

            if (record is null)
                return null;

            string? name = parser.ReadNameString(nameTable, table, record);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private static TrueTypeNameRecord? FindRecord(TrueTypeNameRecord[] records, ushort nameId,
            ushort? platformId = null, ushort? encodingId = null, ushort? languageId = null)
        {
            foreach (var record in records)
            {
                if (record.NameId == nameId &&
                    (!platformId.HasValue || record.PlatformId == platformId.Value) &&
                    (!encodingId.HasValue || record.EncodingId == encodingId.Value) &&
                    (!languageId.HasValue || record.LanguageId == languageId.Value))
                {
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Reduces a font name to the characters that can be written in a PDF name without escaping:
        /// printable ASCII except the delimiters; a space becomes a hyphen.
        /// </summary>
        /// <returns>The name; null if nothing is left of it.</returns>
        private static string? ToPdfName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var cleaned = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (c == ' ')
                {
                    cleaned.Append('-');
                }
                else if (c > 32 && c < 127 &&
                         c != '#' && c != '/' && c != '(' && c != ')' &&
                         c != '<' && c != '>' && c != '[' && c != ']' &&
                         c != '{' && c != '}' && c != '%')
                {
                    cleaned.Append(c);
                }
            }

            return cleaned.Length == 0 ? null : cleaned.ToString();
        }
    }
}
