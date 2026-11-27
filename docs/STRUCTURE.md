# Library Architecture

[← Back to Documentation Index](INDEX.md)

---

## Overview

Haru.NET is organized around a clean, hierarchical architecture that mirrors the PDF specification while providing a developer-friendly API. The library uses modern .NET design patterns including extension methods, polymorphic interfaces, and SOLID principles.

---

## High-Level Architecture

### System Overview

```mermaid
graph TB
    subgraph "User Code"
        APP[Application]
    end

    subgraph "Haru.NET Public API"
        DOC[HpdfDocument]
        PAGE[HpdfPage]
        FONT[HpdfFont]
        IMG[HpdfImage]
        EXT[Extension Methods]
    end

    subgraph "Internal Systems"
        XREF[HpdfXref<br/>Cross-Reference Table]
        OBJECTS[PDF Objects<br/>Dict, Array, Stream]
        FONTS[Font Implementations<br/>Standard, TrueType, CID]
        STREAMS[Stream Management<br/>Compression]
    end

    subgraph "External Dependencies"
        STB[StbImageSharp<br/>Image Parsing]
        CP[CodePages<br/>Encodings]
        ZIP[System.IO.Compression<br/>Deflate]
    end

    APP --> DOC
    APP --> PAGE
    APP --> FONT
    APP --> IMG
    APP --> EXT

    DOC --> XREF
    PAGE --> XREF
    FONT --> FONTS
    IMG --> STB

    XREF --> OBJECTS
    OBJECTS --> STREAMS
    STREAMS --> ZIP
    FONTS --> CP

    style APP fill:#e1f5ff
    style DOC fill:#fff3e0
    style PAGE fill:#fff3e0
    style FONT fill:#fff3e0
    style IMG fill:#fff3e0
    style EXT fill:#fff3e0
```

---

## Core Class Hierarchy

### Document Object Model

```mermaid
classDiagram
    class HpdfDocument {
        +HpdfXref Xref
        +HpdfCatalog Catalog
        +HpdfInfo Info
        +HpdfVersion Version
        +HpdfCompressionMode CompressionMode
        +IReadOnlyList~HpdfPage~ Pages
        +int PageCount
        +AddPage() HpdfPage
        +SaveToFile(filename)
        +SaveToMemory() byte[]
        +SetEncryption(userPassword, ownerPassword, permission, mode)
    }

    class HpdfPage {
        +HpdfDict Dict
        +HpdfStreamObject Contents
        +HpdfGraphicsState GraphicsState
        +float Width
        +float Height
        +int Rotate
        +SetSize(width, height)
        +CreateTextAnnotation(rect, text, icon) HpdfTextAnnotation
        +CreateLinkAnnotation(rect, uri) HpdfLinkAnnotation
        +SetSlideShow(type, displayTime, transTime)
    }

    class HpdfPages {
        +HpdfDict Dict
        +HpdfPages Parent
        +int Count
        +AddKid(kid)
        +GetAllPages() List~HpdfPage~
    }

    class HpdfCatalog {
        +HpdfDict Dict
        +HpdfPageLayout PageLayout
        +HpdfPageMode PageMode
        +HpdfViewerPreferences ViewerPreferences
        +SetOpenAction(destination)
    }

    class HpdfXref {
        +HpdfDict Trailer
        +Add(obj) uint
        +GetEntryByObjectId(objectId) HpdfXrefEntry
        +WriteToStream(stream)
    }

    HpdfDocument "1" --> "1" HpdfXref : manages
    HpdfDocument "1" --> "1" HpdfCatalog : contains
    HpdfDocument "1" --> "1" HpdfPages : root pages
    HpdfCatalog "1" --> "1" HpdfPages : references
    HpdfPages "1" --> "*" HpdfPage : contains
    HpdfPage "1" --> "1" HpdfPages : parent
    HpdfXref "1" --> "*" HpdfObject : tracks
```

### Extension Methods Organization

