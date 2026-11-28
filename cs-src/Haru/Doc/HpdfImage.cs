using System;
using System.IO;
using Haru.Objects;
using Haru.Xref;
using Haru.Types;
using Haru.Streams;
using Haru.Png;

namespace Haru.Doc
{
    /// <summary>
    /// Represents a PDF image XObject.
    /// Images are external objects that can be drawn on pages using the Do operator.
    /// </summary>
    public class HpdfImage : IDisposable
    {
        private readonly HpdfStreamObject _streamObject;
        private readonly HpdfXref _xref;

        /// <summary>
        /// Gets the cross-reference table of the document the image belongs to.
        /// </summary>
        internal HpdfXref Xref => _xref;
        private readonly string _localName;
        private bool _disposed = false;
        private bool _usedAsSoftMask;

        /// <summary>
        /// Gets the underlying stream object.
        /// </summary>
        public HpdfStreamObject StreamObject => _streamObject;

        /// <summary>
        /// Gets the image dictionary (the stream object itself is a dictionary).
        /// </summary>
        public HpdfDict Dict => _streamObject;

        /// <summary>
        /// Gets the local resource name (e.g., "Im1").
        /// </summary>
        public string LocalName => _localName;

        /// <summary>
        /// Gets the image width in pixels.
        /// </summary>
        public uint Width
        {
            get
            {
                if (Dict.TryGetValue("Width", out var widthObj) && widthObj is HpdfNumber number)
                    return (uint)number.Value;
                return 0;
            }
        }

        /// <summary>
        /// Gets the image height in pixels.
        /// </summary>
        public uint Height
        {
            get
            {
                if (Dict.TryGetValue("Height", out var heightObj) && heightObj is HpdfNumber number)
                    return (uint)number.Value;
                return 0;
            }
        }

        /// <summary>
        /// Gets the number of bits per color component, or 0 if not set.
        /// </summary>
        public int BitsPerComponent =>
            Dict.TryGetValue("BitsPerComponent", out var bpc) && bpc is HpdfNumber number ? number.Value : 0;

        /// <summary>
        /// Gets whether this image is a stencil mask (/ImageMask true), e.g. after being passed to <see cref="SetMaskImage"/>.
        /// </summary>
        public bool IsImageMask =>
            Dict.TryGetValue(ImageMaskKey, out var value) && value is HpdfBoolean { Value: true };

        /// <summary>
        /// Gets or sets whether a viewer should smooth the image when it is scaled up (/Interpolate true), so that
        /// it does not show the pixels as squares; false by default. A hint only: viewers may ignore it, and
        /// PDF/A-1 does not allow it (saving a PDF/A document with an interpolated image fails).
        /// </summary>
        public bool Interpolate
        {
            get => Dict.TryGetValue(InterpolateKey, out var value) && value is HpdfBoolean { Value: true };
            set
            {
                if (value)
                    Dict[InterpolateKey] = new HpdfBoolean(true);
                else
                    Dict.Remove(InterpolateKey);
            }
        }

        /// <summary>
        /// The key of the interpolation flag of an image dictionary.
        /// </summary>
        internal const string InterpolateKey = "Interpolate";

        private const string ColorSpaceGray = "DeviceGray";
        private const string ColorSpaceRgb = "DeviceRGB";
        private const string MaskKey = "Mask";
        private const string SoftMaskKey = "SMask";
        private const string ImageMaskKey = "ImageMask";

        /// <summary>
        /// Gets the color space name, or null if the color space is not a simple name (e.g. Indexed) or not set.
        /// </summary>
        private string? ColorSpaceName =>
            Dict.TryGetValue("ColorSpace", out var colorSpace) && colorSpace is HpdfName name ? name.Value : null;

        /// <summary>
        /// The device color space of the image's colors: its color space name, or the alternate of its ICC profile.
        /// </summary>
        private string? DeviceColorSpaceName => IccProfile is { } profile ? DeviceName(profile.AlternateColorSpace) : ColorSpaceName;

        private static string DeviceName(HpdfColorSpace colorSpace) => colorSpace switch
        {
            HpdfColorSpace.DeviceGray => ColorSpaceGray,
            HpdfColorSpace.DeviceRgb => ColorSpaceRgb,
            _ => "DeviceCMYK"
        };

        /// <summary>
        /// Gets the ICC profile of the image's color space, or null for a device color space.
        /// </summary>
        public HpdfIccProfile? IccProfile { get; private set; }

        /// <summary>
        /// Whether the profile came from the image file (PNG iCCP, JPEG APP2) rather than from
        /// <see cref="SetIccProfile"/>: PDF/A-1 saving leaves such a profile out if PDF/A-1 does not allow it.
        /// </summary>
        internal bool IccProfileFromFile { get; private set; }

