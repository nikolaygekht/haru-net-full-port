# HpdfPage Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

**Base class:** [HpdfCanvas](HpdfCanvas.md)

---

## Overview

`HpdfPage` represents a single page in a PDF document. It serves as the target for all graphics and text operations. All drawing,  text rendering, and annotations are added to specific pages.

### Purpose

- **Page dimensions** - Width, height, and various PDF boxes (MediaBox, CropBox, TrimBox, BleedBox, ArtBox)
- **Content stream management** - Stores all graphics and text commands
- **Graphics state tracking** - Current colors, line widths, fonts, transformations
- **Resource management** - Fonts, images, and extended graphics states used on the page. They are registered under their resource name (`LocalName`); a name refers to one object per page, so using a second, different object with a name already in use on the page throws `HpdfException` (`DuplicateRegistration`). Using the same object again is fine. Fonts from `GetFont` and images loaded through the document get generated names that start with `HpdfDocument.GeneratedNamePrefix` (`"Haru"`); do not start your own names with it.
- **Annotations** - Links, notes, review comments (markup annotations), file attachments, 3D models, and form fields

### Basic Usage

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// Add a page
var page = pdf.AddPage();

// Page is ready for content
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Content on the page");
page.EndText();

pdf.SaveToFile("output.pdf");
```

---

## Properties

### Width

Gets or sets the page width in points.

```csharp
public float Width { get; set; }
```

**Units:** Points (72 points = 1 inch)

**Example:**

```csharp
var page = pdf.AddPage();
Console.WriteLine($"Default width: {page.Width}");  // 595.276 (A4)

page.Width = 612;  // Letter width
Console.WriteLine($"New width: {page.Width}");      // 612
```

### Height

Gets or sets the page height in points.

```csharp
public float Height { get; set; }
```

**Units:** Points (72 points = 1 inch)

**Example:**

```csharp
var page = pdf.AddPage();
Console.WriteLine($"Default height: {page.Height}");  // 841.89 (A4)

page.Height = 792;  // Letter height
Console.WriteLine($"New height: {page.Height}");      // 792
```

### Zoom

Gets or sets the preferred zoom factor for showing the page (`/PZ`; `HPDF_Page_SetZoom` of the C library). A viewer
may use it when it shows the page; 1 is 100 %, and `null` (the default) leaves the zoom to the viewer.

```csharp
public float? Zoom { get; set; }
```

A value outside 0.08 to 32 throws `HpdfException` (`InvalidParameter`).

```csharp
page.Zoom = 1.25f;   // 125 %
```

### Rotate

Gets or sets the clockwise rotation, in degrees, with which viewers display and print the page (`/Rotate`).

```csharp
public int Rotate { get; set; }
```

**Values:** A multiple of 90. The value is stored normalized to 0, 90, 180 or 270 (for example, `-90` becomes `270`
and `450` becomes `90`); 0 removes the entry. Any other value throws `HpdfException` with
`HpdfErrorCode.PageInvalidRotateValue`.

Rotation changes only how the page is shown: the page content and its coordinate system stay as they are, so you
draw on a rotated page exactly as on an unrotated one.

**Example:**

```csharp
var page = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);
page.Rotate = 90;               // shown as landscape, turned clockwise
Console.WriteLine(page.Rotate); // 90

page.Rotate = -90;
Console.WriteLine(page.Rotate); // 270
```

### Dict

Gets the underlying PDF dictionary object for this page (advanced use).

```csharp
public HpdfDict Dict { get; }
```

**Usage:** For low-level PDF manipulation.

### Parent

Gets the parent pages object.

```csharp
public HpdfPages? Parent { get; }
```

### Annotations

Gets the annotations created on this page or added to it (notes, links, markup annotations, pop-ups, form
widgets), in the order of the page's `/Annots` array.

```csharp
public IReadOnlyList<HpdfAnnotation> Annotations { get; }
```

**Example:**

```csharp
foreach (var annotation in page.Annotations)
    Console.WriteLine($"{annotation.Type}: {annotation.Flags}");
```

### Contents

Gets the content stream the page draws into now (inherited from [HpdfCanvas](HpdfCanvas.md)).

```csharp
public HpdfStreamObject Contents { get; }
```

**Usage:** The content stream contains the graphics and text operators. A page has one stream until
[NewContentStream()](#newcontentstream) starts another; `Contents` is then the current one. The drawing properties
`Contents`, `Stream`, `GraphicsState`, `CurrentPos`, `PathInProgress`, `TextMatrix`, `TextLineMatrix`, `CurrentFont`
and `InTextObject` belong to [HpdfCanvas](HpdfCanvas.md), which pages share with form XObjects.

### Stream

Gets the underlying memory stream for writing PDF operators.

```csharp
public HpdfMemoryStream Stream { get; }
```

**Usage:** For low-level content stream manipulation (advanced).

### GraphicsState

Gets the current graphics state.

```csharp
public HpdfGraphicsState GraphicsState { get; }
```

**Contains:**
- Current colors (stroke and fill)
- Line width, cap, join
- Current font and font size
- Text properties (character/word spacing, etc.)
- Transformation matrix

**Example:**

```csharp
var page = pdf.AddPage();

page.SetRgbFill(1, 0, 0);  // Red
Console.WriteLine($"Fill color: {page.GraphicsState.RgbFill}");

page.SetLineWidth(3);
Console.WriteLine($"Line width: {page.GraphicsState.LineWidth}");
```

### CurrentPos

Gets or sets the current position in the path.

```csharp
public HpdfPoint CurrentPos { get; set; }
```

**Usage:** Tracks the current point for path operations (MoveTo, LineTo, Rectangle, etc.).

### PathInProgress

Gets or sets whether a path is being built.

```csharp
public bool PathInProgress { get; set; }
```

**Usage:** Set by the path construction methods (`MoveTo`, `LineTo`, `CurveTo`, `Rectangle`, shapes) and cleared by the path painting methods (`Stroke`, `Fill`, `EndPath`, ...). `Arc` continues a path in progress with a line instead of starting a new subpath (see [HpdfPageShapes](../extensions/HpdfPageShapes.md#arc)). Part of the `IDrawable` interface; normally not set directly.

### TextMatrix

Gets the current text matrix (internal).

```csharp
public HpdfTransMatrix TextMatrix { get; }
```

### TextLineMatrix

Gets the current text line matrix (internal).

```csharp
public HpdfTransMatrix TextLineMatrix { get; }
```

### CurrentFont

Gets the current font set for this page.

```csharp
public HpdfFont? CurrentFont { get; }
```

**Example:**

```csharp
var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