```mermaid
graph LR
    subgraph "Target Classes"
        PAGE[HpdfPage]
        DOC[HpdfDocument]
    end

    subgraph "Extension Method Classes"
        GRAPHICS[HpdfPageGraphics<br/>Graphics Operations]
        SHAPES[HpdfPageShapes<br/>Shape Drawing]
        TEXT[HpdfPageText<br/>Text Operations]
        PAGEEXT[HpdfPageExtensions<br/>Convenience Methods]
        DOCEXT[HpdfDocumentExtensions<br/>Document Operations]
    end

    GRAPHICS -.extends.-> PAGE
    SHAPES -.extends.-> PAGE
    TEXT -.extends.-> PAGE
    PAGEEXT -.extends.-> PAGE
    DOCEXT -.extends.-> DOC

    style PAGE fill:#fff3e0
    style DOC fill:#fff3e0
    style GRAPHICS fill:#e8f5e9
    style SHAPES fill:#e8f5e9
    style TEXT fill:#e8f5e9
    style PAGEEXT fill:#e8f5e9
    style DOCEXT fill:#e8f5e9
```

**Extension Method Categories:**

- **HpdfPageGraphics** - Low-level graphics primitives
  - Path operations: `MoveTo`, `LineTo`, `CurveTo`, `Rectangle`, `ClosePath`
  - Color: `SetRgbStroke`, `SetRgbFill`, `SetCmykStroke`, `SetCmykFill`, `SetGrayStroke`, `SetGrayFill`
  - Line styles: `SetLineWidth`, `SetLineCap`, `SetLineJoin`, `SetDash`
  - Transformations: `Concat`
  - Path painting: `Stroke`, `Fill`, `FillStroke`, `Clip`
  - State: `GSave`, `GRestore`, `SetExtGState`
  - Images: `DrawImage(image, x, y, width, height)`

- **HpdfPageShapes** - High-level shape drawing
  - `Circle(x, y, radius)`
  - `Ellipse(x, y, xRadius, yRadius)`
  - `Arc(x, y, radius, startAngle, endAngle)`

- **HpdfPageText** - Text operations
  - `BeginText`, `EndText`
  - `MoveTextPos(x, y)`
  - `ShowText(text)`
  - `ShowTextNextLine(text)`
  - `SetTextLeading(leading)`
  - `SetTextRenderingMode(mode)`
  - `SetTextMatrix(matrix)`

- **HpdfPageExtensions** - Convenience methods
  - `TextOut(x, y, text)`, `TextWidth(text)`
  - `CreateDestination()`
  - `ExecuteXObject(image)`

- **HpdfDocumentExtensions** - Document-level operations
  - `GetFont(fontName, encodingName)` - Get a Base14 font by name, or a font registered with `RegisterFont`; new
    Base14 fonts get a resource name starting with `HpdfDocument.GeneratedNamePrefix` (`HaruF` and a number)
  - `SetCompressionMode(mode)` - Same as setting the `CompressionMode` property
  - Images are loaded with methods of `HpdfDocument` itself (`LoadPngImageFromFile`, `LoadJpegImageFromFile`, ...),
    which name them `HaruIm` and a number

---

## Font System Architecture

### Font Interface Hierarchy

```mermaid
classDiagram
    class IHpdfFontImplementation {
        <<interface>>
        +HpdfDict Dict
        +string BaseFont
        +string LocalName
        +int? CodePage
        +int Ascent
        +int Descent
        +int XHeight
        +HpdfBox FontBBox
        +GetCharWidth(charCode) float
        +MeasureText(text, fontSize) float
        +ConvertTextToGlyphIDs(text) byte[]
    }

    class HpdfFont {
        -IHpdfFontImplementation _implementation
        +HpdfDict Dict
        +string BaseFont
        +string LocalName
        +int? EncodingCodePage
        +bool IsCIDFont
        +MeasureText(text, fontSize) float
        +GetCharWidth(charCode) float
    }

    class HpdfStandardFontImpl {
        <<internal>>
        +HpdfStandardFontImpl(xref, standardFont, localName, codePage)
    }

    class HpdfTrueTypeFont {
        +HpdfDict Descriptor
        +LoadFromFile(xref, localName, filePath, embedding, codePage) HpdfTrueTypeFont
        +LoadFromCollection(xref, localName, filePath, fontIndex, embedding, codePage) HpdfTrueTypeFont
        +AsFont() HpdfFont
    }

    class HpdfType1Font {
        +HpdfDict Descriptor
        +LoadFromFile(xref, localName, afmPath, pfbPath, codePage) HpdfType1Font
        +AsFont() HpdfFont
    }

    class HpdfCIDFont {
        +LoadFromTrueTypeFile(document, localName, filePath, codePage) HpdfCIDFont
        +LoadFromTrueTypeCollection(document, localName, filePath, fontIndex, codePage) HpdfCIDFont
        +AsFont() HpdfFont
    }

    class HpdfCIDFontType0 {
        +Create(document, fontName, localName, codePage, encodingName) HpdfCIDFontType0
        +AsFont() HpdfFont
    }

    HpdfFont "1" --> "1" IHpdfFontImplementation : wraps
    IHpdfFontImplementation <|.. HpdfStandardFontImpl : implements
    IHpdfFontImplementation <|.. HpdfTrueTypeFont : implements
    IHpdfFontImplementation <|.. HpdfType1Font : implements
    IHpdfFontImplementation <|.. HpdfCIDFont : implements
    IHpdfFontImplementation <|.. HpdfCIDFontType0 : implements
```

