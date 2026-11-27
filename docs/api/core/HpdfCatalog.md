# HpdfCatalog Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfCatalog` is the document catalog (`/Type /Catalog`), the root object of a PDF file. Through it you control how a
viewer opens the document: the page layout, the panel shown next to the pages (bookmarks, thumbnails, full screen),
the viewer window preferences and the page or view shown first. It also holds references to document-level
structures such as the page tree, page labels and the interactive form.

---

## Getting an Instance

Every document creates its catalog in its constructor. Use the `Catalog` property of the document:

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
HpdfCatalog catalog = pdf.Catalog;
```

The class also has a public constructor, `HpdfCatalog(HpdfXref xref, HpdfPages rootPages)`, which the document uses
internally. Do not call it yourself: a catalog created this way is added to the cross-reference table but is not the
root of the document (the trailer refers to `pdf.Catalog`), so its settings have no effect. A `null` argument throws
`HpdfException` with `HpdfErrorCode.InvalidParameter`.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var cover = pdf.AddPage();
var contents = pdf.AddPage();

// The second page, fitted to the window
var start = contents.CreateDestination();
start.SetFit();

// Show pages two by two, with the bookmarks panel open
pdf.Catalog.PageLayout = HpdfPageLayout.TwoColumnLeft;
pdf.Catalog.PageMode = HpdfPageMode.UseOutlines;
pdf.CreateOutline("Contents").SetDestination(start);

// Center the viewer window and open the document at the second page
pdf.Catalog.ViewerPreferences = HpdfViewerPreferences.CenterWindow | HpdfViewerPreferences.FitWindow;
pdf.Catalog.SetOpenAction(start);

pdf.SaveToFile("catalog.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying catalog dictionary |
| [PageLayout](#property-pagelayout) | [HpdfPageLayout](../types/HpdfPageLayout.md) | How pages are arranged when the document is opened (`/PageLayout`) |
| [PageMode](#property-pagemode) | [HpdfPageMode](../types/HpdfPageMode.md) | Which panel is shown when the document is opened (`/PageMode`) |
| [ViewerPreferences](#property-viewerpreferences) | [HpdfViewerPreferences](../types/HpdfViewerPreferences.md) | How the viewer presents the document window (`/ViewerPreferences`) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetPageMode()](#method-setpagemode) | Sets `PageMode` (method form, for compatibility with the C library) |
| [SetOpenAction()](#method-setopenaction) | Sets or removes the destination shown when the document is opened |
| [GetOpenAction()](#method-getopenaction) | Returns the destination array of the open action, or `null` |
| [GetRootPages()](#method-getrootpages) | Returns the root page tree dictionary |
| [SetNames()](#method-setnames) | Sets the name dictionary (`/Names`) |
| [GetNames()](#method-getnames) | Returns the name dictionary, or `null` |
| [SetPageLabels()](#method-setpagelabels) | Sets the page labels dictionary (`/PageLabels`) |
| [GetPageLabels()](#method-getpagelabels) | Returns the page labels dictionary, or `null` |
| [SetAcroForm()](#method-setacroform) | Sets the interactive form dictionary (`/AcroForm`) |
| [GetAcroForm()](#method-getacroform) | Returns the interactive form dictionary, or `null` |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The catalog dictionary that is written to the file. It always contains `/Type /Catalog` and `/Pages` (the root of
the page tree). The document adds `/Outlines` when the first outline is created, and `/PageLabels`, `/AcroForm`,
`/Metadata` and `/OutputIntents` when it is saved with page labels, a form or PDF/A compliance. Use the dictionary
only for entries this class has no member for.

---

#### Property: *PageLayout*

**Declaration:**
```csharp
public HpdfPageLayout PageLayout { get; set; }
```

**Description:**
The arrangement of pages when the document is opened, written as `/PageLayout`:

| Value | Written as | Meaning |
|-------|-----------|---------|
| `SinglePage` | `/SinglePage` | One page at a time |
| `OneColumn` | `/OneColumn` | Pages in one continuous column |
| `TwoColumnLeft` | `/TwoColumnLeft` | Two continuous columns, odd pages on the left |
| `TwoColumnRight` | `/TwoColumnRight` | Two continuous columns, odd pages on the right |
| `TwoPageLeft` | `/TwoPageLeft` | Two pages at a time, odd pages on the left |
| `TwoPageRight` | `/TwoPageRight` | Two pages at a time, odd pages on the right |

A new document has no `/PageLayout` entry; the getter then returns `SinglePage`, which is also the PDF default. The
getter returns `SinglePage` for any value it does not recognize, and the setter writes `/SinglePage` for a value
outside the enumeration.

`TwoPageLeft` and `TwoPageRight` were introduced in PDF 1.5: assigning either raises the document version to at least
1.5 (see `HpdfDocument.Version` in [HpdfDocument](HpdfDocument.md)). The version is never lowered again, even if you
later assign another layout.

**Example:**
```csharp
pdf.Catalog.PageLayout = HpdfPageLayout.TwoPageLeft;   // the file becomes PDF 1.5
Console.WriteLine(pdf.Version);                         // Version15
```

---

#### Property: *PageMode*

**Declaration:**
```csharp
public HpdfPageMode PageMode { get; set; }
```

**Description:**
The panel the viewer shows when the document is opened, written as `/PageMode`:

| Value | Written as | Meaning | Required version |
|-------|-----------|---------|------------------|
| `UseNone` | `/UseNone` | No panel | - |
| `UseOutlines` | `/UseOutlines` | Bookmarks (outlines) panel | - |
| `UseThumbs` | `/UseThumbs` | Page thumbnails panel | - |
| `FullScreen` | `/FullScreen` | Full-screen mode, no menus or panels | - |
| `UseOC` | `/UseOC` | Optional content (layers) panel | 1.5 |
| `UseAttachments` | `/UseAttachments` | Attachments panel | 1.6 |

A new document has no `/PageMode` entry; the getter then returns `UseNone`. The getter returns `UseNone` for a value
it does not recognize, and the setter writes `/UseNone` for `HpdfPageMode.Eof` or any other value outside the list.

Assigning `UseOC` raises the document version to at least 1.5, and `UseAttachments` to at least 1.6. Creating
outlines does not switch the page mode: set `UseOutlines` yourself to open the bookmarks panel.

The library does not create thumbnails or optional content; `UseThumbs` and `UseOC` only select the panel.
`UseAttachments` opens the panel that lists the files attached with `HpdfDocument.AttachFile()`.

**Example:**
```csharp
pdf.CreateOutline("Chapter 1");
pdf.Catalog.PageMode = HpdfPageMode.UseOutlines;
```

---

#### Property: *ViewerPreferences*

**Declaration:**
```csharp
public HpdfViewerPreferences ViewerPreferences { get; set; }
```

**Description:**
Flags that control the viewer window, written as the `/ViewerPreferences` dictionary. Each assignment replaces the
whole dictionary: each flag that is set is written as `true` (`/HideToolbar`, `/HideMenubar`, `/HideWindowUI`,
`/FitWindow`, `/CenterWindow`), and `PrintScalingNone` is written as `/PrintScaling /None`. Assigning
`HpdfViewerPreferences.None` removes the dictionary.

The getter reads the dictionary back and returns `None` when there is none. `PrintScalingNone` requires PDF 1.6:
assigning a value that contains it raises the document version to at least 1.6. See
[HpdfViewerPreferences](../types/HpdfViewerPreferences.md) for the flags.

**Example:**
```csharp
pdf.Catalog.ViewerPreferences = HpdfViewerPreferences.HideToolbar | HpdfViewerPreferences.PrintScalingNone;

