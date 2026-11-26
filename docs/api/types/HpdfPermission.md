# HpdfPermission Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfPermission` is a set of flags (`[Flags]`, underlying type `uint`) that controls what a user who opens an
encrypted document with the **user** password may do. Someone who opens it with the **owner** password has all
permissions. The flags are combined with `|` and passed to `HpdfDocument.SetEncryption`; they are written as the
`/P` entry of the encryption dictionary and are part of the encryption key computation.

The permissions are enforced by the PDF viewer, not by the encryption itself.

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | `0x0000` | No permissions: the document can only be viewed. |
| `Print` | `0x0004` | Print the document. With revision 3 or later (see [HpdfEncryptMode](HpdfEncryptMode.md)) printing may be limited to low quality unless `PrintHighQuality` is also set. The default of `SetEncryption`. |
| `Edit` | `0x0008` | Modify the contents of the document (other than annotations and form fields). |
| `Copy` | `0x0010` | Copy or otherwise extract text and graphics. |
| `EditAnnotations` | `0x0020` | Add or modify text annotations and fill in or modify interactive form fields. |
| `FillForm` | `0x0100` | Revision 3+: fill in existing form fields (including signature fields), even if `EditAnnotations` is not set. |
| `ExtractAccess` | `0x0200` | Revision 3+: extract text and graphics for accessibility (for example, screen readers). |
| `Assemble` | `0x0400` | Revision 3+: assemble the document: insert, rotate or delete pages and create bookmarks or thumbnail images. |
| `PrintHighQuality` | `0x0800` | Revision 3+: print at full quality. |
| `All` | `0x0F3C` | All of the flags above. |
| `PaddingBits` | `0xFFFFF0C0` | The reserved bits that the PDF specification requires to be set. **Do not pass it:** the library always adds these bits to the permissions it writes. |

"Revision 3+" flags have no meaning for documents encrypted with `HpdfEncryptMode.R2`.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.AddPage();

// Users may print at full quality and fill in forms, but not copy or edit
var permissions = HpdfPermission.Print
                | HpdfPermission.PrintHighQuality
                | HpdfPermission.FillForm;

pdf.SetEncryption("user", "owner", permissions, HpdfEncryptMode.R3);
pdf.SaveToFile("permissions.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfDocument.SetEncryption(string userPassword, string ownerPassword, HpdfPermission permission = HpdfPermission.Print, HpdfEncryptMode mode = HpdfEncryptMode.R3)` | Encrypts the document with the given user permissions. See [HpdfDocument](../core/HpdfDocument.md). |
| `HpdfEncryptDict.SetPermission(HpdfPermission permission)` | Lower-level: sets the permissions of an encryption dictionary, adding `PaddingBits`. |
| `HpdfEncrypt.Permission { get; set; }` | Lower-level (namespace `Haru.Security`): the permission value used by the encryption engine, including the padding bits. |

[HpdfConst](HpdfConst.md) has older integer constants from the C library (`EnablePrint`, `EnableEditAll`,
`EnableCopy`, `EnableEdit`) with the values of `Print`, `Edit`, `Copy` and `EditAnnotations`.

## Related Types

- [HpdfEncryptMode](HpdfEncryptMode.md) - encryption algorithm and revision
- [HpdfDocument](../core/HpdfDocument.md) - `SetEncryption`

---

[Back to Index](../../INDEX.md)
