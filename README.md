# Haru.NET

[![NuGet](https://img.shields.io/nuget/v/Haru.svg)](https://www.nuget.org/packages/Haru)
[![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://nikolaygekht.github.io/haru-net-full-port/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**Create PDF files from .NET code - text, vector graphics, images, forms, bookmarks and encryption - with no native
dependencies.**

Haru.NET is a complete C# port of [libharu](https://github.com/libharu/libharu), the Haru Free PDF Library. It started
as a .NET wrapper around the native library ([Haru.Net](https://github.com/gehtsoft-usa/PDFLib)) and is now pure
managed code, extended beyond the original with interactive forms, Unicode text, modern encryption and stricter
validation. It runs anywhere .NET 8 or later runs: Windows, Linux and macOS.

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);
var font = pdf.GetFont("Helvetica", null);

page.SetFontAndSize(font, 24);
page.TextOut(50, 780, "Hello, PDF!");

page.SetFontAndSize(font, 11);
page.BeginText();
page.TextRect(50, 760, 545, 650,
    "Haru.NET writes PDF files from .NET code: text, vector graphics, images, " +
    "interactive forms, bookmarks and encryption, without native dependencies.",
    HpdfTextAlignment.Justify);
page.EndText();

page.SetRgbFill(0.2f, 0.4f, 0.8f);
page.Rectangle(50, 500, 200, 100);
page.Fill();

pdf.SaveToFile("hello.pdf");
```

## Features

- **Text and fonts** - the 14 standard PDF fonts, embedded TrueType and Type1 fonts, code pages for Central and
  Eastern European, Cyrillic, Greek, Turkish, Hebrew, Arabic, Baltic and Vietnamese text, Chinese, Japanese and Korean
  fonts, and Unicode (UTF-8) text through CID fonts. Text measuring, word wrapping and text in a rectangle with left,
  right, centered or justified alignment.
- **Graphics** - lines, curves, rectangles, circles, ellipses, arcs; stroke and fill with RGB, CMYK or gray; dash
  patterns, line caps and joins; transformations; clipping; transparency and blend modes.
- **Images** - PNG (including the alpha channel) and JPEG files, raw pixel data; color-key, stencil and soft masks.
- **Interactive forms** - text fields, checkboxes, radio buttons, combo boxes, list boxes and signature fields, with
  appearance streams that show the values everywhere, also when printing.
- **Navigation** - bookmarks, links to pages and web addresses, sticky notes, page labels, open action, page layout
  and viewer preferences.
- **Document features** - metadata in any language, compression, page sizes and boxes, page rotation, slide-show
  transitions, PDF/A-1b, and RC4 40/128-bit and AES-128 encryption with permissions.
- **Correct output** - the PDF version is raised automatically to what the content needs, and invalid input is
  rejected with an `HpdfException` instead of producing a broken file, following the checks of the C library. A document can be saved any number of times.

## Installation

The package is [Haru on NuGet](https://www.nuget.org/packages/Haru):

```
dotnet add package Haru
```

It targets .NET 8 (`net8.0`) and works in any later .NET version.

## More Examples

An interactive form field:

```csharp
var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var name = new HpdfTextField(pdf.Xref, "Name");          // namespace Haru.Forms
name.SetValue("Jane Doe");
page.AddWidgetAnnotation(name.CreateWidget(new HpdfRect(150, 700, 400, 720)));
form.AddField(name);

pdf.SaveToFile("form.pdf");
```

A password-protected document:

```csharp
var pdf = new HpdfDocument();
pdf.AddPage();
pdf.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R4);   // AES-128
pdf.SaveToFile("protected.pdf");
```

The [demo project](cs-src/Haru.Demos) builds a PDF for every feature area: fonts, CJK text, shapes, images,
transparency, forms, annotations, outlines, encryption, PDF/A and more.

## Documentation

The developer documentation is published at **https://nikolaygekht.github.io/haru-net-full-port/** (its sources are in the
[`docs`](docs) folder):

- [Documentation home](https://nikolaygekht.github.io/haru-net-full-port/) - overview and navigation
- [Quick start](https://nikolaygekht.github.io/haru-net-full-port/USAGE.html) and [Getting Started tutorial](https://nikolaygekht.github.io/haru-net-full-port/guides/GettingStarted.html)
- Guides: [Fonts](https://nikolaygekht.github.io/haru-net-full-port/guides/FontsGuide.html), [Text](https://nikolaygekht.github.io/haru-net-full-port/guides/TextGuide.html),
  [Graphics](https://nikolaygekht.github.io/haru-net-full-port/guides/GraphicsGuide.html), [Images](https://nikolaygekht.github.io/haru-net-full-port/guides/ImagesGuide.html),
  [Forms](https://nikolaygekht.github.io/haru-net-full-port/guides/FormsGuide.html), [Annotations](https://nikolaygekht.github.io/haru-net-full-port/guides/AnnotationsGuide.html),
  [Outlines](https://nikolaygekht.github.io/haru-net-full-port/guides/OutlinesGuide.html), [Encryption](https://nikolaygekht.github.io/haru-net-full-port/guides/EncryptionGuide.html)
- [Table of contents](https://nikolaygekht.github.io/haru-net-full-port/CONTENTS.html) and [alphabetical index](https://nikolaygekht.github.io/haru-net-full-port/API_INDEX.html) of every type and member
- [Architecture](https://nikolaygekht.github.io/haru-net-full-port/STRUCTURE.html)

## Building from Source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) (the library itself targets .NET 8; tests and
demos target .NET 10).

```
dotnet build cs-src/Haru.sln
dotnet test --project cs-src/Haru.Test/Haru.Test.csproj
dotnet run --project cs-src/Haru.Demos/BasicDemos.csproj     # writes the PDFs to pdfs/ next to the executable
```

The tests use xUnit v3 on Microsoft Testing Platform; see [BUILD.md](BUILD.md) for test filters and coverage.

| Folder | Contents |
|--------|----------|
| `cs-src/Haru` | The library |
| `cs-src/Haru.Test` | Unit and integration tests (generated PDFs are read back with PdfPig) |
| `cs-src/Haru.Demos` | Demo application and its fonts and images |
| `docs` | Developer documentation (GitHub Pages) |
| `c-src` | The original C sources, kept as a reference for the port |

## Status and Limitations

All core features of libharu are ported. Not available yet:

- markup annotations (highlight, free text, stamps, shapes, ...), 3D annotations, file attachments, JavaScript;
- CCITT and 1-bit image loading, ICC-based color spaces;
- PDF/A beyond basic PDF/A-1b checks.

The known issues are listed in [DOC_STATUS.md](DOC_STATUS.md).

## License

MIT License - see [LICENSE](LICENSE).
Copyright (c) 1999-2006 Takeshi Kanno, (c) 2007-2009 Antony Dovgal (original Haru library),
(c) 2016-2026 Nikolay Gekht (C# port).

The original libharu is distributed under the ZLIB/LIBPNG license; its notice is reproduced in [LICENSE](LICENSE)
as that license requires.

PNG and JPEG images are decoded with [StbImageSharp](https://github.com/StbSharp/StbImageSharp) (public domain / MIT).