### Font Types and Use Cases

| Font Type | Implementation | Use Case | Embedding | Code Pages |
|-----------|---------------|----------|-----------|------------|
| **Standard** | HpdfStandardFontImpl | Base14 PDF fonts | No | - |
| **TrueType / OpenType** | HpdfTrueTypeFont | Custom .ttf / .otf / .ttc fonts (TrueType or CFF outlines) | Yes | CP1251-1258 |
| **Type1** | HpdfType1Font | PostScript fonts | Yes | Standard |
| **CID** | HpdfCIDFont | CJK languages, Unicode (.ttf / .otf / .ttc) | Yes | CP932, CP936, CP949, CP950, UTF-8 |
| **CID (predefined)** | HpdfCIDFontType0 | CJK languages | No | CP932, CP936, CP949, CP950 |

**Base14 Standard Fonts:**
- Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic
- Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique
- Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique
- Symbol, ZapfDingbats

---

## PDF Object Hierarchy

### Object Model

```mermaid
classDiagram
    class HpdfObject {
        <<abstract>>
        +uint ObjectId
        +ushort GenerationNumber
        +WriteValue(stream)
        +Write(stream)
    }

    class HpdfNull {
        +WriteValue(stream)
    }

    class HpdfBoolean {
        +bool Value
        +WriteValue(stream)
    }

    class HpdfNumber {
        +int Value
        +WriteValue(stream)
    }

    class HpdfReal {
        +float Value
        +WriteValue(stream)
    }

    class HpdfString {
        +byte[] Value
        +WriteValue(stream)
    }

    class HpdfName {
        +string Value
        +WriteValue(stream)
    }

    class HpdfArray {
        +int Count
        +Add(obj)
        +Insert(index, obj)
        +WriteValue(stream)
    }

    class HpdfDict {
        +int Count
        +Add(key, value)
        +TryGetValue(key, value) bool
        +WriteValue(stream)
    }

    class HpdfStreamObject {
        +HpdfMemoryStream Stream
        +HpdfStreamFilter Filter
        +WriteValue(stream)
    }

    HpdfObject <|-- HpdfNull
    HpdfObject <|-- HpdfBoolean
    HpdfObject <|-- HpdfNumber
    HpdfObject <|-- HpdfReal
    HpdfObject <|-- HpdfString
    HpdfObject <|-- HpdfName
    HpdfObject <|-- HpdfArray
    HpdfObject <|-- HpdfDict
    HpdfDict <|-- HpdfStreamObject

    HpdfArray "1" --> "*" HpdfObject : contains
    HpdfDict "1" --> "*" HpdfObject : contains
    HpdfStreamObject "1" --> "1" HpdfMemoryStream : has
```

**Object Types:**

- **HpdfNull** - PDF null object
- **HpdfBoolean** - PDF boolean (true/false)
- **HpdfNumber** - PDF integer values
- **HpdfReal** - PDF real values
- **HpdfString** - PDF strings (literal or hexadecimal)
- **HpdfName** - PDF name objects (identifiers)
- **HpdfArray** - PDF arrays (ordered collections)
- **HpdfDict** - PDF dictionaries (key-value pairs)
- **HpdfStreamObject** - PDF streams (dictionary + binary data)

---

## Graphics State Management

### Graphics State Structure