        /// <summary>
        /// Sets the ICC profile of the image's colors (HPDF_AddColorspaceFromProfile): the color space becomes
        /// <c>[/ICCBased profile]</c>, so the colors are reproduced as the profile describes, for example an image in
        /// Adobe RGB or a CMYK image of a printing condition. Null returns the image to its device color space.
        /// </summary>
        /// <param name="profile">
        /// A profile of this document with as many components as the image (1 for gray, 3 for RGB, 4 for CMYK), or null.
        /// </param>
        public void SetIccProfile(HpdfIccProfile? profile) => SetIccProfile(profile, fromFile: false);

        internal void SetIccProfile(HpdfIccProfile? profile, bool fromFile)
        {
            string? device = DeviceColorSpaceName;
            if (profile is null)
            {
                if (IccProfile != null && device != null)
                    Dict["ColorSpace"] = new HpdfName(device);
                IccProfile = null;
                IccProfileFromFile = false;
                return;
            }

            if (IsImageMask || _usedAsSoftMask)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "A mask image has no color space of its own and cannot have an ICC profile");
            int components = device switch
            {
                ColorSpaceGray => 1,
                ColorSpaceRgb => 3,
                "DeviceCMYK" => 4,
                _ => throw new HpdfException(HpdfErrorCode.InvalidColorSpace,
                    "Only images of a DeviceGray, DeviceRGB or DeviceCMYK color space can get an ICC profile")
            };
            if (profile.ComponentCount != components)
                throw new HpdfException(HpdfErrorCode.InvalidIccComponentNum,
                    $"The profile has {profile.ComponentCount} components, the image {components}");
            profile.UseIn(_xref);

            IccProfile = profile;
            IccProfileFromFile = fromFile;
            Dict["ColorSpace"] = profile.ColorSpace;
            HpdfIccProfileRegistry.Of(_xref).Track(this);
        }

        /// <summary>
        /// The component count of profile data from an image file, or 0 for data that is not a usable profile: a
        /// damaged profile in a file is ignored, never thrown.
        /// </summary>
        private static int ProfileComponents(byte[] profile)
        {
            try
            {
                return HpdfIccProfile.ReadHeader(profile).Components;
            }
            catch (HpdfException)
            {
                return 0;
            }
        }

        private static byte[] GrayToRgb(byte[] gray)
        {
            var rgb = new byte[gray.Length * 3];
            for (int i = 0; i < gray.Length; i++)
                rgb[3 * i] = rgb[3 * i + 1] = rgb[3 * i + 2] = gray[i];
            return rgb;
        }

        /// <summary>
        /// Writes the color space of an image with an ICC profile for a save: the profile, or - in a PDF/A-1 document,
        /// for a profile from the image file that PDF/A-1 does not allow - the device color space; a profile set by
        /// hand that PDF/A-1 does not allow fails the save.
        /// </summary>
        internal void PrepareColorSpace(bool pdfA1)
        {
            if (IccProfile is not { } profile)
                return;

            if (pdfA1 && !profile.IsPdfA1Compatible)
            {
                if (!IccProfileFromFile)
                    throw new HpdfException(HpdfErrorCode.InvalidOperation,
                        $"PDF/A-1 needs ICC profiles of version 2; the profile of image {LocalName} is of version {profile.ProfileVersion}");
                Dict["ColorSpace"] = new HpdfName(DeviceName(profile.AlternateColorSpace));
                return;
            }

            Dict["ColorSpace"] = profile.ColorSpace;
        }

