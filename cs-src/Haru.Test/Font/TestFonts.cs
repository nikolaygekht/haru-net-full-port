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
        /// A TrueType font collection of two fonts: [0] Noto Sans (2048 units per em),
        /// [1] Space Mono (1000 units per em); every font has its own tables.
        /// </summary>
        public static string Collection =>
            Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "NotoSansAndSpaceMono.ttc");

        /// <summary>
        /// Opens the font collection of <see cref="Collection"/> that is embedded in the test assembly
        /// as a resource; the caller disposes the stream.
        /// </summary>
        public static Stream OpenCollectionResource() => TestHelper.GetResourceStream("Fonts.NotoSansAndSpaceMono.ttc");

        /// <summary>
        /// Makes a font collection of copies of one font file that share its tables, as the fonts of
        /// most collections share some: every font has its own table directory, the tables are
        /// stored once.
        /// </summary>
        public static byte[] SharedTableCollection(byte[] fontData, int fontCount)
        {
            ArgumentNullException.ThrowIfNull(fontData);

            int numTables = BinaryPrimitives.ReadUInt16BigEndian(fontData.AsSpan(4));
            int directoryLength = 12 + numTables * 16;
            int headerLength = 12 + 4 * fontCount;
            int tablesStart = headerLength + fontCount * directoryLength;

            var collection = new byte[tablesStart + fontData.Length];
            System.Text.Encoding.ASCII.GetBytes("ttcf", collection.AsSpan(0, 4));
            BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(4), 0x00010000);
            BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(8), (uint)fontCount);
            fontData.CopyTo(collection, tablesStart);

            for (int font = 0; font < fontCount; font++)
            {
                int directory = headerLength + font * directoryLength;
                BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(12 + 4 * font), (uint)directory);
                fontData.AsSpan(0, directoryLength).CopyTo(collection.AsSpan(directory));

                // The tables are where the copy of the font file put them
                for (int i = 0; i < numTables; i++)
                {
                    Span<byte> offset = collection.AsSpan(directory + 12 + i * 16 + 8, 4);
                    BinaryPrimitives.WriteUInt32BigEndian(offset, BinaryPrimitives.ReadUInt32BigEndian(offset) + (uint)tablesStart);
                }
            }
            return collection;
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