```mermaid
graph TB
    subgraph "Graphics State Stack"
        GS1[Graphics State 1<br/>Initial]
        GS2[Graphics State 2<br/>After GSave]
        GS3[Graphics State 3<br/>After GSave]
    end

    subgraph "State Properties"
        COLOR[Color<br/>RGB/CMYK]
        LINE[Line Style<br/>Width, Dash, Cap, Join]
        FONT[Font & Size]
        TRANS[Transformation Matrix]
        BLEND[Blend Mode & Opacity]
    end

    GS1 --> GS2
    GS2 --> GS3

    GS3 -.contains.-> COLOR
    GS3 -.contains.-> LINE
    GS3 -.contains.-> FONT
    GS3 -.contains.-> TRANS
    GS3 -.contains.-> BLEND

    GRESTORE[GRestore] -.pops.-> GS3

    style GS1 fill:#e3f2fd
    style GS2 fill:#bbdefb
    style GS3 fill:#90caf9
```

**Graphics State Properties:**

```csharp
public class HpdfGraphicsState
{
    // Color
    public HpdfColorSpace StrokeColorSpace { get; set; }
    public HpdfColorSpace FillColorSpace { get; set; }
    public HpdfRgbColor RgbStroke { get; set; }
    public HpdfRgbColor RgbFill { get; set; }
    public HpdfCmykColor CmykStroke { get; set; }
    public HpdfCmykColor CmykFill { get; set; }
    public float GrayStroke { get; set; }
    public float GrayFill { get; set; }

    // Line style
    public float LineWidth { get; set; }
    public HpdfLineCap LineCap { get; set; }
    public HpdfLineJoin LineJoin { get; set; }
    public float MiterLimit { get; set; }
    public HpdfDashMode DashMode { get; set; }
    public float Flatness { get; set; }

    // Font
    public HpdfFont? CurrentFont { get; set; }
    public float FontSize { get; set; }

    // Text
    public float CharSpace { get; set; }
    public float WordSpace { get; set; }
    public float HorizontalScaling { get; set; }
    public float TextLeading { get; set; }
    public int RenderingMode { get; set; }
    public float TextRise { get; set; }

    // Transformation
    public HpdfTransMatrix TransMatrix { get; set; }

    // Stack
    public HpdfGraphicsState? Previous { get; set; }
    public int Depth { get; set; }
}
```

Blend mode and opacity are not tracked here: they are set with an `HpdfExtGState` (`SetExtGState`), which
`GSave`/`GRestore` save and restore in the PDF viewer like the rest of the state.

---

## Document Creation Workflow

### Typical Document Creation Flow

```mermaid
sequenceDiagram
    actor User
    participant Doc as HpdfDocument
    participant Xref as HpdfXref
    participant Page as HpdfPage
    participant Stream as Content Stream
    participant File as PDF File

    User->>Doc: new HpdfDocument()
    Doc->>Xref: Initialize Xref
    Doc->>Doc: Create Catalog
    Doc->>Doc: Create Root Pages

    User->>Doc: new HpdfFont(...)
    Doc->>Xref: Register Font

    User->>Doc: AddPage()
    Doc->>Page: Create Page
    Doc->>Xref: Register Page
    Page->>Stream: Create Content Stream
    Doc-->>User: return Page

    User->>Page: SetFontAndSize(font, 24)
    Page->>Stream: Write font operator

    User->>Page: BeginText()
    Page->>Stream: Write BT

    User->>Page: MoveTextPos(100, 700)
    Page->>Stream: Write Td operator

    User->>Page: ShowText("Hello")
    Page->>Stream: Write Tj operator

    User->>Page: EndText()
    Page->>Stream: Write ET

    User->>Page: Rectangle(50, 50, 200, 100)
    Page->>Stream: Write re operator

    User->>Page: Stroke()
    Page->>Stream: Write S operator

    User->>Doc: SaveToFile("output.pdf")
    Doc->>Page: Check GSave/GRestore balance, apply compression mode
    Doc->>Xref: Generate Xref Table
    Doc->>File: Write Header
    Doc->>File: Write Objects
    Doc->>File: Write Xref
    Doc->>File: Write Trailer
    File-->>User: PDF Created
```

---

## Type System

### Geometric Types