        private HpdfImage(HpdfXref xref, string localName)
        {
            if (xref is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            if (string.IsNullOrEmpty(localName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Local name cannot be null or empty");

            _xref = xref;
            _localName = localName;
            _streamObject = new HpdfStreamObject();

            // Set up basic image XObject dictionary
            Dict.Add("Type", new HpdfName("XObject"));
            Dict.Add("Subtype", new HpdfName("Image"));

            // Add to xref
            xref.Add(_streamObject);
        }

        /// <summary>
        /// Loads a PNG image from a file.
        /// </summary>
        public static HpdfImage LoadPngImage(HpdfXref xref, string localName, string filePath)
        {
            if (!File.Exists(filePath))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"File not found: {filePath}");

            using (var fileStream = File.OpenRead(filePath))
            {
                return LoadPngImage(xref, localName, fileStream);
            }
        }

        /// <summary>
        /// Loads a PNG image from a file (alias for LoadPngImage for API compatibility).
        /// </summary>
        public static HpdfImage LoadPngImageFromFile(HpdfXref xref, string localName, string filePath)
        {
            return LoadPngImage(xref, localName, filePath);
        }

        /// <summary>
        /// Loads a PNG image from memory (byte array).
        /// </summary>
        public static HpdfImage LoadPngImageFromMem(HpdfXref xref, string localName, byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "PNG data cannot be null or empty");

            using (var memoryStream = new MemoryStream(data))
            {
                return LoadPngImage(xref, localName, memoryStream);
            }
        }

        /// <summary>
        /// Loads raw image data from memory.
        /// This is used for grayscale or RGB images where pixel data is already decoded. A DeviceGray image of 1 bit
        /// per component is stored with CCITT Group 4 as by <see cref="LoadRaw1BitImageFromMem"/> (0 bits black), as
        /// in the C library; the other images are compressed with Flate.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">The local resource name for the image.</param>
        /// <param name="data">Raw pixel data.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="colorSpace">Color space (DeviceGray, DeviceRGB, or DeviceCMYK).</param>
        /// <param name="bitsPerComponent">Bits per color component (typically 8).</param>
        /// <returns>The loaded image.</returns>
        public static HpdfImage LoadRawImageFromMem(HpdfXref xref, string localName, byte[] data,
            uint width, uint height, HpdfColorSpace colorSpace, byte bitsPerComponent)
        {
            if (data == null || data.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image data cannot be null or empty");

            if (width == 0 || height == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image dimensions must be greater than zero");

            // Calculate expected data size
            // As in the C library: 1-bit gray images are coded with CCITT Group 4, the 0 bits black
            if (colorSpace == HpdfColorSpace.DeviceGray && bitsPerComponent == 1)
                return LoadRaw1BitImageFromMem(xref, localName, data, width, height, (width + 7) / 8);

            int componentsPerPixel = colorSpace switch
            {
                HpdfColorSpace.DeviceGray => 1,
                HpdfColorSpace.DeviceRgb => 3,
                HpdfColorSpace.DeviceCmyk => 4,
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Unsupported color space: {colorSpace}")
            };

            if (bitsPerComponent != 1 && bitsPerComponent != 2 && bitsPerComponent != 4 &&
                bitsPerComponent != 8 && bitsPerComponent != 16)
                throw new HpdfException(HpdfErrorCode.InvalidBitPerComponent,
                    $"Bits per component must be 1, 2, 4, 8 or 16, got {bitsPerComponent}");

            // Each row starts on a byte boundary (PDF 8.9.5.1)
            long bytesPerRow = ((long)width * componentsPerPixel * bitsPerComponent + 7) / 8;
            long expectedLong = bytesPerRow * height;
            if (expectedLong > int.MaxValue)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image is too large");
            int expectedSize = (int)expectedLong;

            if (data.Length < expectedSize)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"Image data size mismatch: expected at least {expectedSize} bytes, got {data.Length}");

            var image = new HpdfImage(xref, localName);

            // Set image properties
            image.Dict.Add("Width", new HpdfNumber((int)width));
            image.Dict.Add("Height", new HpdfNumber((int)height));
            image.Dict.Add("BitsPerComponent", new HpdfNumber(bitsPerComponent));

            // Set color space
            string colorSpaceName = colorSpace switch
            {
                HpdfColorSpace.DeviceGray => "DeviceGray",
                HpdfColorSpace.DeviceRgb => "DeviceRGB",
                HpdfColorSpace.DeviceCmyk => "DeviceCMYK",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Unsupported color space: {colorSpace}")
            };
            image.Dict.Add("ColorSpace", new HpdfName(colorSpaceName));

            // Write the pixel data of the image; bytes after it are not part of the image
            image._streamObject.Stream.Write(data, 0, expectedSize);

            // Apply compression
            image._streamObject.Filter = HpdfStreamFilter.FlateDecode;

            return image;
        }

        /// <summary>
        /// Loads a 1-bit (black and white) image from raw rows and stores it compressed with CCITT Group 4
        /// (CCITTFaxDecode, HPDF_Image_LoadRaw1BitImageFromMem), which makes scanned text and line art much
        /// smaller than Flate. The image is DeviceGray with one bit per pixel.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">The local resource name for the image.</param>
        /// <param name="data">The rows of the image, one bit per pixel, the leftmost pixel in the high bit of a byte.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="lineWidth">The length of a row in bytes, at least (width + 7) / 8.</param>
        /// <param name="oneIsBlack">
        /// True if the 1 bits of the data are black (fax and TIFF data); false (the default) if the 0 bits are black
        /// and the 1 bits white, as in DeviceGray. The C library's <c>black_is1</c> is the opposite: it is the /BlackIs1
        /// entry of the filter, which inverts the encoder's 1-is-black.
        /// </param>
        /// <param name="topIsFirst">True if the first row of the data is the top row, false if it is the bottom row.</param>
        /// <returns>The loaded image.</returns>
        public static HpdfImage LoadRaw1BitImageFromMem(HpdfXref xref, string localName, byte[] data,
            uint width, uint height, uint lineWidth, bool oneIsBlack = false, bool topIsFirst = true)
        {
            if (data == null || data.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image data cannot be null or empty");
            if (width == 0 || height == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Image dimensions must be greater than zero");
            if (lineWidth < (width + 7) / 8)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"A row of {width} pixels needs at least {(width + 7) / 8} bytes, the line width is {lineWidth}");
            if ((long)lineWidth * height > data.Length)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"Image data size mismatch: expected at least {(long)lineWidth * height} bytes, got {data.Length}");

            byte[] encoded = CcittG4Encoder.Encode(data, (int)width, (int)height, (int)lineWidth, topIsFirst);

            var image = new HpdfImage(xref, localName);
            image.Dict.Add("Width", new HpdfNumber((int)width));
            image.Dict.Add("Height", new HpdfNumber((int)height));
            image.Dict.Add("BitsPerComponent", new HpdfNumber(1));
            image.Dict.Add("ColorSpace", new HpdfName(ColorSpaceGray));

            var parameters = new HpdfDict();
            parameters.Add("K", new HpdfNumber(-1));            // pure two-dimensional coding (Group 4)
            parameters.Add("Columns", new HpdfNumber((int)width));
            parameters.Add("Rows", new HpdfNumber((int)height));
            // The encoder codes 1 bits as black runs; a decoder outputs black runs as 1 with /BlackIs1 true, which
            // DeviceGray shows as white: data where 0 is black needs /BlackIs1 true, data where 1 is black false
            parameters.Add("BlackIs1", new HpdfBoolean(!oneIsBlack));
            // /Filter is written as an array, so /DecodeParms is an array of the same length
            var decodeParms = new HpdfArray();
            decodeParms.Add(parameters);
            image.Dict.Add("DecodeParms", decodeParms);

            // The data is coded already: PrepareStreamData passes it through
            image._streamObject.Stream.Write(encoded, 0, encoded.Length);
            image._streamObject.Filter = HpdfStreamFilter.CcittFaxDecode;
            return image;
        }

        /// <summary>
        /// Loads a PNG image from a stream.
        /// </summary>
        public static HpdfImage LoadPngImage(HpdfXref xref, string localName, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // The reader checks the signature and steps back, so a forward-only stream is buffered first
            stream = EnsureSeekable(stream);
            byte[]? fileProfile = EmbeddedIccProfileReader.ReadPng(stream);

            PngImageData decoded;
            using (var pngReader = PngReaderFactory.Create())
            {
                if (!pngReader.ValidateSignature(stream))
                    throw new HpdfException(HpdfErrorCode.InvalidPngImage, "Invalid PNG signature");

                decoded = pngReader.ReadImage(stream);
            }

            var info = decoded.Info;
            var image = new HpdfImage(xref, localName);

            image.Dict.Add("Width", new HpdfNumber((int)info.Width));
            image.Dict.Add("Height", new HpdfNumber((int)info.Height));
            image.Dict.Add("BitsPerComponent", new HpdfNumber(info.BitDepth));

            byte[] colorData = decoded.PixelData;
            switch (info.ColorType)
            {
                case PngColorType.Grayscale:
                    image.Dict.Add("ColorSpace", new HpdfName(ColorSpaceGray));
                    break;

                case PngColorType.Rgb:
                    image.Dict.Add("ColorSpace", new HpdfName(ColorSpaceRgb));
                    break;

                case PngColorType.Palette:
                    image.SetIndexedColorSpace(info);
                    break;

                case PngColorType.GrayscaleAlpha:
                case PngColorType.RgbAlpha:
                    // The alpha channel goes to a separate soft mask image (SMask)
                    int colorComponents = info.Channels - 1;
                    image.Dict.Add("ColorSpace", new HpdfName(colorComponents == 1 ? ColorSpaceGray : ColorSpaceRgb));
                    (colorData, byte[] alphaData) = SplitAlphaChannel(decoded.PixelData, colorComponents);
                    image.AddAlphaSoftMask(alphaData, (int)info.Width, (int)info.Height, info.BitDepth);
                    break;

                default:
                    throw new HpdfException(HpdfErrorCode.InvalidPngImage, $"Unsupported PNG color type: {info.ColorType}");
            }

            // The profile of the file (iCCP); all-gray pixels of an RGB profile go back to RGB to keep their colors
            HpdfIccProfile? profile = null;
            if (fileProfile != null && image.ColorSpaceName is { } device)
            {
                int components = device == ColorSpaceGray ? 1 : 3;
                int profileComponents = ProfileComponents(fileProfile);
                if (profileComponents == 3 && components == 1 && info.BitDepth == 8)
                {
                    colorData = GrayToRgb(colorData);
                    image.Dict["ColorSpace"] = new HpdfName(ColorSpaceRgb);
                    components = 3;
                }
                if (profileComponents == components)
                    profile = HpdfIccProfileRegistry.Of(xref).GetOrAdd(fileProfile);
            }

            image._streamObject.Stream.Write(colorData, 0, colorData.Length);
            image._streamObject.Filter = HpdfStreamFilter.FlateDecode;
            if (profile != null)
                image.SetIccProfile(profile, fromFile: true);

            return image;
        }

        private void SetIndexedColorSpace(PngImageInfo info)
        {
            if (info.Palette == null || info.Palette.Colors.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidPngImage, "Palette PNG image has no palette data");

            // Create indexed color space array: [/Indexed /DeviceRGB maxIndex paletteData]
            var colorSpaceArray = new HpdfArray();
            colorSpaceArray.Add(new HpdfName("Indexed"));
            colorSpaceArray.Add(new HpdfName("DeviceRGB"));
            colorSpaceArray.Add(new HpdfNumber(info.Palette.ColorCount - 1));

            // Create palette data as binary string
            var paletteData = new byte[info.Palette.ColorCount * 3];
            for (int i = 0; i < info.Palette.ColorCount; i++)
            {
                var color = info.Palette.GetColor(i);
                paletteData[i * 3] = color.R;
                paletteData[i * 3 + 1] = color.G;
                paletteData[i * 3 + 2] = color.B;
            }
            colorSpaceArray.Add(new HpdfBinary(paletteData));

            Dict.Add("ColorSpace", colorSpaceArray);
        }

        /// <summary>
        /// Loads a JPEG image from a file.
        /// </summary>
        public static HpdfImage LoadJpegImage(HpdfXref xref, string localName, string filePath)
        {
            if (!File.Exists(filePath))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"File not found: {filePath}");

            using (var fileStream = File.OpenRead(filePath))
            {
                return LoadJpegImage(xref, localName, fileStream);
            }
        }

        /// <summary>
        /// Loads a JPEG image from a stream.
        /// </summary>
        public static HpdfImage LoadJpegImage(HpdfXref xref, string localName, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // The JPEG data starts at the current position of the stream (as for PNG images)
            byte[] jpegData;
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                jpegData = buffer.ToArray();
            }

            // Parse JPEG header to get dimensions and color space before creating any PDF object
            var jpegInfo = ParseJpegHeader(jpegData);
            var image = new HpdfImage(xref, localName);

            // Set image properties
            image.Dict.Add("Width", new HpdfNumber((int)jpegInfo.Width));
            image.Dict.Add("Height", new HpdfNumber((int)jpegInfo.Height));
            image.Dict.Add("BitsPerComponent", new HpdfNumber(jpegInfo.BitsPerComponent));
            image.Dict.Add("ColorSpace", new HpdfName(jpegInfo.ColorSpace));

            // For CMYK JPEG, add Decode array
            if (jpegInfo.Components == 4)
            {
                var decodeArray = new HpdfArray();
                for (int i = 0; i < 4; i++)
                {
                    decodeArray.Add(new HpdfNumber(1));
                    decodeArray.Add(new HpdfNumber(0));
                }
                image.Dict.Add("Decode", decodeArray);
            }

            // Copy JPEG data directly to stream (already DCT-compressed)
            image._streamObject.Stream.Write(jpegData, 0, jpegData.Length);

            // Set DCTDecode filter - PrepareStreamData will pass the data through unchanged
            // since JPEG files are already DCT-compressed
            image._streamObject.Filter = HpdfStreamFilter.DctDecode;

            // The profile of the file (APP2 ICC_PROFILE), if it fits the components of the image
            if (EmbeddedIccProfileReader.ReadJpeg(jpegData) is { } fileProfile
                && ProfileComponents(fileProfile) == jpegInfo.Components)
                image.SetIccProfile(HpdfIccProfileRegistry.Of(xref).GetOrAdd(fileProfile), fromFile: true);

            return image;
        }

