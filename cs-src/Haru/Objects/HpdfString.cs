using System;
using System.Text;
using Haru.Streams;

namespace Haru.Objects
{
    /// <summary>
    /// PDF string object (text in parentheses or hex)
    /// </summary>
    public class HpdfString : HpdfObject
    {
        private byte[] _value = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the string value as bytes
        /// </summary>
        public byte[] Value
        {
            get => _value;
            set
            {
                _value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets whether to write as hex string (&lt;...&gt;) instead of literal string (...)
        /// </summary>
        public bool WriteAsHex { get; set; }

        /// <inheritdoc/>
        public override HpdfObjectClass ObjectClass => HpdfObjectClass.String;

        /// <summary>
        /// Creates a new string object from bytes
        /// </summary>
        public HpdfString(byte[] value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a PDF text string (PDF 1.7, 7.9.2.2): text of printable ASCII characters
        /// (and tab, line feed, carriage return) is stored as it is, which is the same in
        /// PDFDocEncoding; any other text is stored as UTF-16BE with the byte order mark FE FF,
        /// so viewers show every Unicode character.
        /// </summary>
        public HpdfString(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            Value = EncodeTextString(text);
        }

        /// <summary>
        /// Creates a new string object from text with specified encoding
        /// </summary>
        public HpdfString(string text, System.Text.Encoding encoding)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            Value = encoding.GetBytes(text);
        }

        /// <summary>
        /// Gets the string value as text using the specified encoding
        /// </summary>
        public string GetText(System.Text.Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(encoding);
            return encoding.GetString(Value);
        }

        /// <summary>
        /// Gets the value as text, decoded as a PDF text string: UTF-16BE when it starts with
        /// the byte order mark FE FF, UTF-8 when it starts with EF BB BF (PDF 2.0), and
        /// otherwise single bytes (PDFDocEncoding, read as ISO Latin-1).
        /// </summary>
        public string GetText()
        {
            var value = Value;
            if (value.Length >= 2 && value[0] == 0xFE && value[1] == 0xFF)
                return System.Text.Encoding.BigEndianUnicode.GetString(value, 2, value.Length - 2);
            if (value.Length >= 3 && value[0] == 0xEF && value[1] == 0xBB && value[2] == 0xBF)
                return System.Text.Encoding.UTF8.GetString(value, 3, value.Length - 3);
            return System.Text.Encoding.Latin1.GetString(value);
        }

        /// <summary>
        /// Encodes text as a PDF text string: ASCII as it is, anything else as UTF-16BE with a byte order mark.
        /// </summary>
        public static byte[] EncodeTextString(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            foreach (char c in text)
            {
                if (!IsPlainAscii(c))
                {
                    var bytes = new byte[2 + text.Length * 2];
                    bytes[0] = 0xFE;
                    bytes[1] = 0xFF;
                    System.Text.Encoding.BigEndianUnicode.GetBytes(text, 0, text.Length, bytes, 2);
                    return bytes;
                }
            }

            return System.Text.Encoding.ASCII.GetBytes(text);
        }

        /// <summary>
        /// Printable ASCII and the white space that PDFDocEncoding maps to the same codes.
        /// </summary>
        private static bool IsPlainAscii(char c) =>
            (c >= 0x20 && c <= 0x7E) || c == 0x09 || c == 0x0A || c == 0x0D;

        /// <inheritdoc/>
        public override void WriteValue(HpdfStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            byte[] dataToWrite = Value;

            // Encrypt if encryption context is set
            if (stream.EncryptionContext != null)
            {
                dataToWrite = stream.EncryptionContext.Encrypt(Value);
            }

            if (WriteAsHex)
            {
                stream.WriteHexString(dataToWrite);
            }
            else
            {
                // Write bytes as literal string with proper escaping
                stream.WriteLiteralString(dataToWrite);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string text = GetText();
            return WriteAsHex ? $"<{BitConverter.ToString(Value).Replace("-", "")}>" : $"({text})";
        }
    }
}
