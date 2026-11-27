using System;
using System.IO;
using System.Security.Cryptography;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// A file embedded in the document: a file specification (/Type /Filespec) with the file's data in an
    /// embedded file stream (/Type /EmbeddedFile), compressed. Create it with
    /// <see cref="HpdfDocument.AttachFile(string, byte[], string?)"/> (a document attachment, listed by viewers
    /// in their attachments panel) or with <see cref="HpdfPage.CreateFileAttachmentAnnotation(HpdfRect, string, byte[], string, Annotations.HpdfFileAttachmentIcon)"/>
    /// (an icon on a page).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "The stream is registered in the cross-reference table, which owns every PDF object of the document.")]
    public class HpdfEmbeddedFile
    {
        private readonly HpdfDict _fileSpec;
        private readonly HpdfStreamObject _stream;
        private readonly HpdfDict _params;
        private readonly HpdfXref _xref;

        /// <summary>
        /// Creates an embedded file.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="name">The file name (without a directory), shown by viewers and used when the file is saved.</param>
        /// <param name="data">The file's contents.</param>
        /// <param name="mimeType">The MIME type, such as <c>text/plain</c>, or null.</param>
        internal HpdfEmbeddedFile(HpdfXref xref, string name, byte[] data, string? mimeType)
        {
            ValidateArguments(name, data, mimeType);
            _xref = xref;

            Name = name;
            Size = data.Length;
            MimeType = mimeType;

            _stream = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
            _stream.Add("Type", new HpdfName("EmbeddedFile"));
            if (mimeType != null)
                _stream.Add("Subtype", new HpdfName(mimeType));
            _stream.WriteToStream(data);

            _params = new HpdfDict();
            _params.Add("Size", new HpdfNumber(data.Length));
            _params.Add("CheckSum", new HpdfString(MD5.HashData(data)));
            _stream.Add("Params", _params);
            xref.Add(_stream);

            var ef = new HpdfDict();
            ef.Add("F", _stream);

            _fileSpec = new HpdfDict();
            _fileSpec.Add("Type", new HpdfName("Filespec"));
            _fileSpec.Add("F", new HpdfString(ToFileSpecificationString(name)));
            _fileSpec.Add("EF", ef);
            xref.Add(_fileSpec);

            // /EF and /Params came with PDF 1.3; a name that /F cannot hold goes to /UF (PDF 1.7)
            if (HasNonAscii(name.Replace(' ', '_')))
            {
                _fileSpec.Add("UF", new HpdfString(name));
                xref.RequireVersion(HpdfVersion.Version17);
            }
            else
            {
                xref.RequireVersion(HpdfVersion.Version13);
            }
        }

        /// <summary>
        /// Checks the arguments of an embedded file (before any object is created).
        /// </summary>
        internal static void ValidateArguments(string name, byte[] data, string? mimeType)
        {
            if (string.IsNullOrEmpty(name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The file name cannot be null or empty");
            if (name.IndexOfAny(new[] { '/', '\\' }) >= 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The file name must not contain a directory");
            if (data is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The file data cannot be null");
            if (mimeType != null && (mimeType.Length == 0 || mimeType.IndexOf('/') <= 0 || HasNonAscii(mimeType)))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The MIME type must be of the form type/subtype in ASCII");
        }

        private static bool HasNonAscii(string text)
        {
            foreach (char c in text)
            {
                if (c <= ' ' || c > '~')
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the MIME type, or null.
        /// </summary>
        public string? MimeType { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the file specification dictionary.
        /// </summary>
        public HpdfDict Dict => _fileSpec;

        /// <summary>
        /// Sets the description of the file (/Desc, PDF 1.6), shown by viewers in the attachments panel.
        /// </summary>
        /// <param name="description">The description.</param>
        public void SetDescription(string description)
        {
            if (description is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Description cannot be null");

            _fileSpec["Desc"] = new HpdfString(description);
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        /// <summary>
        /// Sets the date the file was created (/CreationDate of the parameters).
        /// </summary>
        public void SetCreationDate(DateTimeOffset date) => _params["CreationDate"] = HpdfDateFormat.Create(date);

        /// <summary>
        /// Sets the date the file was last modified (/ModDate of the parameters).
        /// </summary>
        public void SetModificationDate(DateTimeOffset date) => _params["ModDate"] = HpdfDateFormat.Create(date);

        /// <summary>
        /// Reads a file for embedding: its name without the directory and its contents.
        /// </summary>
        internal static (string Name, byte[] Data) ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The path cannot be null or empty");
            if (!File.Exists(path))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"File not found: {path}");

            return (Path.GetFileName(path), File.ReadAllBytes(path));
        }

        /// <summary>
        /// The file specification string (/F): the name in PDFDocEncoding where it is ASCII; other characters,
        /// which the byte string cannot hold portably, become underscores (/UF has the real name).
        /// </summary>
        private static byte[] ToFileSpecificationString(string name)
        {
            var bytes = new byte[name.Length];
            for (int i = 0; i < name.Length; i++)
                bytes[i] = name[i] >= ' ' && name[i] <= '~' ? (byte)name[i] : (byte)'_';
            return bytes;
        }
    }
}