        /// <summary>
        /// Returns the stream itself if it can seek, otherwise a memory copy of its remaining data.
        /// </summary>
        private static Stream EnsureSeekable(Stream stream)
        {
            if (stream.CanSeek)
                return stream;

            var buffered = new MemoryStream();
            stream.CopyTo(buffered);
            buffered.Position = 0;
            return buffered;
        }

        private static JpegInfo ParseJpegHeader(byte[] jpegData)
        {
            try
            {
                using var stream = new MemoryStream(jpegData, writable: false);
                using var reader = new BinaryReader(stream);
                return ParseJpegHeader(stream, reader);
            }
            catch (EndOfStreamException)
            {
                throw new HpdfException(HpdfErrorCode.InvalidJpegData,
                    "JPEG data ends before a supported frame header (baseline, extended or progressive)");
            }
        }

        private static JpegInfo ParseJpegHeader(Stream stream, BinaryReader reader)
        {
            {
                // Check JPEG signature (0xFF 0xD8)
                ushort marker = ReadUInt16BE(reader);
                if (marker != 0xFFD8)
                    throw new HpdfException(HpdfErrorCode.InvalidJpegData, "Invalid JPEG signature");

                // Find SOF (Start of Frame) marker
                while (true)
                {
                    marker = ReadUInt16BE(reader);
                    ushort length = ReadUInt16BE(reader);

                    // Check for SOF markers (0xFFC0-0xFFC3, 0xFFC5-0xFFC7, 0xFFC9-0xFFCB, 0xFFCD-0xFFCF)
                    if (marker == 0xFFC0 || marker == 0xFFC1 || marker == 0xFFC2 || marker == 0xFFC9)
                    {
                        byte precision = reader.ReadByte();
                        ushort height = ReadUInt16BE(reader);
                        ushort width = ReadUInt16BE(reader);
                        byte components = reader.ReadByte();

                        string colorSpace;
                        switch (components)
                        {
                            case 1:
                                colorSpace = "DeviceGray";
                                break;
                            case 3:
                                colorSpace = "DeviceRGB";
                                break;
                            case 4:
                                colorSpace = "DeviceCMYK";
                                break;
                            default:
                                throw new HpdfException(HpdfErrorCode.UnsupportedJpegFormat, $"Unsupported JPEG component count: {components}");
                        }

                        return new JpegInfo
                        {
                            Width = width,
                            Height = height,
                            BitsPerComponent = precision,
                            Components = components,
                            ColorSpace = colorSpace
                        };
                    }

                    // Skip to next marker
                    if ((marker & 0xFF00) != 0xFF00)
                        throw new HpdfException(HpdfErrorCode.UnsupportedJpegFormat, "Lost JPEG marker");

                    stream.Seek(length - 2, SeekOrigin.Current);
                }
            }
        }