// Add a flag to the current ones
pdf.Catalog.ViewerPreferences |= HpdfViewerPreferences.CenterWindow;
```

---

#### Method: *SetPageMode()*

**Declaration:**
```csharp
public void SetPageMode(HpdfPageMode mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| mode | [HpdfPageMode](../types/HpdfPageMode.md) | The page mode |

**Return Value:**
`void`

**Description:**
Same as assigning [PageMode](#property-pagemode), including the version requirements. It exists for compatibility
with `HPDF_SetPageMode` of the C library.

**Example:**
```csharp
pdf.Catalog.SetPageMode(HpdfPageMode.UseThumbs);
```

---

#### Method: *SetOpenAction()*

**Declaration:**
```csharp
public void SetOpenAction(HpdfDestination? destination)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| destination | [HpdfDestination](HpdfDestination.md) | The destination to show when the document is opened, with its view already set; `null` removes the open action |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidDestination`) | The destination has no view: none of `SetXYZ`, `SetFit`, `SetFitH`, `SetFitV`, `SetFitR`, `SetFitB`, `SetFitBH` or `SetFitBV` has been called |

**Description:**
Writes the destination array as `/OpenAction`, so the viewer opens the document at that page and view instead of the
first page. The array is stored by reference: calling another `Set...` method on the same destination later changes
the open action too. The method does not check that the destination's page belongs to this document.

**Example:**
```csharp
var last = pdf.Pages[pdf.PageCount - 1];
var start = last.CreateDestination();
start.SetXYZ(0, last.Height, 1.5f);   // top-left corner, 150 %
pdf.Catalog.SetOpenAction(start);

// Open at the first page again
pdf.Catalog.SetOpenAction(null);
```

---

#### Method: *GetOpenAction()*

**Declaration:**
```csharp
public HpdfArray? GetOpenAction()
```

**Return Value:**
The destination array set with [SetOpenAction()](#method-setopenaction) (the same object as
`HpdfDestination.DestArray`), or `null` when there is no open action.

**Description:**
Reads the `/OpenAction` entry. If the entry is not an array, the method returns `null`.

**Example:**
```csharp
bool hasOpenAction = pdf.Catalog.GetOpenAction() is not null;
```

---

#### Method: *GetRootPages()*

**Declaration:**
```csharp
public HpdfDict? GetRootPages()
```

**Return Value:**
The dictionary of the root page tree node (`/Pages`); never `null` for the catalog of a document.

**Description:**
Returns the same dictionary as `pdf.RootPages.Dict`. To work with the page tree, use
[HpdfPages](HpdfPages.md) through `pdf.RootPages` instead.

---

#### Method: *SetNames()*

**Declaration:**
```csharp
public void SetNames(HpdfDict namesDict)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| namesDict | `HpdfDict` | The name dictionary to write as `/Names` |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `namesDict` is `null` |

**Description:**
Adds a name dictionary you have built yourself, for example with named destinations or document JavaScript, for
which the library has no methods. Calling the method again replaces the previous dictionary.

The library writes one name tree itself: the `EmbeddedFiles` tree of the files attached with
`HpdfDocument.AttachFile()`. When the document is saved, it is added to the dictionary set here (an `EmbeddedFiles`
entry of your own is replaced), or, without one, to a name dictionary the document creates.

---

#### Method: *GetNames()*

**Declaration:**
```csharp
public HpdfDict? GetNames()
```

**Return Value:**
The `/Names` dictionary, or `null` if none has been set.

---

#### Method: *SetPageLabels()*

**Declaration:**
```csharp
public void SetPageLabels(HpdfDict pageLabelsDict)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| pageLabelsDict | `HpdfDict` | The page labels number tree (a dictionary with a `/Nums` array) |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `pageLabelsDict` is `null` |

**Description:**
Sets or replaces `/PageLabels`. Use `HpdfDocument.AddPageLabel` instead (see [HpdfPageLabel](HpdfPageLabel.md)): it
builds this dictionary when the document is saved and raises the version to 1.3. If any label was added with
`AddPageLabel`, saving replaces a dictionary set with this method.

---

#### Method: *GetPageLabels()*

**Declaration:**
```csharp
public HpdfDict? GetPageLabels()
```

**Return Value:**
The `/PageLabels` dictionary, or `null` if none has been set. Labels added with `HpdfDocument.AddPageLabel` appear
here only after the document has been saved.

---

#### Method: *SetAcroForm()*

**Declaration:**
```csharp
public void SetAcroForm(HpdfDict acroForm)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| acroForm | `HpdfDict` | The interactive form dictionary |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `acroForm` is `null` |

**Description:**
Sets or replaces `/AcroForm`. Use `HpdfDocument.GetOrCreateAcroForm()` instead (see
[HpdfAcroForm](../forms/HpdfAcroForm.md)): the document then writes the form dictionary when it is saved and raises
the version to 1.5, replacing a dictionary set with this method. This method itself does not change the version.

---

#### Method: *GetAcroForm()*

**Declaration:**
```csharp
public HpdfDict? GetAcroForm()
```

**Return Value:**
The `/AcroForm` dictionary, or `null` if none has been set. A form created with `GetOrCreateAcroForm()` appears here
only after the document has been saved; use `HpdfDocument.GetAcroForm()` to get the form object itself.

---

## Remarks

- **Version raising.** `TwoPageLeft`/`TwoPageRight` layouts and the `UseOC` page mode need PDF 1.5; the
  `UseAttachments` page mode and `PrintScalingNone` need PDF 1.6. The catalog calls `RequireVersion` on the
  cross-reference table, so the effective `HpdfDocument.Version` is raised and never lowered.
- **Saving more than once.** The catalog entries that the document writes on saving (`/PageLabels`, `/AcroForm`) are
  assigned, not added, so a document can be saved several times.

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - Owns the catalog (`pdf.Catalog`)
- [HpdfDestination](HpdfDestination.md) - Destinations for `SetOpenAction`
- [HpdfOutline](HpdfOutline.md) - Bookmarks shown with `HpdfPageMode.UseOutlines`
- [HpdfPageLabel](HpdfPageLabel.md) - Page labels written as `/PageLabels`
- [HpdfPages](HpdfPages.md) - The page tree referenced by `/Pages`
- [HpdfPageLayout](../types/HpdfPageLayout.md), [HpdfPageMode](../types/HpdfPageMode.md),
  [HpdfViewerPreferences](../types/HpdfViewerPreferences.md) - Values of the properties

---

[Back to Index](../../INDEX.md)
