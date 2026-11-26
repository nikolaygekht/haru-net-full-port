# HpdfPageMode Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfPageMode` tells the viewer which panel to show, or whether to go full screen, when the document is opened.
It is written as the `/PageMode` entry of the document catalog.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `UseNone` | 0 | Neither the outline nor the thumbnails panel is shown. Writes `/PageMode /UseNone`. This is the default (`HpdfConst.DefPageMode`) and is also what the `PageMode` getter returns when no page mode is set. |
| `UseOutlines` | 1 | Show the document outline (bookmarks) panel. Writes `/UseOutlines`. |
| `UseThumbs` | 2 | Show the page thumbnails panel. Writes `/UseThumbs`. The library does not embed thumbnail images; the viewer generates them. |
| `FullScreen` | 3 | Full-screen mode, with no menu bar, window controls or other windows visible. Writes `/FullScreen`. |
| `UseOC` | 4 | Show the optional content (layers) panel. Writes `/UseOC`. Requires **PDF 1.5**: setting it raises the document version automatically. |
| `UseAttachments` | 5 | Show the attachments panel. Writes `/UseAttachments`. Requires **PDF 1.6**: setting it raises the document version automatically. |
| `Eof` | 6 | Sentinel kept from the C library. **Not a valid argument:** it is written as `/UseNone`. |

The version is raised through the document's required version (see `HpdfDocument.Version`), so it never goes down
and setting a lower `Version` afterwards has no effect.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.AddPage();
pdf.CreateOutline("Chapter 1");

// Open the document with the bookmarks panel visible
pdf.Catalog.PageMode = HpdfPageMode.UseOutlines;

pdf.SaveToFile("pagemode.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfCatalog.PageMode { get; set; }` | Gets or sets `/PageMode` of the catalog (`pdf.Catalog.PageMode`). See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfCatalog.SetPageMode(HpdfPageMode mode)` | Same as setting `PageMode` (kept for compatibility with the C API). |
| `HpdfConst.DefPageMode` | The default page mode, `UseNone`. See [HpdfConst](HpdfConst.md). |

## Related Types

- [HpdfPageLayout](HpdfPageLayout.md) - how pages are arranged when the document is opened
- [HpdfViewerPreferences](HpdfViewerPreferences.md) - viewer window options
- [HpdfDocument](../core/HpdfDocument.md) - `Catalog` property

---

[Back to Index](../../INDEX.md)
