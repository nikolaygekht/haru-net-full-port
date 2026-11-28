/* An ICC color profile embedded in a document: the stream of an ICCBased color space. */

using System;
using System.Buffers.Binary;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// An ICC color profile embedded in the document (ISO 32000-1, 8.6.5.5): it makes colors device independent, as
    /// the color space <c>[/ICCBased profile]</c> of images (<see cref="HpdfImage.SetIccProfile(HpdfIccProfile?)"/>) and of the fill
    /// and stroke colors of a page, and it describes the output device of the PDF/A output intent. Get one with
    /// <see cref="HpdfDocument.LoadIccProfile(string)"/> or the built-in <see cref="HpdfDocument.SRgbProfile"/>; a
    /// document embeds equal profiles once.
    /// </summary>
    public sealed class HpdfIccProfile
    {
        private const int HeaderLength = 128;

        private readonly HpdfXref _xref;

        /// <summary>
        /// Gets the number of color components of the profile's color space: 1 (gray), 3 (RGB) or 4 (CMYK).
        /// </summary>
        public int ComponentCount { get; }

        /// <summary>
        /// Gets the device color space a reader uses when it cannot use the profile (/Alternate):
        /// <see cref="HpdfColorSpace.DeviceGray"/>, <see cref="HpdfColorSpace.DeviceRgb"/> or
        /// <see cref="HpdfColorSpace.DeviceCmyk"/>.
        /// </summary>
        public HpdfColorSpace AlternateColorSpace { get; }

        /// <summary>
        /// Gets the profile class of the header, such as <c>mntr</c> (monitor), <c>prtr</c> (printer) or <c>scnr</c>
        /// (input device).
        /// </summary>
        public string DeviceClass { get; }

        /// <summary>
        /// Gets the version of the ICC specification the profile follows (2.1, 4.3, ...).
        /// </summary>
        public Version ProfileVersion { get; }

        /// <summary>
        /// Gets the resource name of the color space on the pages that draw with it.
        /// </summary>
        public string LocalName { get; }

        /// <summary>
        /// Gets the profile stream (/N, /Alternate, the profile data, compressed).
        /// </summary>
        public HpdfStreamObject StreamObject { get; }

        /// <summary>
        /// The color space <c>[/ICCBased stream]</c>, one array shared by every user of the profile.
        /// </summary>
        internal HpdfArray ColorSpace { get; }

        /// <summary>
        /// The PDF version that supports the profile's ICC version (ISO 32000-1, 8.6.5.5; PDF Reference, table 4.17).
        /// </summary>
        internal HpdfVersion RequiredPdfVersion { get; }

        /// <summary>
        /// Whether PDF/A-1 allows the profile: ISO 19005-1 refers to ICC.1:1998-09, version 2 profiles.
        /// </summary>
        internal bool IsPdfA1Compatible => ProfileVersion.Major < 4;

        private HpdfIccProfile(HpdfXref xref, byte[] data, string localName, int componentCount,
            HpdfColorSpace alternate, string deviceClass, Version version)
        {
            _xref = xref;
            LocalName = localName;
            ComponentCount = componentCount;
            AlternateColorSpace = alternate;
            DeviceClass = deviceClass;
            ProfileVersion = version;
            RequiredPdfVersion = PdfVersionFor(version);

            StreamObject = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
            StreamObject.Add("N", new HpdfNumber(componentCount));
            StreamObject.Add("Alternate", new HpdfName(alternate switch
            {
                HpdfColorSpace.DeviceGray => "DeviceGray",
                HpdfColorSpace.DeviceRgb => "DeviceRGB",
                _ => "DeviceCMYK"
            }));
            StreamObject.WriteToStream(data);
            xref.Add(StreamObject);

            ColorSpace = new HpdfArray();
            ColorSpace.Add(new HpdfName("ICCBased"));
            ColorSpace.Add(StreamObject);
        }

        /// <summary>
        /// Reads the header of a profile and creates the profile object; the caller registers it.
        /// </summary>
        /// <exception cref="HpdfException">The data is not an ICC profile of a gray, RGB or CMYK color space.</exception>
        internal static HpdfIccProfile Create(HpdfXref xref, byte[] data, string localName)
        {
            var (components, alternate, deviceClass, version) = ReadHeader(data);
            return new HpdfIccProfile(xref, data, localName, components, alternate, deviceClass, version);
        }

        /// <summary>
        /// Checks the header of profile data (ICC.1, 7.2): the size, the 'acsp' signature, and the data color space,
        /// which gives the component count.
        /// </summary>
        internal static (int Components, HpdfColorSpace Alternate, string DeviceClass, Version Version) ReadHeader(
            byte[] data)
        {
            if (data is null || data.Length < HeaderLength)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "ICC profile data is shorter than its header");

            var span = data.AsSpan();
            uint size = BinaryPrimitives.ReadUInt32BigEndian(span);
            if (size < HeaderLength || size > (uint)data.Length)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"ICC profile size {size} does not match the data of {data.Length} bytes");
            if (Signature(span, 36) != "acsp")
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The data is not an ICC profile (no 'acsp' signature)");

            (int components, HpdfColorSpace alternate) = Signature(span, 16) switch
            {
                "GRAY" => (1, HpdfColorSpace.DeviceGray),
                "RGB " => (3, HpdfColorSpace.DeviceRgb),
                "CMYK" => (4, HpdfColorSpace.DeviceCmyk),
                var other => throw new HpdfException(HpdfErrorCode.InvalidIccComponentNum,
                    $"ICC profiles of the color space '{other.TrimEnd()}' are not supported: only gray, RGB and CMYK")
            };

            var version = new Version(data[8], data[9] >> 4);
            return (components, alternate, Signature(span, 12).TrimEnd(), version);
        }

        private static string Signature(ReadOnlySpan<byte> data, int offset) =>
            System.Text.Encoding.ASCII.GetString(data.Slice(offset, 4));

        /// <summary>
        /// ISO 32000-1, 8.6.5.5 (PDF Reference 1.7, table 4.17): ICC 3.3 (profile version 2.1) needs PDF 1.3, ICC.1:1998-09 (2.2) PDF 1.4,
        /// ICC.1:2001-12 (4.0) PDF 1.5, ICC.1:2003-09 (4.1) PDF 1.6, ICC.1:2004-10 (4.2 and later) PDF 1.7.
        /// </summary>
        private static HpdfVersion PdfVersionFor(Version version) => (version.Major, version.Minor) switch
        {
            ( < 2, _) or (2, < 2) => HpdfVersion.Version13,
            (2, _) or (3, _) => HpdfVersion.Version14,
            (4, 0) => HpdfVersion.Version15,
            (4, 1) => HpdfVersion.Version16,
            _ => HpdfVersion.Version17
        };

        /// <summary>
        /// Checks that the profile is one of the document of <paramref name="xref"/> and raises the document version
        /// to what the profile needs; called where the profile is used.
        /// </summary>
        internal void UseIn(HpdfXref xref)
        {
            if (xref != _xref)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The ICC profile belongs to another document");
            _xref.RequireVersion(RequiredPdfVersion);
        }
    }
}
