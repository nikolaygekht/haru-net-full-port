/* Writes the charstrings of some glyphs without subroutines (desubroutinizes them) */
using System;
using System.Collections.Generic;

namespace Haru.Font.Cff
{
    /// <summary>
    /// Writes Type 2 charstrings (Adobe Technical Note #5177) with the subroutines they call put
    /// in place of the calls, so that a subset needs no subroutines at all: the subroutines of a
    /// CJK font run to thousands, and a few glyphs use few of them. A charstring is followed as an
    /// interpreter follows it, so every subroutine is read with the stack and the number of stem
    /// hints of its call (a hint mask is as long as the stems require). Nothing is drawn: the
    /// tokens are copied. The glyphs an accented glyph is made of (endchar with four arguments,
    /// seac) are added to the glyphs. A charstring this writer does not understand (arithmetic
    /// operators, a damaged charstring) makes it give up; the font program is then written with
    /// its subroutines.
    /// </summary>
    internal sealed class CffCharStringFlattener
    {
        private const int MaxCallDepth = 10;        // the limit of the Type 2 format
        private const int MaxStack = 513;
        private const int MaxCharStringLength = 65535;

        // Operators
        private const int Hstem = 1;
        private const int Vstem = 3;
        private const int CallSubr = 10;
        private const int Return = 11;
        private const int Escape = 12;
        private const int EndChar = 14;
        private const int HstemHm = 18;
        private const int HintMask = 19;
        private const int CntrMask = 20;
        private const int VstemHm = 23;
        private const int ShortInt = 28;
        private const int CallGSubr = 29;

        private readonly CffFont _font;

        // The state of the glyph being written: the charstring so far, the operand stack (values,
        // and where each operand is in the output) and the number of stem hints
        private readonly List<byte> _output = new List<byte>();
        private readonly List<double> _stack = new List<double>();
        private readonly List<int> _operandPositions = new List<int>();
        private int _stems;
        private CffPrivateDict _private = null!;
        private List<int>? _components;

        private enum Result
        {
            Continue,   // a subroutine returned
            End,        // endchar: the glyph is finished
            Fail,       // the charstring cannot be followed
        }

        private CffCharStringFlattener(CffFont font)
        {
            _font = font;
        }

        /// <summary>
        /// Writes the charstrings of the glyphs without subroutines, and adds the glyphs that
        /// accented glyphs are made of to the glyphs.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="glyphs">The glyphs in use; the components of accented glyphs are added.</param>
        /// <returns>The charstrings by glyph; null if a charstring could not be followed.</returns>
        public static Dictionary<int, byte[]>? Flatten(CffFont font, SortedSet<int> glyphs)
        {
            ArgumentNullException.ThrowIfNull(font);
            ArgumentNullException.ThrowIfNull(glyphs);

            var flattener = new CffCharStringFlattener(font);
            var charStrings = new Dictionary<int, byte[]>(glyphs.Count);
            var pending = new Queue<int>(glyphs);
            while (pending.Count > 0)
            {
                int glyph = pending.Dequeue();
                byte[]? charString = flattener.FlattenGlyph(glyph);
                if (charString is null)
                    return null;

                charStrings.Add(glyph, charString);
                if (flattener._components is null)
                    continue;

                foreach (int component in flattener._components)
                {
                    if (glyphs.Add(component))
                        pending.Enqueue(component);
                }
            }

            return charStrings;
        }

        private byte[]? FlattenGlyph(int glyph)
        {
            _output.Clear();
            _stack.Clear();
            _operandPositions.Clear();
            _stems = 0;
            _private = _font.GetPrivate(glyph);
            _components = null;

            Result result = Execute(_font.CharStrings.GetItemData(_font.Data, glyph), 0);
            return result == Result.End && _output.Count <= MaxCharStringLength ? _output.ToArray() : null;
        }

