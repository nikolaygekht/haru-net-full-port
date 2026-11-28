# HpdfDocument Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Overview

`HpdfDocument` is the main entry point for creating PDF documents with Haru.NET. It manages the document catalog, page tree, cross-reference table, and all document-level structures. Every PDF creation workflow starts with creating an instance of this class.

### Purpose

- **Document lifecycle management** - Creation, configuration, and output
- **Page management** - Adding and organizing pages
- **Resource management** - Fonts, images, forms, file attachments and 3D models
- **Document metadata** - Title, author, keywords, etc.
- **Security** - Encryption and permissions
- **Standards compliance** - PDF/A support
- **Output** - Save to file or memory

### Basic Usage

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Hello, PDF!");
page.EndText();

pdf.SaveToFile("output.pdf");
```

---

## Constructor

### HpdfDocument()

Creates a new PDF document with default settings.

```csharp
public HpdfDocument()
```

**Default Configuration:**
- PDF version: 1.2
- Compression: Disabled
- No encryption
- No PDF/A compliance

**Example:**

```csharp
var pdf = new HpdfDocument();
Console.WriteLine($"PDF Version: {pdf.Version}");  // Version12
```

---

## Constants

### GeneratedNamePrefix

The prefix of the resource names that the document generates: fonts returned by `GetFont` are named `HaruF<n>`,
images loaded with the document's `Load...Image` methods `HaruIm<n>`.

```csharp
public const string GeneratedNamePrefix = "Haru";
```

A resource name identifies a font or an image on a page, and on one page a name refers to one object only: using a
second, different object under the same name throws `HpdfException` (`DuplicateRegistration`). Names you choose
yourself for fonts and images (for example `"F1"` for `new HpdfFont(...)` or `"Im1"` for
`HpdfImage.LoadPngImageFromMem(...)`) should not start with this prefix; then they never clash with the generated ones.

**Example:**

```csharp
var pdf = new HpdfDocument();
var font = pdf.GetFont("Helvetica");
Console.WriteLine(font.LocalName.StartsWith(HpdfDocument.GeneratedNamePrefix));  // True
```

---

## Properties

### Version

Gets or sets the PDF version for this document.

```csharp
public HpdfVersion Version { get; set; }
```

**Values:**
- `HpdfVersion.Version12` - PDF 1.2 (default)
- `HpdfVersion.Version13` - PDF 1.3 (required for page labels, color key and stencil image masks, markup annotations, embedded files)
- `HpdfVersion.Version14` - PDF 1.4 (required for transparency, CID fonts, 128-bit RC4 encryption, PDF/A-1, document attachments)
- `HpdfVersion.Version15` - PDF 1.5 (required for AcroForms; polygon, polyline and caret annotations)
- `HpdfVersion.Version16` - PDF 1.6 (required for AES encryption, U3D models and 3D annotations)
- `HpdfVersion.Version17` - PDF 1.7 (required for redaction annotations, embedded files with names outside ASCII, the
  3D lighting, cross sections, nodes and toolbar settings, and with Adobe ExtensionLevel 3 for PRC models, 3D
  measurements and projection annotations)

The entries of markup annotations need versions from 1.3 to 1.7; see
[HpdfMarkupAnnotation](../annotations/HpdfMarkupAnnotation.md#pdf-versions).

**Adobe extension level:** PRC models, 3D measurements and projection annotations are not part of ISO 32000-1 but of
Adobe's supplement to it, ExtensionLevel 3 (now in PDF 2.0). A document that uses them declares it in the catalog when
it is saved, `/Extensions << /Type /Extensions /ADBE << /Type /DeveloperExtensions /BaseVersion /1.7 /ExtensionLevel 3
>> >>`, and is written as PDF 1.7. `pdf.Xref.RequiredAdobeExtensionLevel` tells the level the content needs (0 for
none); like the version, it is never lowered.

**Note:** The library automatically upgrades the version when you use features that require it
(for example, loading a PNG with an alpha channel or calling `HpdfExtGState.SetAlphaFill` raises it to 1.4).
The value you set acts as a minimum: the getter returns the higher of the value set and the version
required by the content, so setting a lower version than the content needs has no effect.

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.Version = HpdfVersion.Version17;
```

### Catalog

Gets the document catalog (root object). The catalog holds the settings for how a viewer opens the document.

```csharp
public HpdfCatalog Catalog { get; }
```

| Member | Description |
|--------|-------------|
| `PageLayout` | Page arrangement: `SinglePage`, `OneColumn`, `TwoColumnLeft`, `TwoColumnRight`, `TwoPageLeft`, `TwoPageRight` (the two-page layouts need PDF 1.5) |
| `PageMode` / `SetPageMode(mode)` | Panel shown on opening: `UseNone`, `UseOutlines`, `UseThumbs`, `FullScreen`, `UseOC` (PDF 1.5), `UseAttachments` (PDF 1.6) |
| `ViewerPreferences` | `HpdfViewerPreferences` flags: `HideToolbar`, `HideMenubar`, `HideWindowUI`, `FitWindow`, `CenterWindow`, `PrintScalingNone` (PDF 1.6). `None` removes the preferences |
| `SetOpenAction(destination)` | Page and view shown when the document is opened; `null` removes it. The destination must have a view (`SetXYZ`, `SetFit`, ...), otherwise `HpdfException` with `InvalidDestination` is thrown |
| `GetOpenAction()` | The destination array of the open action, or `null` |

