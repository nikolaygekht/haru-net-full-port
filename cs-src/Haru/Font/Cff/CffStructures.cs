/* The building blocks of the CFF format (Adobe Technical Note #5176): INDEX and DICT data */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Haru.Font.Cff
{
    /// <summary>
    /// An INDEX of a CFF font: a count, the offsets of the items and the items. Only where it is in
    /// the file is kept; an item is found from the bytes when it is asked for.
    /// </summary>
    internal readonly struct CffIndex
    {
        private readonly int _offsetArray;   // position of the offset array in the file
        private readonly int _dataBase;      // position of the item data minus 1 (offsets start at 1)

        private CffIndex(int start, int count, int offSize, int offsetArray, int dataBase, int end)
        {
            Start = start;
            Count = count;
            OffSize = offSize;
            _offsetArray = offsetArray;
            _dataBase = dataBase;
            End = end;
        }

        /// <summary>Where the INDEX starts in the file.</summary>
        public int Start { get; }

        /// <summary>The number of items.</summary>
        public int Count { get; }

        /// <summary>The size of an offset, 1 to 4 bytes.</summary>
        public int OffSize { get; }

        /// <summary>Where the INDEX ends in the file: the first byte after it.</summary>
        public int End { get; }

        /// <summary>
        /// Reads the header of an INDEX and checks that its items are in the file.
        /// </summary>
        /// <param name="data">The whole file.</param>
        /// <param name="position">Where the INDEX starts.</param>
        /// <param name="limit">The end of the CFF table: the INDEX must not go beyond it.</param>
        public static CffIndex Read(ReadOnlySpan<byte> data, int position, int limit)
        {
            if (position < 0 || position + 2 > limit)
                throw CffFont.Damaged("an INDEX is outside the table");

            int count = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(position));
            if (count == 0)
                return new CffIndex(position, 0, 0, position + 2, position + 2, position + 2);

            if (position + 3 > limit)
                throw CffFont.Damaged("an INDEX is outside the table");

            int offSize = data[position + 2];
            if (offSize < 1 || offSize > 4)
                throw CffFont.Damaged($"an INDEX has offsets of {offSize} bytes");

            int offsetArray = position + 3;
            long dataBase = offsetArray + (long)(count + 1) * offSize - 1;
            if (dataBase + 1 > limit)
                throw CffFont.Damaged("an INDEX is outside the table");

            long last = ReadOffset(data, offsetArray + count * offSize, offSize);
            long end = dataBase + last;
            if (ReadOffset(data, offsetArray, offSize) != 1 || end > limit)
                throw CffFont.Damaged("an INDEX is outside the table");

            return new CffIndex(position, count, offSize, offsetArray, (int)dataBase, (int)end);
        }

        /// <summary>
        /// Gets where an item is in the file.
        /// </summary>
        public (int Offset, int Length) GetItem(ReadOnlySpan<byte> data, int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int start = (int)ReadOffset(data, _offsetArray + index * OffSize, OffSize);
            int end = (int)ReadOffset(data, _offsetArray + (index + 1) * OffSize, OffSize);
            if (end < start || _dataBase + end > End)
                throw CffFont.Damaged("the offsets of an INDEX are not in order");

            return (_dataBase + start, end - start);
        }

        /// <summary>
        /// Gets the bytes of an item.
        /// </summary>
        public ReadOnlySpan<byte> GetItemData(ReadOnlySpan<byte> data, int index)
        {
            (int offset, int length) = GetItem(data, index);
            return data.Slice(offset, length);
        }

        private static long ReadOffset(ReadOnlySpan<byte> data, int position, int offSize)
        {
            long value = 0;
            for (int i = 0; i < offSize; i++)
                value = (value << 8) | data[position + i];
            return value;
        }
    }

    /// <summary>
    /// An entry of a CFF DICT: an operator with its operands, and where it is in the file, so that
    /// it can be copied as it is.
    /// </summary>
    internal sealed class CffDictEntry
    {
        public CffDictEntry(int op, double[] operands, int start, int length)
        {
            Operator = op;
            Operands = operands;
            Start = start;
            Length = length;
        }

        /// <summary>The operator: 0-21, or 0x0C00 plus the second byte of a two-byte operator.</summary>
        public int Operator { get; }

        /// <summary>The operands.</summary>
        public double[] Operands { get; }

        /// <summary>Where the entry (operands and operator) starts in the file.</summary>
        public int Start { get; }

        /// <summary>The length of the entry in the file.</summary>
        public int Length { get; }

        /// <summary>Gets an operand as an integer.</summary>
        public int GetInt(int index)
        {
            if (index >= Operands.Length)
                throw CffFont.Damaged($"the DICT operator {Operator} has too few operands");
            return (int)Operands[index];
        }
    }

    /// <summary>
    /// Reads and writes the DICT data of a CFF font.
    /// </summary>
    internal static class CffDict
    {
        // Top DICT operators
        public const int Version = 0;
        public const int Notice = 1;
        public const int FullName = 2;
        public const int FamilyName = 3;
        public const int Weight = 4;
        public const int UniqueId = 13;
        public const int Xuid = 14;
        public const int Charset = 15;
        public const int Encoding = 16;
        public const int CharStrings = 17;
        public const int Private = 18;
        public const int CharstringType = 0x0C06;
        public const int FontMatrix = 0x0C07;
        public const int Ros = 0x0C1E;
        public const int CidCount = 0x0C22;
        public const int UidBase = 0x0C23;
        public const int FdArray = 0x0C24;
        public const int FdSelect = 0x0C25;
        public const int FontName = 0x0C26;

        // Private DICT operator
        public const int Subrs = 19;

        /// <summary>
        /// Parses DICT data.
        /// </summary>
        /// <param name="data">The whole file.</param>
        /// <param name="start">Where the DICT data starts.</param>
        /// <param name="length">The length of the DICT data.</param>
        public static List<CffDictEntry> Parse(ReadOnlySpan<byte> data, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > data.Length)
                throw CffFont.Damaged("a DICT is outside the table");

            var entries = new List<CffDictEntry>();
            var operands = new List<double>();
            int entryStart = start;
            int position = start;
            int end = start + length;
            while (position < end)
            {
                int b0 = data[position];
                if (b0 <= 21)
                {
                    int op = b0;
                    position++;
                    if (b0 == 12)
                    {
                        if (position >= end)
                            throw CffFont.Damaged("a DICT ends inside an operator");
                        op = 0x0C00 | data[position++];
                    }

                    entries.Add(new CffDictEntry(op, operands.ToArray(), entryStart, position - entryStart));
                    operands.Clear();
                    entryStart = position;
                }
                else
                {
                    operands.Add(ReadOperand(data, ref position, end));
                }
            }

            if (operands.Count > 0)
                throw CffFont.Damaged("a DICT ends with operands without an operator");

            return entries;
        }

        private static double ReadOperand(ReadOnlySpan<byte> data, ref int position, int end)
        {
            int b0 = data[position];
            int need = b0 switch
            {
                28 => 3,
                29 => 5,
                >= 32 and <= 246 => 1,
                >= 247 and <= 254 => 2,
                _ => 1,
            };
            if (b0 != 30 && position + need > end)
                throw CffFont.Damaged("a DICT ends inside an operand");

            switch (b0)
            {
                case 28:
                    position += 3;
                    return BinaryPrimitives.ReadInt16BigEndian(data.Slice(position - 2));
                case 29:
                    position += 5;
                    return BinaryPrimitives.ReadInt32BigEndian(data.Slice(position - 4));
                case 30:
                    return ReadReal(data, ref position, end);
                case >= 32 and <= 246:
                    position += 1;
                    return b0 - 139;
                case >= 247 and <= 250:
                    position += 2;
                    return (b0 - 247) * 256 + data[position - 1] + 108;
                case >= 251 and <= 254:
                    position += 2;
                    return -(b0 - 251) * 256 - data[position - 1] - 108;
                default:
                    throw CffFont.Damaged($"a DICT has the reserved byte {b0}");
            }
        }

        private static double ReadReal(ReadOnlySpan<byte> data, ref int position, int end)
        {
            var text = new StringBuilder();
            position++; // the byte 30
            while (true)
            {
                if (position >= end)
                    throw CffFont.Damaged("a DICT ends inside a real number");

                int b = data[position++];
                for (int half = 0; half < 2; half++)
                {
                    int nibble = half == 0 ? b >> 4 : b & 0x0F;
                    switch (nibble)
                    {
                        case <= 9: text.Append((char)('0' + nibble)); break;
                        case 0x0A: text.Append('.'); break;
                        case 0x0B: text.Append('E'); break;
                        case 0x0C: text.Append("E-"); break;
                        case 0x0E: text.Append('-'); break;
                        case 0x0F:
                            return text.Length == 0 ? 0 : double.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                        default:
                            throw CffFont.Damaged("a DICT has a reserved nibble in a real number");
                    }
                }
            }
        }

        /// <summary>
        /// Writes an integer operand in five bytes, whatever its value, so that an offset can be
        /// written before the value is known without moving what follows.
        /// </summary>
        public static void WriteFixedInt(List<byte> output, int value)
        {
            output.Add(29);
            output.Add((byte)(value >> 24));
            output.Add((byte)(value >> 16));
            output.Add((byte)(value >> 8));
            output.Add((byte)value);
        }

        /// <summary>
        /// Writes an integer operand in its shortest form.
        /// </summary>
        public static void WriteInt(List<byte> output, int value)
        {
            if (value >= -107 && value <= 107)
            {
                output.Add((byte)(value + 139));
            }
            else if (value >= 108 && value <= 1131)
            {
                value -= 108;
                output.Add((byte)((value >> 8) + 247));
                output.Add((byte)value);
            }
            else if (value >= -1131 && value <= -108)
            {
                value = -value - 108;
                output.Add((byte)((value >> 8) + 251));
                output.Add((byte)value);
            }
            else if (value >= short.MinValue && value <= short.MaxValue)
            {
                output.Add(28);
                output.Add((byte)(value >> 8));
                output.Add((byte)value);
            }
            else
            {
                WriteFixedInt(output, value);
            }
        }

        /// <summary>
        /// Writes an operator.
        /// </summary>
        public static void WriteOperator(List<byte> output, int op)
        {
            if (op >= 0x0C00)
            {
                output.Add(12);
                output.Add((byte)(op & 0xFF));
            }
            else
            {
                output.Add((byte)op);
            }
        }
    }
}