page.SetFontAndSize(font, 16);
Console.WriteLine($"Current font: {page.CurrentFont.BaseFont}");  // "Helvetica"
```

---

## Constants

### DefaultWidth

Default page width in points (A4 width).

```csharp
public const float DefaultWidth = 595.276f;
```

### DefaultHeight

Default page height in points (A4 height).

```csharp
public const float DefaultHeight = 841.89f;
```

---

## Page Sizing Methods

### SetSize(HpdfPageSize, HpdfPageDirection)

Sets the page size using a predefined size and orientation.

```csharp
public void SetSize(HpdfPageSize size, HpdfPageDirection direction)
```

**Parameters:**
- `size` - Standard page size
- `direction` - Portrait or Landscape

**Throws:** `HpdfException` (`PageInvalidSize`) for a `size` value that is not defined in `HpdfPageSize`, and
`HpdfException` (`PageInvalidDirection`) for a `direction` other than `Portrait` or `Landscape`.

**Example:**

```csharp
var page = pdf.AddPage();

// A4 portrait (default)
page.SetSize(HpdfPageSize.A4, HpdfPageDirection.Portrait);

// Letter landscape
page.SetSize(HpdfPageSize.Letter, HpdfPageDirection.Landscape);

// A3 portrait
page.SetSize(HpdfPageSize.A3, HpdfPageDirection.Portrait);
```

**Available Sizes:**
- Letter (612 × 792 pt)
- Legal (612 × 1008 pt)
- A3 (842 × 1191 pt)
- A4 (595 × 842 pt)
- A5 (420 × 595 pt)
- B4 (709 × 1001 pt)
- B5 (499 × 709 pt)
- Executive, US4x6, US4x8, US5x7, Comm10

### SetSize(float, float)

Sets the page size using custom width and height.

```csharp
public void SetSize(float width, float height)
```

**Parameters:**
- `width` - Page width in points
- `height` - Page height in points

**Example:**

```csharp
// Square page
page.SetSize(600, 600);

// Business card (3.5" × 2" = 252 × 144 points)
page.SetSize(252, 144);

// Wide banner
page.SetSize(1200, 400);
```

### SetMediaBox(float, float, float, float)

Sets the MediaBox for this page (advanced).

```csharp
public void SetMediaBox(float llx, float lly, float urx, float ury)
```

**Parameters:**
- `llx` - Lower-left X coordinate
- `lly` - Lower-left Y coordinate
- `urx` - Upper-right X coordinate
- `ury` - Upper-right Y coordinate

**Usage:** The MediaBox defines the boundaries of the physical medium.

**Example:**

```csharp
// Standard usage: origin at (0,0)
page.SetMediaBox(0, 0, 612, 792);  // Letter size

// Advanced: offset MediaBox
page.SetMediaBox(10, 10, 605, 832);
```

---

## PDF Page Boxes

PDF supports multiple "boxes" that define different regions of a page. These are primarily used in professional printing workflows.

### SetCropBox

Sets the CropBox, which defines the region to which page contents are clipped when displayed or printed.

```csharp
public void SetCropBox(float left, float bottom, float right, float top)
public void SetCropBox(HpdfBox box)
```

**Example:**

```csharp
// Crop 1/2 inch from each edge
page.SetCropBox(36, 36, page.Width - 36, page.Height - 36);

// Using HpdfBox
var cropBox = new HpdfBox(36, 36, page.Width - 36, page.Height - 36);
page.SetCropBox(cropBox);
```

### GetCropBox()

Gets the CropBox if set.

```csharp
public HpdfBox? GetCropBox()
```

**Returns:** `HpdfBox` or null if not set.

### SetBleedBox

Sets the BleedBox, which defines the region for production environments (printing with bleed).

```csharp
public void SetBleedBox(float left, float bottom, float right, float top)
public void SetBleedBox(HpdfBox box)
```

**Usage:** The bleed box extends beyond the TrimBox to account for printing inaccuracies.

**Example:**

```csharp
// Extend 0.125 inch (9 points) beyond trim for bleed
float bleed = 9;
page.SetBleedBox(-bleed, -bleed, page.Width + bleed, page.Height + bleed);
```

### GetBleedBox()

Gets the BleedBox if set.

```csharp
public HpdfBox? GetBleedBox()
```

### SetTrimBox

Sets the TrimBox, which defines the intended dimensions after trimming.

```csharp
public void SetTrimBox(float left, float bottom, float right, float top)
public void SetTrimBox(HpdfBox box)
```

**Example:**

```csharp
// Final trimmed size
page.SetTrimBox(9, 9, page.Width - 9, page.Height - 9);
```

### GetTrimBox()

Gets the TrimBox if set.

```csharp
public HpdfBox? GetTrimBox()
```

### SetArtBox

Sets the ArtBox, which defines the extent of meaningful content.

```csharp
public void SetArtBox(float left, float bottom, float right, float top)
public void SetArtBox(HpdfBox box)
```

**Example:**

```csharp
// Define content area (with margins)
page.SetArtBox(50, 50, page.Width - 50, page.Height - 50);
```

### GetArtBox()

Gets the ArtBox if set.

```csharp
public HpdfBox? GetArtBox()
```

---

## Content Stream Methods

A page writes its content into one stream at first. `NewContentStream` continues in a new stream, and a finished,
self-contained stream can be shown on other pages: see [HpdfContentStream](HpdfContentStream.md). To repeat content
at other positions or sizes, a [form XObject](HpdfFormXObject.md) is often simpler.

### CurrentContentStream

```csharp
public HpdfContentStream CurrentContentStream { get; }
```

The stream the page draws into now.

### ContentStreams

```csharp
public IReadOnlyList<HpdfContentStream> ContentStreams { get; }
```

The streams of the page in the order of `/Contents`: its own and the shared streams inserted.

### NewContentStream()

Ends the current content stream and continues in a new one (`HPDF_Page_New_Content_Stream` of the C library); the
page's `/Contents` becomes an array of streams.

```csharp
public HpdfContentStream NewContentStream()
```

**Returns:** The new [HpdfContentStream](HpdfContentStream.md), which drawing goes into from now on. The stream before
it is finished and can be shared if it is self-contained.

### InsertSharedContentStream(HpdfContentStream)

Shows a finished content stream of this or another page here (`HPDF_Page_Insert_Shared_Content_Stream`).

```csharp
public void InsertSharedContentStream(HpdfContentStream stream)
```

The stream is added to `/Contents` inside `q` ... `Q`, the resources it uses are added to this page (in the C library
they are not, and the shared text is lost), and the page continues in a new stream. The shared content inherits the
state of the page where it is inserted, except the font, which it must set itself.

**Throws:** `HpdfException` with `InvalidParameter` for `null` or a stream of another document, `InvalidOperation` for
a stream that is not finished or not self-contained (the message says why), or when a text object is open or a path
is being built on this page.

**Example:**

```csharp
var footer = first.NewContentStream();
first.GSave();
first.SetFontAndSize(font, 9);
first.TextOut(50, 30, "Example Inc.");
first.GRestore();
first.NewContentStream();

