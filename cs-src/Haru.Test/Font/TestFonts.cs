using System;
using System.Buffers.Binary;
using System.IO;

namespace Haru.Test.Font
{
    /// <summary>
    /// Font files for the font tests: the fonts of the demo project, and copies of them changed
    /// to have the properties a test needs.
    /// </summary>
    internal static class TestFonts
    {
        public const string Japanese = "demo/ttfont/noto-jp.ttf";
        public const string SimplifiedChinese = "demo/ttfont/noto-chs.ttf";
        public const string TraditionalChinese = "demo/ttfont/noto-cht.ttf";
        public const string Korean = "demo/ttfont/noto-kr.ttf";
        public const string Roboto = "demo/ttfont/Roboto-Regular.ttf";
        public const string PenguinAttack = "demo/ttfont/PenguinAttack.ttf";

        /// <summary>
        /// Gets the path of a resource of the demo project (Haru.Demos/Resource).
        /// </summary>
        public static string DemoResource(string relativePath)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "Haru.Demos", "Resource", relativePath);
                if (File.Exists(candidate))
                    return candidate;
            }
            throw new FileNotFoundException("Demo resource not found", relativePath);
        }

        /// <summary>
        /// Writes font data to a temporary file; the caller deletes it.
        /// </summary>
        public static string WriteTempFile(byte[] fontData)
        {
            string path = Path.Combine(Path.GetTempPath(), "haru-test-" + Guid.NewGuid().ToString("N") + ".ttf");
            File.WriteAllBytes(path, fontData);
            return path;
        }

        /// <summary>
        /// Returns a copy of a font file with another fsType (embedding permissions) in its OS/2 table.
        /// </summary>
        public static byte[] WithFsType(byte[] fontData, ushort fsType)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            byte[] copy = (byte[])fontData.Clone();
            int table = TableOffset(copy, "OS/2");
            BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(table + 8), fsType);
            return copy;
        }

        /// <summary>
        /// Returns a copy of a font file in which a table has another tag, so the font no longer
        /// has the table.
        /// </summary>
        public static byte[] WithRenamedTable(byte[] fontData, string tag, string newTag)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            byte[] copy = (byte[])fontData.Clone();
            System.Text.Encoding.ASCII.GetBytes(newTag, copy.AsSpan(FindTableRecord(copy, tag), 4));
            return copy;
        }

        /// <summary>
        /// Returns a copy of a font file in which the features of a tag in the 'GSUB' table have
        /// another tag, so the font no longer has the feature.
        /// </summary>
        public static byte[] WithRenamedFeature(byte[] fontData, string tag, string newTag)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            byte[] copy = (byte[])fontData.Clone();
            int gsub = TableOffset(copy, "GSUB");
            int featureList = gsub + BinaryPrimitives.ReadUInt16BigEndian(copy.AsSpan(gsub + 6));
            int count = BinaryPrimitives.ReadUInt16BigEndian(copy.AsSpan(featureList));
            for (int i = 0; i < count; i++)
            {
                int record = featureList + 2 + i * 6;
                if (System.Text.Encoding.ASCII.GetString(copy, record, 4) == tag)
                    System.Text.Encoding.ASCII.GetBytes(newTag, copy.AsSpan(record, 4));
            }
            return copy;
        }

        /// <summary>
        /// Returns a copy of a font file with another number of font units per em (unitsPerEm of
        /// the head table): every value of the font scales to glyph space with it.
        /// </summary>
        public static byte[] WithUnitsPerEm(byte[] fontData, ushort unitsPerEm)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            byte[] copy = (byte[])fontData.Clone();
            BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(TableOffset(copy, "head") + 18), unitsPerEm);
            return copy;
        }

        /// <summary>
        /// Returns a copy of a font file whose only Unicode cmap subtables are the ones for all of
        /// Unicode (format 12): the format 4 subtables are moved to a platform readers ignore.
        /// </summary>
        public static byte[] WithFullUnicodeCmapOnly(byte[] fontData)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            byte[] copy = (byte[])fontData.Clone();
            int cmap = TableOffset(copy, "cmap");
            int numTables = BinaryPrimitives.ReadUInt16BigEndian(copy.AsSpan(cmap + 2));
            for (int i = 0; i < numTables; i++)
            {
                int record = cmap + 4 + i * 8;
                int subtable = cmap + (int)BinaryPrimitives.ReadUInt32BigEndian(copy.AsSpan(record + 4));
                if (BinaryPrimitives.ReadUInt16BigEndian(copy.AsSpan(subtable)) == 4)
                    BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(record), 2); // platform 2 (ISO), deprecated
            }
            return copy;
        }

        private static int TableOffset(byte[] fontData, string tag)
        {
            return (int)BinaryPrimitives.ReadUInt32BigEndian(fontData.AsSpan(FindTableRecord(fontData, tag) + 8));
        }

        private static int FindTableRecord(byte[] fontData, string tag)
        {
            int numTables = BinaryPrimitives.ReadUInt16BigEndian(fontData.AsSpan(4));
            for (int i = 0; i < numTables; i++)
            {
                int record = 12 + i * 16;
                if (System.Text.Encoding.ASCII.GetString(fontData, record, 4) == tag)
                    return record;
            }
            throw new InvalidOperationException($"The font has no '{tag}' table");
        }
    }
}
