# HpdfImage Class

[Back to Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Overview

`HpdfImage` represents an image XObject: picture data stored once in the PDF file and drawn on any number of pages,
at any position and size. Images are loaded from PNG files, JPEG files or raw pixel data, and can carry
transparency (a PNG alpha channel, a color key mask, a stencil mask or a soft mask).

### Purpose

- **Image loading** - PNG (any color type, decoded with StbImageSharp), JPEG (embedded unchanged) and raw pixel data
- **Reuse** - one image object can be drawn many times on many pages; its data is written to the file only once
- **Transparency** - automatic soft mask for PNG alpha channels, plus `SetColorMask`, `SetMaskImage` and `SetSoftMask`
- **Information** - pixel size, bits per component, stencil mask state

### Basic Usage

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// Load the image once
using var logo = pdf.LoadPngImageFromFile("logo.png");

// Draw it with its lower-left corner at (50, 700), 120 x 60 points large
page.DrawImage(logo, 50, 700, 120, 60);

pdf.SaveToFile("image.pdf");
```

---

## Getting an Instance

`HpdfImage` has no public constructor. Images are created by the loader methods below; every loader adds
the image to the document's cross-reference table, so it is written to the file when the document is saved.

### Document Methods (Recommended)

| Method | Description |
|--------|-------------|
| `HpdfDocument.LoadPngImageFromFile(string filePath)` | Loads a PNG file |
| `HpdfDocument.LoadPngImage(Stream stream)` | Loads a PNG image from a stream |
| `HpdfDocument.LoadJpegImageFromFile(string filePath)` | Loads a JPEG file |
| `HpdfDocument.LoadJpegImage(Stream stream)` | Loads a JPEG image from a stream |

The document methods give each image a unique resource name (`HaruIm` followed by a number; the prefix is
[HpdfDocument.GeneratedNamePrefix](HpdfDocument.md#generatednameprefix)), so you never have to choose one. See
[HpdfDocument](HpdfDocument.md) for the document itself.

### Static Methods of HpdfImage

These lower-level methods take the cross-reference table (`pdf.Xref`) and a resource name chosen by you.
They are needed for raw pixel data and for PNG data held in a byte array.

| Method | Description |
|--------|-------------|
| `LoadRawImageFromMem(HpdfXref xref, string localName, byte[] data, uint width, uint height, HpdfColorSpace colorSpace, byte bitsPerComponent)` | Creates an image from uncompressed pixel data |
| `LoadRaw1BitImageFromMem(HpdfXref xref, string localName, byte[] data, uint width, uint height, uint lineWidth, bool oneIsBlack = false, bool topIsFirst = true)` | Creates a black and white image, stored with CCITT Group 4 |
| `LoadPngImageFromMem(HpdfXref xref, string localName, byte[] data)` | Loads a PNG image from a byte array |
| `LoadPngImage(HpdfXref xref, string localName, string filePath)` | Loads a PNG file |
| `LoadPngImageFromFile(HpdfXref xref, string localName, string filePath)` | Same as `LoadPngImage(xref, localName, filePath)` |
| `LoadPngImage(HpdfXref xref, string localName, Stream stream)` | Loads a PNG image from a stream |
| `LoadJpegImage(HpdfXref xref, string localName, string filePath)` | Loads a JPEG file |
| `LoadJpegImage(HpdfXref xref, string localName, Stream stream)` | Loads a JPEG image from a stream |

**Choosing a resource name:** the name identifies the image in the resources of every page it is drawn on.
A name on a page refers to one image only: drawing a second, different image with the same name on that page
throws `HpdfException` with `DuplicateRegistration`. Use a different name for every image, and do not start it with
`HpdfDocument.GeneratedNamePrefix` (`"Haru"`), the prefix of the names the document methods produce: for example
`Raw1`, `Raw2`.

### PNG Images

```csharp
// From a file
using var photo = pdf.LoadPngImageFromFile("photo.png");

// From a stream
using var stream = File.OpenRead("icon.png");
using var icon = pdf.LoadPngImage(stream);

// From a byte array
byte[] pngData = File.ReadAllBytes("badge.png");
using var badge = HpdfImage.LoadPngImageFromMem(pdf.Xref, "Raw1", pngData);
```

How PNG data is stored:

- The image is decoded to 8-bit RGBA, so the stored image always has **8 bits per component**
  (16-bit images are reduced, palette and 1/2/4-bit images are expanded).
- **Every pixel** is examined to choose the most compact lossless form:

| Pixels of the PNG | Stored as |
|-------------------|-----------|
| All gray (R = G = B), all fully opaque | `DeviceGray` |
| Some colored, all fully opaque | `DeviceRGB` |
| All gray, some not fully opaque | `DeviceGray` + soft mask with the alpha channel |
| Some colored, some not fully opaque | `DeviceRGB` + soft mask with the alpha channel |

  A single colored or transparent pixel is enough to keep the color or the alpha channel.
  A gray PNG saved as RGB is therefore stored as `DeviceGray`, which matters for [SetColorMask](#setcolormask)
  and for mask images.
- The pixel data is compressed with FlateDecode.
- `LoadPngImage(Stream)` reads the image that starts at the **current position** of the stream and restores
  that position afterwards. A stream that cannot seek (`CanSeek` is `false`) is accepted too: its remaining data is
  read to the end and buffered in memory first.

**Errors:** a missing file throws `HpdfException` with `InvalidParameter`; data without a PNG signature throws
`HpdfException` with `InvalidPngImage`. Damaged image data is reported by the decoder.

### JPEG Images

```csharp
using var photo = pdf.LoadJpegImageFromFile("photo.jpg");

using var stream = File.OpenRead("scan.jpg");
using var scan = pdf.LoadJpegImage(stream);
```

How JPEG data is stored:

- The JPEG data is embedded unchanged with the DCTDecode filter; it is not decoded or recompressed.
- Width, height, bits per component and color space are read from the frame header (baseline, extended and
  progressive JPEGs). 1, 3 and 4 components become `DeviceGray`, `DeviceRGB` and `DeviceCMYK`;
  CMYK images get an inverted `/Decode` array.
- `LoadJpegImage(Stream)` reads the image that starts at the **current position** of the stream, up to the end of
  the stream (as for PNG images, the stream does not need to start with the JPEG data). It does not restore the
  position.

**Errors:** a missing file throws `HpdfException` with `InvalidParameter`; data without a JPEG signature, or data
that ends before a supported frame header (a truncated file), throws `HpdfException` with `InvalidJpegData`; an
unsupported component count throws `HpdfException` with `UnsupportedJpegFormat`. The header is checked before
any PDF object is created, so a failed load leaves nothing behind in the document.

### Raw Pixel Data

`LoadRawImageFromMem` creates an image from pixel data you have already decoded or generated.

| Parameter | Type | Description |
|-----------|------|-------------|
| xref | HpdfXref | Cross-reference table of the document (`pdf.Xref`) |
| localName | string | Resource name of the image; must not be empty |
| data | byte[] | Pixels row by row, top row first, components interleaved (R, G, B for RGB); each row starts on a byte boundary |
| width | uint | Width in pixels; greater than zero |
| height | uint | Height in pixels; greater than zero |
| colorSpace | HpdfColorSpace | `DeviceGray`, `DeviceRgb` or `DeviceCmyk` |
| bitsPerComponent | byte | Bits per color component: 1, 2, 4, 8 or 16, normally 8 |

Each row takes `(width * components * bitsPerComponent + 7) / 8` bytes (rows with 1, 2 or 4 bits per component
are padded to whole bytes), and the data must hold at least `height` such rows; otherwise `HpdfException` with
`InvalidParameter` is thrown. Bytes after the last row are not part of the image and are ignored. Other bit
depths throw `InvalidBitPerComponent`; other color spaces, empty data and a zero size throw `InvalidParameter`.
The data is compressed with FlateDecode, except a `DeviceGray` image of 1 bit per component, which is stored with
CCITT Group 4 as by [LoadRaw1BitImageFromMem](#black-and-white-images) (0 bits black), as in the C library.

```csharp
using Haru.Types;

// 2 x 2 pixels: red, green / blue, yellow
byte[] pixels =
{
    255, 0, 0,     0, 255, 0,
    0, 0, 255,     255, 255, 0
};

using var tile = HpdfImage.LoadRawImageFromMem(pdf.Xref, "Raw1", pixels, 2, 2,
    HpdfColorSpace.DeviceRgb, 8);

page.DrawImage(tile, 100, 500, 100, 100);
```

### Black and White Images

`LoadRaw1BitImageFromMem` (`HPDF_Image_LoadRaw1BitImageFromMem` of the C library) creates a `DeviceGray` image of
one bit per pixel and stores it compressed with **CCITT Group 4** (`/Filter /CCITTFaxDecode`, `/K -1`), the
compression of fax machines and scanned documents. For scanned text and line art it is much smaller than Flate (a
page of text at 300 dpi: about two thirds of the Flate size); for noise-like images such as dithered photos it is
larger.

```csharp
public static HpdfImage LoadRaw1BitImageFromMem(HpdfXref xref, string localName, byte[] data,
    uint width, uint height, uint lineWidth, bool oneIsBlack = false, bool topIsFirst = true)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| xref | HpdfXref | Cross-reference table of the document (`pdf.Xref`) |
| localName | string | Resource name of the image; must not be empty |
| data | byte[] | Rows of one bit per pixel, the leftmost pixel in the highest bit of a byte |
| width | uint | Width in pixels; greater than zero |
| height | uint | Height in pixels; greater than zero |
| lineWidth | uint | Length of a row in bytes, at least `(width + 7) / 8`; larger for rows padded to 4 bytes (BMP, many scanner APIs). Bits and bytes after the pixels of a row are ignored |
| oneIsBlack | bool | `false` (default): the **0 bits are black** and the 1 bits white, as in `DeviceGray`. `true`: the **1 bits are black**, as in fax and TIFF data |
| topIsFirst | bool | `true` (default): the first row of `data` is the top row; `false`: the bottom row (BMP order) |

The parameter is named by what it means for the data. The C library's `black_is1` is the opposite: it is written as
the `/BlackIs1` entry of the filter, and since the encoder codes 1 bits as black, `/BlackIs1 true` shows the 0 bits
black. A C call with `black_is1 = HPDF_TRUE` is `oneIsBlack: false` here, and `HPDF_FALSE` is `oneIsBlack: true`.

**Throws:** `HpdfException` (`InvalidParameter`) for empty data, a zero size, a `lineWidth` shorter than a row or
data shorter than `lineWidth * height`.

**Example:**

```csharp
// A 1 bit per pixel scan from a scanner API: rows padded to 4 bytes, 1 = black
using var scan = HpdfImage.LoadRaw1BitImageFromMem(pdf.Xref, "Scan1", scanData, 2480, 3508,
    lineWidth: 312, oneIsBlack: true);

page.DrawImage(scan, 0, 0, page.Width, page.Height);
```

### Lifetime and Disposal

`HpdfImage` implements `IDisposable`. The document refers to the image's data until it is written, so dispose
an image only **after** the document has been saved. Declaring it with `using var` in the method that saves the
document does exactly that, because the image is disposed when the method ends:

```csharp
static void CreateReport(string imagePath, string outputPath)
{
    var pdf = new HpdfDocument();
    var page = pdf.AddPage();

    using var chart = pdf.LoadPngImageFromFile(imagePath);
    page.DrawImage(chart, 50, 400, 400, 300);

    pdf.SaveToFile(outputPath);
}   // chart is disposed here, after saving
```

Do not wrap the image in a `using` block that ends before `SaveToFile` or `SaveToMemory` is called.

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Width](#width) | uint | Image width in pixels |
| [Height](#height) | uint | Image height in pixels |
| [BitsPerComponent](#bitspercomponent) | int | Bits per color component |
| [IsImageMask](#isimagemask) | bool | Whether the image has been converted to a stencil mask |
| [Interpolate](#interpolate) | bool | Whether viewers should smooth the image when it is scaled up (`/Interpolate`) |
| [LocalName](#localname) | string | Resource name used on pages |
| [Dict](#dict) | HpdfDict | Image dictionary (advanced) |
| [StreamObject](#streamobject) | HpdfStreamObject | Underlying stream object (advanced) |

### Width

Gets the image width in pixels, or 0 if it is not set.

```csharp
public uint Width { get; }
```

### Height

Gets the image height in pixels, or 0 if it is not set.

```csharp
public uint Height { get; }
```

**Example:**

```csharp
using var image = pdf.LoadPngImageFromFile("photo.png");

// Fit the image into a 200 point wide box, keeping the aspect ratio
float drawWidth = 200;
float drawHeight = drawWidth * image.Height / image.Width;

page.DrawImage(image, 50, 500, drawWidth, drawHeight);
```

### BitsPerComponent

Gets the number of bits per color component, or 0 if it is not set.

```csharp
public int BitsPerComponent { get; }
```

PNG images always report 8. JPEG images report the precision from the frame header. A stencil mask
(see [SetMaskImage](#setmaskimage)) reports 1.

### IsImageMask

Gets whether the image is a stencil mask, which is the case after it has been passed to
[SetMaskImage](#setmaskimage).

```csharp
public bool IsImageMask { get; }
```

**Example:**

```csharp
using var picture = pdf.LoadPngImageFromFile("picture.png");
using var stencil = pdf.LoadPngImageFromFile("stencil.png");

Console.WriteLine(stencil.IsImageMask);  // False

picture.SetMaskImage(stencil);

Console.WriteLine(stencil.IsImageMask);  // True
Console.WriteLine(stencil.BitsPerComponent);  // 1
```

### Interpolate

Gets or sets whether a viewer should smooth the image when it is drawn larger than its pixel size, instead of showing
each pixel as a square (`/Interpolate true`). `false` by default; setting it to `false` removes the entry.

```csharp
public bool Interpolate { get; set; }
```

It is a hint: viewers may ignore it, and many smooth images anyway or only at some zoom levels. It suits photos and
small icons drawn large; leave it off for pixel art, charts made of pixels and scans of text, where sharp edges
matter. PDF/A-1 does not allow it: saving a PDF/A document with an interpolated image throws `HpdfException`
(`InvalidOperation`). The flag is not in the C library.

**Example:**

```csharp
using var icon = pdf.LoadPngImageFromFile("icon-16.png");
icon.Interpolate = true;
page.DrawImage(icon, 50, 600, 128, 128);   // 16 x 16 pixels drawn at 128 x 128 points
```

### LocalName

Gets the resource name of the image (for example `"HaruIm5"` for an image loaded through the document), which the
page content uses to refer to it.

```csharp
public string LocalName { get; }
```

### Dict

Gets the image dictionary (`/Type /XObject /Subtype /Image`, `/Width`, `/Height`, `/ColorSpace`, mask entries).

```csharp
public HpdfDict Dict { get; }
```

**Usage:** For low-level inspection or additions to the PDF object.

### StreamObject

Gets the stream object that holds the image data. It is the same object as `Dict`.

```csharp
public HpdfStreamObject StreamObject { get; }
```

**Usage:** For low-level PDF manipulation (advanced).

---

## Methods

| Name | Description |
|------|-------------|
| [SetColorMask(uint, uint, uint, uint, uint, uint)](#setcolormask) | Makes pixels whose color falls within the given ranges transparent |
| [SetMaskImage(HpdfImage)](#setmaskimage) | Shows or hides each pixel according to a black-and-white mask image |
| [SetSoftMask(HpdfImage)](#setsoftmask) | Sets per-pixel opacity from a grayscale image |
| [Dispose()](#lifetime-and-disposal) | Disposes the image; call it after the document is saved |

The loader methods are described in [Getting an Instance](#getting-an-instance).

---

## Drawing Images

Images are drawn with page extension methods (namespace `Haru.Doc`). An image is defined on a 1 x 1 unit
square; the methods below place and scale that square on the page. Drawing an image also adds it to the
page resources, once per page.

### DrawImage

Draws an image with its lower-left corner at `(x, y)` and the given size. Defined in
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md).

```csharp
public static void DrawImage(this HpdfPage page, HpdfImage image,
    float x, float y, float width, float height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page to draw on |
| image | HpdfImage | The image to draw |
| x | float | X coordinate of the lower-left corner |
| y | float | Y coordinate of the lower-left corner |
| width | float | Width on the page in points; must be positive |
| height | float | Height on the page in points; must be positive |

**Description:**
Writes `q`, `width 0 0 height x y cm`, `/Name Do`, `Q`, so the graphics state is unchanged afterwards.
The size on the page does not depend on the pixel size: pass `image.Width` and `image.Height` to draw one
pixel per point. A `null` image throws `HpdfException` with `InvalidParameter`; a width or height of zero or
less throws `HpdfException` with `PageOutOfRange`.

**Example:**

```csharp
using var image = pdf.LoadPngImageFromFile("logo.png");

// One pixel per point
page.DrawImage(image, 50, 700, image.Width, image.Height);

// Stretched horizontally
page.DrawImage(image, 50, 550, image.Width * 2, image.Height);

// The same image object, drawn on another page
var page2 = pdf.AddPage();
page2.DrawImage(image, 100, 100, 300, 150);
```

### ExecuteXObject

Draws an image into the unit square of the current transformation matrix (the `Do` operator alone).
Defined in `HpdfPageExtensions`.

```csharp
public static void ExecuteXObject(this HpdfPage page, HpdfImage image)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page to draw on |
| image | HpdfImage | The image to draw |

**Description:**
Use it for transformations that `DrawImage` cannot express, such as rotation, skewing and mirroring. Set up
the matrix with `Concat` between `GSave` and `GRestore`; the image fills the unit square of that matrix.
A `null` image throws `HpdfException` with `InvalidParameter`.

**Example:**

```csharp
using var image = pdf.LoadJpegImageFromFile("photo.jpg");
float w = 200;
float h = 150;

// Rotated by 30 degrees around the lower-left corner (300, 400)
double rad = 30 * Math.PI / 180;
page.GSave();
page.Concat((float)(w * Math.Cos(rad)), (float)(w * Math.Sin(rad)),
    (float)(-h * Math.Sin(rad)), (float)(h * Math.Cos(rad)), 300, 400);
page.ExecuteXObject(image);
page.GRestore();

// Mirrored horizontally, occupying the box (50, 100) - (250, 250)
page.GSave();
page.Concat(-w, 0, 0, h, 50 + w, 100);
page.ExecuteXObject(image);
page.GRestore();
```

---

## Transparency

An image can be made partly transparent in five ways:

| Technique | How | Effect | PDF version |
|-----------|-----|--------|-------------|
| PNG alpha channel | Automatic when loading a PNG with transparent pixels | Per-pixel opacity from the alpha channel | 1.4 |
| [Color key mask](#setcolormask) | `SetColorMask` | Pixels of a color range are not painted | 1.3 |
| [Stencil mask](#setmaskimage) | `SetMaskImage` | Each pixel fully shown or fully hidden by a mask image | 1.3 |
| [Soft mask](#setsoftmask) | `SetSoftMask` | Per-pixel opacity from a grayscale image | 1.4 |
| [Whole-image opacity](#whole-image-opacity) | `HpdfExtGState.SetAlphaFill` before drawing | Same opacity for the whole image | 1.4 |

### One Mask per Image

An image can carry only **one** mask: a PNG alpha channel, a color key mask, a stencil mask or a soft mask.
(A PDF viewer would ignore a `/Mask` when an `/SMask` is present.) `SetColorMask`, `SetMaskImage` and
`SetSoftMask` throw `HpdfException` with `InvalidOperation` when the image

- already has a mask of any kind, including the soft mask created for a PNG alpha channel,
- is itself a stencil mask (it was passed to `SetMaskImage` before), or
- is itself used as a soft mask (it was passed to `SetSoftMask` before): a soft mask cannot have a mask of its own.

A mask cannot be removed or replaced. Whole-image opacity is not a mask and can be combined with any of them.

### PDF Version

The document version is raised automatically: to 1.3 by `SetColorMask` and `SetMaskImage`, and to 1.4 when a
PNG with transparency is loaded, when `SetSoftMask` is called, and when an `HpdfExtGState` alpha value is set.
Setting `HpdfDocument.Version` lower has no effect. (A `SetColorMask` call on a gray image that writes no mask
does not raise the version.)

### PNG Alpha Channel

When a PNG has at least one pixel that is not fully opaque, the alpha channel is stored as a separate
8-bit `DeviceGray` image and attached as the soft mask (`/SMask`) of the color image. Nothing else is needed:

```csharp
using var icon = pdf.LoadPngImageFromFile("icon-with-alpha.png");

// The page background shows through the transparent pixels
page.SetRgbFill(0.9f, 0.9f, 1.0f);
page.Rectangle(40, 590, 220, 120);
page.Fill();

page.DrawImage(icon, 50, 600, 200, 100);
```

A fully opaque PNG gets no soft mask, even if the file has an alpha channel.

### Whole-Image Opacity

To draw a whole image with the same opacity, set the fill alpha of an extended graphics state before drawing.
The alpha value applies to everything painted afterwards, so enclose it in `GSave` / `GRestore`:

```csharp
using var watermark = pdf.LoadPngImageFromFile("watermark.png");

var halfTransparent = new HpdfExtGState(pdf.Xref, "GS_Image50");
halfTransparent.SetAlphaFill(0.5f);    // 0 = invisible, 1 = opaque

page.GSave();
page.SetExtGState(halfTransparent);
page.DrawImage(watermark, 100, 300, 400, 200);
page.GRestore();
```

This works for every image and together with any mask, including a PNG alpha channel. `SetAlphaFill` raises the
document version to 1.4.

---

## Method Details

### SetColorMask

Sets a color key mask: pixels whose color falls within all given ranges are not painted.

```csharp
public void SetColorMask(uint rmin, uint rmax, uint gmin, uint gmax, uint bmin, uint bmax)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| rmin | uint | Minimum red value (0-255) |
| rmax | uint | Maximum red value (0-255) |
| gmin | uint | Minimum green value (0-255) |
| gmax | uint | Maximum green value (0-255) |
| bmin | uint | Minimum blue value (0-255) |
| bmax | uint | Maximum blue value (0-255) |

**Return Value:**
`void`

**Description:**
A pixel is hidden when its red, green and blue values all lie within their ranges (bounds included).

- **DeviceRGB image:** the six values are written as the `/Mask` array.
- **DeviceGray image:** a gray level `g` stands for the color `(g, g, g)`, so the mask contains the gray levels
  that lie within **all three** ranges: from `max(rmin, gmin, bmin)` to `min(rmax, gmax, bmax)`. If no gray level
  lies within all three ranges, no pixel can match and **no mask is written** (no exception).
  This matters for PNG files whose pixels are all gray: they are stored as `DeviceGray`, see [PNG Images](#png-images).

The image must be an 8-bit `DeviceRGB` or `DeviceGray` image without a mask. PNG images and raw 8-bit RGB or gray
images qualify; JPEG images qualify when they are 8-bit RGB or gray, but lossy compression changes colors slightly,
so use a range rather than a single color.

**Exceptions** (`HpdfException.ErrorCode`):

| Error code | Reason |
|------------|--------|
| `InvalidOperation` | The image already has a mask (including a PNG alpha channel), is a stencil mask, or is used as a soft mask |
| `InvalidBitPerComponent` | The image does not have 8 bits per component |
| `InvalidColorSpace` | The image is neither `DeviceRGB` nor `DeviceGray` (for example CMYK) |
| `InvalidParameter` | A minimum is greater than its maximum, or a maximum is greater than 255 |

Color key masking requires PDF 1.3; the document version is raised automatically when a mask is written.

**Example:**

```csharp
using var diagram = pdf.LoadPngImageFromFile("diagram-white-background.png");

// Make the white (and almost white) background transparent
diagram.SetColorMask(240, 255, 240, 255, 240, 255);

page.DrawImage(diagram, 50, 400, 300, 200);
```

---

### SetMaskImage

Sets a hard-edged (stencil) mask: each pixel of this image is either fully shown or fully hidden.

```csharp
public void SetMaskImage(HpdfImage maskImage)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| maskImage | HpdfImage | An 8-bit `DeviceGray` image of the same pixel size, or an image already used as a stencil mask |

**Return Value:**
`void`

**Description:**
Mask pixels with a value of **128 or more (light) hide** the image; pixels **below 128 (dark) show** it.

On first use the mask image is **converted in place** into a 1-bit stencil mask (`/ImageMask true`,
`BitsPerComponent` 1, no color space; `IsImageMask` becomes `true`). After that:

- The same mask can be passed to `SetMaskImage` of other images with the same pixel size; it is converted only once.
- It is no longer an ordinary image. Drawing it paints the current fill color where the mask was dark, and every
  place where it was drawn before the conversion changes in the same way. To show the mask picture itself as
  well, load the file a second time.
- It cannot be used as a soft mask afterwards.

Requirements for a mask image that is not a stencil mask yet:

- `DeviceGray` with 8 bits per component, for example a PNG whose pixels are all gray and fully opaque
  (a PNG with a colored pixel is stored as RGB, a PNG with transparency has a mask of its own),
  or a raw gray image.
- Not a JPEG image (the pixel data must be uncompressed).
- No mask of its own, and not already used as a soft mask.

**Exceptions** (`HpdfException.ErrorCode`):

| Error code | Reason |
|------------|--------|
| `InvalidParameter` | `maskImage` is `null` or this image itself |
| `InvalidOperation` | This image already has a mask (including a PNG alpha channel), is a stencil mask, or is used as a soft mask |
| `InvalidParameter` | The mask image's width or height differs from this image |
| `InvalidParameter` | The mask image is already used as a soft mask, is a JPEG image, or has a mask of its own |
| `InvalidColorSpace` | The mask image is not `DeviceGray` |
| `InvalidBitPerComponent` | The mask image does not have 8 bits per component |
| `InvalidImage` | The mask image holds fewer than `Width * Height` bytes |

When an exception is thrown, neither image is changed.

Stencil masking requires PDF 1.3; the document version is raised automatically.

**Example:**

```csharp
using var photo = pdf.LoadPngImageFromFile("photo.png");    // e.g. 64 x 64, RGB
using var second = pdf.LoadPngImageFromFile("photo2.png");  // another 64 x 64 image
using var stencil = pdf.LoadPngImageFromFile("stencil.png"); // 64 x 64, black and white

// Dark stencil pixels show the photo, light ones hide it
photo.SetMaskImage(stencil);

// The converted stencil can be reused for another image of the same size
second.SetMaskImage(stencil);

page.DrawImage(photo, 50, 600, 128, 128);
page.DrawImage(second, 250, 600, 128, 128);
```

---

### SetSoftMask

Sets a soft mask: a grayscale image whose pixels define the opacity of this image.

```csharp
public void SetSoftMask(HpdfImage maskImage)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| maskImage | HpdfImage | A `DeviceGray` image; 0 = fully transparent, 255 = fully opaque, values between are partially transparent |

**Return Value:**
`void`

**Description:**
The mask image is attached as `/SMask`. Its pixel size may differ from this image; it is stretched over the same
area. The mask image itself is **not modified**: it can still be drawn as an ordinary image and can be the soft
mask of several images. It can no longer be passed to `SetMaskImage`, and it cannot get a mask of its own
(`SetColorMask`, `SetMaskImage` and `SetSoftMask` on it throw `InvalidOperation`).

Soft masks require PDF 1.4; the document version is raised automatically.

**Exceptions** (`HpdfException.ErrorCode`):

| Error code | Reason |
|------------|--------|
| `InvalidParameter` | `maskImage` is `null` or this image itself |
| `InvalidOperation` | This image already has a mask (including a PNG alpha channel), is a stencil mask, or is used as a soft mask |
| `InvalidParameter` | The mask image is a stencil mask or has a mask of its own (for example a PNG with transparency) |
| `InvalidColorSpace` | The mask image is not `DeviceGray` (for example a PNG with colored pixels) |

**Example:**

```csharp
using Haru.Types;

using var photo = pdf.LoadJpegImageFromFile("photo.jpg");

// A 256 x 1 horizontal gradient: transparent on the left, opaque on the right
byte[] ramp = new byte[256];
for (int i = 0; i < ramp.Length; i++)
    ramp[i] = (byte)i;

using var fade = HpdfImage.LoadRawImageFromMem(pdf.Xref, "Raw1", ramp, 256, 1,
    HpdfColorSpace.DeviceGray, 8);

photo.SetSoftMask(fade);
page.DrawImage(photo, 50, 400, 400, 300);
```

---

## Complete Examples

### Gallery Page

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);

using var photo = pdf.LoadJpegImageFromFile("photo.jpg");
using var logo = pdf.LoadPngImageFromFile("logo-with-alpha.png");

// Title
page.SetFontAndSize(font, 20);
page.BeginText();
page.MoveTextPos(50, page.Height - 60);
page.ShowText("Image Gallery");
page.EndText();

// Photo scaled to 400 points wide, aspect ratio kept
float photoWidth = 400;
float photoHeight = photoWidth * photo.Height / photo.Width;
page.DrawImage(photo, 50, page.Height - 100 - photoHeight, photoWidth, photoHeight);

// Logo with transparent background in the lower-right corner
page.DrawImage(logo, page.Width - 150, 50, 100, 100 * (float)logo.Height / logo.Width);

pdf.SaveToFile("gallery.pdf");
```

### All Mask Types on One Page

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A checkerboard background makes the transparency visible
page.SetRgbFill(0.8f, 0.8f, 0.8f);
for (int row = 0; row < 20; row++)
    for (int col = 0; col < 12; col++)
        if ((row + col) % 2 == 0)
            page.Rectangle(50 + col * 40, 50 + row * 40, 40, 40);
page.Fill();

// 1. Color key: hide pure white
using var keyed = pdf.LoadPngImageFromFile("white-background.png");
keyed.SetColorMask(255, 255, 255, 255, 255, 255);
page.DrawImage(keyed, 60, 600, 200, 150);

// 2. Stencil: black-and-white mask of the same pixel size
using var masked = pdf.LoadPngImageFromFile("picture.png");
using var stencil = pdf.LoadPngImageFromFile("picture-stencil.png");
masked.SetMaskImage(stencil);
page.DrawImage(masked, 300, 600, 200, 150);

// 3. Soft mask: grayscale opacity, any pixel size
using var faded = pdf.LoadPngImageFromFile("picture.png");
using var opacity = pdf.LoadPngImageFromFile("vignette-gray.png");
faded.SetSoftMask(opacity);
page.DrawImage(faded, 60, 400, 200, 150);

// 4. PNG alpha channel: automatic
using var alpha = pdf.LoadPngImageFromFile("icon-with-alpha.png");
page.DrawImage(alpha, 300, 400, 200, 150);

// 5. Whole-image opacity
using var plain = pdf.LoadJpegImageFromFile("photo.jpg");
var gs = new HpdfExtGState(pdf.Xref, "GS_Alpha30");
gs.SetAlphaFill(0.3f);
page.GSave();
page.SetExtGState(gs);
page.DrawImage(plain, 60, 200, 200, 150);
page.GRestore();

pdf.SaveToFile("masks.pdf");   // version 1.4 or higher, raised automatically
```

---

## Best Practices

### 1. Load Once, Draw Many Times

```csharp
using var logo = pdf.LoadPngImageFromFile("logo.png");

for (int i = 0; i < 10; i++)
{
    var p = pdf.AddPage();
    p.DrawImage(logo, 50, p.Height - 80, 100, 50);   // stored in the file only once
}
```

Loading the same file twice stores its data twice.

### 2. Keep Images Alive Until the Document Is Saved

Use `using var` in the method that calls `SaveToFile` or `SaveToMemory`, as shown in
[Lifetime and Disposal](#lifetime-and-disposal).

### 3. Choose the Right Transparency Technique

- Transparent PNG: nothing to do; the alpha channel is applied automatically.
- Solid background color to drop: `SetColorMask`.
- Sharp cut-out shape: `SetMaskImage`.
- Gradual fade or vignette: `SetSoftMask`.
- Uniform translucency: `HpdfExtGState.SetAlphaFill`.

### 4. Keep the Aspect Ratio

`DrawImage` stretches the image to the box you give it. Compute one side from the other:

```csharp
float height = 300f * image.Height / image.Width;
page.DrawImage(image, 50, 400, 300, height);
```

### 5. Use JPEG for Photographs, PNG for Graphics

JPEG data is embedded unchanged and stays small; PNG data is decoded and recompressed losslessly, which suits
diagrams, screenshots and images with transparency.

---

## See Also

### Core Classes
- [HpdfDocument](HpdfDocument.md) - Document management and image loading
- [HpdfPage](HpdfPage.md) - Pages that images are drawn on
- [HpdfFont](HpdfFont.md) - Font handling

### Extension Methods
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - `DrawImage`, `Concat`, `SetExtGState` and other graphics operations
- [HpdfPageShapes](../extensions/HpdfPageShapes.md) - Shape drawing
- [HpdfPageText](../extensions/HpdfPageText.md) - Text operations

### Guides
- [Getting Started](../../guides/GettingStarted.md) - Step-by-step tutorial
- [Usage](../../USAGE.md) - Including the library in a project
- [Structure](../../STRUCTURE.md) - Library structure

---

[Back to Index](../../INDEX.md)