        private static ushort ReadUInt16BE(BinaryReader reader)
        {
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();
            return (ushort)((b1 << 8) | b2);
        }

        /// <summary>
        /// Splits interleaved color+alpha pixel data into separate color and alpha planes in a single pass.
        /// </summary>
        /// <param name="pixelData">Interleaved pixels; alpha is the last component of each pixel.</param>
        /// <param name="colorComponents">Number of color components per pixel (1 for gray, 3 for RGB).</param>
        private static (byte[] Color, byte[] Alpha) SplitAlphaChannel(ReadOnlySpan<byte> pixelData, int colorComponents)
        {
            int bytesPerPixel = colorComponents + 1;
            int pixelCount = pixelData.Length / bytesPerPixel;
            byte[] color = new byte[pixelCount * colorComponents];
            byte[] alpha = new byte[pixelCount];

            for (int i = 0; i < pixelCount; i++)
            {
                pixelData.Slice(i * bytesPerPixel, colorComponents).CopyTo(color.AsSpan(i * colorComponents, colorComponents));
                alpha[i] = pixelData[i * bytesPerPixel + colorComponents];
            }

            return (color, alpha);
        }

        /// <summary>
        /// Creates a soft mask (SMask) image from alpha channel data and attaches it to this image.
        /// </summary>
        private void AddAlphaSoftMask(byte[] alphaData, int width, int height, int bitsPerComponent)
        {
            var smask = new HpdfStreamObject();
            smask.Add("Type", new HpdfName("XObject"));
            smask.Add("Subtype", new HpdfName("Image"));
            smask.Add("Width", new HpdfNumber(width));
            smask.Add("Height", new HpdfNumber(height));
            smask.Add("ColorSpace", new HpdfName(ColorSpaceGray));
            smask.Add("BitsPerComponent", new HpdfNumber(bitsPerComponent));

            smask.Stream.Write(alphaData, 0, alphaData.Length);
            smask.Filter = HpdfStreamFilter.FlateDecode;

            _xref.Add(smask);
            Dict.Add(SoftMaskKey, smask);
            _xref.RequireVersion(HpdfVersion.Version14); // soft masks were introduced in PDF 1.4
        }