second.InsertSharedContentStream(footer);
```

---

## Annotation Methods

### CreateTextAnnotation

Creates a text annotation (sticky note) on the page.

```csharp
public HpdfTextAnnotation CreateTextAnnotation(
    HpdfRect rect,
    string text,
    HpdfAnnotationIcon icon = HpdfAnnotationIcon.Note)
```

**Parameters:**
- `rect` - Annotation rectangle (left, bottom, right, top); reversed edges are swapped
- `text` - Text content
- `icon` - Icon to display (Note, Comment, Help, etc.)

**Returns:** `HpdfTextAnnotation` object.

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or empty, and with `AnnotInvalidIcon` if
`icon` is not an `HpdfAnnotationIcon` value. The arguments are checked first: after an exception, nothing has been
added to the page or the document.

**Example:**

```csharp
var page = pdf.AddPage();

// Add a note at top-right
var note = page.CreateTextAnnotation(
    new HpdfRect(500, 750, 520, 770),
    "Remember to review this section!",
    HpdfAnnotationIcon.Comment
);
```

**Available icons:**
- `Note` (default)
- `Comment`
- `Help`
- `Key`
- `NewParagraph`
- `Paragraph`
- `Insert`

### CreateLinkAnnotation(HpdfRect, string)

Creates a URI link annotation (external link).

```csharp
public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, string uri)
```

**Parameters:**
- `rect` - Clickable area (left, bottom, right, top); reversed edges are swapped
- `uri` - URL to link to

**Returns:** `HpdfLinkAnnotation` object.

**Throws:** `HpdfException` with `InvalidParameter` if `uri` is `null` or empty; nothing is added to the page or the
document.

**Example:**

```csharp
var page = pdf.AddPage();

// Create clickable link
var link = page.CreateLinkAnnotation(
    new HpdfRect(100, 700, 300, 720),
    "https://www.example.com"
);

// Add text to show what's clickable
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 702);
page.ShowText("Visit our website");
page.EndText();
```

### CreateLinkAnnotation(HpdfRect, HpdfDestination)

Creates an internal link annotation (GoTo destination) to a [destination](HpdfDestination.md).

```csharp
public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfDestination destination)
```

**Parameters:**
- `rect` - Clickable area
- `destination` - Target page and view; its view must be set (`SetXYZ`, `SetFit`, ...). The link stores the
  destination's `DestArray`

**Throws:** `HpdfException` with `InvalidParameter` if `destination` is `null`, and with `InvalidDestination` if the
destination has no view. Nothing is added to the page or the document.

**Example:**

```csharp
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();

// Create destination for page 2, showing the whole page
var dest = page2.CreateDestination();
dest.SetFit();

// Add link on page 1 to page 2
var link = page1.CreateLinkAnnotation(new HpdfRect(100, 700, 250, 720), dest);
```

### CreateLinkAnnotation(HpdfRect, HpdfArray)

Creates an internal link annotation (GoTo destination) from a destination array.

```csharp
public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfArray destination)
```

**Parameters:**
- `rect` - Clickable area
- `destination` - PDF destination array (`[page /View ...]`), for example `HpdfDestination.DestArray`

**Throws:** `HpdfException` with `InvalidParameter` if `destination` is `null`, and with `InvalidDestination` if the
array has no view (fewer than two elements, as `[page]`). Nothing is added to the page or the document.

**Example:**

```csharp
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();

// Create destination for page 2, showing the whole page
var dest = page2.CreateDestination();
dest.SetFit();

// Add link on page 1 to page 2
var link = page1.CreateLinkAnnotation(
    new HpdfRect(100, 700, 250, 720),
    dest.DestArray
);
```

### CreateLinkAnnotation(HpdfRect, HpdfNamedDestination)

Creates an internal link annotation to a [named destination](HpdfNamedDestination.md): the link writes the name
(`/Dest (name)`), the document's `Dests` name tree maps it to the target.

```csharp
public HpdfLinkAnnotation CreateLinkAnnotation(HpdfRect rect, HpdfNamedDestination destination)
```

**Parameters:**
- `rect` - Clickable area
- `destination` - A named destination of this document, from `HpdfDocument.CreateNamedDestination()`. Its target
  may still be unset; it must be set before the document is saved

**Throws:** `HpdfException` with `InvalidParameter` if `destination` is `null`, and with `InvalidDestination` if it is
a named destination of another document. Nothing is added to the page or the document.

**Example:**

```csharp
var chapter2 = pdf.CreateNamedDestination("chapter2");
contents.CreateLinkAnnotation(new HpdfRect(70, 700, 220, 716), chapter2);

var page = pdf.AddPage();
var target = page.CreateDestination();
target.SetFit();
chapter2.SetDestination(target);
```

### CreateJavaScriptLinkAnnotation(HpdfRect, string)

Creates a link annotation that runs a JavaScript program when it is clicked (a JavaScript action; the C library
sets one with `HPDF_LinkAnnot_SetJavaScript`). The program can call what the document scripts
([HpdfDocument.AddJavaScript()](HpdfDocument.md#addjavascriptstring-string)) define. Only viewers that run
JavaScript, such as Adobe Acrobat and Adobe Reader, do something when the link is clicked.

```csharp
public HpdfLinkAnnotation CreateJavaScriptLinkAnnotation(HpdfRect rect, string script)
```

**Parameters:**
- `rect` - Clickable area
- `script` - The JavaScript code, written as a compressed text stream

**Returns:** The [HpdfLinkAnnotation](../annotations/HpdfLinkAnnotation.md). Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `script` is `null`; nothing is added to the page or the
document. PDF/A-1 allows no JavaScript: saving a PDF/A document with such a link throws `HpdfException`
(`InvalidOperation`).

**Example:**

```csharp
page.CreateJavaScriptLinkAnnotation(new HpdfRect(50, 600, 200, 620),
    "app.alert('This document has ' + this.numPages + ' pages.');");
page.CreateJavaScriptLinkAnnotation(new HpdfRect(50, 570, 200, 590), "this.gotoNamedDest('chapter2');");
```

### CreateSquareAnnotation

Creates a square annotation: a rectangle drawn inside `rect`, a review comment that marks an area of the page
([HpdfShapeAnnotation](../annotations/HpdfShapeAnnotation.md)).

```csharp
public HpdfShapeAnnotation CreateSquareAnnotation(HpdfRect rect, string text)
```

**Parameters:**
- `rect` - Annotation rectangle; the shape is drawn inside it. Reversed edges are swapped
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfShapeAnnotation` object: red outline 1 point wide, no fill, Print flag. Its appearance stream is
generated when the document is saved. Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or a coordinate of `rect` is NaN or
infinite.

