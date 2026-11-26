namespace Haru.Png
{
    /// <summary>
    /// A decoded image: header information together with the pixel data
    /// laid out according to <see cref="PngImageInfo.ColorType"/>.
    /// </summary>
    /// <param name="Info">Image information.</param>
    /// <param name="PixelData">Pixel data, <see cref="PngImageInfo.BytesPerRow"/> bytes per row, no row padding.</param>
    public sealed record PngImageData(PngImageInfo Info, byte[] PixelData);
}