```csharp
// 2D Point
public struct HpdfPoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

// Rectangle by its edges (left, bottom, right, top)
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

// Bounding Box (left, bottom, right, top)
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

// 2D Transformation Matrix
public struct HpdfTransMatrix
{
    public float A { get; set; }  // Horizontal scaling
    public float B { get; set; }  // Vertical skewing
    public float C { get; set; }  // Horizontal skewing
    public float D { get; set; }  // Vertical scaling
    public float X { get; set; }  // Horizontal translation
    public float Y { get; set; }  // Vertical translation
}
```

### Color Types

```csharp
// RGB Color (0.0 to 1.0)
public struct HpdfRgbColor
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
}

// CMYK Color (0.0 to 1.0)
public struct HpdfCmykColor
{
    public float C { get; set; }
    public float M { get; set; }
    public float Y { get; set; }
    public float K { get; set; }
}
```

### Page Types

```csharp
// Standard Page Sizes
public enum HpdfPageSize
{
    Letter,      // 612 x 792 points
    Legal,       // 612 x 1008 points
    A3,          // 842 x 1191 points
    A4,          // 595 x 842 points
    A5,          // 420 x 595 points
    B4,          // 709 x 1001 points
    B5,          // 499 x 709 points
    // ... more sizes
}

// Page Orientation
public enum HpdfPageDirection
{
    Portrait,
    Landscape
}
```

---

## Stream Processing

### Compression Pipeline

```mermaid
graph LR
    INPUT[Content Data] --> MEMORY[HpdfMemoryStream]
    MEMORY --> CHECK{Compression<br/>Enabled?}
    CHECK -->|Yes| DEFLATE[DeflateStream<br/>Compression]
    CHECK -->|No| RAW[Raw Data]
    DEFLATE --> FILTER[/FlateDecode Filter/]
    RAW --> OUTPUT[PDF Stream Object]
    FILTER --> OUTPUT

    style INPUT fill:#e1f5ff
    style OUTPUT fill:#c8e6c9
    style DEFLATE fill:#fff3e0
```

**Compression Modes:**

```csharp
[Flags]
public enum HpdfCompressionMode : uint
{
    None = 0,
    Text = 1,      // Compress page content streams (of all pages, also those added earlier)
    Image = 2,     // Compatibility value, no effect: image data is always compressed
    Metadata = 4,  // Compatibility value, no effect
    All = 15       // Compress everything
}
```

---

## Encryption Architecture

### Encryption Flow

```mermaid
graph TB
    DOC[HpdfDocument] --> ENCRYPT{SetEncryption<br/>Called?}
    ENCRYPT -->|Yes| MODE{Encryption<br/>Mode}
    ENCRYPT -->|No| NOSEC[No Encryption]

    MODE --> R2[R2 - RC4 40-bit]
    MODE --> R3[R3 - RC4 128-bit]
    MODE --> R4[R4 - AES 128-bit]

    R2 --> DICT[HpdfEncryptDict]
    R3 --> DICT
    R4 --> DICT

    DICT --> PERMS[Permissions]
    DICT --> KEYS[User/Owner Keys]

    PERMS --> PRINT[Print]
    PERMS --> COPY[Copy]
    PERMS --> MODIFY[Edit]
    PERMS --> ANNOT[Edit Annotations]

    style DOC fill:#fff3e0
    style DICT fill:#e1f5ff
    style R4 fill:#c8e6c9
```

**Permission Flags:**

```csharp
[Flags]
public enum HpdfPermission : uint
{
    None = 0,
    Print = 0x4,
    Edit = 0x8,
    Copy = 0x10,
    EditAnnotations = 0x20,
    FillForm = 0x100,
    ExtractAccess = 0x200,
    Assemble = 0x400,
    PrintHighQuality = 0x800,
    All = Print | Edit | Copy | EditAnnotations | FillForm |
          ExtractAccess | Assemble | PrintHighQuality,
    PaddingBits = 0xFFFFF0C0  // Used internally for PDF encryption
}
```

---

## Design Patterns

### Patterns Used in Haru.NET

**1. Composite Pattern** - Document/Pages/Page hierarchy
```csharp
HpdfDocument → HpdfPages → HpdfPage (tree structure)
```

**2. Strategy Pattern** - Font implementations
```csharp
IHpdfFontImplementation → HpdfStandardFontImpl, HpdfTrueTypeFont, etc.
```