**Example:**

```csharp
var square = page.CreateSquareAnnotation(new HpdfRect(50, 600, 250, 700), "This table is out of date");
square.SetInteriorRgbColor(1f, 0.95f, 0.8f);
```

### CreateCircleAnnotation

Creates a circle annotation: an ellipse inscribed in `rect` ([HpdfShapeAnnotation](../annotations/HpdfShapeAnnotation.md)).
Parameters, defaults and exceptions are those of `CreateSquareAnnotation`.

```csharp
public HpdfShapeAnnotation CreateCircleAnnotation(HpdfRect rect, string text)
```

**Example:**

```csharp
var circle = page.CreateCircleAnnotation(new HpdfRect(300, 600, 450, 700), "Check this figure");
circle.SetBorderStyle(HpdfBorderStyle.Dashed, 2, 6, 3);
```

### CreateLineAnnotation

Creates a line annotation from `start` to `end`, with optional line endings, leader lines and caption
([HpdfLineAnnotation](../annotations/HpdfLineAnnotation.md)).

```csharp
public HpdfLineAnnotation CreateLineAnnotation(HpdfPoint start, HpdfPoint end, string text)
```

**Parameters:**
- `start`, `end` - The points of the line, in page coordinates
- `text` - Text of the annotation, shown in its pop-up window and as the caption; may be empty

**Returns:** `HpdfLineAnnotation` object: a red line 1 point wide without endings, Print flag. The annotation
rectangle is computed from the line when the document is saved. Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or a coordinate is NaN or infinite.

**Example:**

```csharp
var arrow = page.CreateLineAnnotation(new HpdfPoint(300, 500), new HpdfPoint(150, 560), "Look here");
arrow.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
```

### CreateHighlightAnnotation

Creates a highlight annotation over text on the page ([HpdfTextMarkupAnnotation](../annotations/HpdfTextMarkupAnnotation.md)).
It is painted in the annotation color (yellow until changed) with the Multiply blend mode, so the text stays readable.

```csharp
public HpdfTextMarkupAnnotation CreateHighlightAnnotation(HpdfRect rect, string text)
```

**Parameters:**
- `rect` - Rectangle of the marked text (one line); for several lines call `SetQuadPoints()` on the result
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfTextMarkupAnnotation` object with the Print flag. The annotation rectangle is computed from the
quadrilaterals when the document is saved. Raises the document version to PDF 1.4 (the blend mode of the
appearance).

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or a coordinate of `rect` is NaN or
infinite.

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.TextOut(50, 450, "Important sentence");
float width = font.MeasureText("Important sentence", 12);
var highlight = page.CreateHighlightAnnotation(new HpdfRect(50, 447, 50 + width, 461), "Keep this");
```

### CreateUnderlineAnnotation

Creates an underline annotation, drawn in the annotation color (red until changed). Parameters, return value and
exceptions are those of `CreateHighlightAnnotation`; raises the document version to PDF 1.3.

```csharp
public HpdfTextMarkupAnnotation CreateUnderlineAnnotation(HpdfRect rect, string text)
```

### CreateSquigglyAnnotation

Creates a squiggly underline annotation, drawn in the annotation color (red until changed). Parameters, return value
and exceptions are those of `CreateHighlightAnnotation`; raises the document version to PDF 1.4.

```csharp
public HpdfTextMarkupAnnotation CreateSquigglyAnnotation(HpdfRect rect, string text)
```

### CreateStrikeOutAnnotation

Creates a strikeout annotation, drawn in the annotation color (red until changed). Parameters, return value and
exceptions are those of `CreateHighlightAnnotation`; raises the document version to PDF 1.3.

```csharp
public HpdfTextMarkupAnnotation CreateStrikeOutAnnotation(HpdfRect rect, string text)
```

**Example:**

```csharp
var strikeOut = page.CreateStrikeOutAnnotation(new HpdfRect(50, 400, 200, 414), "Delete");
strikeOut.SetQuadPoints(new HpdfRect(50, 400, 200, 414), new HpdfRect(50, 383, 120, 397));
```

### CreateFreeTextAnnotation

Creates a free text annotation: text shown in a box on the page, wrapped to its width
([HpdfFreeTextAnnotation](../annotations/HpdfFreeTextAnnotation.md)).

```csharp
public HpdfFreeTextAnnotation CreateFreeTextAnnotation(HpdfRect rect, string text, HpdfFont font, float fontSize)
```

**Parameters:**
- `rect` - The text box; reversed edges are swapped
- `text` - The text; may be empty
- `font` - The font of the text: any font of the document of horizontal writing
- `fontSize` - The font size, positive

**Returns:** `HpdfFreeTextAnnotation` object: black text, a black border 1 point wide, no background, Print flag. Its
appearance stream is generated with the font when the document is saved. Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `font` is `null`, the font is of vertical writing,
or `fontSize` is not a positive number, or a coordinate of `rect` is NaN or infinite. Nothing is added to the page or the document.

**Example:**

```csharp
var comment = page.CreateFreeTextAnnotation(new HpdfRect(300, 400, 540, 460), "Approved with changes", font, 12);
comment.SetRgbColor(1f, 1f, 0.8f);   // background
```

### CreateStampAnnotation

Creates a rubber stamp annotation with one of the standard stamps
([HpdfStampAnnotation](../annotations/HpdfStampAnnotation.md)).

```csharp
public HpdfStampAnnotation CreateStampAnnotation(HpdfRect rect, HpdfStampName name, string text, HpdfFont font)
```

**Parameters:**
- `rect` - The box of the stamp; reversed edges are swapped
- `name` - The standard stamp (`Approved`, `Draft`, `Confidential`, ...; see [HpdfStampName](../annotations/HpdfStampName.md))
- `text` - Text of the annotation, shown in its pop-up window; may be empty
- `font` - The font of the label: any font of the document of horizontal writing

**Returns:** `HpdfStampAnnotation` object: a frame with rounded corners and the label in capitals, green, red or blue
by the meaning of the stamp, Print flag. Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `font` is `null`, the font is of vertical writing,
or `name` is not an `HpdfStampName` value, or a coordinate of `rect` is NaN or infinite. Nothing is added to the page or the document.

**Example:**

```csharp
var bold = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F2");
page.CreateStampAnnotation(new HpdfRect(400, 760, 560, 800), HpdfStampName.Draft, "Not final yet", bold);
```