Values that need a newer PDF version raise the document version automatically (see [Version](#version)).

**Usage:**

```csharp
pdf.Catalog.PageLayout = HpdfPageLayout.TwoPageLeft;
pdf.Catalog.SetPageMode(HpdfPageMode.UseOutlines);
pdf.Catalog.ViewerPreferences = HpdfViewerPreferences.FitWindow | HpdfViewerPreferences.CenterWindow;

// Open the document on the second page, zoomed to fit the window
var destination = pdf.Pages[1].CreateDestination();
destination.SetFit();
pdf.Catalog.SetOpenAction(destination);
```

### RootPages

Gets the root pages object.

```csharp
public HpdfPages RootPages { get; }
```

### CurrentPages

Gets or sets the current pages object for adding new pages.

```csharp
public HpdfPages CurrentPages { get; set; }
```

### CurrentPage

Gets the most recently added page.

```csharp
public HpdfPage? CurrentPage { get; }
```

**Example:**

```csharp
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();

Console.WriteLine(pdf.CurrentPage == page2);  // True
```

### Pages

Gets a read-only list of all pages in the document, in page tree order: the order in which viewers show them and in
which the page indexes of [AddPageLabel](#addpagelabel) count. Pages added under an intermediate node
(`InsertPagesNode`, `HpdfPages.AddKid`) appear where the tree puts them, which can differ from the order in which they
were created; see [HpdfPages](HpdfPages.md#page-order).

```csharp
public IReadOnlyList<HpdfPage> Pages { get; }
```

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.AddPage();
pdf.AddPage();
pdf.AddPage();

Console.WriteLine($"Total pages: {pdf.Pages.Count}");  // 3

foreach (var page in pdf.Pages)
{
    // Process each page
}
```

### Info

Gets the document information dictionary for metadata.

```csharp
public HpdfInfo Info { get; }
```

**Example:**

```csharp
pdf.Info.Title = "My Document";
pdf.Info.Author = "John Doe";
pdf.Info.Subject = "Important Report";
pdf.Info.Keywords = "report, data, analysis";
pdf.Info.Creator = "My Application v1.0";
```

See [Metadata Methods](#metadata-methods) for all metadata members.

### Xref

Gets the cross-reference table (for advanced use).

```csharp
public HpdfXref Xref { get; }
```

**Usage:** Required when creating fonts and other PDF objects.

```csharp
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
```

### CompressionMode

Gets or sets the compression mode for the document.

```csharp
public HpdfCompressionMode CompressionMode { get; set; }
```

**Values:**
- `HpdfCompressionMode.None` - No compression of page content (default)
- `HpdfCompressionMode.Text` - Compress page content streams (FlateDecode)
- `HpdfCompressionMode.Image` - Compatibility value (C library `HPDF_COMP_IMAGE`) with no effect: image data is always compressed, whatever the mode
- `HpdfCompressionMode.Metadata` - Compatibility value (C library `HPDF_COMP_METADATA`) with no effect
- `HpdfCompressionMode.All` - All of the above

The mode applies to every page of the document, including pages added before it was set and pages added with
`HpdfPages.AddKid`, so it can be set at any time before saving.

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;
// All page content streams will be compressed, also those of pages added earlier
```

### PageCount

Gets the total number of pages in the document: the pages of the page tree, including pages added with
`HpdfPages.AddKid`. It is always equal to `Pages.Count` and `RootPages.Count`.

```csharp
public int PageCount { get; }
```

**Example:**

```csharp
for (int i = 0; i < 10; i++)
    pdf.AddPage();

Console.WriteLine($"Pages: {pdf.PageCount}");  // 10
```

### AttachedFiles

Gets the files attached to the document with [AttachFile()](#attachfilestring-byte-string), in the order they were
attached.

```csharp
public IReadOnlyList<HpdfEmbeddedFile> AttachedFiles { get; }
```

The files of file attachment annotations created with a file name and data are not in this list.

**Example:**

```csharp
foreach (var attached in pdf.AttachedFiles)
    Console.WriteLine($"{attached.Name} ({attached.Size} bytes)");
```

### NamedDestinations

Gets the named destinations created with [CreateNamedDestination()](#createnameddestinationstring-hpdfdestination),
in the order they were created.

```csharp
public IReadOnlyList<HpdfNamedDestination> NamedDestinations { get; }
```

**Example:**

```csharp
foreach (var named in pdf.NamedDestinations)
    Console.WriteLine($"{named.Name}: {(named.Destination is null ? "no target yet" : "set")}");
```

### IsEncrypted

Gets whether encryption is enabled for this document.

```csharp
public bool IsEncrypted { get; }
```

### IsPdfACompliant

Gets whether PDF/A compliance is enabled.

```csharp
public bool IsPdfACompliant { get; }
```

---

## Page Management Methods

### AddPage()

Adds a new page with default size (A4 portrait).

```csharp
public HpdfPage AddPage()
```

**Returns:** The newly created `HpdfPage`.

**Default size:** 595.276 x 841.89 points (A4)

**Example:**

```csharp
var page = pdf.AddPage();
Console.WriteLine($"Page size: {page.Width} x {page.Height}");
```

### AddPage(HpdfPageSize, HpdfPageDirection)

Adds a new page with a specific size and orientation.

```csharp
public HpdfPage AddPage(HpdfPageSize size, HpdfPageDirection direction)
```

**Parameters:**
- `size` - Standard page size (A4, Letter, Legal, etc.)
- `direction` - Portrait or Landscape

**Example:**

```csharp
// A4 portrait
var page1 = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);

// Letter landscape
var page2 = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Landscape);

// A3 portrait
var page3 = pdf.AddPage(HpdfPageSize.A3, HpdfPageDirection.Portrait);
```

**Available sizes:**
- `HpdfPageSize.Letter` - 612 x 792 points (8.5" x 11")
- `HpdfPageSize.Legal` - 612 x 1008 points (8.5" x 14")
- `HpdfPageSize.A3` - 842 x 1191 points
- `HpdfPageSize.A4` - 595 x 842 points
- `HpdfPageSize.A5` - 420 x 595 points
- `HpdfPageSize.B4` - 709 x 1001 points
- `HpdfPageSize.B5` - 499 x 709 points
- And more (`Executive`, `US4x6`, `US4x8`, `US5x7`, `Comm10`)

### AddPage(float, float)

Adds a new page with custom dimensions.

```csharp
public HpdfPage AddPage(float width, float height)
```

**Parameters:**
- `width` - Page width in points (1 point = 1/72 inch)
- `height` - Page height in points

**Example:**

```csharp
// Square page
var page1 = pdf.AddPage(600, 600);

// Custom business card size (3.5" x 2")
var page2 = pdf.AddPage(252, 144);

// Wide banner
var page3 = pdf.AddPage(1200, 400);
```

### InsertPage(HpdfPage)

Inserts a new page with the default size in front of a page of the document (`HPDF_InsertPage` of the C library).

```csharp
public HpdfPage InsertPage(HpdfPage target)
```

**Parameters:**
- `target` - A page of this document; the new page comes right before it, in the same pages node

**Returns:** The new page. It follows the document's compression mode; the current page (`CurrentPage`) does not
change.

**Throws:** `HpdfException` (`InvalidPage`) for `null`, a page of another document or a page that is not in the
page tree.

**Example:**

```csharp
var body = pdf.AddPage();
// ... later: a title page in front of it
var title = pdf.InsertPage(body);
```

### InsertPagesNode(HpdfPages?)

Inserts a new intermediate pages node for hierarchical page organization.

```csharp
public HpdfPages InsertPagesNode(HpdfPages? parent = null)
```

**Parameters:**
- `parent` - Parent pages node, or null to use current pages

**Returns:** The newly created `HpdfPages` node.

**Usage:** For large documents with hierarchical page organization.

### SetPagesConfiguration(int)

Groups the pages added from now on into pages nodes of at most `pagesPerNode` pages each, below the root of the page
tree (`HPDF_SetPagesConfiguration` of the C library). Without it all pages are kids of the root node; a document with
thousands of pages then has one huge `/Kids` array that a viewer reads whole to find a page.

```csharp
public void SetPagesConfiguration(int pagesPerNode)
```

**Parameters:**
- `pagesPerNode` - The largest number of pages in a node; `0` puts all pages into one node below the root

**Returns:** `void`

As in the C library the tree has two levels: the root holds the nodes, a node holds up to `pagesPerNode` pages, and
a new node is started when the current one is full. The order of the pages is the order they were added.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidDocumentState`) | A page has already been added |
| `HpdfException` (`InvalidParameter`) | `pagesPerNode` is negative or above `HpdfConst.LimitMaxArray` |

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.SetPagesConfiguration(100);      // nodes of 100 pages

for (int i = 0; i < 2500; i++)
    pdf.AddPage();                   // 25 nodes below the root
```

---

## Resource Management Methods

The image loaders below give each image a resource name that starts with [GeneratedNamePrefix](#generatednameprefix)
(`HaruIm` followed by a number), so images loaded this way never clash with each other or with names you choose.

### LoadPngImageFromFile(string)

Loads a PNG image from a file.

```csharp
public HpdfImage LoadPngImageFromFile(string filePath)
```

**Parameters:**
- `filePath` - Path to the PNG file

**Returns:** `HpdfImage` object ready to draw

**Example:**

```csharp
var image = pdf.LoadPngImageFromFile("logo.png");

var page = pdf.AddPage();
page.DrawImage(image, 100, 600, 200, 100);
```

**Supported PNG features:**
- Grayscale, RGB, indexed color
- Transparency (alpha channel)
- Interlaced images

### LoadPngImage(Stream)

Loads a PNG image from a stream.

```csharp
public HpdfImage LoadPngImage(Stream stream)
```

The image starts at the current position of the stream. The stream does not need to be seekable: a forward-only
stream is read to its end and buffered in memory first.

**Example:**

```csharp
using var fileStream = File.OpenRead("logo.png");
var image = pdf.LoadPngImage(fileStream);
```

### LoadJpegImageFromFile(string)

Loads a JPEG image from a file.

```csharp
public HpdfImage LoadJpegImageFromFile(string filePath)
```

**Parameters:**
- `filePath` - Path to the JPEG file

**Returns:** `HpdfImage` object ready to draw

**Example:**

```csharp
var photo = pdf.LoadJpegImageFromFile("photo.jpg");

var page = pdf.AddPage();
page.DrawImage(photo, 50, 400, 500, 300);
```

### LoadJpegImage(Stream)

Loads a JPEG image from a stream.

```csharp
public HpdfImage LoadJpegImage(Stream stream)
```

PNG and JPEG images that embed an ICC profile (a PNG `iCCP` chunk, JPEG `APP2 ICC_PROFILE` segments) get it as their
color space; see [HpdfImage.IccProfile](HpdfImage.md#iccprofile).

---

## Form XObject Methods

A [form XObject](HpdfFormXObject.md) is content drawn once and shown on any number of pages and forms, at any size:
a letterhead, a watermark, a logo. Draw into it like on a page ([HpdfCanvas](HpdfCanvas.md)) and show it with
`DrawFormXObject` ([HpdfPageExtensions](../extensions/HpdfPageExtensions.md)).

### CreateFormXObject(float, float)

```csharp
public HpdfFormXObject CreateFormXObject(float width, float height)
```

Creates an empty form with the box (0, 0)-(width, height); what is drawn outside the box is cut off. Its graphics
state starts at the defaults: set the font and colors inside it. Throws `HpdfException` (`InvalidParameter`) for a
size that is not positive.

### CreateFormXObjectFromImage(HpdfImage, HpdfRect, bool)

```csharp
public HpdfFormXObject CreateFormXObjectFromImage(HpdfImage image, HpdfRect rect, bool zoom)
```

A form of the rectangle's size that shows an image (`HPDF_Page_CreateXObjectFromImage`): `zoom: true` scales the
image to the form, `zoom: false` draws it at its pixel size, one point per pixel. Unlike the C library, the box starts
at (0, 0) whatever the rectangle's corner is.

### CreateWhiteRectFormXObject(HpdfRect)

```csharp
public HpdfFormXObject CreateWhiteRectFormXObject(HpdfRect rect)
```

A form of the rectangle's size filled with white (`HPDF_Page_CreateXObjectAsWhiteRect`), which covers what it is
drawn over.

### FormXObjects

```csharp
public IReadOnlyList<HpdfFormXObject> FormXObjects { get; }
```

The form XObjects of the document, in the order they were created. Saving checks that each has balanced
`GSave`/`GRestore` calls and no open text object.

**Example:**

```csharp
var stamp = pdf.CreateFormXObject(120, 40);
stamp.SetRgbStroke(0.8f, 0, 0);
stamp.SetLineWidth(3);
stamp.Rectangle(2, 2, 116, 36);
stamp.Stroke();

foreach (var page in pdf.Pages)
    page.DrawFormXObject(stamp, page.Width - 160, page.Height - 80);
```

---

## Color Profile Methods

ICC color profiles make colors device independent: an [HpdfIccProfile](HpdfIccProfile.md) is the color space of
images (`HpdfImage.SetIccProfile`) and of fill and stroke colors (`page.SetIccFill`, `page.SetIccStroke`), and the
profile of the PDF/A output intent. The document embeds each distinct profile once.

### LoadIccProfile(string)

Loads an ICC profile file (`HPDF_LoadIccProfileFromFile` of the C library).

```csharp
public HpdfIccProfile LoadIccProfile(string path)
```

**Parameters:**
- `path` - The path of the profile (`.icc`, `.icm`)

**Returns:** The [HpdfIccProfile](HpdfIccProfile.md); the same object for a profile with the same content loaded before.
The component count comes from the profile header.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `path` is `null` or empty, or the file is not an ICC profile |
| `HpdfException` (`FileNotFound`) | The file does not exist |
| `HpdfException` (`InvalidIccComponentNum`) | The profile is not of a gray, RGB or CMYK color space |

### LoadIccProfile(byte[])

Loads an ICC profile from memory (`HPDF_ICC_LoadIccFromMem`); the data is copied. Throws as above, `InvalidParameter`
for `null` data.

```csharp
public HpdfIccProfile LoadIccProfile(byte[] data)
```

### SRgbProfile

Gets the built-in sRGB profile (ICC v2, 456 bytes, CC0): the profile of the PDF/A output intent unless another one is
given. It is embedded when it is first asked for.

```csharp
public HpdfIccProfile SRgbProfile { get; }
```

### IccProfiles

Gets the profiles embedded in the document, in the order they were loaded (also the ones from image files).

```csharp
public IReadOnlyList<HpdfIccProfile> IccProfiles { get; }
```

**Example:**

```csharp
var adobeRgb = pdf.LoadIccProfile("AdobeRGB1998.icc");
page.SetIccFill(adobeRgb, 0.1f, 0.5f, 0.9f);

foreach (var profile in pdf.IccProfiles)
    Console.WriteLine($"{profile.LocalName}: {profile.ComponentCount} components, ICC {profile.ProfileVersion}");
```

---

## Attachment Methods

A **document attachment** is a file embedded in the PDF that viewers list in their attachments panel, from where the
reader opens or saves it (set `pdf.Catalog.SetPageMode(HpdfPageMode.UseAttachments)` to open that panel with the
document). The attached files are written as the `EmbeddedFiles` name tree of the catalog's name dictionary when the
document is saved, with the names sorted as a name tree requires. If a name dictionary was set with
`Catalog.SetNames()`, the tree is added to it; otherwise the document creates one. To show a file at a place of a page,
create a [file attachment annotation](../annotations/HpdfFileAttachmentAnnotation.md) for it as well.

PDF/A-1 allows no embedded files: saving a PDF/A document with attached files throws `HpdfException`
(`InvalidOperation`).

### AttachFile(string)

Attaches a file on disk (`HPDF_AttachFile` of the C library).

```csharp
public HpdfEmbeddedFile AttachFile(string path)
```

**Parameters:**
- `path` - The path of the file. It is embedded under its file name without the directory

**Returns:** The [HpdfEmbeddedFile](HpdfEmbeddedFile.md), without a MIME type. To give one, read the file yourself and
call the other overload.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `path` is `null` or empty |
| `HpdfException` (`FileNotFound`) | The file does not exist |
| `HpdfException` (`InvalidParameter`) | A file with the same name is already attached |

**Example:**

```csharp
var report = pdf.AttachFile("revenue.csv");
report.SetDescription("The revenue table");
```

### AttachFile(string, byte[], string?)

Attaches data in memory as a file.

```csharp
public HpdfEmbeddedFile AttachFile(string name, byte[] data, string? mimeType = null)
```

**Parameters:**
- `name` - The file name, without a directory, shown by viewers and used when the file is saved; unique among the
  attached files (the comparison is case-sensitive)
- `data` - The contents of the file (copied)
- `mimeType` - The MIME type, such as `text/plain` or `application/pdf`, or `null`

**Returns:** The [HpdfEmbeddedFile](HpdfEmbeddedFile.md). The file is compressed and carries its size and MD5
checksum. Raises the document version to PDF 1.4 (1.7 when the name has characters outside printable ASCII).

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `name` is `null` or empty, or contains `/` or `\` |
| `HpdfException` (`InvalidParameter`) | `data` is `null` |
| `HpdfException` (`InvalidParameter`) | `mimeType` is not of the form `type/subtype` in ASCII |
| `HpdfException` (`InvalidParameter`) | A file with the same name is already attached |

The arguments are checked first: after an exception nothing has been added to the document.

**Example:**

```csharp
var data = System.Text.Encoding.UTF8.GetBytes("quarter,revenue\nQ1,100\nQ2,120\n");
var table = pdf.AttachFile("revenue-2026.csv", data, "text/csv");
table.SetModificationDate(DateTimeOffset.Now);
pdf.Catalog.SetPageMode(HpdfPageMode.UseAttachments);
```

---

## Named Destination and JavaScript Methods

The catalog's name dictionary holds, besides the attached files, the **named destinations** (the `Dests` name tree)
and the **document-level JavaScript** (the `JavaScript` name tree). Both trees are written when the document is saved,
with the names sorted as a name tree requires, into the name dictionary set with `Catalog.SetNames()` or into one the
document creates.

### CreateNamedDestination(string, HpdfDestination?)

Creates a [named destination](HpdfNamedDestination.md): a name that stands for a destination. Links
(`HpdfPage.CreateLinkAnnotation(rect, namedDestination)`) and outline items (`HpdfOutline.SetDestination(namedDestination)`)
that go to it write only the name, so they can be created before the target page exists. A URL opens the document
at a name with `file.pdf#nameddest=name`.

```csharp
public HpdfNamedDestination CreateNamedDestination(string name, HpdfDestination? destination = null)
```

**Parameters:**
- `name` - The name, unique among the named destinations of the document (case-sensitive). Any text is allowed; use
  ASCII letters, digits and `-` / `_` for names that go into URLs
- `destination` - The target, with its view set (`SetXYZ`, `SetFit`, ...), or `null` to set it later with
  [HpdfNamedDestination.SetDestination()](HpdfNamedDestination.md#method-setdestination)

**Returns:** The [HpdfNamedDestination](HpdfNamedDestination.md). The name dictionary is a PDF 1.2 feature: the
document version is not raised.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `name` is `null` or empty, or a named destination with that name exists |
| `HpdfException` (`InvalidDestination`) | `destination` has no view, or is on a page of another document |

Saving throws `HpdfException` (`InvalidDestination`) while a named destination has no target.

**Example:**

```csharp
var pdf = new HpdfDocument();
var contents = pdf.AddPage();

// The link is created before the page it goes to
var appendix = pdf.CreateNamedDestination("appendix");
contents.CreateLinkAnnotation(new HpdfRect(50, 700, 200, 720), appendix);

var page = pdf.AddPage();
var target = page.CreateDestination();
target.SetFit();
appendix.SetDestination(target);
```

### AddJavaScript(string, string)

Adds a document-level JavaScript program (the `JavaScript` name tree). A viewer that runs JavaScript, such as Adobe
Acrobat and Adobe Reader, runs the document scripts when it opens the document: they define functions and variables
for the scripts of links ([HpdfPage.CreateJavaScriptLinkAnnotation()](HpdfPage.md#createjavascriptlinkannotationhpdfrect-string))
or do something at once. Support differs between viewers: the PDF viewers of web browsers run a part of Acrobat's
JavaScript API, many other viewers run none.

```csharp
public void AddJavaScript(string name, string script)
```

**Parameters:**
- `name` - The name of the script, unique among the document's scripts (case-sensitive)
- `script` - The JavaScript code. It is written as a compressed text stream (PDFDocEncoding for ASCII, otherwise
  UTF-16BE)

**Returns:** `void`. Raises the document version to PDF 1.3.

The scripts run in the order of their names (sorted by their bytes), not in the order they were added: name them
`1-helpers`, `2-start` when the order matters. `HPDF_CreateJavaScript` and `HPDF_LoadJSFromFile` of the C library
are not needed: pass the code as a string (`File.ReadAllText(path)` for a file).

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `name` is `null` or empty, or a script with that name exists |
| `HpdfException` (`InvalidParameter`) | `script` is `null` |

PDF/A-1 allows no JavaScript: saving a PDF/A document with document scripts throws `HpdfException`
(`InvalidOperation`).

**Example:**

```csharp
pdf.AddJavaScript("helpers", "function greet(name) { app.alert('Hello, ' + name); }");
page.CreateJavaScriptLinkAnnotation(new HpdfRect(50, 650, 200, 670), "greet('reader');");
```

### SetOpenJavaScript(string?)

Sets a JavaScript program that a viewer runs when the document is opened: the catalog's `/OpenAction` as a JavaScript
action. It runs after the document scripts of [AddJavaScript()](#addjavascriptstring-string), so it can call their
functions. Not in the C library.

```csharp
public void SetOpenJavaScript(string? script)
```

**Parameters:**
- `script` - The JavaScript code, or `null` to remove the script

**Returns:** `void`. Raises the document version to PDF 1.3.

The open action is a single entry: the script replaces a destination set with
[Catalog.SetOpenAction()](HpdfCatalog.md#method-setopenaction), and setting a destination replaces the script.
`SetOpenJavaScript(null)` removes only a script; a destination open action stays. A later call replaces the script.
As for document scripts, only viewers that run JavaScript run it, and PDF/A-1 does not allow it: saving a PDF/A
document whose open action is a script throws `HpdfException` (`InvalidOperation`).

**Example:**

```csharp
pdf.AddJavaScript("helpers", "function greet(name) { app.alert('Hello, ' + name); }");
pdf.SetOpenJavaScript("greet('reader');");
```

---

## 3D Model Methods

A 3D model is shown on a page by a [3D annotation](../annotations/Hpdf3DAnnotation.md); see the
[3D Guide](../../guides/ThreeDGuide.md). The library does not read the model: it embeds the data and tells U3D and PRC
apart by their first bytes.

### Load3DModel(string)

Loads a U3D or PRC model from a file (`HPDF_LoadU3DFromFile` of the C library).

```csharp
public Hpdf3DModel Load3DModel(string path)
```

**Parameters:**
- `path` - The path of a `.u3d` or `.prc` file

**Returns:** The [Hpdf3DModel](../threed/Hpdf3DModel.md), embedded in the document.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `path` is `null` or empty |
| `HpdfException` (`FileNotFound`) | The file does not exist |
| `HpdfException` (`InvalidU3dData`) | The data is neither U3D nor PRC |

**Example:**

```csharp
var model = pdf.Load3DModel("part.prc");
page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");
```

### Load3DModel(byte[])

Loads a U3D or PRC model from memory (`HPDF_LoadU3DFromMem` of the C library).

```csharp
public Hpdf3DModel Load3DModel(byte[] data)
```

**Parameters:**
- `data` - The model data (copied): U3D starts with `U3D` and a zero byte, PRC with `PRC`

**Returns:** The [Hpdf3DModel](../threed/Hpdf3DModel.md). A U3D model raises the document version to PDF 1.6; a PRC
model declares Adobe ExtensionLevel 3 and raises it to PDF 1.7.

**Throws:** `HpdfException` (`InvalidU3dData`) if `data` is `null`, shorter than 4 bytes or starts with neither
signature.

**Example:**

```csharp
byte[] data = File.ReadAllBytes("part.prc");
var model = pdf.Load3DModel(data);
Console.WriteLine(model.Format);   // PRC
```

---

## Metadata Methods

### Info Property

Access document metadata through the `Info` property:

| Member | Description |
|--------|-------------|
| `Title`, `Author`, `Subject`, `Keywords`, `Creator`, `Producer` | `string?` properties; `null` removes the entry |
| `Trapped` | `string?` property: `"True"`, `"False"` or `"Unknown"` (other values throw `HpdfException`) |
| `SetCreationDate(DateTime)`, `SetModificationDate(DateTime)` | Set the dates; a UTC value (`DateTimeKind.Utc`) is written as UTC, any other with the offset of the local time zone |
| `SetCreationDate(DateTimeOffset)`, `SetModificationDate(DateTimeOffset)` | Set the dates with the offset of the value |
| `GetCreationDate()`, `GetModificationDate()` | The dates as PDF date strings, or `null` |
| `SetCustomMetadata(key, value)`, `GetCustomMetadata(key)` | Any other entry of the information dictionary |

Text in any language can be used: text of plain ASCII characters is stored as it is, any other text as UTF-16
(the PDF text string encoding), so viewers show it correctly.

**Example:**

```csharp
var pdf = new HpdfDocument();

pdf.Info.Title = "Annual Report 2025";
pdf.Info.Author = "Finance Department";
pdf.Info.Subject = "Company Financial Results";
pdf.Info.Keywords = "finance, annual, report, 2025";
pdf.Info.Creator = "Corporate Reporting System";
pdf.Info.SetCreationDate(DateTime.Now);
```

---

## Security Methods

### SetEncryption

Sets encryption on the document with user and owner passwords.

```csharp
public void SetEncryption(
    string userPassword,
    string ownerPassword,
    HpdfPermission permission = HpdfPermission.Print,
    HpdfEncryptMode mode = HpdfEncryptMode.R3)
```

**Parameters:**
- `userPassword` - User password (opens with restricted permissions); an empty string or `null` lets anyone open the document
- `ownerPassword` - Owner password (opens with full permissions); required, and different from the user password
- `permission` - Permission flags for user password
- `mode` - Encryption mode (R2, R3, R4)

**Encryption Modes:**
- `HpdfEncryptMode.R2` - 40-bit RC4 (PDF 1.1+; the document version is not raised)
- `HpdfEncryptMode.R3` - 128-bit RC4 (raises the version to PDF 1.4)
- `HpdfEncryptMode.R4` - 128-bit AES (raises the version to PDF 1.6)

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `mode` is not `R2`, `R3` or `R4` |
| `HpdfException` (`EncryptInvalidPassword`) | `ownerPassword` is empty or `null` |
| `HpdfException` (`EncryptInvalidPassword`) | The two passwords are equal. They are compared as the encryption uses them: as Latin-1 bytes, of which only the first 32 count |
| `HpdfException` (`EncryptInvalidPassword`) | Either password contains a character above U+00FF (outside Latin-1) |

The passwords are checked before anything changes: after an exception, the document is as it was before the call.
See the [Encryption Guide](../../guides/EncryptionGuide.md#password-rules) for the password rules.

**Permission Flags:**
- `HpdfPermission.None` - No permissions
- `HpdfPermission.Print` - Allow printing
- `HpdfPermission.Edit` - Allow modifying the contents
- `HpdfPermission.Copy` - Allow copying text/graphics
- `HpdfPermission.EditAnnotations` - Allow adding or modifying annotations and form fields
- `HpdfPermission.FillForm` - Allow form filling (R3+)
- `HpdfPermission.ExtractAccess` - Allow content extraction for accessibility (R3+)
- `HpdfPermission.Assemble` - Allow document assembly (R3+)
- `HpdfPermission.PrintHighQuality` - Allow high-quality printing (R3+)
- `HpdfPermission.All` - All of the above

**Example:**

```csharp
// Basic encryption - allow printing only
pdf.SetEncryption(
    userPassword: "user123",
    ownerPassword: "owner456",
    permission: HpdfPermission.Print,
    mode: HpdfEncryptMode.R3
);

// Multiple permissions using bitwise OR
var permissions = HpdfPermission.Print |
                  HpdfPermission.Copy |
                  HpdfPermission.EditAnnotations;

pdf.SetEncryption("user", "owner", permissions, HpdfEncryptMode.R4);

// No user password - anyone can open with restrictions
pdf.SetEncryption("", "owner", HpdfPermission.Print);
```

---

## Outline (Bookmarks) Methods

### GetOutlineRoot()

Gets the root outline (bookmarks) for the document. Creates it if it doesn't exist.

```csharp
public HpdfOutline GetOutlineRoot()
```

**Returns:** Root `HpdfOutline` object.

### CreateOutline(string)

Creates a top-level outline entry.

```csharp
public HpdfOutline CreateOutline(string title)
```

**Parameters:**
- `title` - Outline entry title

**Returns:** New `HpdfOutline` object.

**Example:**

```csharp
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();
var page3 = pdf.AddPage();

// A destination is a page and a view; SetFit shows the whole page
HpdfDestination FitPage(HpdfPage target)
{
    var destination = target.CreateDestination();
    destination.SetFit();
    return destination;
}

// Create top-level bookmarks
var chapter1 = pdf.CreateOutline("Chapter 1");
chapter1.SetDestination(FitPage(page1));

var chapter2 = pdf.CreateOutline("Chapter 2");
chapter2.SetDestination(FitPage(page2));

// Create nested bookmarks
var section21 = chapter2.CreateChild("Section 2.1");
section21.SetDestination(FitPage(page2));

var section22 = chapter2.CreateChild("Section 2.2");
section22.SetDestination(FitPage(page3));

pdf.SaveToFile("bookmarks.pdf");
```

`SetDestination` can be called again; the new destination replaces the previous one. An item closed with
`SetOpened(false)` hides its children: they are not counted as visible items of its parents (the `/Count` entries
are computed when the document is saved).

---

## Forms Methods

### GetOrCreateAcroForm()

Gets the AcroForm for this document, creating it if it doesn't exist.

```csharp
public HpdfAcroForm GetOrCreateAcroForm()
```

**Returns:** `HpdfAcroForm` instance.

**Usage:** For adding interactive form fields.

**Example:**

```csharp
var acroForm = pdf.GetOrCreateAcroForm();
var page = pdf.AddPage();

// Create a text field: the field holds the value, its widget places it on the page
var textField = new HpdfTextField(pdf.Xref, "name");
textField.SetValue("Jane Doe");
page.AddWidgetAnnotation(textField.CreateWidget(new HpdfRect(100, 700, 300, 720)));
acroForm.AddField(textField);

// Create a checkbox
var checkbox = new HpdfCheckbox(pdf.Xref, "agree");
page.AddWidgetAnnotation(checkbox.CreateWidget(new HpdfRect(100, 650, 115, 665)));
acroForm.AddField(checkbox);

pdf.SaveToFile("form.pdf");
```

Forms require PDF 1.5; the version is raised when the document is saved. See the
[Forms Guide](../../guides/FormsGuide.md) for all field types.

### GetAcroForm()

Gets the AcroForm if it exists.

```csharp
public HpdfAcroForm? GetAcroForm()
```

**Returns:** `HpdfAcroForm` instance or null if not created.

---

## Page Labeling Methods

### AddPageLabel

Adds a page label to define custom page numbering for a page range.

```csharp
public void AddPageLabel(
    int pageNum,
    HpdfPageNumStyle style,
    int firstPage = 1,
    string? prefix = null)
```

**Parameters:**
- `pageNum` - Starting page number (0-based index)
- `style` - Numbering style
- `firstPage` - Value of numeric portion for first page (default: 1); must be at least 1
- `prefix` - Optional prefix string

**Page Number Styles:**
- `HpdfPageNumStyle.Decimal` - Arabic numerals (1, 2, 3...)
- `HpdfPageNumStyle.UpperRoman` - Uppercase Roman (I, II, III...)
- `HpdfPageNumStyle.LowerRoman` - Lowercase Roman (i, ii, iii...)
- `HpdfPageNumStyle.UpperLetters` - Uppercase letters (A, B, C...)
- `HpdfPageNumStyle.LowerLetters` - Lowercase letters (a, b, c...)

Page labels were introduced in PDF 1.3; the document version is raised to 1.3 automatically. If no label is
added for page index 0, the document adds a decimal label (1, 2, 3...) for it when it is saved, because the PDF format
requires the labels to start at the first page.

**Throws:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `pageNum` is negative or `firstPage` is less than 1 |
| `HpdfException` (`PageNumStyleOutOfRange`) | `style` is not one of the five styles below (for example `HpdfPageNumStyle.Eof`) |

**Example:**

```csharp
var pdf = new HpdfDocument();

// Add 3 pages for front matter
pdf.AddPage();  // Page 0
pdf.AddPage();  // Page 1
pdf.AddPage();  // Page 2

// Add 10 pages for main content
for (int i = 0; i < 10; i++)
    pdf.AddPage();

// Front matter: lowercase Roman numerals (i, ii, iii)
pdf.AddPageLabel(0, HpdfPageNumStyle.LowerRoman, 1);

// Main content: Arabic numerals starting at 1 (1, 2, 3...)
pdf.AddPageLabel(3, HpdfPageNumStyle.Decimal, 1);

pdf.SaveToFile("labeled.pdf");
```

**With prefix:**

```csharp
// Chapter pages with prefix "C-1", "C-2", etc.
pdf.AddPageLabel(0, HpdfPageNumStyle.Decimal, 1, "C-");

// Appendix pages with prefix "A-1", "A-2", etc.
pdf.AddPageLabel(20, HpdfPageNumStyle.Decimal, 1, "A-");
```

---

## PDF/A Compliance Methods

### SetPdfACompliance(string)

Enables PDF/A compliance for archival-quality PDFs.

```csharp
public void SetPdfACompliance(string conformance = "1B")
```

**Parameters:**
- `conformance` - Conformance level ("1B", "2B", "3B")

**Effects:**
- Adds XMP metadata
- Adds the output intent with its ICC profile (`/DestOutputProfile`): the built-in sRGB profile, registered as
  `sRGB IEC61966-2.1`, or the profile of the overload below
- Saving fails for an image whose ICC profile of version 4 was set with `SetIccProfile`, and for fill and stroke colors
  (`SetIccFill`, `SetIccStroke`) in a version 4 profile (PDF/A-1 allows ICC version 2);
  a version 4 profile carried over from an image file is left out, and the image is written in its device color space
- Adds Document ID
- Sets minimum PDF version (1.4 for PDF/A-1)
- Sets the Print flag (`/F 4`) on every annotation of every page when the document is saved and clears the
  Invisible, Hidden and NoView flags, as PDF/A-1 requires
- Saving throws `HpdfException` (`InvalidOperation`) if a markup annotation has an opacity below 1
  (`SetTransparency`); highlight annotations are drawn as an outline, without the Multiply blend mode
- Saving throws `HpdfException` (`InvalidOperation`) for annotation types that PDF/A-1 does not allow (polygon,
  polyline, caret, redaction, file attachment, 3D and projection annotations; ISO 19005-1 allows the types of PDF 1.4
  except file attachments, sounds and movies) and for document attachments (`AttachFile`), since PDF/A-1 allows no
  embedded files
- Saving throws `HpdfException` (`InvalidOperation`) for JavaScript: document scripts (`AddJavaScript`), the open
  script (`SetOpenJavaScript`) and JavaScript links (`HpdfPage.CreateJavaScriptLinkAnnotation`)
- Saving throws `HpdfException` (`InvalidOperation`) for an interpolated image (`HpdfImage.Interpolate`)

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.SetPdfACompliance("1B");  // PDF/A-1B

var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var page = pdf.AddPage();

// All fonts must be embedded for PDF/A
// Standard fonts (Base14) are acceptable

pdf.SaveToFile("pdfa.pdf");
```

**Note:** PDF/A has additional requirements:
- All fonts must be embedded
- No encryption
- XMP metadata required
- Device colors must match the output intent (an sRGB intent for RGB colors, a CMYK intent for CMYK colors); the
  library does not check this

### SetPdfACompliance(string, HpdfIccProfile, string)

Enables PDF/A compliance with an output intent of your own (`HPDF_PDFA_AppendOutputIntents` of the C library): the
profile of the device the colors are meant for, such as a CMYK printing condition, instead of the built-in sRGB.

```csharp
public void SetPdfACompliance(string conformance, HpdfIccProfile outputProfile, string outputCondition)
```

**Parameters:**
- `conformance` - Conformance level, such as `"1B"`
- `outputProfile` - A profile of this document of the output device (`prtr`) or monitor (`mntr`) class; ICC version 2
  for PDF/A-1
- `outputCondition` - The identifier and description of the output condition, written as `/OutputConditionIdentifier`,
  `/OutputCondition` and `/Info` (for example `"Coated FOGRA39"`)

**Throws:** `HpdfException` (`InvalidParameter`) for a `null` profile, an empty condition, a profile of another class,
a profile that is not of version 2 with a PDF/A-1 conformance, or a profile of another document. Nothing is changed
then.

**Example:**

```csharp
var pdf = new HpdfDocument();
var press = pdf.LoadIccProfile("CoatedFOGRA39.icc");
pdf.SetPdfACompliance("1B", press, "Coated FOGRA39");
```

---

## Compression Methods

### SetCompressionMode(HpdfCompressionMode)

Extension method from `HpdfDocumentExtensions`, kept for compatibility with the C API. It has the same effect as
setting the [CompressionMode](#compressionmode) property; an overload takes the flags as `uint`.

```csharp
pdf.SetCompressionMode(HpdfCompressionMode.All);

// Same as:
pdf.CompressionMode = HpdfCompressionMode.All;
```

---

## Save Methods

A document can be saved any number of times, in any of the ways below. Each save writes the document as it is at
that moment; saving an unchanged document again produces the same file. Form field appearances, outline counts,
PDF/A metadata and encryption data are prepared during every save.

**Throws:** `HpdfException` (`PageCannotRestoreGState`) if a page has `GSave` calls without a matching `GRestore`:
its content stream would be invalid. The exception is thrown before any PDF data is written (`SaveToFile` has
already created the file, which stays empty).

### Save(Stream)

Saves the document to a stream.

```csharp
public void Save(Stream stream)
```

**Parameters:**
- `stream` - Output stream to write to

**Example:**

```csharp
using var memStream = new MemoryStream();
pdf.Save(memStream);

byte[] pdfData = memStream.ToArray();
// Use pdfData (e.g., send over network)
```

### SaveToFile(string)

Saves the document to a file.

```csharp
public void SaveToFile(string filename)
```

**Parameters:**
- `filename` - File path to save to

**Example:**

```csharp
pdf.SaveToFile("output.pdf");
pdf.SaveToFile(@"C:\Documents\report.pdf");
pdf.SaveToFile("pdfs/invoice-2025.pdf");
```

### SaveToMemory()

Saves the document to a byte array.

```csharp
public byte[] SaveToMemory()
```

**Returns:** PDF document as byte array.

**Example:**

```csharp
byte[] pdfBytes = pdf.SaveToMemory();

// Write to file
await File.WriteAllBytesAsync("output.pdf", pdfBytes);

// Send as email attachment
var attachment = new System.Net.Mail.Attachment(new MemoryStream(pdfBytes), "report.pdf");

// Return from an ASP.NET Core controller action:
// return File(pdfBytes, "application/pdf", "document.pdf");
```

---

## Complete Examples

### Basic Document

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

pdf.Info.Title = "Hello World PDF";
pdf.Info.Author = "John Doe";

var page = pdf.AddPage();
page.SetFontAndSize(font, 24);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Hello, World!");
page.EndText();

pdf.SaveToFile("hello.pdf");
```

### Multi-Page Document with Metadata

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// Set metadata
pdf.Info.Title = "Multi-Page Report";
pdf.Info.Author = "Report Generator";
pdf.Info.Subject = "Monthly Analysis";
pdf.Info.Keywords = "report, analysis, data";

// Add multiple pages
for (int i = 1; i <= 5; i++)
{
    var page = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Portrait);

    page.SetFontAndSize(font, 20);
    page.BeginText();
    page.MoveTextPos(50, page.Height - 50);
    page.ShowText($"Page {i}");
    page.EndText();
}

pdf.SaveToFile("report.pdf");
```

### Encrypted Document

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var page = pdf.AddPage();
page.SetFontAndSize(font, 16);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Confidential Document");
page.EndText();

// Set encryption - user can only print
pdf.SetEncryption(
    userPassword: "user123",
    ownerPassword: "admin456",
    permission: HpdfPermission.Print,
    mode: HpdfEncryptMode.R4  // AES 128-bit
);

pdf.SaveToFile("secure.pdf");
```

### Document with Images

```csharp
var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;

var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
var logo = pdf.LoadPngImageFromFile("logo.png");
var photo = pdf.LoadJpegImageFromFile("photo.jpg");

var page = pdf.AddPage();

// Draw logo
page.DrawImage(logo, 50, 750, 100, 50);

// Title
page.SetFontAndSize(font, 20);
page.BeginText();
page.MoveTextPos(50, 700);
page.ShowText("Image Gallery");
page.EndText();

// Draw photo
page.DrawImage(photo, 50, 300, 400, 300);

pdf.SaveToFile("images.pdf");
```

### Document with Bookmarks

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");

// Create pages
var titlePage = pdf.AddPage();
var chapter1Page = pdf.AddPage();
var chapter2Page = pdf.AddPage();
var chapter3Page = pdf.AddPage();

// Add content to pages
titlePage.SetFontAndSize(font, 24);
titlePage.BeginText();
titlePage.MoveTextPos(50, 700);
titlePage.ShowText("Table of Contents");
titlePage.EndText();

// A bookmark opens its page at a view; here the page top at 100% zoom
HpdfDestination TopOf(HpdfPage target)
{
    var destination = target.CreateDestination();
    destination.SetXYZ(0, target.Height, 1);
    return destination;
}

// Create bookmarks
var toc = pdf.CreateOutline("Table of Contents");
toc.SetDestination(TopOf(titlePage));

var ch1 = pdf.CreateOutline("Chapter 1: Introduction");
ch1.SetDestination(TopOf(chapter1Page));

var ch2 = pdf.CreateOutline("Chapter 2: Methods");
ch2.SetDestination(TopOf(chapter2Page));

var section21 = ch2.CreateChild("Section 2.1");
section21.SetDestination(TopOf(chapter2Page));

var section22 = ch2.CreateChild("Section 2.2");
section22.SetDestination(TopOf(chapter2Page));

var ch3 = pdf.CreateOutline("Chapter 3: Conclusions");
ch3.SetDestination(TopOf(chapter3Page));

pdf.SaveToFile("bookmarks.pdf");
```

---

## Best Practices

### 1. Set Compression for Smaller Files

```csharp
var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;
```

### 2. Always Set Metadata

```csharp
pdf.Info.Title = "Document Title";
pdf.Info.Author = "Your Name";
pdf.Info.SetCreationDate(DateTime.Now);
```

### 3. Use Appropriate Encryption

```csharp
// For sensitive documents
pdf.SetEncryption("user", "owner", HpdfPermission.Print, HpdfEncryptMode.R4);
```

### 4. Handle Errors

```csharp
try
{
    var pdf = new HpdfDocument();
    // ... create content ...
    pdf.SaveToFile("output.pdf");
}
catch (HpdfException ex)
{
    Console.Error.WriteLine($"PDF Error: {ex.Message}");
}
catch (IOException ex)
{
    Console.Error.WriteLine($"File Error: {ex.Message}");
}
```

### 5. Dispose Resources Properly

```csharp
// HpdfDocument doesn't implement IDisposable currently,
// but ensure streams are properly disposed
using var imageStream = File.OpenRead("image.png");
var image = pdf.LoadPngImage(imageStream);
```

---

## Related Types

### HpdfVersion Enum

```csharp
public enum HpdfVersion
{
    Version12,  // PDF 1.2 (default)
    Version13,  // PDF 1.3 (page labels, image masks, JavaScript)
    Version14,  // PDF 1.4 (CID fonts)
    Version15,  // PDF 1.5 (AcroForms, polygon / polyline / caret annotations)
    Version16,  // PDF 1.6 (AES encryption, U3D, 3D annotations)
    Version17   // PDF 1.7 (redaction annotations; PRC and 3D measurements with Adobe ExtensionLevel 3)
}
```

### HpdfCompressionMode Enum

```csharp
[Flags]
public enum HpdfCompressionMode : uint
{
    None = 0,
    Text = 1,
    Image = 2,
    Metadata = 4,
    All = 15
}
```

---

## See Also

- [HpdfPage](HpdfPage.md) - Page manipulation
- [HpdfFont](HpdfFont.md) - Font management
- [HpdfImage](HpdfImage.md) - Image handling
- [HpdfEmbeddedFile](HpdfEmbeddedFile.md) - Attached files
- [HpdfNamedDestination](HpdfNamedDestination.md) - Named destinations
- [HpdfIccProfile](HpdfIccProfile.md) - ICC color profiles
- [HpdfFormXObject](HpdfFormXObject.md) - Form XObjects, [HpdfCanvas](HpdfCanvas.md) - what pages and forms share
- [Hpdf3DModel](../threed/Hpdf3DModel.md) - 3D models ([3D Guide](../../guides/ThreeDGuide.md))
- [Forms Guide](../../guides/FormsGuide.md) - Interactive forms
- [USAGE.md](../../USAGE.md) - Quick start guide
- [STRUCTURE.md](../../STRUCTURE.md) - Architecture overview

[HpdfDocumentExtensions](../extensions/HpdfDocumentExtensions.md) (namespace `Haru.Doc`) adds `GetFont(name, encoding)`
and `SetCompressionMode(...)` to the document. `SetCompressionMode` is described above. `GetFont` returns a Base14 font
by its PostScript name (for example `"Helvetica-Bold"`) or a font registered earlier with `RegisterFont(font, encoding)`.
It does not load font files: see [HpdfFont](HpdfFont.md#loading-custom-fonts).

---

[← Back to Documentation Index](../../INDEX.md)

*Last updated: 2026-11-27*