**3. Builder Pattern** - Page content construction (each call appends operators to the content stream;
the methods return `void`, so they are called one after another rather than chained)
```csharp
page.BeginText();
page.SetFontAndSize(font, 12);
page.MoveTextPos(100, 700);
page.ShowText("Hello");
page.EndText();
```

**4. Extension Method Pattern** - API organization
```csharp
public static void Stroke(this IDrawable drawable)            // HpdfPageGraphics
public static void ShowText(this HpdfPage page, string text)  // HpdfPageText
```

**5. Facade Pattern** - HpdfFont wraps IHpdfFontImplementation
```csharp
HpdfFont → IHpdfFontImplementation (simplified interface)
```

**6. Registry Pattern** - Xref table manages PDF objects
```csharp
HpdfXref assigns object IDs and tracks every indirect object
```

---

## Porting from C to .NET

### Key Differences from Original C Library

| Aspect | C Library | .NET Port |
|--------|-----------|-----------|
| **Memory Management** | Manual malloc/free | Automatic GC |
| **Strings** | char* pointers | System.String |
| **Collections** | Manual arrays | List<T>, Dictionary<K,V> |
| **Compression** | zlib library | System.IO.Compression |
| **Error Handling** | Error codes | Exceptions |
| **Buffers** | Manual buffer management | Span<T>, Memory<T> |
| **Encoding** | Manual encoding | Encoding.GetEncoding() |

### .NET-Specific Enhancements

- **Properties** - C-style getters/setters converted to properties
- **Extension Methods** - Organized API without monolithic classes
- **Nullable Reference Types** - Compile-time null safety
- **Span<T>** - Efficient buffer operations without allocation
- **async/await** - Future enhancement for I/O operations

---

## Performance Considerations

### Optimization Strategies

**1. Object Reuse**
```csharp
// Good - reuse font across pages
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");
foreach (var page in pages)
{
    page.SetFontAndSize(font, 12);
}
```

**2. Compression**
```csharp
// Enable compression for smaller files
pdf.SetCompressionMode(HpdfCompressionMode.All);
```

**3. Stream Writing**
```csharp
// Content streams write directly to memory
// No intermediate string allocations
```

**4. Font Metric Caching**
```csharp
// Font metrics loaded once and cached
// Fast text measurement operations
```

---

## Extending Haru.NET

### Adding Custom Functionality

**Custom Extension Methods:**

```csharp
public static class MyPageExtensions
{
    public static void DrawGrid(this HpdfPage page,
        float spacing, HpdfRgbColor color)
    {
        page.GSave();
        page.SetRgbStroke(color.R, color.G, color.B);
        page.SetLineWidth(0.5f);

        for (float x = 0; x < page.Width; x += spacing)
        {
            page.MoveTo(x, 0);
            page.LineTo(x, page.Height);
            page.Stroke();
        }

        for (float y = 0; y < page.Height; y += spacing)
        {
            page.MoveTo(0, y);
            page.LineTo(page.Width, y);
            page.Stroke();
        }

        page.GRestore();
    }
}
```

**Custom Font Implementation:**

```csharp
public class MyCustomFont : IHpdfFontImplementation
{
    public HpdfDict Dict { get; }
    public string BaseFont { get; }
    public string LocalName { get; }
    // ... implement interface methods
}
```

`HpdfFont` has no public constructor that takes an arbitrary `IHpdfFontImplementation`, so a custom implementation
cannot be passed to `SetFontAndSize` without a change to the library.

---

## Related Documentation

### Core Concepts
- [Quick Start Guide](USAGE.md) - Basic usage patterns
- [Getting Started](guides/GettingStarted.md) - Step-by-step tutorial

### Detailed Guides
- [Forms Guide](guides/FormsGuide.md) - Interactive form fields
- [Fonts](guides/FontsGuide.md), [Graphics](guides/GraphicsGuide.md) and [Text](guides/TextGuide.md) guides

### API Reference
- [HpdfDocument](api/core/HpdfDocument.md) - Document class reference
- [HpdfPage](api/core/HpdfPage.md) - Page class reference
- [HpdfFont](api/core/HpdfFont.md) - Font class reference
- [HpdfImage](api/core/HpdfImage.md) - Image class reference

---

[← Back to Documentation Index](INDEX.md)

*Last updated: 2026-11-26*
