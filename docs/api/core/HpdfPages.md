# HpdfPages Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfPages` is a node of the page tree (`/Type /Pages`): the structure through which a PDF file lists its pages.
Every document has a root node that holds all pages; intermediate nodes group pages, for example by chapter. Viewers
show the pages in the order of the tree. Most documents need nothing but the root node, which `HpdfDocument.AddPage`
uses automatically.

---

## Getting an Instance

| Member of HpdfDocument | Description |
|------------------------|-------------|
| `RootPages` | The root node of the page tree, created with the document and referenced by the catalog's `/Pages` |
| `CurrentPages` | The node that `AddPage` adds new pages to; initially `RootPages` |
| `InsertPagesNode(HpdfPages? parent = null)` | Creates a node as the last child of `parent` (default: `CurrentPages`) and makes it the current node |

**Declarations (HpdfDocument):**
```csharp
public HpdfPages RootPages { get; }
public HpdfPages CurrentPages { get; set; }
public HpdfPages InsertPagesNode(HpdfPages? parent = null)
```

Assigning `null` to `CurrentPages` selects `RootPages`. After `InsertPagesNode`, the pages added with `AddPage` go
into the new node until you assign another node to `CurrentPages` or insert another node.

The class also has a public constructor, `HpdfPages(HpdfXref xref, HpdfPages? parent = null)`, which adds the new
node to the cross-reference table and, when `parent` is given, as the last child of `parent`. `InsertPagesNode` uses
it; prefer `InsertPagesNode`, which also selects the node for `AddPage`. A `null` `xref` throws `HpdfException` with
`HpdfErrorCode.InvalidParameter`.

---

## Example

```csharp
using Haru.Doc;

var pdf = new HpdfDocument();
var font = pdf.GetFont("Helvetica");

void AddPages(string chapter, int count)
{
    for (int i = 1; i <= count; i++)
    {
        var page = pdf.AddPage();
        page.SetFontAndSize(font, 14);
        page.TextOut(50, 750, $"{chapter}, page {i}");
    }
}

// One page tree node per chapter, both children of the root
var chapter1 = pdf.InsertPagesNode(pdf.RootPages);
AddPages("Chapter 1", 3);

var chapter2 = pdf.InsertPagesNode(pdf.RootPages);
AddPages("Chapter 2", 2);

Console.WriteLine(chapter1.Count);       // 3
Console.WriteLine(chapter2.Count);       // 2
Console.WriteLine(pdf.RootPages.Count);  // 5

pdf.SaveToFile("page-tree.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying page tree node dictionary |
| [Parent](#property-parent) | `HpdfPages?` | The parent node, or `null` for the root |
| [Count](#property-count) | `int` | The number of pages in this node and all its descendants |

---

## Methods

| Name | Description |
|------|-------------|
| [AddKid()](#method-addkid) | Appends a page or a node to this node |
| [GetChildren()](#method-getchildren) | Returns the direct children (pages and nodes) |
| [GetAllPages()](#method-getallpages) | Returns all pages under this node, in tree order |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The dictionary written to the file: `/Type /Pages`, `/Kids` (the children), `/Count`, and `/Parent` for every node
except the root.

---

#### Property: *Parent*

**Declaration:**
```csharp
public HpdfPages? Parent { get; }
```

**Description:**
The node this node was added to, or `null` for the root and for a node that has not been added to another node.

---

#### Property: *Count*

**Declaration:**
```csharp
public int Count { get; }
```

**Description:**
The number of pages (leaves) in this node and all its descendants, as written to `/Count`. It is updated in this node
and all its ancestors whenever a page or a node is added. `pdf.RootPages.Count` is the number of pages in the
document.

---

#### Method: *AddKid()*

**Declaration:**
```csharp
public void AddKid(object kid)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| kid | `object` | An [HpdfPage](HpdfPage.md) or an `HpdfPages` node |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `kid` is `null`, or neither an `HpdfPage` nor an `HpdfPages` |
| `HpdfException` (`PageCannotSetParent`) | `kid` already belongs to another node |
| `ArgumentException` | `kid` has already been added to this node |

**Description:**
Appends the kid to `/Kids`, sets its `/Parent` and adds its page count to this node and all ancestors. The document
calls this method from `AddPage` and from the `HpdfPages` constructor; you rarely need it yourself.

A page added with `AddKid` instead of `HpdfDocument.AddPage` is written to the file, but it is not listed in
`HpdfDocument.Pages` and does not follow `HpdfDocument.CompressionMode`. Do not add a node to itself or to one of its
descendants: this is not checked and leads to endless recursion.

---

#### Method: *GetChildren()*

**Declaration:**
```csharp
public IReadOnlyList<object> GetChildren()
```

**Return Value:**
A read-only view of the direct children in order; each element is an `HpdfPage` or an `HpdfPages`.

**Example:**
```csharp
foreach (var child in pdf.RootPages.GetChildren())
{
    if (child is HpdfPages node)
        Console.WriteLine($"Node with {node.Count} pages");
    else if (child is HpdfPage)
        Console.WriteLine("Page");
}
```

---

#### Method: *GetAllPages()*

**Declaration:**
```csharp
public List<HpdfPage> GetAllPages()
```

**Return Value:**
A new list of all pages under this node, depth first, in the order a viewer shows them.

**Example:**
```csharp
var pagesInViewerOrder = pdf.RootPages.GetAllPages();
Console.WriteLine(pagesInViewerOrder.Count == pdf.PageCount);   // True when every page was added with AddPage
```

---

## Remarks

### Page order

Viewers show the pages in tree order: the children of a node in the order they were added, with each node's pages
at the node's position. `HpdfDocument.Pages` lists pages in the order they were created instead. The two orders
differ when you add pages to a node that already has a later sibling:

```csharp
var doc = new HpdfDocument();
var a = doc.AddPage();                 // root: a
var node = doc.InsertPagesNode();      // root: a, node
var b = doc.AddPage();                 // node: b
doc.CurrentPages = doc.RootPages;
var c = doc.AddPage();                 // root: a, node, c
doc.CurrentPages = node;
var d = doc.AddPage();                 // node: b, d

// doc.Pages:                  a, b, c, d
// doc.RootPages.GetAllPages(): a, b, d, c  (the order in the viewer)
Console.WriteLine(doc.RootPages.GetAllPages()[3] == c);   // True
```

Page indexes in `HpdfDocument.AddPageLabel` ([HpdfPageLabel](HpdfPageLabel.md)) refer to the viewer order.

### Inherited attributes

The library writes page attributes such as `/MediaBox` and `/Resources` on each page, not on page tree nodes.

---

## Related Types

- [HpdfDocument](HpdfDocument.md) - `RootPages`, `CurrentPages`, `InsertPagesNode()`, `AddPage()`
- [HpdfPage](HpdfPage.md) - The pages (leaves) of the tree
- [HpdfCatalog](HpdfCatalog.md) - References the root node as `/Pages`

---

[Back to Index](../../INDEX.md)
