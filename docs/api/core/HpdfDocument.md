# HpdfDocument Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Overview

`HpdfDocument` is the main entry point for creating PDF documents with Haru.NET. It manages the document catalog, page tree, cross-reference table, and all document-level structures. Every PDF creation workflow starts with creating an instance of this class.

### Purpose

- **Document lifecycle management** - Creation, configuration, and output
- **Page management** - Adding and organizing pages
- **Resource management** - Fonts, images, and forms
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

## Properties

### Version

Gets or sets the PDF version for this document.

```csharp
public HpdfVersion Version { get; set; }
```

**Values:**
- `HpdfVersion.Version12` - PDF 1.2 (default)
- `HpdfVersion.Version13` - PDF 1.3
- `HpdfVersion.Version14` - PDF 1.4 (required for transparency, CID fonts, 128-bit RC4 encryption, PDF/A-1)
- `HpdfVersion.Version15` - PDF 1.5 (required for AcroForms)
- `HpdfVersion.Version16` - PDF 1.6 (required for AES encryption)
- `HpdfVersion.Version17` - PDF 1.7

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

Gets a read-only list of all pages in the document.

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
- `HpdfCompressionMode.Image` - Reserved; image data is always compressed, whatever the mode
- `HpdfCompressionMode.Metadata` - Reserved; currently has no effect
- `HpdfCompressionMode.All` - All of the above

The mode is applied to a page when the page is added, so set it before calling `AddPage()`; pages added earlier
keep the mode they were created with.

**Example:**

```csharp
var pdf = new HpdfDocument();
pdf.CompressionMode = HpdfCompressionMode.All;
// All subsequently created content will be compressed
```

### PageCount

Gets the total number of pages in the document.

```csharp
public int PageCount { get; }
```

**Example:**

```csharp
for (int i = 0; i < 10; i++)
    pdf.AddPage();

Console.WriteLine($"Pages: {pdf.PageCount}");  // 10
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

### InsertPagesNode(HpdfPages?)

Inserts a new intermediate pages node for hierarchical page organization.

```csharp
public HpdfPages InsertPagesNode(HpdfPages? parent = null)
```

**Parameters:**
- `parent` - Parent pages node, or null to use current pages

**Returns:** The newly created `HpdfPages` node.

**Usage:** For large documents with hierarchical page organization.

---

## Resource Management Methods

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

---

## Metadata Methods

### Info Property

Access document metadata through the `Info` property:

| Member | Description |
|--------|-------------|
| `Title`, `Author`, `Subject`, `Keywords`, `Creator`, `Producer` | `string?` properties; `null` removes the entry |
| `Trapped` | `string?` property: `"True"`, `"False"` or `"Unknown"` (other values throw `HpdfException`) |
| `SetCreationDate(DateTime)`, `SetModificationDate(DateTime)` | Set the dates |
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
- `userPassword` - User password (opens with restricted permissions)
- `ownerPassword` - Owner password (opens with full permissions)
- `permission` - Permission flags for user password
- `mode` - Encryption mode (R2, R3, R4)

**Encryption Modes:**
- `HpdfEncryptMode.R2` - 40-bit RC4 (PDF 1.2+)
- `HpdfEncryptMode.R3` - 128-bit RC4 (PDF 1.4+)
- `HpdfEncryptMode.R4` - 128-bit AES (PDF 1.6+)

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
- `firstPage` - Value of numeric portion for first page (default: 1)
- `prefix` - Optional prefix string

**Page Number Styles:**
- `HpdfPageNumStyle.Decimal` - Arabic numerals (1, 2, 3...)
- `HpdfPageNumStyle.UpperRoman` - Uppercase Roman (I, II, III...)
- `HpdfPageNumStyle.LowerRoman` - Lowercase Roman (i, ii, iii...)
- `HpdfPageNumStyle.UpperLetters` - Uppercase letters (A, B, C...)
- `HpdfPageNumStyle.LowerLetters` - Lowercase letters (a, b, c...)

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
- Adds Output Intent (sRGB color profile)
- Adds Document ID
- Sets minimum PDF version (1.4 for PDF/A-1)

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
- Specific color profiles

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
    Version13,  // PDF 1.3
    Version14,  // PDF 1.4 (CID fonts)
    Version15,  // PDF 1.5 (AcroForms)
    Version16,  // PDF 1.6 (AES encryption)
    Version17   // PDF 1.7
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
- [Forms Guide](../../guides/FormsGuide.md) - Interactive forms
- [USAGE.md](../../USAGE.md) - Quick start guide
- [STRUCTURE.md](../../STRUCTURE.md) - Architecture overview

`HpdfDocumentExtensions` (namespace `Haru.Doc`) adds `GetFont(name, encoding)`, `SetCompressionMode(...)` and
`LoadPngImageFromFile(path)` to the document. `SetCompressionMode` and `LoadPngImageFromFile` are described above;
`GetFont` returns a Base14 font by its PostScript name (for example `"Helvetica-Bold"`) or a font registered earlier
with `RegisterFont(font, encoding)`. It does not load font files: see [HpdfFont](HpdfFont.md#loading-custom-fonts).

---

[← Back to Documentation Index](../../INDEX.md)

*Last updated: 2026-11-26*
