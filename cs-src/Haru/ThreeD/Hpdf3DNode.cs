using Haru.Objects;
using Haru.Types;

namespace Haru.ThreeD
{
    /// <summary>
    /// The state of a node of a 3D model in a view (/Type /3DNode, PDF 1.7): its opacity, visibility and
    /// position. Create it with <see cref="Hpdf3DView.CreateNode"/>; the node is found by its name in the model.
    /// </summary>
    public class Hpdf3DNode
    {
        private readonly HpdfDict _dict;

        internal Hpdf3DNode(string name)
        {
            Name = name;
            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("3DNode"));
            _dict.Add("N", new HpdfString(name));
        }

        /// <summary>
        /// Gets the node dictionary.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the name of the node in the model.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Sets the opacity of the node (/O).
        /// </summary>
        /// <param name="opacity">The opacity, 0 (invisible) to 1 (opaque).</param>
        public void SetOpacity(float opacity)
        {
            if (!(opacity >= 0 && opacity <= 1))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The opacity must be between 0 and 1");
            _dict["O"] = new HpdfReal(opacity);
        }

        /// <summary>
        /// Sets whether the node is visible (/V).
        /// </summary>
        public void SetVisibility(bool visible) => _dict["V"] = new HpdfBoolean(visible);

        /// <summary>
        /// Sets the position of the node: its matrix relative to its parent (/M).
        /// </summary>
        public void SetMatrix(Hpdf3DMatrix matrix) => _dict["M"] = Hpdf3DView.ToArray(matrix);
    }
}
