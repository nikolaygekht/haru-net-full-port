# HpdfPageLayout Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPageLayout` tells the viewer how to arrange pages when the document is opened: one page at a time, a
continuous column, or two pages side by side. It is written as the `/PageLayout` entry of the document catalog.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `SinglePage` | 0 | Display one page at a time. Writes `/PageLayout /SinglePage`. This is the default (`HpdfConst.DefPageLayout`) and is also what the `PageLayout` getter returns when no layout is set. |
| `OneColumn` | 1 | Display the pages in one continuous column. Writes `/OneColumn`. |
| `TwoColumnLeft` | 2 | Display the pages in two continuous columns, odd-numbered pages on the left. Writes `/TwoColumnLeft`. |
| `TwoColumnRight` | 3 | Display the pages in two continuous columns, odd-numbered pages on the right. Writes `/TwoColumnRight`. |
| `TwoPageLeft` | 4 | Display two pages at a time, odd-numbered pages on the left. Writes `/TwoPageLeft`. Requires **PDF 1.5**: setting it raises the document version automatically. |
| `TwoPageRight` | 5 | Display two pages at a time, odd-numbered pages on the right. Writes `/TwoPageRight`. Requires **PDF 1.5**: setting it raises the document version automatically. |

The version is raised through the document's required version (see `HpdfDocument.Version`), so it never goes down
and setting a lower `Version` afterwards has no effect. A value outside this list is written as `/SinglePage`.

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
pdf.AddPage();
pdf.AddPage();

// Two pages side by side, like an open book
pdf.Catalog.PageLayout = HpdfPageLayout.TwoPageRight;

Console.WriteLine(pdf.Version);  // Version15
pdf.SaveToFile("pagelayout.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfCatalog.PageLayout { get; set; }` | Gets or sets `/PageLayout` of the catalog (`pdf.Catalog.PageLayout`). See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfConst.DefPageLayout` | The default layout, `SinglePage`. See [HpdfConst](HpdfConst.md). |

## Related Types

- [HpdfPageMode](HpdfPageMode.md) - which panel is shown when the document is opened
- [HpdfViewerPreferences](HpdfViewerPreferences.md) - viewer window options
- [HpdfDocument](../core/HpdfDocument.md) - `Catalog` property

---

[Back to Index](../../INDEX.md)
