using System;
using System.IO;

namespace Haru.Png
{
    /// <summary>
    /// Facade interface for PNG image reading operations.
    /// Abstracts the underlying PNG library implementation.
    /// </summary>
    public interface IPngReader : IDisposable
    {
        /// <summary>
        /// Validates if the stream contains a valid PNG signature.
        /// </summary>
        bool ValidateSignature(Stream stream);

        /// <summary>
        /// Reads PNG image information from the stream.
        /// </summary>
        PngImageInfo ReadImageInfo(Stream stream);

        /// <summary>
        /// Reads image information and pixel data in one operation.
        /// Implementations should override this to decode the image only once.
        /// </summary>
        /// <remarks>
        /// The image starts at the current stream position. The default implementation returns
        /// a seekable stream to that position before reading the pixel data, so implementations
        /// of <see cref="ReadImageInfo"/> do not have to restore the position themselves.
        /// </remarks>
        /// <param name="stream">The stream containing PNG data</param>
        /// <returns>Image information and pixel data</returns>
        PngImageData ReadImage(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            long? start = stream.CanSeek ? stream.Position : null;
            var info = ReadImageInfo(stream);
            if (start.HasValue)
                stream.Position = start.Value;

            return new PngImageData(info, ReadPixelData(stream, info));
        }

        /// <summary>
        /// Reads the raw pixel data from the PNG image.
        /// </summary>
        /// <param name="stream">The stream containing PNG data</param>
        /// <param name="info">Previously read image information</param>
        /// <returns>Raw pixel data</returns>
        byte[] ReadPixelData(Stream stream, PngImageInfo info);

        /// <summary>
        /// Reads pixel data row by row for non-interlaced images.
        /// </summary>
        /// <param name="stream">The stream containing PNG data</param>
        /// <param name="info">Previously read image information</param>
        /// <param name="rowCallback">Callback invoked for each row</param>
        void ReadPixelDataByRow(Stream stream, PngImageInfo info, Action<byte[], int> rowCallback);
    }
}