### CreatePolygonAnnotation

Creates a polygon annotation: a closed shape through the vertices, drawn in the annotation color and filled with the
interior color, if one is set ([HpdfPolygonAnnotation](../annotations/HpdfPolygonAnnotation.md)).

```csharp
public HpdfPolygonAnnotation CreatePolygonAnnotation(HpdfPoint[] vertices, string text)
```

**Parameters:**
- `vertices` - At least three vertices in page coordinates; the array is copied
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfPolygonAnnotation` object: red outline 1 point wide, no fill, Print flag. The annotation rectangle is
computed from the vertices when the document is saved. Raises the document version to PDF 1.5; not allowed in
PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `vertices` is `null`, there are fewer than three
vertices, or a coordinate is NaN or infinite. Nothing is added to the page or the document.

**Example:**

```csharp
var triangle = page.CreatePolygonAnnotation(new HpdfPoint[] { new(60, 620), new(180, 620), new(120, 715) }, "Check");
triangle.SetInteriorRgbColor(1f, 0.9f, 0.8f);
```

### CreatePolyLineAnnotation

Creates a polyline annotation: connected lines through the vertices, with optional line endings
([HpdfPolyLineAnnotation](../annotations/HpdfPolyLineAnnotation.md)).

```csharp
public HpdfPolyLineAnnotation CreatePolyLineAnnotation(HpdfPoint[] vertices, string text)
```

**Parameters:**
- `vertices` - At least two vertices in page coordinates; the array is copied
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfPolyLineAnnotation` object: a red line 1 point wide without endings, Print flag. The annotation
rectangle is computed from the vertices when the document is saved. Raises the document version to PDF 1.5; not
allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `vertices` is `null`, there are fewer than two
vertices, or a coordinate is NaN or infinite.

**Example:**

```csharp
var path = page.CreatePolyLineAnnotation(new HpdfPoint[] { new(400, 630), new(440, 700), new(490, 640) }, "This way");
path.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
```

### CreateInkAnnotation

Creates an ink annotation: freehand strokes drawn in the annotation color with the border width
([HpdfInkAnnotation](../annotations/HpdfInkAnnotation.md)).

```csharp
public HpdfInkAnnotation CreateInkAnnotation(HpdfPoint[][] strokes, string text)
```

**Parameters:**
- `strokes` - At least one stroke; every stroke is a path of at least one point (one point is a dot)
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfInkAnnotation` object: red strokes 1 point wide with round ends and joins, Print flag. The annotation
rectangle is computed from the points. Raises the document version to PDF 1.3; allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `strokes` is `null`, there is no stroke, a stroke
is `null` or empty, or a coordinate is NaN or infinite.

**Example:**

```csharp
var check = page.CreateInkAnnotation(new[] { new HpdfPoint[] { new(300, 520), new(310, 505), new(335, 545) } }, "Done");
check.SetBorderStyle(HpdfBorderStyle.Solid, 3);
```

### CreateCaretAnnotation

Creates a caret annotation that marks where text is to be inserted; the text to insert is the text of the annotation
([HpdfCaretAnnotation](../annotations/HpdfCaretAnnotation.md)).

```csharp
public HpdfCaretAnnotation CreateCaretAnnotation(HpdfRect rect, string text)
```

**Parameters:**
- `rect` - Annotation rectangle; the caret fills it. Reversed edges are swapped
- `text` - The text to insert, shown in the pop-up window; may be empty

**Returns:** `HpdfCaretAnnotation` object: a blue filled caret, no symbol, Print flag. Raises the document version to
PDF 1.5; not allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or a coordinate of `rect` is NaN or infinite.

**Example:**

```csharp
var caret = page.CreateCaretAnnotation(new HpdfRect(146, 692, 154, 704), "that are missing");
```

### CreateRedactAnnotation

Creates a redaction annotation: it marks content that a viewer which applies redactions removes; the library itself
removes nothing ([HpdfRedactAnnotation](../annotations/HpdfRedactAnnotation.md)).

```csharp
public HpdfRedactAnnotation CreateRedactAnnotation(HpdfRect rect, string text)
```

**Parameters:**
- `rect` - The area to redact; for several areas call `SetQuadPoints()` on the result. Reversed edges are swapped
- `text` - Text of the annotation, shown in its pop-up window; may be empty

**Returns:** `HpdfRedactAnnotation` object: a red outline of the area, no interior color, no overlay text, Print flag.
Raises the document version to PDF 1.7; not allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `text` is `null` or a coordinate of `rect` is NaN or infinite.

**Example:**

```csharp
var redact = page.CreateRedactAnnotation(new HpdfRect(50, 697, 230, 711), "Remove the account number");
redact.SetInteriorGrayColor(0);
```

### CreateFileAttachmentAnnotation(HpdfRect, HpdfEmbeddedFile, string, HpdfFileAttachmentIcon)

Creates a file attachment annotation: an icon that opens an embedded file, usually a document attachment
([HpdfFileAttachmentAnnotation](../annotations/HpdfFileAttachmentAnnotation.md)).

```csharp
public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, HpdfEmbeddedFile file, string text,
    HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)
```

**Parameters:**
- `rect` - Annotation rectangle; the icon fills it (about 20 x 20 points is usual). Reversed edges are swapped
- `file` - The embedded file, for example the result of `HpdfDocument.AttachFile()`; it is embedded once, however many
  annotations show it
- `text` - Text of the annotation, shown in its pop-up window; may be empty
- `icon` - `PushPin` (default), `Graph`, `Paperclip` or `Tag`

**Returns:** `HpdfFileAttachmentAnnotation` object: the icon in blue (`0.2 0.3 0.8`), Print flag. Raises the document
version to PDF 1.3; not allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `file` or `text` is `null` or a coordinate of `rect` is NaN or
infinite, and with `AnnotInvalidIcon` if `icon` is not an `HpdfFileAttachmentIcon` value.

**Example:**

```csharp
var csv = pdf.AttachFile("revenue.csv", System.Text.Encoding.UTF8.GetBytes("Q1,100\n"), "text/csv");
page.CreateFileAttachmentAnnotation(new HpdfRect(50, 700, 70, 720), csv, "The revenue table", HpdfFileAttachmentIcon.Paperclip);
```

### CreateFileAttachmentAnnotation(HpdfRect, string, byte[], string, HpdfFileAttachmentIcon)

Creates a file attachment annotation with a file of its own, which is not one of the document's attachments
(`HpdfDocument.AttachedFiles`).

```csharp
public HpdfFileAttachmentAnnotation CreateFileAttachmentAnnotation(HpdfRect rect, string fileName, byte[] data, string text,
    HpdfFileAttachmentIcon icon = HpdfFileAttachmentIcon.PushPin)
