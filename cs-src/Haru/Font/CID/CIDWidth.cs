namespace Haru.Font.CID
{
    /// <summary>
    /// Represents a CID (Character ID) to width mapping.
    /// Width is in 1000-unit glyph space.
    /// </summary>
    public struct CIDWidth
    {
        /// <summary>
        /// Gets or sets the Character ID.
        /// </summary>
        public ushort CID { get; set; }

        /// <summary>
        /// Gets or sets the character width in 1000-unit glyph space.
        /// </summary>
        public short Width { get; set; }
    }
}
