# HpdfContentStream Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfContentStream` is a content stream of a page. A page draws into one stream at a time; with
`HpdfPage.NewContentStream()` (`HPDF_Page_New_Content_Stream`) it continues in a new one, and a finished stream can
be shown on other pages too with `HpdfPage.InsertSharedContentStream()` (`HPDF_Page_Insert_Shared_Content_Stream`),
for example a footer drawn once. The stream remembers the resources it uses and whether it is self-contained enough
to be shared.

---

## Getting an Instance

| Member | Gives |
|--------|-------|
| `HpdfPage.NewContentStream()` | The new stream the page draws into from now on |
| `HpdfPage.CurrentContentStream` | The stream the page draws into now |
| `HpdfPage.ContentStreams` | The streams of the page in the order of `/Contents`, its own and the shared ones inserted |

A page has one stream at first, and `/Contents` is that stream; with the second one `/Contents` becomes an array.

**Sharing.** `InsertSharedContentStream(stream)` adds a finished stream of this or another page to the page's
`/Contents`:

- the resources the stream uses (fonts, images, ...) are added to the page, so its text and images show;
- the stream is enclosed in `q` ... `Q`, so its changes of the graphics state do not reach what follows;
- the page continues in a new stream.

The shared content inherits the state of the place where it is inserted (colors, transformation, line width it does
not set itself), but **not the font**: text in a shared stream must use a font set inside the stream.

**What can be shared** ([IsShareable](#property-isshareable)): a stream that is finished (its page went on to a new
stream), starts and ends outside text objects and paths, has balanced `GSave`/`GRestore` calls that never restore a
state saved before the stream, and shows text only with a font set inside it. Inserting another stream throws
`HpdfException` (`InvalidOperation`) that says why. The streams around an insertion (ending with `q`, starting with
`Q`) cannot be shared themselves.

---

## Example

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

var first = pdf.AddPage();
first.SetFontAndSize(font, 12);
first.TextOut(50, 750, "Page 1");

// The footer in a stream of its own, self-contained
var footer = first.NewContentStream();
first.GSave();
first.SetFontAndSize(font, 9);
first.TextOut(50, 30, "Example Inc. - confidential");
first.GRestore();
first.NewContentStream();                         // the footer stream is finished

for (int i = 2; i <= 3; i++)
    pdf.AddPage().InsertSharedContentStream(footer);

pdf.SaveToFile("footer.pdf");
```

The footer sets its font inside its stream: the font of page 1 set before it would not reach the other pages.

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Page](#property-page) | `HpdfPage` | The page the stream was drawn on |
| [StreamObject](#property-streamobject) | `HpdfStreamObject` | The stream object |
| [IsFinished](#property-isfinished) | `bool` | Whether drawing into the stream has ended |
| [IsShareable](#property-isshareable) | `bool` | Whether the stream can be inserted on other pages |

---

## Detailed Descriptions

#### Property: *Page*

**Declaration:**
```csharp
public HpdfPage Page { get; }
```

**Description:**
The page the stream was drawn on. Its compression follows that page; the pages it is inserted into only refer to it.

---

#### Property: *StreamObject*

**Declaration:**
```csharp
public HpdfStreamObject StreamObject { get; }
```

**Description:**
The stream object, an indirect object written once and listed in the `/Contents` of every page that shows it.

---

#### Property: *IsFinished*

**Declaration:**
```csharp
public bool IsFinished { get; }
```

**Description:**
Whether drawing into the stream has ended: its page called `NewContentStream` (or inserted a shared stream) after
it. Only a finished stream can be shared.

---

#### Property: *IsShareable*

**Declaration:**
```csharp
public bool IsShareable { get; }
```

**Description:**
Whether the finished stream can be shown on other pages: see "What can be shared" above.

---

## Related Types

- [HpdfPage](HpdfPage.md) - `NewContentStream()`, `InsertSharedContentStream()`, `CurrentContentStream`, `ContentStreams`
- [HpdfFormXObject](HpdfFormXObject.md) - form XObjects, which can also be placed and scaled
- [Graphics Guide](../../guides/GraphicsGuide.md) - "Reusing Content"

---

[Back to Index](../../INDEX.md)
