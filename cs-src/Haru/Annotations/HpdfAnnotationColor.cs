/* A color of an annotation (/C, /IC): no color, gray, RGB or CMYK. */
using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;

namespace Haru.Annotations
{
    /// <summary>
    /// A color as annotations store it: an array of 0 (transparent), 1 (gray), 3 (RGB) or 4 (CMYK)
    /// components between 0 and 1. The same value writes the dictionary entry and the operators of
    /// the generated appearance stream.
    /// </summary>
    internal sealed class HpdfAnnotationColor
    {
        private readonly float[] _components;

        private HpdfAnnotationColor(float[] components)
        {
            foreach (float component in components)
            {
                if (!(component >= 0 && component <= 1))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Color values must be between 0 and 1");
            }
            _components = components;
        }

        /// <summary>
        /// No color: the element is not painted.
        /// </summary>
        public static HpdfAnnotationColor Transparent { get; } = new HpdfAnnotationColor(Array.Empty<float>());

        public static HpdfAnnotationColor Gray(float gray) => new HpdfAnnotationColor(new[] { gray });

        public static HpdfAnnotationColor Rgb(float r, float g, float b) => new HpdfAnnotationColor(new[] { r, g, b });

        public static HpdfAnnotationColor Cmyk(float c, float m, float y, float k) =>
            new HpdfAnnotationColor(new[] { c, m, y, k });

        /// <summary>
        /// Whether the color paints nothing.
        /// </summary>
        public bool IsTransparent => _components.Length == 0;

        /// <summary>
        /// Creates the array of the dictionary entry.
        /// </summary>
        public HpdfArray ToArray()
        {
            var array = new HpdfArray();
            foreach (float component in _components)
                array.Add(new HpdfReal(component));
            return array;
        }

        /// <summary>
        /// Gets the operator that sets the color for filling, as written in a default appearance string
        /// (<c>0 g</c>, <c>1 0 0 rg</c>, <c>0 0 0 1 k</c>); empty for a transparent color.
        /// </summary>
        public string ToFillOperator()
        {
            string op = _components.Length switch
            {
                1 => "g",
                3 => "rg",
                4 => "k",
                _ => ""
            };
            if (op.Length == 0)
                return "";

            var parts = new string[_components.Length + 1];
            for (int i = 0; i < _components.Length; i++)
                parts[i] = _components[i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
            parts[_components.Length] = op;
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Sets the color for filling (a transparent color sets nothing).
        /// </summary>
        public void SetFill(IDrawable canvas)
        {
            switch (_components.Length)
            {
                case 1:
                    canvas.SetGrayFill(_components[0]);
                    break;
                case 3:
                    canvas.SetRgbFill(_components[0], _components[1], _components[2]);
                    break;
                case 4:
                    canvas.SetCmykFill(_components[0], _components[1], _components[2], _components[3]);
                    break;
            }
        }

        /// <summary>
        /// Sets the color for stroking (a transparent color sets nothing).
        /// </summary>
        public void SetStroke(IDrawable canvas)
        {
            switch (_components.Length)
            {
                case 1:
                    canvas.SetGrayStroke(_components[0]);
                    break;
                case 3:
                    canvas.SetRgbStroke(_components[0], _components[1], _components[2]);
                    break;
                case 4:
                    canvas.SetCmykStroke(_components[0], _components[1], _components[2], _components[3]);
                    break;
            }
        }
    }
}
