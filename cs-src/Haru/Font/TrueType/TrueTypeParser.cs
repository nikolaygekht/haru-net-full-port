using System;
using System.IO;
using System.Text;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// Parses TrueType font files and extracts table data.
    /// </summary>
    internal class TrueTypeParser : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private bool _disposed = false;

        public TrueTypeParser(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        /// <summary>
        /// Reads a big-endian UInt16.
        /// </summary>
        public ushort ReadUInt16()
        {
            byte b1 = _reader.ReadByte();
            byte b2 = _reader.ReadByte();
            return (ushort)((b1 << 8) | b2);
        }

        /// <summary>
        /// Reads a big-endian Int16.
        /// </summary>
        public short ReadInt16()
        {
            byte b1 = _reader.ReadByte();
            byte b2 = _reader.ReadByte();
            return (short)((b1 << 8) | b2);
        }

        /// <summary>
        /// Reads a big-endian UInt32.
        /// </summary>
        public uint ReadUInt32()
        {
            byte b1 = _reader.ReadByte();
            byte b2 = _reader.ReadByte();
            byte b3 = _reader.ReadByte();
            byte b4 = _reader.ReadByte();
            return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
        }

        /// <summary>
        /// Reads a big-endian Int32.
        /// </summary>
        public int ReadInt32()
        {
            byte b1 = _reader.ReadByte();
            byte b2 = _reader.ReadByte();
            byte b3 = _reader.ReadByte();
            byte b4 = _reader.ReadByte();
            return (int)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
        }

        /// <summary>
        /// Reads a 4-byte tag as a string.
        /// </summary>
        public string ReadTag()
        {
            byte[] bytes = _reader.ReadBytes(4);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Seeks to a specific position in the stream.
        /// </summary>
        public void Seek(long offset)
        {
            _stream.Position = offset;
        }

        /// <summary>
        /// Gets the current position in the stream.
        /// </summary>
        public long Position => _stream.Position;

        /// <summary>
        /// Reads a byte array.
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        /// <summary>
        /// Parses the offset table (table directory) from the font file.
        /// </summary>
        public TrueTypeOffsetTable ParseOffsetTable()
        {
            _stream.Position = 0;

            var offsetTable = new TrueTypeOffsetTable
            {
                SfntVersion = ReadUInt32(),
                NumTables = ReadUInt16(),
                SearchRange = ReadUInt16(),
                EntrySelector = ReadUInt16(),
                RangeShift = ReadUInt16()
            };

            offsetTable.Tables = new TrueTypeTable[offsetTable.NumTables];

            for (int i = 0; i < offsetTable.NumTables; i++)
            {
                offsetTable.Tables[i] = new TrueTypeTable
                {
                    Tag = ReadTag(),
                    CheckSum = ReadUInt32(),
                    Offset = ReadUInt32(),
                    Length = ReadUInt32()
                };
            }

            return offsetTable;
        }

        /// <summary>
        /// Finds a table by its tag.
        /// </summary>
        public TrueTypeTable? FindTable(TrueTypeOffsetTable offsetTable, string tag)
        {
            if (offsetTable?.Tables is null)
                return null;

            foreach (var table in offsetTable.Tables)
            {
                if (table.Tag == tag)
                    return table;
            }

            return null;
        }

        /// <summary>
        /// Parses the 'head' table.
        /// </summary>
        public TrueTypeHead ParseHead(TrueTypeTable table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "head table not found");

            Seek(table.Offset);

            var head = new TrueTypeHead
            {
                VersionNumber = ReadBytes(4),
                FontRevision = ReadUInt32(),
                CheckSumAdjustment = ReadUInt32(),
                MagicNumber = ReadUInt32(),
                Flags = ReadUInt16(),
                UnitsPerEm = ReadUInt16(),
                Created = ReadBytes(8),
                Modified = ReadBytes(8),
                XMin = ReadInt16(),
                YMin = ReadInt16(),
                XMax = ReadInt16(),
                YMax = ReadInt16(),
                MacStyle = ReadUInt16(),
                LowestRecPPEM = ReadUInt16(),
                FontDirectionHint = ReadInt16(),
                IndexToLocFormat = ReadInt16(),
                GlyphDataFormat = ReadInt16()
            };

            // Validate magic number
            if (head.MagicNumber != 0x5F0F3CF5)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "Invalid head table magic number");

            return head;
        }

        /// <summary>
        /// Parses the 'maxp' table.
        /// </summary>
        public TrueTypeMaxp ParseMaxp(TrueTypeTable table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "maxp table not found");

            Seek(table.Offset);

            var maxp = new TrueTypeMaxp
            {
                Version = ReadUInt32(),
                NumGlyphs = ReadUInt16()
            };

            // Version 1.0 has additional fields
            if (maxp.Version == 0x00010000)
            {
                maxp.MaxPoints = ReadUInt16();
                maxp.MaxContours = ReadUInt16();
                maxp.MaxCompositePoints = ReadUInt16();
                maxp.MaxCompositeContours = ReadUInt16();
                maxp.MaxZones = ReadUInt16();
                maxp.MaxTwilightPoints = ReadUInt16();
                maxp.MaxStorage = ReadUInt16();
                maxp.MaxFunctionDefs = ReadUInt16();
                maxp.MaxInstructionDefs = ReadUInt16();
                maxp.MaxStackElements = ReadUInt16();
                maxp.MaxSizeOfInstructions = ReadUInt16();
                maxp.MaxComponentElements = ReadUInt16();
                maxp.MaxComponentDepth = ReadUInt16();
            }

            return maxp;
        }

        /// <summary>
        /// Parses the 'hhea' table.
        /// </summary>
        public TrueTypeHhea ParseHhea(TrueTypeTable table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "hhea table not found");

            Seek(table.Offset);

            var hhea = new TrueTypeHhea
            {
                Version = ReadUInt32(),
                Ascender = ReadInt16(),
                Descender = ReadInt16(),
                LineGap = ReadInt16(),
                AdvanceWidthMax = ReadUInt16(),
                MinLeftSideBearing = ReadInt16(),
                MinRightSideBearing = ReadInt16(),
                XMaxExtent = ReadInt16(),
                CaretSlopeRise = ReadInt16(),
                CaretSlopeRun = ReadInt16(),
                CaretOffset = ReadInt16(),
                Reserved1 = ReadInt16(),
                Reserved2 = ReadInt16(),
                Reserved3 = ReadInt16(),
                Reserved4 = ReadInt16(),
                MetricDataFormat = ReadInt16(),
                NumberOfHMetrics = ReadUInt16()
            };

            return hhea;
        }

        /// <summary>
        /// Parses the 'hmtx' table.
        /// </summary>
        public TrueTypeLongHorMetric[] ParseHmtx(TrueTypeTable table, ushort numberOfHMetrics, ushort numGlyphs)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "hmtx table not found");

            Seek(table.Offset);

            var metrics = new TrueTypeLongHorMetric[numGlyphs];

            // Read long horizontal metrics
            for (int i = 0; i < numberOfHMetrics; i++)
            {
                metrics[i] = new TrueTypeLongHorMetric
                {
                    AdvanceWidth = ReadUInt16(),
                    LeftSideBearing = ReadInt16()
                };
            }

            // Remaining glyphs have the same advance width as the last metric
            if (numberOfHMetrics > 0)
            {
                ushort lastAdvanceWidth = metrics[numberOfHMetrics - 1].AdvanceWidth;
                for (int i = numberOfHMetrics; i < numGlyphs; i++)
                {
                    metrics[i] = new TrueTypeLongHorMetric
                    {
                        AdvanceWidth = lastAdvanceWidth,
                        LeftSideBearing = ReadInt16()
                    };
                }
            }

            return metrics;
        }

        /// <summary>
        /// Parses the 'name' table.
        /// </summary>
        public TrueTypeNameTable ParseName(TrueTypeTable table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "name table not found");

            Seek(table.Offset);
            long tableStart = table.Offset;

            var nameTable = new TrueTypeNameTable
            {
                Format = ReadUInt16(),
                Count = ReadUInt16(),
                StringOffset = ReadUInt16()
            };

            nameTable.NameRecords = new TrueTypeNameRecord[nameTable.Count];

            for (int i = 0; i < nameTable.Count; i++)
            {
                nameTable.NameRecords[i] = new TrueTypeNameRecord
                {
                    PlatformId = ReadUInt16(),
                    EncodingId = ReadUInt16(),
                    LanguageId = ReadUInt16(),
                    NameId = ReadUInt16(),
                    Length = ReadUInt16(),
                    Offset = ReadUInt16()
                };
            }

            return nameTable;
        }

        /// <summary>
        /// Reads a string from the 'name' table.
        /// </summary>
        public string? ReadNameString(TrueTypeNameTable nameTable, TrueTypeTable table, TrueTypeNameRecord record)
        {
            if (nameTable is null || table is null || record is null)
                return null;

            Seek(table.Offset + nameTable.StringOffset + record.Offset);
            byte[] bytes = ReadBytes(record.Length);

            // Unicode strings (platform 0 or 3)
            if (record.PlatformId == 0 || record.PlatformId == 3)
            {
                return System.Text.Encoding.BigEndianUnicode.GetString(bytes);
            }
            else
            {
                return System.Text.Encoding.ASCII.GetString(bytes);
            }
        }

        /// <summary>
        /// Parses the 'post' table.
        /// </summary>
        public TrueTypePost? ParsePost(TrueTypeTable table)
        {
            if (table is null)
                return null;

            Seek(table.Offset);

            var post = new TrueTypePost
            {
                Version = ReadUInt32(),
                ItalicAngle = ReadInt32(),
                UnderlinePosition = ReadInt16(),
                UnderlineThickness = ReadInt16(),
                IsFixedPitch = ReadUInt32(),
                MinMemType42 = ReadUInt32(),
                MaxMemType42 = ReadUInt32(),
                MinMemType1 = ReadUInt32(),
                MaxMemType1 = ReadUInt32()
            };

            return post;
        }

        /// <summary>
        /// Parses the 'OS/2' table.
        /// </summary>
        public TrueTypeOS2 ParseOS2(TrueTypeTable table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, "OS/2 table not found");

            Seek(table.Offset);

            var os2 = new TrueTypeOS2
            {
                Version = ReadUInt16(),
                XAvgCharWidth = ReadInt16(),
                WeightClass = ReadUInt16(),
                WidthClass = ReadUInt16(),
                FsType = ReadUInt16(),
                YSubscriptXSize = ReadInt16(),
                YSubscriptYSize = ReadInt16(),
                YSubscriptXOffset = ReadInt16(),
                YSubscriptYOffset = ReadInt16(),
                YSuperscriptXSize = ReadInt16(),
                YSuperscriptYSize = ReadInt16(),
                YSuperscriptXOffset = ReadInt16(),
                YSuperscriptYOffset = ReadInt16(),
                YStrikeoutSize = ReadInt16(),
                YStrikeoutPosition = ReadInt16(),
                SFamilyClass = ReadInt16(),
                Panose = ReadBytes(10)
            };

            os2.UnicodeRange1 = ReadUInt32();
            os2.UnicodeRange2 = ReadUInt32();
            os2.UnicodeRange3 = ReadUInt32();
            os2.UnicodeRange4 = ReadUInt32();
            os2.AchVendID = ReadBytes(4);
            os2.FsSelection = ReadUInt16();
            os2.FirstCharIndex = ReadUInt16();
            os2.LastCharIndex = ReadUInt16();
            os2.STypoAscender = ReadInt16();
            os2.STypoDescender = ReadInt16();
            os2.STypoLineGap = ReadInt16();
            os2.UsWinAscent = ReadUInt16();
            os2.UsWinDescent = ReadUInt16();

            if (os2.Version >= 1)
            {
                os2.CodePageRange1 = ReadUInt32();
                os2.CodePageRange2 = ReadUInt32();
            }

            if (os2.Version >= 2)
            {
                os2.SxHeight = ReadInt16();
                os2.SCapHeight = ReadInt16();
            }

            return os2;
        }

        /// <summary>
        /// Reads the top of the bounding box of a glyph (yMax of its header in the 'glyf' table),
        /// in font units.
        /// </summary>
        /// <param name="loca">The 'loca' table; null if the font has none.</param>
        /// <param name="glyf">The 'glyf' table; null if the font has none.</param>
        /// <param name="longOffsets">True if 'loca' holds 32-bit offsets (indexToLocFormat of 'head').</param>
        /// <param name="glyphId">The glyph.</param>
        /// <returns>The top of the glyph; null if the font has no TrueType outlines, the glyph has
        /// no outline or the tables do not hold it.</returns>
        public short? ReadGlyphTop(TrueTypeTable? loca, TrueTypeTable? glyf, bool longOffsets, ushort glyphId)
        {
            const int GlyphHeaderLength = 10;  // numberOfContours, xMin, yMin, xMax, yMax
            const int YMaxOffset = 8;

            if (loca is null || glyf is null)
                return null;

            // The glyph takes the bytes from its offset to the offset of the next glyph
            int entryLength = longOffsets ? 4 : 2;
            if ((glyphId + 2L) * entryLength > loca.Length)
                return null;

            Seek(loca.Offset + (long)glyphId * entryLength);
            long start = longOffsets ? ReadUInt32() : ReadUInt16() * 2L;
            long end = longOffsets ? ReadUInt32() : ReadUInt16() * 2L;
            if (end - start < GlyphHeaderLength || end > glyf.Length)
                return null;

            Seek(glyf.Offset + start + YMaxOffset);
            return ReadInt16();
        }

        /// <summary>
        /// Parses the Unicode subtables of the 'cmap' table: platform 3 (Microsoft) with encoding 1
        /// (BMP) or 10 (full repertoire), or platform 0 (Unicode); a Microsoft subtable is preferred
        /// to a Unicode platform one. Format 4 maps the Basic Multilingual Plane, format 12 all of
        /// Unicode.
        /// </summary>
        /// <exception cref="HpdfException">The font has no 'cmap' table or no Unicode subtable of a
        /// supported format.</exception>
        public TrueTypeCmap ParseCmap(TrueTypeTable? table)
        {
            if (table is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidCmapTable, "cmap table not found");

            Seek(table.Offset);
            ReadUInt16(); // version
            ushort numTables = ReadUInt16();

            var records = new (ushort PlatformId, ushort EncodingId, uint Offset)[numTables];
            for (int i = 0; i < numTables; i++)
            {
                records[i] = (ReadUInt16(), ReadUInt16(), ReadUInt32());
            }

            uint format4Offset = 0;
            uint format12Offset = 0;
            bool format4IsMicrosoft = false;
            bool format12IsMicrosoft = false;
            int unsupportedFormat = -1;

            foreach (var record in records)
            {
                bool microsoft = record.PlatformId == 3 && (record.EncodingId == 1 || record.EncodingId == 10);
                if (!microsoft && record.PlatformId != 0)
                    continue;

                Seek(table.Offset + record.Offset);
                ushort format = ReadUInt16();

                if (format == 4)
                {
                    if (format4Offset == 0 || (microsoft && !format4IsMicrosoft))
                    {
                        format4Offset = record.Offset;
                        format4IsMicrosoft = microsoft;
                    }
                }
                else if (format == 12)
                {
                    if (format12Offset == 0 || (microsoft && !format12IsMicrosoft))
                    {
                        format12Offset = record.Offset;
                        format12IsMicrosoft = microsoft;
                    }
                }
                else
                {
                    unsupportedFormat = format;
                }
            }

            if (format4Offset == 0 && format12Offset == 0)
            {
                if (unsupportedFormat >= 0)
                    throw new HpdfException(HpdfErrorCode.UnsupportedFunc, $"Unsupported cmap format: {unsupportedFormat}");

                throw new HpdfException(HpdfErrorCode.TtInvalidCmapTable, "No suitable cmap subtable found");
            }

            TrueTypeCmapFormat4? bmp = null;
            if (format4Offset != 0)
            {
                Seek(table.Offset + format4Offset + 2); // after the format field
                bmp = ParseCmapFormat4();
            }

            TrueTypeCmapFormat12? full = null;
            if (format12Offset != 0)
            {
                Seek(table.Offset + format12Offset + 2);
                full = ParseCmapFormat12();
            }

            return new TrueTypeCmap(bmp, full);
        }

        private TrueTypeCmapFormat4 ParseCmapFormat4()
        {
            var cmap = new TrueTypeCmapFormat4
            {
                Format = 4,
                Length = ReadUInt16(),
                Language = ReadUInt16(),
                SegCountX2 = ReadUInt16()
            };

            int segCount = cmap.SegCountX2 / 2;

            cmap.SearchRange = ReadUInt16();
            cmap.EntrySelector = ReadUInt16();
            cmap.RangeShift = ReadUInt16();

            cmap.EndCount = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                cmap.EndCount[i] = ReadUInt16();

            cmap.ReservedPad = ReadUInt16();

            cmap.StartCount = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                cmap.StartCount[i] = ReadUInt16();

            cmap.IdDelta = new short[segCount];
            for (int i = 0; i < segCount; i++)
                cmap.IdDelta[i] = ReadInt16();

            cmap.IdRangeOffset = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                cmap.IdRangeOffset[i] = ReadUInt16();

            // The glyph IDs fill the rest of the subtable
            int glyphIdArraySize = (cmap.Length - (16 + segCount * 8)) / 2;
            cmap.GlyphIdArray = new ushort[glyphIdArraySize];
            for (int i = 0; i < glyphIdArraySize; i++)
                cmap.GlyphIdArray[i] = ReadUInt16();

            return cmap;
        }

        private TrueTypeCmapFormat12 ParseCmapFormat12()
        {
            ReadUInt16(); // reserved
            ReadUInt32(); // length
            ReadUInt32(); // language
            uint numGroups = ReadUInt32();

            var cmap = new TrueTypeCmapFormat12
            {
                StartCharCode = new uint[numGroups],
                EndCharCode = new uint[numGroups],
                StartGlyphId = new uint[numGroups]
            };

            for (int i = 0; i < numGroups; i++)
            {
                cmap.StartCharCode[i] = ReadUInt32();
                cmap.EndCharCode[i] = ReadUInt32();
                cmap.StartGlyphId[i] = ReadUInt32();
            }

            return cmap;
        }

        /// <summary>
        /// Reads a Fixed-point number (16.16 format).
        /// </summary>
        public int ReadFixed()
        {
            return ReadInt32();
        }

        /// <summary>
        /// Disposes the parser and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the parser
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose the BinaryReader (which wraps the stream)
                    // Stream is left open due to leaveOpen: true
                    _reader?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