        /// <summary>
        /// Sets a color key mask: pixels whose color falls within all given ranges are not painted (transparent).
        /// Requires an 8-bit DeviceRGB or DeviceGray image without another mask.
        /// </summary>
        /// <param name="rmin">Minimum red value (0-255).</param>
        /// <param name="rmax">Maximum red value (0-255).</param>
        /// <param name="gmin">Minimum green value (0-255).</param>
        /// <param name="gmax">Maximum green value (0-255).</param>
        /// <param name="bmin">Minimum blue value (0-255).</param>
        /// <param name="bmax">Maximum blue value (0-255).</param>
        /// <remarks>
        /// A DeviceGray image (for example, a PNG whose pixels are all gray is stored that way) holds
        /// the gray level g for the color (g, g, g), so it gets the equivalent single-range mask:
        /// the gray levels that fall within all three ranges. If no gray level does, the color key
        /// cannot match any pixel and no mask is written.
        /// </remarks>
        /// <exception cref="HpdfException">
        /// The image is a stencil mask or already has a mask (<c>InvalidOperation</c>),
        /// is not 8-bit (<c>InvalidBitPerComponent</c>), is neither DeviceRGB nor DeviceGray
        /// (<c>InvalidColorSpace</c>), or a range is invalid (<c>InvalidParameter</c>).
        /// </exception>
        public void SetColorMask(uint rmin, uint rmax, uint gmin, uint gmax, uint bmin, uint bmax)
        {
            EnsureCanAttachMask();

            if (BitsPerComponent != 8)
                throw new HpdfException(HpdfErrorCode.InvalidBitPerComponent,
                    $"Color mask requires 8 bits per component, image has {BitsPerComponent}");

            string? colorSpace = DeviceColorSpaceName;
            if (colorSpace != ColorSpaceRgb && colorSpace != ColorSpaceGray)
                throw new HpdfException(HpdfErrorCode.InvalidColorSpace,
                    $"Color mask requires a {ColorSpaceRgb} or {ColorSpaceGray} image, image color space is {colorSpace ?? "not a device color space"}");

            if (rmin > rmax || gmin > gmax || bmin > bmax || rmax > 255 || gmax > 255 || bmax > 255)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "Color mask ranges must satisfy min <= max <= 255");