```

**Parameters:**
- `rect` - Annotation rectangle; the icon fills it. Reversed edges are swapped
- `fileName` - The file name without a directory
- `data` - The contents of the file (copied); it is embedded without a MIME type
- `text` - Text of the annotation, shown in its pop-up window; may be empty
- `icon` - `PushPin` (default), `Graph`, `Paperclip` or `Tag`

**Returns:** `HpdfFileAttachmentAnnotation` object, as the other overload; its `File` is the new embedded file.

**Throws:** `HpdfException` with `InvalidParameter` if `text` or `data` is `null`, `fileName` is `null`, empty or
contains `/` or `\`, or a coordinate of `rect` is NaN or infinite, and with `AnnotInvalidIcon` for an invalid icon.
Nothing is added to the page or the document.

**Example:**

```csharp
var notes = page.CreateFileAttachmentAnnotation(new HpdfRect(50, 650, 70, 670), "notes.txt",
    System.Text.Encoding.UTF8.GetBytes("Measured on 2026-11-20"), "Measurement notes");
notes.File.SetDescription("Measurement notes");
```

### Create3DAnnotation

Creates a 3D annotation: the model is shown in `rect` by viewers that show 3D (Adobe Acrobat and Reader, Foxit); other
viewers show the poster image, or a gray box with a cube when no image is given
([Hpdf3DAnnotation](../annotations/Hpdf3DAnnotation.md), [3D Guide](../../guides/ThreeDGuide.md)).

```csharp
public Hpdf3DAnnotation Create3DAnnotation(HpdfRect rect, Hpdf3DModel model, string text = "3D Model",
    HpdfImage? poster = null)
```

**Parameters:**
- `rect` - The area of the page that shows the model. Reversed edges are swapped
- `model` - The model, loaded by `HpdfDocument.Load3DModel()`
- `text` - Text of the annotation, a description of the model
- `poster` - An image of the document shown before the model is activated, scaled to `rect`; or `null`

**Returns:** `Hpdf3DAnnotation` object: activated when the page opens (live), deactivated when it closes, Print flag.
Raises the document version to PDF 1.6; not allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `model` or `text` is `null`, the model or the poster belongs to
another document, or a coordinate of `rect` is NaN or infinite.

**Example:**

```csharp
var model3d = pdf.Load3DModel("part.prc");
var view3d = page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model3d, "A machine part");
view3d.SetActivation(Hpdf3DActivation.Explicit);
```

### CreateProjectionAnnotation

Creates a projection annotation: it lists a 3D measurement among the comments of the page
([HpdfProjectionAnnotation](../annotations/HpdfProjectionAnnotation.md)).

```csharp
public HpdfProjectionAnnotation CreateProjectionAnnotation(string text, Hpdf3DMeasure measure)
```

**Parameters:**
- `text` - Text of the annotation, shown in the comments list
- `measure` - A measurement created by `Hpdf3DModel.CreateCommentNote()` or `CreatePerpendicularMeasure()`

**Returns:** `HpdfProjectionAnnotation` object without an appearance stream, without flags and without an area: it is
always written with `/Rect [0 0 0 0]`, because Adobe Acrobat crashes on a projection annotation that has an area (the
3D viewer draws the measurement). Declares Adobe ExtensionLevel 3 and raises the document version to PDF 1.7; not
allowed in PDF/A-1.

**Throws:** `HpdfException` with `InvalidParameter` if `measure` or `text` is `null`, and with `InvalidOperation` if
the measurement already has a projection annotation.

**Example:**

```csharp
var origin = model.CreateCommentNote(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(20, 20, 20));
view.AddMeasure(origin);
page.CreateProjectionAnnotation("The origin", origin);
```

### CreatePopupAnnotation

Creates the pop-up window of a markup annotation of this page and connects the two (`/Parent` of the pop-up,
`/Popup` of the parent) ([HpdfPopupAnnotation](../annotations/HpdfPopupAnnotation.md)).

```csharp
public HpdfPopupAnnotation CreatePopupAnnotation(HpdfRect rect, HpdfMarkupAnnotation parent)
```

**Parameters:**
- `rect` - The rectangle of the window; reversed edges are swapped
- `parent` - The markup annotation whose text the window shows

**Returns:** `HpdfPopupAnnotation` object. Raises the document version to PDF 1.3.

**Throws:** `HpdfException` with `InvalidParameter` if `parent` is `null` or not an annotation of this page or a
coordinate of `rect` is NaN or infinite, and with `InvalidOperation` if `parent` already has a pop-up annotation.

**Example:**

```csharp
var note = page.CreateTextAnnotation(new HpdfRect(60, 700, 80, 720), "See the appendix");
var popup = page.CreatePopupAnnotation(new HpdfRect(100, 620, 330, 720), note);
popup.SetOpened(true);
```

### AddWidgetAnnotation

Adds a widget annotation (form field) to the page.

```csharp
public void AddWidgetAnnotation(HpdfWidgetAnnotation widget)
```

**Parameters:**
- `widget` - The widget annotation (form field)

**Example:**

```csharp
var acroForm = pdf.GetOrCreateAcroForm();
var page = pdf.AddPage();

// Create a text field and place its widget on the page
var textField = new HpdfTextField(pdf.Xref, "username");
var widget = textField.CreateWidget(new HpdfRect(100, 700, 300, 720));
page.AddWidgetAnnotation(widget);
acroForm.AddField(textField);
```

The widget must be added to the page explicitly; a field can have several widgets on different pages.
Each widget appears on exactly one page: adding the same widget to the same page again changes nothing, and
adding a widget that is already placed on another page throws `HpdfException` (`InvalidOperation`).
See the [Forms Guide](../../guides/FormsGuide.md).

---

## Graphics State Methods

### GSave()

Saves the current graphics state onto the stack (`q` operator). Extension method from `HpdfPageGraphics`.

```csharp
public static void GSave(this IDrawable drawable)
```

**Usage:** Use before making temporary changes to graphics state. Graphics states can be nested up to 28 levels
deep; a further `GSave` throws `HpdfException` (`ExceedGStateLimit`).

**Example:**

```csharp
// Save current state
page.GSave();

// Make temporary changes
page.SetLineWidth(5);
page.SetRgbStroke(1, 0, 0);
page.Rectangle(100, 100, 200, 100);
page.Stroke();

// Restore previous state
page.GRestore();

