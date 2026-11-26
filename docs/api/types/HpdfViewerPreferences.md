# HpdfViewerPreferences Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfViewerPreferences` is a set of flags (`[Flags]`, combine them with `|`) that tells the viewer how to present
the document window: hide tool bars or menus, fit or center the window, or preselect "actual size" in the print
dialog. It is written as the `/ViewerPreferences` dictionary of the document catalog.

The numeric values match the `HPDF_HIDE_TOOLBAR` ... `HPDF_PRINT_SCALING_NONE` constants of the C library and the
corresponding constants of [HpdfConst](HpdfConst.md).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No preferences; the viewer uses its defaults. Assigning `None` removes the `/ViewerPreferences` dictionary. |
| `HideToolbar` | 1 | Hide the viewer's tool bars while the document is active. Writes `/HideToolbar true`. |
| `HideMenubar` | 2 | Hide the viewer's menu bar while the document is active. Writes `/HideMenubar true`. |
| `HideWindowUI` | 4 | Hide user interface elements in the document window (scroll bars, navigation controls), leaving only the contents. Writes `/HideWindowUI true`. |
| `FitWindow` | 8 | Resize the document window to fit the first displayed page. Writes `/FitWindow true`. |
| `CenterWindow` | 16 | Position the document window in the center of the screen. Writes `/CenterWindow true`. |
| `PrintScalingNone` | 32 | Preselect "no scaling" (actual size) in the print dialog. Writes `/PrintScaling /None`. Requires **PDF 1.6**: setting it raises the document version automatically. |

Each assignment replaces the whole dictionary: flags that are not set are not written. The getter reads the
dictionary back and returns `None` when it is missing. The version is raised through the document's required
version (see `HpdfDocument.Version`), so it never goes down.

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
pdf.AddPage();

pdf.Catalog.ViewerPreferences = HpdfViewerPreferences.FitWindow
                              | HpdfViewerPreferences.CenterWindow
                              | HpdfViewerPreferences.PrintScalingNone;

bool centered = (pdf.Catalog.ViewerPreferences & HpdfViewerPreferences.CenterWindow) != 0;
Console.WriteLine($"{centered} {pdf.Version}");  // True Version16

pdf.SaveToFile("viewerprefs.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfCatalog.ViewerPreferences { get; set; }` | Gets or sets the `/ViewerPreferences` dictionary of the catalog (`pdf.Catalog.ViewerPreferences`). See [HpdfDocument](../core/HpdfDocument.md). |

## Related Types

- [HpdfPageLayout](HpdfPageLayout.md) - how pages are arranged when the document is opened
- [HpdfPageMode](HpdfPageMode.md) - which panel is shown when the document is opened
- [HpdfConst](HpdfConst.md) - `HideToolbar` ... `PrintScalingNone` integer constants with the same values

---

[Back to Index](../../INDEX.md)