        private Result Execute(ReadOnlySpan<byte> code, int depth)
        {
            int i = 0;
            while (i < code.Length)
            {
                int b0 = code[i];
                if (b0 >= 32 || b0 == ShortInt)
                {
                    if (!CopyNumber(code, ref i))
                        return Result.Fail;
                    continue;
                }

                i++;
                switch (b0)
                {
                    case Hstem:
                    case Vstem:
                    case HstemHm:
                    case VstemHm:
                        _stems += _stack.Count / 2;
                        Emit(b0);
                        break;

                    case HintMask:
                    case CntrMask:
                    {
                        // Operands before a hint mask are the stems of an implied vstemhm
                        _stems += _stack.Count / 2;
                        int maskLength = (_stems + 7) / 8;
                        if (i + maskLength > code.Length)
                            return Result.Fail;
                        Emit(b0);
                        for (int k = 0; k < maskLength; k++)
                            _output.Add(code[i + k]);
                        i += maskLength;
                        break;
                    }

                    case CallSubr:
                    case CallGSubr:
                    {
                        Result result = Call(b0 == CallGSubr, depth);
                        if (result != Result.Continue)
                            return result;
                        break;
                    }

                    case Return:
                        return depth > 0 ? Result.Continue : Result.Fail;

                    case EndChar:
                        // endchar with four arguments (and maybe a width) is an accented glyph
                        // made of two glyphs of the standard encoding (seac)
                        if (_stack.Count >= 4 && !AddComponents())
                            return Result.Fail;
                        Emit(EndChar);
                        return Result.End;

                    case Escape:
                    {
                        if (i >= code.Length)
                            return Result.Fail;
                        int b1 = code[i++];
                        // flex, hflex, hflex1, flex1 and the deprecated dotsection; the arithmetic
                        // and storage operators could compute the number of a subroutine
                        if (b1 != 0 && (b1 < 34 || b1 > 37))
                            return Result.Fail;
                        Emit(Escape);
                        _output.Add((byte)b1);
                        break;
                    }

                    case 4:   // vmoveto
                    case 5:   // rlineto
                    case 6:   // hlineto
                    case 7:   // vlineto
                    case 8:   // rrcurveto
                    case 21:  // rmoveto
                    case 22:  // hmoveto
                    case 24:  // rcurveline
                    case 25:  // rlinecurve
                    case 26:  // vvcurveto
                    case 27:  // hhcurveto
                    case 30:  // vhcurveto
                    case 31:  // hvcurveto
                        Emit(b0);
                        break;

                    default:
                        // A reserved operator
                        return Result.Fail;
                }
            }

            // A subroutine may end without return, as interpreters accept; a glyph must end with endchar
            return depth > 0 ? Result.Continue : Result.Fail;
        }

        /// <summary>
        /// Writes an operator; it takes the operands from the stack.
        /// </summary>
        private void Emit(int op)
        {
            _output.Add((byte)op);
            _stack.Clear();
            _operandPositions.Clear();
        }

        private Result Call(bool global, int depth)
        {
            if (_stack.Count == 0 || depth >= MaxCallDepth)
                return Result.Fail;

            // The number of the subroutine is the last operand; nothing was written after it (an
            // operator would have taken it), so it is removed from the end of the output
            double operand = _stack[^1];
            int position = _operandPositions[^1];
            _stack.RemoveAt(_stack.Count - 1);
            _operandPositions.RemoveAt(_operandPositions.Count - 1);
            _output.RemoveRange(position, _output.Count - position);

            CffIndex? subrs = global ? _font.GlobalSubrs : _private.LocalSubrs;
            if (subrs is null || operand != Math.Floor(operand))
                return Result.Fail;

            int index = (int)operand + Bias(subrs.Value.Count);
            if (index < 0 || index >= subrs.Value.Count)
                return Result.Fail;

            return Execute(subrs.Value.GetItemData(_font.Data, index), depth + 1);
        }

        private bool AddComponents()
        {
            // The last four operands: adx ady bchar achar
            if (!AddComponent((int)_stack[^2]) || !AddComponent((int)_stack[^1]))
                return false;
            return true;
        }

        private bool AddComponent(int code)
        {
            if (code < 0 || code > 255 || _font.IsCidKeyed)
                return false;

            string? name = CffStandardData.StandardEncoding[code];
            int component = name is null ? -1 : _font.FindGlyph(name);
            if (component < 0)
                return false;

            (_components ??= new List<int>()).Add(component);
            return true;
        }

        private bool CopyNumber(ReadOnlySpan<byte> code, ref int i)
        {
            if (_stack.Count >= MaxStack)
                return false;

            int b0 = code[i];
            int length;
            double value;
            if (b0 == ShortInt)
            {
                length = 3;
                if (i + length > code.Length)
                    return false;
                value = (short)((code[i + 1] << 8) | code[i + 2]);
            }
            else if (b0 <= 246)
            {
                length = 1;
                value = b0 - 139;
            }
            else if (b0 <= 254)
            {
                length = 2;
                if (i + length > code.Length)
                    return false;
                value = b0 <= 250
                    ? (b0 - 247) * 256 + code[i + 1] + 108
                    : -(b0 - 251) * 256 - code[i + 1] - 108;
            }
            else
            {
                // 16.16 fixed-point number
                length = 5;
                if (i + length > code.Length)
                    return false;
                value = ((code[i + 1] << 24) | (code[i + 2] << 16) | (code[i + 3] << 8) | code[i + 4]) / 65536.0;
            }

            _stack.Add(value);
            _operandPositions.Add(_output.Count);
            for (int k = 0; k < length; k++)
                _output.Add(code[i + k]);
            i += length;
            return true;
        }

        /// <summary>
        /// The number added to a subroutine number in a charstring, by the number of subroutines.
        /// </summary>
        public static int Bias(int count) => count < 1240 ? 107 : count < 33900 ? 1131 : 32768;
    }
}