// Back to previous line width and color
page.Rectangle(100, 250, 200, 100);
page.Stroke();
```

### GRestore()

Restores the graphics state from the stack (`Q` operator). Extension method from `HpdfPageGraphics`.

```csharp
public static void GRestore(this IDrawable drawable)
```

**Note:** Must be balanced with `GSave()` calls. `GRestore` without a matching `GSave` throws `HpdfException`
(`PageCannotRestoreGState`), and so does saving the document while a page has a `GSave` without a matching
`GRestore`.

---

## Transition Effects (Slide Shows)

### SetSlideShow

Sets a transition effect for the page (for PDF presentations).

```csharp
public void SetSlideShow(
    HpdfTransitionStyle type,
    float displayTime,
    float transTime)
```

**Parameters:**
- `type` - Transition style
- `displayTime` - Display time in seconds before transitioning; 0 or less writes no display time, so the viewer does not advance by itself
- `transTime` - Transition duration in seconds

The method can be called again: the new transition replaces the previous one (and a `displayTime` of 0 or less
removes a display time set before).

**Example:**

```csharp
var page1 = pdf.AddPage();
var page2 = pdf.AddPage();
var page3 = pdf.AddPage();

// Wipe right transition
page1.SetSlideShow(HpdfTransitionStyle.WipeRight, 3.0f, 1.0f);

// Dissolve transition
page2.SetSlideShow(HpdfTransitionStyle.Dissolve, 3.0f, 1.5f);

// Box out transition
page3.SetSlideShow(HpdfTransitionStyle.BoxOut, 3.0f, 1.0f);

// Set document to full screen mode
pdf.Catalog.SetPageMode(HpdfPageMode.FullScreen);
```

**Available transition styles:**
- `Replace` - Simple replacement (no effect)
- `WipeRight`, `WipeLeft`, `WipeUp`, `WipeDown`
- `BarnDoorsHorizontalOut`, `BarnDoorsHorizontalIn`
- `BarnDoorsVerticalOut`, `BarnDoorsVerticalIn`
- `BoxOut`, `BoxIn`
- `BlindsHorizontal`, `BlindsVertical`
- `Dissolve`
- `GlitterRight`, `GlitterDown`, `GlitterTopLeftToBottomRight`

---

## Extension Methods

`HpdfPage` is extended with many methods through extension method classes. These are organized by functionality:

### Graphics Operations

See [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) for:
- **Path operations:** `MoveTo`, `LineTo`, `CurveTo`, `Rectangle`, `ClosePath`
- **Colors:** `SetRgbStroke`, `SetRgbFill`, `SetCmykStroke`, `SetCmykFill`, `SetGrayStroke`, `SetGrayFill`
- **Line styles:** `SetLineWidth`, `SetLineCap`, `SetLineJoin`, `SetDash`, `SetMiterLimit`, `SetFlat`
- **Path painting:** `Stroke`, `Fill`, `EoFill`, `FillStroke`, `EoFillStroke`, `ClosePathStroke`,
  `ClosePathFillStroke`, `ClosePathEoFillStroke`, `Clip`, `EoClip`
- **Transformations:** `Concat`
- **Images and transparency:** `DrawImage`, `SetExtGState`

**Quick example:**

```csharp
// Draw a colored rectangle
page.SetRgbFill(0.8f, 0.2f, 0.2f);
page.SetRgbStroke(0, 0, 0);
page.SetLineWidth(2);
page.Rectangle(100, 500, 200, 100);
page.FillStroke();
```

### Shape Drawing

See [HpdfPageShapes](../extensions/HpdfPageShapes.md) for:
- `Circle(x, y, radius)`
- `Ellipse(x, y, xRadius, yRadius)`
- `Arc(x, y, radius, startAngle, endAngle)`

**Quick example:**

```csharp
// Draw a circle
page.SetLineWidth(2);
page.Circle(300, 400, 50);
page.Stroke();

// Draw an ellipse
page.Ellipse(300, 250, 80, 40);
page.Stroke();
```

### Text Operations

See [HpdfPageText](../extensions/HpdfPageText.md) for:
- **Text blocks:** `BeginText`, `EndText`
- **Positioning:** `MoveTextPos`, `MoveToNextLine`, `SetTextMatrix`
- **Rendering:** `ShowText`, `ShowTextNextLine`, `SetSpacingAndShowText`
- **Text in a rectangle:** `TextRect` (wrapping with left, right, center or justified alignment)
- **Formatting:** `SetFontAndSize`, `SetTextLeading`, `SetCharSpace`, `SetWordSpace`
- **Styling:** `SetTextRenderingMode`, `SetTextRise`, `SetHorizontalScaling`

**Quick example:**

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("First line");
page.SetTextLeading(20);
page.ShowTextNextLine("Second line (20 points below)");
page.EndText();
```

### Page Extensions

`HpdfPageExtensions` (namespace `Haru.Doc`) adds convenience methods:
- `TextOut(x, y, text)` - Quick text output (begins and ends its own text object)
- `TextWidth(text)` - Measure text width in the current font and size
- `CreateDestination()` - Create a destination for links, bookmarks and the open action
- `ExecuteXObject(image)` - Paint an image in the current transformation (see [HpdfImage](HpdfImage.md))
- `GetCurrentFont()`, `GetCurrentFontSize()`, `GetRGBFill()`, `GetRGBStroke()` - Read the current state

Images are drawn with `DrawImage` from [HpdfPageGraphics](../extensions/HpdfPageGraphics.md).

**Quick example:**

```csharp
// Load and draw image
using var image = pdf.LoadPngImageFromFile("logo.png");
page.DrawImage(image, 100, 600, 200, 100);

// Quick text output
page.SetFontAndSize(font, 12);
page.TextOut(100, 500, "Quick text");

// Measure text
float width = page.TextWidth("Measure this");
```

---

## Complete Examples

### Basic Page with Text and Graphics

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var page = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Portrait);

// Title
page.SetFontAndSize(font, 24);
page.BeginText();
page.MoveTextPos(50, page.Height - 50);
page.ShowText("Graphics and Text Demo");
page.EndText();

// Draw a filled rectangle
page.SetRgbFill(0.2f, 0.4f, 0.8f);
page.SetRgbStroke(0, 0, 0);
page.SetLineWidth(2);
page.Rectangle(50, 600, 200, 100);
page.FillStroke();

// Draw a circle
page.SetRgbStroke(0.8f, 0.2f, 0.2f);
page.SetLineWidth(3);
page.Circle(400, 650, 50);
page.Stroke();

// Add some text
page.SetFontAndSize(font, 14);
page.BeginText();
page.MoveTextPos(50, 500);
page.ShowText("This page demonstrates basic graphics and text.");
page.EndText();