            if (colorSpace == ColorSpaceRgb)
            {
                Dict.Add(MaskKey, new HpdfArray
                {
                    new HpdfNumber((int)rmin), new HpdfNumber((int)rmax),
                    new HpdfNumber((int)gmin), new HpdfNumber((int)gmax),
                    new HpdfNumber((int)bmin), new HpdfNumber((int)bmax)
                });
                _xref.RequireVersion(HpdfVersion.Version13); // color key masking was introduced in PDF 1.3
                return;
            }

            uint grayMin = Math.Max(rmin, Math.Max(gmin, bmin));
            uint grayMax = Math.Min(rmax, Math.Min(gmax, bmax));
            if (grayMin <= grayMax)
            {
                Dict.Add(MaskKey, new HpdfArray { new HpdfNumber((int)grayMin), new HpdfNumber((int)grayMax) });
                _xref.RequireVersion(HpdfVersion.Version13);
            }
        }

        /// <summary>
        /// Sets a hard-edged (stencil) mask: each pixel of this image is either fully shown or fully hidden.
        /// </summary>
        /// <param name="maskImage">
        /// An 8-bit DeviceGray image (e.g. loaded from PNG), or an image already used as a mask.
        /// Mask pixels &gt;= 128 (light) <b>hide</b> the image, pixels &lt; 128 (dark) <b>show</b> it.
        /// </param>
        /// <remarks>
        /// On first use the mask image is converted in place to a 1-bit stencil mask (/ImageMask),
        /// so it can then be shared by several images but no longer drawn as an ordinary image.
        /// For that reason an image already used as a soft mask cannot become a stencil mask.
        /// For gradual transparency use <see cref="SetSoftMask"/>.
        /// </remarks>
        /// <exception cref="HpdfException">
        /// This image is a stencil mask or already has a mask (<c>InvalidOperation</c>),
        /// or the mask image is unsuitable (<c>InvalidParameter</c>, <c>InvalidColorSpace</c>,
        /// <c>InvalidBitPerComponent</c>).
        /// </exception>
        public void SetMaskImage(HpdfImage maskImage)
        {
            ValidateMaskImageArgument(maskImage);
            EnsureCanAttachMask();

            if (maskImage.Width != Width || maskImage.Height != Height)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "Mask image dimensions must match the main image");

            if (!maskImage.IsImageMask)
                maskImage.ConvertToStencilMask();

            Dict.Add(MaskKey, maskImage._streamObject);
            _xref.RequireVersion(HpdfVersion.Version13); // explicit (stencil) masking was introduced in PDF 1.3
        }

        /// <summary>
        /// Sets a soft mask: a grayscale image whose pixels define the opacity of this image
        /// (0 = fully transparent, 255 = fully opaque, values in between are partially transparent).
        /// </summary>
        /// <param name="maskImage">
        /// A DeviceGray image. Its resolution may differ from this image; it is stretched to the same area.
        /// The mask image is not modified and can still be drawn as an ordinary image.
        /// </param>
        /// <remarks>Soft masks require PDF 1.4; the document version is raised automatically.</remarks>
        /// <exception cref="HpdfException">
        /// This image is a stencil mask or already has a mask, including a PNG alpha channel (<c>InvalidOperation</c>),
        /// or the mask image is unsuitable (<c>InvalidParameter</c>, <c>InvalidColorSpace</c>).
        /// </exception>
        public void SetSoftMask(HpdfImage maskImage)
        {
            ValidateMaskImageArgument(maskImage);
            EnsureCanAttachMask();

            if (maskImage.IsImageMask)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "A stencil mask image cannot be used as a soft mask");

            if (maskImage.ColorSpaceName != ColorSpaceGray)
                throw new HpdfException(HpdfErrorCode.InvalidColorSpace,
                    $"Soft mask image must have {ColorSpaceGray} color space");

            if (maskImage.Dict.ContainsKey(MaskKey) || maskImage.Dict.ContainsKey(SoftMaskKey))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "Soft mask image must not have a mask of its own");

            Dict.Add(SoftMaskKey, maskImage._streamObject);
            maskImage._usedAsSoftMask = true;
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        private void ValidateMaskImageArgument(HpdfImage maskImage)
        {
            if (maskImage is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Mask image cannot be null");

            if (ReferenceEquals(maskImage, this))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "An image cannot be its own mask");
        }

        /// <summary>
        /// Ensures a mask of any kind can be attached: stencil masks cannot be masked,
        /// and an image can carry only one mask (a PDF viewer ignores /Mask when /SMask is present).
        /// </summary>
        private void EnsureCanAttachMask()
        {
            if (IsImageMask)
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "A stencil mask image cannot have a mask");

            // A soft mask must not have a mask of its own (SetSoftMask checks it the other way round)
            if (_usedAsSoftMask)
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "An image used as a soft mask cannot have a mask");

            if (Dict.ContainsKey(SoftMaskKey))
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "Image already has a soft mask (for example, a PNG alpha channel)");

            if (Dict.ContainsKey(MaskKey))
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "Image already has a mask");
        }

        /// <summary>
        /// Converts this 8-bit DeviceGray image in place into a 1-bit stencil mask (/ImageMask true).
        /// With the default /Decode [0 1], sample 1 marks a masked-out (not painted) pixel,
        /// so light pixels (&gt;= 128) become 1 and hide the masked image.
        /// </summary>
        private void ConvertToStencilMask()
        {
            // The conversion rewrites this image, which would break every image using it as a soft mask
            if (_usedAsSoftMask)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "An image used as a soft mask cannot be used as a stencil mask");

            if (ColorSpaceName != ColorSpaceGray)
                throw new HpdfException(HpdfErrorCode.InvalidColorSpace,
                    $"Mask image must have {ColorSpaceGray} color space");

            if (BitsPerComponent != 8)
                throw new HpdfException(HpdfErrorCode.InvalidBitPerComponent,
                    $"Mask image must have 8 bits per component, has {BitsPerComponent}");

            if (_streamObject.Filter.HasFlag(HpdfStreamFilter.DctDecode))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "Mask image must contain uncompressed pixel data (JPEG images cannot be used as masks)");

            if (Dict.ContainsKey(MaskKey) || Dict.ContainsKey(SoftMaskKey))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    "Mask image must not have a mask of its own");

            byte[] grayData = _streamObject.Stream.ToArray();
            int width = (int)Width;
            int height = (int)Height;

            if (grayData.Length < width * height)
                throw new HpdfException(HpdfErrorCode.InvalidImage,
                    $"Mask image data size mismatch: expected {width * height}, got {grayData.Length}");

            // Pack to 1 bit per pixel, MSB first, each row padded to a byte boundary
            int bytesPerRow = (width + 7) / 8;
            byte[] packedData = new byte[bytesPerRow * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grayData[y * width + x] >= 128)
                        packedData[y * bytesPerRow + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }

            _streamObject.ClearStream();
            _streamObject.Stream.Write(packedData, 0, packedData.Length);

            Dict.Remove("ColorSpace");
            Dict[ImageMaskKey] = new HpdfBoolean(true);
            Dict["BitsPerComponent"] = new HpdfNumber(1);
        }

        private class JpegInfo
        {
            public ushort Width { get; set; }
            public ushort Height { get; set; }
            public byte BitsPerComponent { get; set; }
            public byte Components { get; set; }
            public string ColorSpace { get; set; } = string.Empty;
        }

        /// <summary>
        /// Disposes the image and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the image
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _streamObject?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
