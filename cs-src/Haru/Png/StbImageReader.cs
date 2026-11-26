using System;
using System.IO;
using StbImageSharp;

namespace Haru.Png
{
    /// <summary>
    /// Implementation of IPngReader using StbImageSharp library.
    /// Supports both PNG and JPEG image formats.
    /// </summary>
    public class StbImageReader : IPngReader
    {
        private const int PngSignatureLength = 8;
        private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        private const int JpegSignatureLength = 2;
        private static readonly byte[] JpegSignature = new byte[] { 0xFF, 0xD8 };

        /// <summary>
        /// Validates if the stream contains a valid PNG signature.
        /// </summary>
        public bool ValidateSignature(Stream stream)
        {
            if (stream == null || !stream.CanRead)
                return false;

            long originalPosition = stream.Position;
            try
            {
                byte[] header = new byte[PngSignatureLength];
                int bytesRead = stream.Read(header, 0, PngSignatureLength);

                if (bytesRead != PngSignatureLength)
                    return false;

                for (int i = 0; i < PngSignatureLength; i++)
                {
                    if (header[i] != PngSignature[i])
                        return false;
                }

                return true;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Validates if the stream contains a valid JPEG signature.
        /// </summary>
        public bool ValidateJpegSignature(Stream stream)
        {
            if (stream == null || !stream.CanRead)
                return false;

            long originalPosition = stream.Position;
            try
            {
                byte[] header = new byte[JpegSignatureLength];
                int bytesRead = stream.Read(header, 0, JpegSignatureLength);

                if (bytesRead != JpegSignatureLength)
                    return false;

                for (int i = 0; i < JpegSignatureLength; i++)
                {
                    if (header[i] != JpegSignature[i])
                        return false;
                }

                return true;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Reads PNG or JPEG image information from the stream.
        /// </summary>
        public PngImageInfo ReadImageInfo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return CreateImageInfo(Decode(stream));
        }

        /// <summary>
        /// Reads image information and pixel data, decoding the image only once.
        /// </summary>
        public PngImageData ReadImage(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            var image = Decode(stream);
            var info = CreateImageInfo(image);
            return new PngImageData(info, ConvertPixels(image, info));
        }

        /// <summary>
        /// Reads the raw pixel data from the PNG or JPEG image.
        /// </summary>
        public byte[] ReadPixelData(Stream stream, PngImageInfo info)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(info);
            return ConvertPixels(Decode(stream), info);
        }

        /// <summary>
        /// Reads pixel data row by row for non-interlaced images.
        /// </summary>
        public void ReadPixelDataByRow(Stream stream, PngImageInfo info, Action<byte[], int> rowCallback)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(info);
            ArgumentNullException.ThrowIfNull(rowCallback);

            var image = Decode(stream);
            byte[] rowBuffer = new byte[info.BytesPerRow];

            for (int y = 0; y < image.Height; y++)
            {
                ConvertRow(image, y, info.ColorType, rowBuffer);
                rowCallback(rowBuffer, y);
            }
        }

        /// <summary>
        /// Decodes the whole image to 8-bit RGBA, preserving the stream position.
        /// </summary>
        private static ImageResult Decode(Stream stream)
        {
            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private static PngImageInfo CreateImageInfo(ImageResult image)
        {
            var info = new PngImageInfo
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                BitDepth = 8, // StbImageSharp normalizes to 8 bits
                IsInterlaced = false,
                ColorType = DetermineColorType(image)
            };
            info.BytesPerRow = (uint)(image.Width * info.Channels);
            return info;
        }

        private static byte[] ConvertPixels(ImageResult image, PngImageInfo info)
        {
            int bytesPerRow = (int)info.BytesPerRow;
            byte[] pixelData = new byte[bytesPerRow * image.Height];

            for (int y = 0; y < image.Height; y++)
            {
                ConvertRow(image, y, info.ColorType, pixelData.AsSpan(y * bytesPerRow, bytesPerRow));
            }

            return pixelData;
        }

        /// <summary>
        /// Converts one row of decoded RGBA data to the layout of the requested color type.
        /// </summary>
        private static void ConvertRow(ImageResult image, int y, PngColorType colorType, Span<byte> destination)
        {
            int width = image.Width;
            ReadOnlySpan<byte> source = image.Data.AsSpan(y * width * 4, width * 4);

            switch (colorType)
            {
                case PngColorType.RgbAlpha:
                    source.CopyTo(destination);
                    break;

                case PngColorType.Rgb:
                    for (int x = 0; x < width; x++)
                        source.Slice(x * 4, 3).CopyTo(destination.Slice(x * 3, 3));
                    break;

                case PngColorType.Grayscale:
                    for (int x = 0; x < width; x++)
                        destination[x] = Luminance(source.Slice(x * 4, 3));
                    break;

                case PngColorType.GrayscaleAlpha:
                    for (int x = 0; x < width; x++)
                    {
                        destination[x * 2] = Luminance(source.Slice(x * 4, 3));
                        destination[x * 2 + 1] = source[x * 4 + 3];
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported target color type: {colorType}", nameof(colorType));
            }
        }

        /// <summary>
        /// ITU-R BT.601 luminance in integer arithmetic; exact (returns the input) when R = G = B.
        /// </summary>
        private static byte Luminance(ReadOnlySpan<byte> rgb)
        {
            return (byte)((299 * rgb[0] + 587 * rgb[1] + 114 * rgb[2] + 500) / 1000);
        }

        /// <summary>
        /// Chooses the most compact color type that represents the image without loss:
        /// alpha is kept only if some pixel is not fully opaque, color is kept only if
        /// some pixel has R, G and B differing. Every pixel is examined, because skipping
        /// any of them could silently drop transparency or color.
        /// </summary>
        private static PngColorType DetermineColorType(ImageResult image)
        {
            bool hasAlpha = false;
            bool hasColor = false;

            ReadOnlySpan<byte> data = image.Data.AsSpan(0, image.Width * image.Height * 4);

            for (int i = 0; i < data.Length && !(hasAlpha && hasColor); i += 4)
            {
                hasAlpha |= data[i + 3] != 255;
                hasColor |= data[i] != data[i + 1] || data[i + 1] != data[i + 2];
            }

            if (hasColor)
                return hasAlpha ? PngColorType.RgbAlpha : PngColorType.Rgb;
            return hasAlpha ? PngColorType.GrayscaleAlpha : PngColorType.Grayscale;
        }

        public void Dispose()
        {
            // No resources to dispose in this implementation
            // Call SuppressFinalize to prevent derived types with finalizers
            // from needing to re-implement IDisposable
            GC.SuppressFinalize(this);
        }
    }
}