pdf.SaveToFile("page-demo.pdf");
```

### Multi-Size Pages

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// A4 portrait page
var page1 = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Portrait);
page1.SetFontAndSize(font, 16);
page1.BeginText();
page1.MoveTextPos(50, page1.Height - 50);
page1.ShowText($"A4 Portrait: {page1.Width} x {page1.Height}");
page1.EndText();

// Letter landscape page
var page2 = pdf.AddPage(HpdfPageSize.Letter, HpdfPageDirection.Landscape);
page2.SetFontAndSize(font, 16);
page2.BeginText();
page2.MoveTextPos(50, page2.Height - 50);
page2.ShowText($"Letter Landscape: {page2.Width} x {page2.Height}");
page2.EndText();

// Custom size page (square)
var page3 = pdf.AddPage(600, 600);
page3.SetFontAndSize(font, 16);
page3.BeginText();
page3.MoveTextPos(50, page3.Height - 50);
page3.ShowText($"Custom Square: {page3.Width} x {page3.Height}");
page3.EndText();

pdf.SaveToFile("multi-size.pdf");
```

### Page with Annotations

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var page = pdf.AddPage();

// Add text
page.SetFontAndSize(font, 14);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Click the link below:");
page.EndText();

// Add clickable link
page.SetRgbFill(0, 0, 1);  // Blue text
page.BeginText();
page.MoveTextPos(100, 670);
page.ShowText("Visit Example.com");
page.EndText();

var link = page.CreateLinkAnnotation(
    new HpdfRect(100, 667, 220, 687),
    "https://www.example.com"
);

// Add a sticky note
var note = page.CreateTextAnnotation(
    new HpdfRect(500, 750, 520, 770),
    "This is an important note!",
    HpdfAnnotationIcon.Note
);

pdf.SaveToFile("annotations.pdf");
```

### Page with Graphics State Management

```csharp
var pdf = new HpdfDocument();
var page = pdf.AddPage();

// Default state
page.SetLineWidth(1);
page.SetRgbStroke(0, 0, 0);

// Draw with default state
page.Rectangle(50, 700, 100, 50);
page.Stroke();

// Save state and modify
page.GSave();
page.SetLineWidth(5);
page.SetRgbStroke(1, 0, 0);

// Draw with modified state
page.Rectangle(200, 700, 100, 50);
page.Stroke();

// Restore original state
page.GRestore();

// Draw with restored state (thin, black)
page.Rectangle(350, 700, 100, 50);
page.Stroke();

pdf.SaveToFile("state-management.pdf");
```

### Slide Show Presentation

```csharp
var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.HelveticaBold, "F1");

// Create 5 slides with different transitions
string[] titles = { "Introduction", "Main Points", "Details", "Summary", "Thank You" };
var transitions = new[] {
    HpdfTransitionStyle.WipeRight,
    HpdfTransitionStyle.Dissolve,
    HpdfTransitionStyle.BoxOut,
    HpdfTransitionStyle.BlindsHorizontal,
    HpdfTransitionStyle.GlitterDown
};

for (int i = 0; i < 5; i++)
{
    var page = pdf.AddPage(HpdfPageSize.A4, HpdfPageDirection.Landscape);

    // Add title
    page.SetFontAndSize(font, 36);
    string title = titles[i];
    float titleWidth = font.MeasureText(title, 36);
    page.BeginText();
    page.MoveTextPos((page.Width - titleWidth) / 2, page.Height / 2);
    page.ShowText(title);
    page.EndText();

    // Set transition (display for 5 seconds, transition for 1 second)
    page.SetSlideShow(transitions[i], 5.0f, 1.0f);
}

// Set full screen mode
pdf.Catalog.SetPageMode(HpdfPageMode.FullScreen);

pdf.SaveToFile("presentation.pdf");
```

---

## Best Practices

### 1. Always Call BeginText/EndText

```csharp
page.SetFontAndSize(font, 12);

// WRONG: text operators outside a text object
page.MoveTextPos(100, 700);
page.ShowText("Text");

// CORRECT
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Text");
page.EndText();
```

### 2. Use GSave/GRestore for Temporary Changes

```csharp
// Save state before temporary changes
page.GSave();

// Temporary styling
page.SetLineWidth(10);
page.SetRgbStroke(1, 0, 0);
// ... draw something ...

// Restore previous state
page.GRestore();
```

### 3. Set Font Before Text Operations

```csharp
// Always set font and size before showing text
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(100, 700);
page.ShowText("Text");
page.EndText();
```

### 4. Check Page Bounds

```csharp
float x = 100;
float y = 700;

// Ensure position is within page bounds
if (x >= 0 && x <= page.Width && y >= 0 && y <= page.Height)
{
    page.SetFontAndSize(font, 12);
    page.BeginText();
    page.MoveTextPos(x, y);
    page.ShowText("Text");
    page.EndText();
}
```

### 5. Use Appropriate Units

```csharp
// Remember: 72 points = 1 inch

// 1 inch margin
float marginPoints = 72;

// 0.5 inch margin
float halfInchMargin = 36;

// Calculate from mm (A4 is 210mm × 297mm)
float mm = 10;
float points = mm * 72 / 25.4f;
```

---

## Related Types

### HpdfGraphicsState Class

Contains current graphics state:
- Colors (RGB/CMYK fill and stroke)
- Line properties (width, cap, join, dash)
- Font and font size
- Text properties (spacing, leading, etc.)
- Transformation matrix

### HpdfBox Struct

Represents a PDF box (CropBox, BleedBox, TrimBox, ArtBox):

```csharp
public struct HpdfBox
{
    public HpdfBox(float left, float bottom, float right, float top);

    public float Left { get; set; }
    public float Bottom { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Width { get; }   // Right - Left
    public float Height { get; }  // Top - Bottom
}
```

### HpdfRect Struct

Represents a rectangle by its edges (for annotations, form widgets, etc.):

```csharp
public struct HpdfRect
{
    public HpdfRect(float left, float bottom, float right, float top);

    public float Left { get; set; }
    public float Bottom { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Width { get; }   // Right - Left
    public float Height { get; }  // Top - Bottom
}
```

---

## See Also

### Core Classes
- [HpdfDocument](HpdfDocument.md) - Document management
- [HpdfFont](HpdfFont.md) - Font handling
- [HpdfImage](HpdfImage.md) - Image operations

### Extension Methods
- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - Graphics operations
- [HpdfPageShapes](../extensions/HpdfPageShapes.md) - Shape drawing
- [HpdfPageText](../extensions/HpdfPageText.md) - Text operations

### Guides
- [Getting Started](../../guides/GettingStarted.md) - Step-by-step tutorial
- [Forms Guide](../../guides/FormsGuide.md) - Interactive form fields placed on pages

---

[← Back to Documentation Index](../../INDEX.md)

*Last updated: 2026-11-26*
