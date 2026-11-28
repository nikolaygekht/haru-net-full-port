using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Haru.Encoding;
using Haru.Font;
using Haru.Forms;
using Haru.Annotations;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// PDF version specification.
    /// </summary>
    public enum HpdfVersion
    {
        /// <summary>
        /// PDF version 1.2
        /// </summary>
        Version12 = 0,

        /// <summary>
        /// PDF version 1.3
        /// </summary>
        Version13,

        /// <summary>
        /// PDF version 1.4
        /// </summary>
        Version14,

        /// <summary>
        /// PDF version 1.5
        /// </summary>
        Version15,

        /// <summary>
        /// PDF version 1.6
        /// </summary>
        Version16,

        /// <summary>
        /// PDF version 1.7
        /// </summary>
        Version17
    }

    /// <summary>
    /// Main entry point for creating PDF documents.
    /// This class manages the document catalog, page tree, and all document-level structures.
    /// </summary>
    public class HpdfDocument
    {
        private static readonly string[] VersionHeaders = new[]
        {
            "%PDF-1.2\n%\u00b7\u00be\u00ad\u00aa\n", // Version 1.2
            "%PDF-1.3\n%\u00b7\u00be\u00ad\u00aa\n", // Version 1.3
            "%PDF-1.4\n%\u00b7\u00be\u00ad\u00aa\n", // Version 1.4
            "%PDF-1.5\n%\u00b7\u00be\u00ad\u00aa\n", // Version 1.5
            "%PDF-1.6\n%\u00b7\u00be\u00ad\u00aa\n", // Version 1.6
            "%PDF-1.7\n%\u00b7\u00be\u00ad\u00aa\n"  // Version 1.7
        };

        private readonly HpdfXref _xref;
        private readonly HpdfCatalog _catalog;
        private readonly HpdfPages _rootPages;
        private readonly HpdfInfo _info;
        /// <summary>
        /// Prefix of the resource names the document generates for fonts (GetFont) and images (Load...Image):
        /// names chosen by the user should not start with it, or they could clash on a page.
        /// </summary>
        public const string GeneratedNamePrefix = "Haru";

        private IReadOnlyList<HpdfPage> _pageList = System.Array.Empty<HpdfPage>();
        private int _pageListRevision = -1;
        private HpdfPages _currentPages = null!;
        private HpdfPage? _currentPage;
        private int _pagesPerNode;
        private int _pagesInNode;
        private HpdfVersion _version;
        private HpdfOutline? _outlineRoot;
        private bool _pdfACompliant;
        private string? _pdfAConformance;
        private HpdfIccProfile? _pdfAOutputProfile;
        private string? _pdfAOutputCondition;
        private HpdfOutputIntent? _outputIntent;
        private HpdfArray? _outputIntents;
        private HpdfEncryptDict? _encryptDict;
        private readonly Dictionary<int, HpdfPageLabel> _pageLabels;
        private HpdfAcroForm? _acroForm;
        private readonly List<HpdfEmbeddedFile> _attachedFiles = new List<HpdfEmbeddedFile>();
        private readonly HpdfNameDictionary _nameDictionary;
        private readonly List<HpdfNamedDestination> _namedDestinations = new List<HpdfNamedDestination>();
        private readonly List<(string Name, HpdfObject Action)> _javaScripts = new List<(string, HpdfObject)>();
        private HpdfDict? _openJavaScript;
        private HpdfCompressionMode _compressionMode;

        /// <summary>
        /// Gets or sets the PDF version for this document.
        /// The value set acts as a minimum: the effective version is never lower than what
        /// the document's content requires (e.g. transparency and PDF/A-1 need 1.4,
        /// AcroForms 1.5, AES encryption 1.6).
        /// </summary>
        public HpdfVersion Version
        {
            get => _version > _xref.RequiredVersion ? _version : _xref.RequiredVersion;
            set => _version = value;
        }

        /// <summary>
        /// Gets the document catalog.
        /// </summary>
        public HpdfCatalog Catalog => _catalog;

        /// <summary>
        /// Gets the root pages object.
        /// </summary>
        public HpdfPages RootPages => _rootPages;

        /// <summary>
        /// Gets the current pages object (for adding new pages).
        /// </summary>
        public HpdfPages CurrentPages
        {
            get => _currentPages;
            set => _currentPages = value ?? _rootPages;
        }

        /// <summary>
        /// Gets the current page.
        /// </summary>
        public HpdfPage? CurrentPage => _currentPage;

        /// <summary>
        /// Gets all pages of the document in page tree order: the order in which viewers show them
        /// and in which page label indexes count (pages added under another pages node with
        /// <see cref="InsertPagesNode"/> or <see cref="HpdfPages.AddKid"/> appear where the tree puts them).
        /// </summary>
        public IReadOnlyList<HpdfPage> Pages
        {
            get
            {
                if (_pageListRevision != _rootPages.Revision)
                {
                    _pageList = _rootPages.GetAllPages().AsReadOnly();
                    _pageListRevision = _rootPages.Revision;
                }
                return _pageList;
            }
        }

        /// <summary>
        /// Gets the document information dictionary.
        /// </summary>
        public HpdfInfo Info => _info;

        /// <summary>
        /// Gets the font registry for tracking loaded fonts.
        /// </summary>
        public Dictionary<string, HpdfFont> FontRegistry { get; } = new();

        /// <summary>
        /// Registers a custom font (TrueType, Type1, or CID) so it can be retrieved later via GetFont().
        /// This method allows dynamically loaded fonts to be cached and reused.
        /// </summary>
        /// <param name="font">The font to register.</param>
        /// <param name="encoding">Optional encoding name (e.g., "UTF-8", "CP1252", "ISO8859-2", "90ms-RKSJ-V"),
        /// as for GetFont(). If null, the font's own encoding is used: its predefined CMap, else its code page
        /// and writing mode.</param>
        /// <remarks>
        /// The font is registered under its BaseFont name and its encoding. Fonts of one name with different
        /// encodings are registered side by side, also when the encodings share a code page ("90ms-RKSJ-H"
        /// and "90ms-RKSJ-V", or an embedded CID font loaded for horizontal and for vertical writing).
        /// A font registered again with the same name and encoding replaces the earlier one.
        /// </remarks>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> for an encoding
        /// name the library does not know.</exception>
        public void RegisterFont(HpdfFont font, string? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(font);

            string encodingKey = string.IsNullOrEmpty(encoding)
                ? HpdfEncodingNames.GetRegistryKey(font)
                : HpdfEncodingNames.GetRegistryKey(encoding);

            FontRegistry[GetFontRegistryKey(font.BaseFont, encodingKey)] = font;
        }

        /// <summary>
        /// Gets the key of a font in <see cref="FontRegistry"/>: the font name and the registry key of its
        /// encoding (<see cref="HpdfEncodingNames.GetRegistryKey(string)"/>).
        /// </summary>
        internal static string GetFontRegistryKey(string fontName, string encodingKey)
        {
            return fontName + "#" + encodingKey;
        }

        /// <summary>
        /// Gets the cross-reference table.
        /// </summary>
        public HpdfXref Xref => _xref;

        /// <summary>
        /// Gets the root outline (bookmarks) for the document.
        /// Creates it if it doesn't exist yet.
        /// </summary>
        public HpdfOutline GetOutlineRoot()
        {
            if (_outlineRoot is null)
            {
                _outlineRoot = new HpdfOutline(_xref);
                _catalog.Dict.Add("Outlines", _outlineRoot.Dict);
            }
            return _outlineRoot;
        }

        /// <summary>
        /// Creates a top-level outline entry.
        /// </summary>
        /// <param name="title">The title of the outline entry.</param>
        /// <returns>The newly created outline.</returns>
        public HpdfOutline CreateOutline(string title)
        {
            var root = GetOutlineRoot();
            return root.CreateChild(title);
        }

        /// <summary>
        /// Enables PDF/A compliance for this document.
        /// This adds XMP metadata, Output Intent, and Document ID.
        /// </summary>
        /// <param name="conformance">PDF/A conformance level (e.g., "1B", "2B", "3B"). Default is "1B".</param>
        public void SetPdfACompliance(string conformance = "1B")
        {
            _pdfACompliant = true;
            _pdfAConformance = conformance ?? "1B";

            // PDF/A-1 requires PDF version 1.4
            if (_pdfAConformance.StartsWith("1"))
                _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Enables PDF/A compliance with an output intent of your own (HPDF_PDFA_AppendOutputIntents): the ICC profile
        /// of the device the colors of the document are meant for, such as a CMYK printing condition, instead of the
        /// built-in sRGB profile.
        /// </summary>
        /// <param name="conformance">PDF/A conformance level, such as "1B".</param>
        /// <param name="outputProfile">
        /// A profile of this document of the output device (prtr) or monitor (mntr) class; PDF/A-1 needs a profile of
        /// ICC version 2.
        /// </param>
        /// <param name="outputCondition">
        /// The identifier and description of the output condition, such as "FOGRA39" or "Coated FOGRA39".
        /// </param>
        public void SetPdfACompliance(string conformance, HpdfIccProfile outputProfile, string outputCondition)
        {
            if (outputProfile is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The output profile cannot be null");
            if (string.IsNullOrEmpty(outputCondition))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The output condition cannot be null or empty");
            if (outputProfile.DeviceClass != "prtr" && outputProfile.DeviceClass != "mntr")
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"A PDF/A output intent needs an output device (prtr) or monitor (mntr) profile, not '{outputProfile.DeviceClass}'");
            if ((conformance ?? "1B").StartsWith("1", StringComparison.Ordinal) && !outputProfile.IsPdfA1Compatible)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"PDF/A-1 needs an ICC profile of version 2, the profile is of version {outputProfile.ProfileVersion}");
            outputProfile.UseIn(_xref);

            SetPdfACompliance(conformance ?? "1B");
            _pdfAOutputProfile = outputProfile;
            _pdfAOutputCondition = outputCondition;
        }

        /// <summary>
        /// Gets whether PDF/A compliance is enabled.
        /// </summary>
        public bool IsPdfACompliant => _pdfACompliant;

        /// <summary>
        /// Gets or sets the compression mode for the document.
        /// Controls which types of content will be compressed in the PDF.
        /// </summary>
        public HpdfCompressionMode CompressionMode
        {
            get => _compressionMode;
            set
            {
                _compressionMode = value;

                // Pages added before the mode was set follow it too
                foreach (var page in Pages)
                    page.SetCompressionMode(value);
            }
        }

        /// <summary>
        /// Creates a new PDF document.
        /// </summary>
        public HpdfDocument()
        {
            _version = HpdfVersion.Version12; // Default to PDF 1.2
            _xref = new HpdfXref(0);
            _pageLabels = new Dictionary<int, HpdfPageLabel>();
            _compressionMode = HpdfCompressionMode.None; // Default to no compression

            // Create root pages
            _rootPages = new HpdfPages(_xref);
            _currentPages = _rootPages;

            // Create catalog
            _catalog = new HpdfCatalog(_xref, _rootPages);
            _nameDictionary = new HpdfNameDictionary(_catalog);

            // Create info dictionary
            _info = new HpdfInfo(_xref);

            // Set up trailer
            _xref.Trailer.Add("Root", _catalog.Dict);
            _xref.Trailer.Add("Info", _info.Dict);
        }

        /// <summary>
        /// Adds a new page to the document with default size.
        /// </summary>
        /// <returns>The newly created page.</returns>
        public HpdfPage AddPage()
        {
            var page = new HpdfPage(_xref);

            // Apply document compression mode to the page
            page.SetCompressionMode(_compressionMode);

            // SetPagesConfiguration: a full pages node gets a new sibling under the root
            if (_pagesPerNode > 0 && _pagesInNode >= _pagesPerNode)
            {
                _currentPages = new HpdfPages(_xref, _rootPages);
                _pagesInNode = 0;
            }

            _currentPages.AddKid(page);
            _pagesInNode++;
            _currentPage = page;
            return page;
        }

        /// <summary>
        /// Groups the pages that are added from now on into pages nodes of at most <paramref name="pagesPerNode"/>
        /// pages each, under the root of the page tree (HPDF_SetPagesConfiguration), instead of one flat list: a
        /// viewer finds a page of a large document without reading one huge /Kids array. Call it before the first
        /// page is added. 0 keeps every page in one node.
        /// </summary>
        /// <param name="pagesPerNode">The largest number of pages in a pages node; 0 for no limit.</param>
        public void SetPagesConfiguration(int pagesPerNode)
        {
            if (_currentPage != null)
                throw new HpdfException(HpdfErrorCode.InvalidDocumentState,
                    "SetPagesConfiguration must be called before the first page is added");
            if (pagesPerNode < 0 || pagesPerNode > HpdfConst.LimitMaxArray)
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"Pages per node must be between 0 and {HpdfConst.LimitMaxArray}");

            // As in the C library: the pages go into nodes below the root, the first one created now
            if (_currentPages == _rootPages)
            {
                _currentPages = new HpdfPages(_xref, _rootPages);
                _pagesInNode = 0;
            }

            _pagesPerNode = pagesPerNode;
        }

        /// <summary>
        /// Adds a new page with a specific size and orientation.
        /// </summary>
        /// <param name="size">The page size.</param>
        /// <param name="direction">The page orientation.</param>
        /// <returns>The newly created page.</returns>
        public HpdfPage AddPage(HpdfPageSize size, HpdfPageDirection direction)
        {
            var page = AddPage();
            page.SetSize(size, direction);
            return page;
        }

        /// <summary>
        /// Adds a new page with custom dimensions.
        /// </summary>
        /// <param name="width">The page width in points.</param>
        /// <param name="height">The page height in points.</param>
        /// <returns>The newly created page.</returns>
        public HpdfPage AddPage(float width, float height)
        {
            var page = AddPage();
            page.SetSize(width, height);
            return page;
        }

        /// <summary>
        /// Inserts a new intermediate pages node for hierarchical page organization.
        /// </summary>
        /// <param name="parent">The parent pages node, or null to use the current pages.</param>
        /// <returns>The newly created pages node.</returns>
        public HpdfPages InsertPagesNode(HpdfPages? parent = null)
        {
            parent = parent ?? _currentPages;
            var pages = new HpdfPages(_xref, parent);
            _currentPages = pages;
            return pages;
        }

        /// <summary>
        /// Sets encryption on the document with user and owner passwords.
        /// </summary>
        /// <param name="userPassword">The user password (opens with restricted permissions).</param>
        /// <param name="ownerPassword">The owner password (opens with full permissions).</param>
        /// <param name="permission">The permission flags for the user password.</param>
        /// <param name="mode">The encryption mode (R2=40-bit RC4, R3=128-bit RC4, R4=128-bit AES).</param>
        public void SetEncryption(string userPassword, string ownerPassword,
            HpdfPermission permission = HpdfPermission.Print,
            HpdfEncryptMode mode = HpdfEncryptMode.R3)
        {
            if (mode != HpdfEncryptMode.R2 && mode != HpdfEncryptMode.R3 && mode != HpdfEncryptMode.R4)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Unsupported encryption mode: {mode}");

            ValidatePasswords(userPassword, ownerPassword);

            // Create encryption dictionary if it doesn't exist
            if (_encryptDict is null)
            {
                _encryptDict = new HpdfEncryptDict(_xref);
            }

            // Set encryption parameters
            int keyLength = mode == HpdfEncryptMode.R2 ? 5 : 16; // 40-bit or 128-bit
            _encryptDict.SetEncryptionMode(mode, keyLength);
            _encryptDict.SetUserPassword(userPassword);
            _encryptDict.SetOwnerPassword(ownerPassword);
            _encryptDict.SetPermission(permission);

            // Update PDF version based on encryption mode
            if (mode == HpdfEncryptMode.R3)
                _xref.RequireVersion(HpdfVersion.Version14); // R3 requires PDF 1.4
            else if (mode == HpdfEncryptMode.R4)
                _xref.RequireVersion(HpdfVersion.Version16); // R4 (AES) requires PDF 1.6
        }

        /// <summary>
        /// Gets whether encryption is enabled for this document.
        /// </summary>
        public bool IsEncrypted => _encryptDict != null;

        /// <summary>
        /// Saves the document to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void Save(Stream stream)
        {
            if (stream is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Stream cannot be null");

            // Every page of the tree follows the document's compression mode, also pages added
            // with HpdfPages.AddKid; an unbalanced GSave would write an invalid content stream
            foreach (var page in Pages)
            {
                page.SetCompressionMode(_compressionMode);
                if (page.GraphicsState.Depth != 0)
                    throw new HpdfException(HpdfErrorCode.PageCannotRestoreGState,
                        $"A page has {page.GraphicsState.Depth} GSave call(s) without a matching GRestore");
            }

            // Generated annotation appearances are drawn before any object is written: the fonts they
            // show text with write their subsets from the glyphs used when the objects are written
            var annotationFonts = PrepareAnnotations();

            // Add PDF/A components if enabled
            if (_pdfACompliant)
                ApplyPdfACompliance();

            // Prepare encryption if enabled
            if (_encryptDict != null)
                ApplyEncryption();

            // Prepare outlines before writing
            if (_outlineRoot != null)
                PrepareOutlines(_outlineRoot);

            // Apply page labels if any have been added
            ApplyPageLabels();

            // The name trees of the name dictionary: attached files, named destinations, document scripts
            ApplyNameTrees();

            // The extension level of Adobe's supplement that the features in use need
            ApplyExtensions();

            // Apply AcroForm if it exists
            if (_acroForm != null)
                ApplyAcroForm(annotationFonts);

            var hpdfStream = new HpdfMemoryStream();

            // Write PDF header
            WriteHeader(hpdfStream);

            // Write all objects and xref table
            _xref.WriteToStream(hpdfStream);

            // Copy to output stream
            var data = hpdfStream.ToArray();
            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Lets every annotation bring its objects up to date (generated appearance streams, PDF/A rules).
        /// </summary>
        /// <returns>The fonts the default appearance strings of the annotations name, for the form's /DR.</returns>
        private List<HpdfFont> PrepareAnnotations()
        {
            var fonts = new List<HpdfFont>();
            var context = new HpdfAnnotationWriteContext(
                Compress: (_compressionMode & HpdfCompressionMode.Text) != 0,
                PdfA: _pdfACompliant);

            foreach (var page in Pages)
            {
                foreach (var annotation in page.Annotations)
                {
                    if (_pdfACompliant && !IsPdfAAnnotationType(annotation.Type))
                        throw new HpdfException(HpdfErrorCode.InvalidOperation,
                            $"PDF/A-1 does not allow {annotation.Type} annotations");
                    // ISO 32000-1, 12.5.6.2: an annotation and the one it replies to are on the same page
                    if (annotation is HpdfMarkupAnnotation { InReplyTo: { } target } && !page.HasAnnotation(target))
                        throw new HpdfException(HpdfErrorCode.InvalidOperation,
                            "An annotation and the annotation it is in reply to must be on the same page");

                    annotation.PrepareForWriting(context);

                    if (annotation.DefaultAppearanceFont is { } font && !fonts.Contains(font))
                        fonts.Add(font);
                }
            }

            return fonts;
        }

        /// <summary>
        /// Whether PDF/A-1 allows an annotation type: the types of PDF 1.4 but FileAttachment, Sound and Movie
        /// (ISO 19005-1, 6.5.2).
        /// </summary>
        private static bool IsPdfAAnnotationType(HpdfAnnotationType type) => type is
            HpdfAnnotationType.Text or HpdfAnnotationType.Link or HpdfAnnotationType.FreeText or HpdfAnnotationType.Line
            or HpdfAnnotationType.Square or HpdfAnnotationType.Circle or HpdfAnnotationType.Highlight
            or HpdfAnnotationType.Underline or HpdfAnnotationType.Squiggly or HpdfAnnotationType.StrikeOut
            or HpdfAnnotationType.Stamp or HpdfAnnotationType.Ink or HpdfAnnotationType.Popup or HpdfAnnotationType.Widget;

        /// <summary>
        /// Applies PDF/A compliance components to the document.
        /// </summary>
        private void ApplyPdfACompliance()
        {
            // The objects are created by the first save; a later save only refreshes the metadata,
            // so saving again neither duplicates objects nor changes the document ID.

            // 0. ISO 19005-1, 6.2.4: the Interpolate flag of an image shall be false
            foreach (var entry in _xref.Entries)
            {
                if (entry.Object is HpdfStreamObject image && image.TryGetValue(HpdfImage.InterpolateKey, out var flag)
                    && flag is HpdfBoolean { Value: true })
                    throw new HpdfException(HpdfErrorCode.InvalidOperation,
                        "PDF/A-1 does not allow interpolated images (HpdfImage.Interpolate)");
            }

            // 1. XMP Metadata (follows the current document information)
            var xmpGenerator = new HpdfXmpMetadata(_info, _pdfAConformance ?? "1B");
            if (_catalog.Dict.TryGetValue("Metadata", out var metadata) && metadata is HpdfStreamObject xmpStream)
                xmpGenerator.UpdateXmpStream(xmpStream);
            else
                _catalog.Dict["Metadata"] = xmpGenerator.GenerateXmpStream(_xref);

            // 2. Output intent with its ICC profile: the built-in sRGB or the one of SetPdfACompliance
            ApplyOutputIntent();

            // 3. Document ID
            if (!_xref.Trailer.ContainsKey("ID"))
                _xref.Trailer.Add("ID", HpdfDocumentId.GenerateId(_info));

            // 4. PDF/A-1 requires the Print flag on every annotation, and the Invisible, Hidden and NoView flags
            //    cleared (ISO 19005-1, 6.5.3)
            const int PrintFlag = 4;
            const int ForbiddenFlags = 1 | 2 | 32;
            foreach (var page in Pages)
            {
                if (!page.Dict.TryGetValue("Annots", out var annotsObj) || annotsObj is not HpdfArray annots)
                    continue;

                foreach (var item in annots)
                {
                    if (item is not HpdfDict annot)
                        continue;

                    int flags = annot.TryGetValue("F", out var f) && f is HpdfNumber n ? n.Value : 0;
                    annot["F"] = new HpdfNumber((flags | PrintFlag) & ~ForbiddenFlags);
                }
            }
        }

        /// <summary>
        /// Writes the PDF/A output intent (ISO 19005-1, 6.2.2), created once and again only when its profile or
        /// condition changed; /OutputIntents that the user put into the catalog are kept.
        /// </summary>
        private void ApplyOutputIntent()
        {
            if (_catalog.Dict.TryGetValue("OutputIntents", out var current) && !ReferenceEquals(current, _outputIntents))
                return;

            var profile = _pdfAOutputProfile ?? SRgbProfile;
            string condition = _pdfAOutputCondition ?? HpdfOutputIntent.SRgbCondition;
            if (_outputIntent is null || _outputIntent.Profile != profile || _outputIntent.Condition != condition)
            {
                profile.UseIn(_xref);
                bool registered = HpdfIccProfileRegistry.Of(_xref).IsSRgb(profile)
                    && condition == HpdfOutputIntent.SRgbCondition;
                _outputIntent = new HpdfOutputIntent(_xref, profile, condition, registered);
                _outputIntents = new HpdfArray();
                _outputIntents.Add(_outputIntent.Dict);
            }

            _catalog.Dict["OutputIntents"] = _outputIntents!;
        }

        /// <summary>
        /// Applies encryption to the document.
        /// </summary>
        private void ApplyEncryption()
        {
            // Check if document ID already exists (e.g., from PDF/A compliance)
            HpdfArray documentId;
            if (_xref.Trailer.TryGetValue("ID", out var existingId) && existingId is HpdfArray existingIdArray)
            {
                documentId = existingIdArray;
            }
            else
            {
                // Generate new document ID (required for encryption)
                documentId = HpdfDocumentId.GenerateId(_info);
                _xref.Trailer.Add("ID", documentId);
            }

            // Extract the encryption ID bytes from the ID array
            byte[]? idBytes = null;
            if (documentId.Count > 0 && documentId[0] is HpdfBinary firstId)
            {
                idBytes = firstId.Value;
            }

            // Prepare encryption dictionary with the ID
            // _encryptDict is guaranteed non-null because this method is only called after null check
            _encryptDict!.Prepare(idBytes!, _info);

            // Add encryption dictionary to trailer (assigned: the document can be saved more than once)
            _xref.Trailer["Encrypt"] = _encryptDict.Dict;

            // Store encryption handler in xref for use during object writing
            _xref.SetEncryption(_encryptDict.Encrypt);
        }

        /// <summary>
        /// Checks the passwords as the C library does (HPDF_SetPassword): the owner password is required
        /// and must differ from the user password, otherwise anyone could open the document with full
        /// permissions. Passwords are compared as the encryption sees them: Latin-1 bytes, at most 32.
        /// </summary>
        private static void ValidatePasswords(string? userPassword, string? ownerPassword)
        {
            byte[] user = PasswordBytes(userPassword ?? string.Empty, nameof(userPassword));
            byte[] owner = PasswordBytes(ownerPassword ?? string.Empty, nameof(ownerPassword));

            if (owner.Length == 0)
                throw new HpdfException(HpdfErrorCode.EncryptInvalidPassword, "The owner password cannot be empty");

            if (owner.AsSpan().SequenceEqual(user))
                throw new HpdfException(HpdfErrorCode.EncryptInvalidPassword,
                    "The owner password must differ from the user password (in its first 32 characters)");
        }

        private static byte[] PasswordBytes(string password, string name)
        {
            foreach (char c in password)
            {
                if (c > 0xFF)
                    throw new HpdfException(HpdfErrorCode.EncryptInvalidPassword,
                        $"The {name} can contain only Latin-1 characters (PDF standard security handler)");
            }

            byte[] bytes = System.Text.Encoding.Latin1.GetBytes(password);
            return bytes.Length > 32 ? bytes[..32] : bytes;
        }

        /// <summary>
        /// Prepares outlines for writing by calling BeforeWrite recursively.
        /// </summary>
        /// <param name="outline">The outline to prepare.</param>
        private static void PrepareOutlines(HpdfOutline outline)
        {
            // Recurses through the outline objects themselves, so each keeps its own open/closed state
            outline.BeforeWrite();
        }

        /// <summary>
        /// Saves the document to a file.
        /// </summary>
        /// <param name="filename">The file path to save to.</param>
        public void SaveToFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Filename cannot be null or empty");

            using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                Save(fileStream);
            }
        }

        /// <summary>
        /// Saves the document to a byte array.
        /// </summary>
        /// <returns>The PDF document as a byte array.</returns>
        public byte[] SaveToMemory()
        {
            using (var memoryStream = new MemoryStream())
            {
                Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Writes the PDF header to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        private void WriteHeader(HpdfStream stream)
        {
            var header = VersionHeaders[(int)Version];
            var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
        }

        /// <summary>
        /// Adds a page label to define custom page numbering for a page range.
        /// </summary>
        /// <param name="pageNum">The starting page number (0-based index).</param>
        /// <param name="style">The numbering style.</param>
        /// <param name="firstPage">The value of the numeric portion for the first page (default: 1).</param>
        /// <param name="prefix">Optional prefix string (default: null).</param>
        public void AddPageLabel(int pageNum, HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)
        {
            if (pageNum < 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Page number must be >= 0");

            if (firstPage < 1)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The first page number must be at least 1");

            var label = new HpdfPageLabel(style, firstPage, prefix);
            _pageLabels[pageNum] = label;

            // Page labels were introduced in PDF 1.3
            _xref.RequireVersion(HpdfVersion.Version13);
        }

        /// <summary>
        /// Applies page labels to the document catalog.
        /// This method is called automatically before saving.
        /// </summary>
        private void ApplyPageLabels()
        {
            if (_pageLabels.Count == 0)
                return;

            // Create PageLabels dictionary with Nums array
            var pageLabelsDict = new HpdfDict();
            var numsArray = new HpdfArray();

            // Sort page labels by page number and add to Nums array
            var sortedLabels = new List<int>(_pageLabels.Keys);
            sortedLabels.Sort();

            // The number tree must have an entry for the first page (PDF 12.4.2): pages before the
            // first label are numbered 1, 2, 3... as viewers do by default
            if (sortedLabels[0] != 0)
            {
                numsArray.Add(new HpdfNumber(0));
                numsArray.Add(new HpdfPageLabel().ToDict());
            }

            foreach (var pageNum in sortedLabels)
            {
                var label = _pageLabels[pageNum];
                numsArray.Add(new HpdfNumber(pageNum));
                numsArray.Add(label.ToDict());
            }

            pageLabelsDict.Add("Nums", numsArray);

            // Add PageLabels to catalog
            _catalog.SetPageLabels(pageLabelsDict);
        }

        /// <summary>
        /// Applies the AcroForm to the document catalog.
        /// This method is called automatically before saving.
        /// </summary>
        /// <param name="annotationFonts">The fonts of the annotations' default appearance strings, which go to
        /// the form's default resources (/DR) too.</param>
        private void ApplyAcroForm(IReadOnlyList<HpdfFont> annotationFonts)
        {
            // Prepare the AcroForm for writing
            // _acroForm is guaranteed non-null because this method is only called after null check
            _acroForm!.PrepareForWriting(annotationFonts);

            // Add AcroForm to catalog
            _catalog.SetAcroForm(_acroForm.Dict);

            // AcroForms require PDF version 1.5 or higher
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Gets the total number of pages in the document.
        /// </summary>
        public int PageCount => _rootPages.Count;

        /// <summary>
        /// Loads a 3D model from a file (HPDF_LoadU3DFromFile): U3D or PRC, told apart by the signature of the data.
        /// </summary>
        /// <param name="path">The path of the model file.</param>
        /// <returns>The model, to show with <see cref="HpdfPage.Create3DAnnotation"/>.</returns>
        public Haru.ThreeD.Hpdf3DModel Load3DModel(string path) => Load3DModel(Haru.ThreeD.Hpdf3DModel.ReadFile(path));

        /// <summary>
        /// Loads a 3D model from memory (HPDF_LoadU3DFromMem): U3D (PDF 1.6) or PRC (Adobe ExtensionLevel 3), told
        /// apart by the signature of the data; other data throws <see cref="HpdfErrorCode.InvalidU3dData"/>.
        /// </summary>
        /// <param name="data">The model data (copied).</param>
        /// <returns>The model, to show with <see cref="HpdfPage.Create3DAnnotation"/>.</returns>
        public Haru.ThreeD.Hpdf3DModel Load3DModel(byte[] data)
        {
            Haru.ThreeD.Hpdf3DModel.DetectFormat(data);
            return new Haru.ThreeD.Hpdf3DModel(_xref, data);
        }

        /// <summary>
        /// Gets the files attached to the document (<see cref="AttachFile(string, byte[], string?)"/>).
        /// </summary>
        public IReadOnlyList<HpdfEmbeddedFile> AttachedFiles => _attachedFiles;

        /// <summary>
        /// Attaches a file to the document (HPDF_AttachFile): viewers list it in their attachments panel.
        /// The file is embedded under its name without the directory.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>The embedded file.</returns>
        public HpdfEmbeddedFile AttachFile(string path)
        {
            var (name, data) = HpdfEmbeddedFile.ReadFile(path);
            return AttachFile(name, data);
        }

        /// <summary>
        /// Attaches data as a file to the document (the EmbeddedFiles name tree, PDF 1.4): viewers list it in their
        /// attachments panel. PDF/A-1 allows no embedded files: saving such a document fails.
        /// </summary>
        /// <param name="name">The file name, without a directory; unique among the attached files.</param>
        /// <param name="data">The file's contents (copied).</param>
        /// <param name="mimeType">The MIME type, such as <c>text/plain</c>, or null.</param>
        /// <returns>The embedded file.</returns>
        public HpdfEmbeddedFile AttachFile(string name, byte[] data, string? mimeType = null)
        {
            HpdfEmbeddedFile.ValidateArguments(name, data, mimeType);
            if (_attachedFiles.Exists(file => file.Name == name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"A file named {name} is already attached");

            var file = new HpdfEmbeddedFile(_xref, name, data, mimeType);
            _attachedFiles.Add(file);
            _xref.RequireVersion(HpdfVersion.Version14);
            return file;
        }

        /// <summary>
        /// Declares the developer extension of Adobe's supplement to ISO 32000 that the document uses
        /// (ISO 32000-1, 7.12): /Extensions &lt;&lt; /ADBE &lt;&lt; /BaseVersion /1.7 /ExtensionLevel n &gt;&gt; &gt;&gt;.
        /// </summary>
        private void ApplyExtensions()
        {
            int level = _xref.RequiredAdobeExtensionLevel;
            if (level == 0)
            {
                _catalog.Dict.Remove("Extensions");
                return;
            }

            var adobe = new HpdfDict();
            adobe.Add("Type", new HpdfName("DeveloperExtensions"));
            adobe.Add("BaseVersion", new HpdfName("1.7"));
            adobe.Add("ExtensionLevel", new HpdfNumber(level));
            var extensions = new HpdfDict();
            extensions.Add("Type", new HpdfName("Extensions"));
            extensions.Add("ADBE", adobe);
            _catalog.Dict["Extensions"] = extensions;
        }

        /// <summary>
        /// Gets the named destinations of the document, in the order they were created.
        /// </summary>
        public IReadOnlyList<HpdfNamedDestination> NamedDestinations => _namedDestinations;

        /// <summary>
        /// Creates a named destination (the Dests name tree): links and outline items can go to it before its
        /// target is known, and a URL can open the document at it (<c>file.pdf#nameddest=name</c>). Its target
        /// must be set (here or with <see cref="HpdfNamedDestination.SetDestination"/>) before the document is saved.
        /// </summary>
        /// <param name="name">The name, unique among the named destinations of the document.</param>
        /// <param name="destination">The target, with its view set, or null to set it later.</param>
        /// <returns>The named destination.</returns>
        public HpdfNamedDestination CreateNamedDestination(string name, HpdfDestination? destination = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Name cannot be null or empty");
            if (_namedDestinations.Exists(d => d.Name == name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"A named destination {name} already exists");

            var namedDestination = new HpdfNamedDestination(_xref, name);
            if (destination != null)
                namedDestination.SetDestination(destination);
            _namedDestinations.Add(namedDestination);
            return namedDestination;
        }

        /// <summary>
        /// Adds a document-level JavaScript program (the JavaScript name tree, PDF 1.3): a viewer that runs
        /// JavaScript, such as Adobe Acrobat, runs it when the document is opened, to define functions and variables
        /// that the scripts of links use, or to do something at once. Scripts run in the order of their names
        /// (sorted by their bytes), not in the order they were added. PDF/A-1 allows no JavaScript: saving such a
        /// document fails.
        /// </summary>
        /// <param name="name">The name of the script, unique among the document's scripts.</param>
        /// <param name="script">The JavaScript code.</param>
        public void AddJavaScript(string name, string script)
        {
            if (string.IsNullOrEmpty(name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Name cannot be null or empty");
            HpdfJavaScript.Validate(script);
            if (_javaScripts.Exists(entry => entry.Name == name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"A script named {name} already exists");

            _javaScripts.Add((name, HpdfJavaScript.CreateAction(_xref, script)));
        }

        /// <summary>
        /// Sets a JavaScript program that a viewer runs when the document is opened (the catalog's /OpenAction, PDF
        /// 1.3), after the document scripts of <see cref="AddJavaScript"/>. The open action is one entry: this
        /// replaces a destination set with <see cref="HpdfCatalog.SetOpenAction"/>, and that replaces the script.
        /// Only viewers that run JavaScript run it. PDF/A-1 allows no JavaScript: saving such a document fails.
        /// </summary>
        /// <param name="script">The JavaScript code, or null to remove the script (a destination open action stays).</param>
        public void SetOpenJavaScript(string? script)
        {
            if (script is null)
            {
                if (IsOpenJavaScriptSet)
                    _catalog.Dict.Remove("OpenAction");
                return;
            }

            // The action and its stream are created once; a later call rewrites the script
            if (_openJavaScript is null)
                _openJavaScript = HpdfJavaScript.CreateAction(_xref, script);
            else
                HpdfJavaScript.Write((HpdfStreamObject)_openJavaScript["JS"], script);

            _catalog.Dict["OpenAction"] = _openJavaScript;
        }

        /// <summary>
        /// Whether the open action of the catalog is the script of <see cref="SetOpenJavaScript"/>.
        /// </summary>
        private bool IsOpenJavaScriptSet =>
            _openJavaScript != null && _catalog.Dict.TryGetValue("OpenAction", out var action)
            && ReferenceEquals(action, _openJavaScript);

        /// <summary>
        /// Writes the name trees of the catalog's name dictionary: the attached files (EmbeddedFiles), the
        /// named destinations (Dests) and the document scripts (JavaScript).
        /// </summary>
        private void ApplyNameTrees()
        {
            if (_pdfACompliant && _attachedFiles.Count > 0)
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "PDF/A-1 does not allow embedded files");
            if (_pdfACompliant && (_javaScripts.Count > 0 || IsOpenJavaScriptSet))
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "PDF/A-1 does not allow JavaScript");

            var files = new List<(string, HpdfObject)>(_attachedFiles.Count);
            foreach (var file in _attachedFiles)
                files.Add((file.Name, file.Dict));
            _nameDictionary.SetTree("EmbeddedFiles", files);

            var destinations = new List<(string, HpdfObject)>(_namedDestinations.Count);
            foreach (var namedDestination in _namedDestinations)
            {
                if (namedDestination.Destination is null)
                    throw new HpdfException(HpdfErrorCode.InvalidDestination,
                        $"The named destination {namedDestination.Name} has no target: call SetDestination");
                destinations.Add((namedDestination.Name, namedDestination.Destination.DestArray));
            }
            _nameDictionary.SetTree("Dests", destinations);

            _nameDictionary.SetTree("JavaScript", _javaScripts);
        }

        /// <summary>
        /// Gets the AcroForm for this document, creating it if it doesn't exist.
        /// Use this to add interactive form fields to your PDF.
        /// </summary>
        /// <returns>The AcroForm instance.</returns>
        public HpdfAcroForm GetOrCreateAcroForm()
        {
            if (_acroForm is null)
            {
                _acroForm = new HpdfAcroForm(_xref);
                // Text and choice fields get generated appearances (standard fonts, WinAnsi text);
                // NeedAppearances still lets interactive viewers redraw them in their own way
                _acroForm.SetNeedAppearances(true);
            }
            return _acroForm;
        }

        /// <summary>
        /// Gets the AcroForm for this document if it exists.
        /// </summary>
        /// <returns>The AcroForm instance, or null if not created yet.</returns>
        public HpdfAcroForm? GetAcroForm()
        {
            return _acroForm;
        }

        /// <summary>
        /// Loads a PNG image from a file.
        /// </summary>
        /// <param name="filePath">The path to the PNG file.</param>
        /// <returns>The loaded image object.</returns>
        public HpdfImage LoadPngImageFromFile(string filePath)
        {
            string localName = $"{GeneratedNamePrefix}Im{_xref.Entries.Count + 1}";
            return HpdfImage.LoadPngImage(_xref, localName, filePath);
        }

        /// <summary>
        /// Loads a PNG image from a stream.
        /// </summary>
        /// <param name="stream">The stream containing PNG data.</param>
        /// <returns>The loaded image object.</returns>
        public HpdfImage LoadPngImage(Stream stream)
        {
            string localName = $"{GeneratedNamePrefix}Im{_xref.Entries.Count + 1}";
            return HpdfImage.LoadPngImage(_xref, localName, stream);
        }

        /// <summary>
        /// Loads a JPEG image from a file.
        /// </summary>
        /// <param name="filePath">The path to the JPEG file.</param>
        /// <returns>The loaded image object.</returns>
        public HpdfImage LoadJpegImageFromFile(string filePath)
        {
            string localName = $"{GeneratedNamePrefix}Im{_xref.Entries.Count + 1}";
            return HpdfImage.LoadJpegImage(_xref, localName, filePath);
        }

        /// <summary>
        /// Loads a JPEG image from a stream.
        /// </summary>
        /// <param name="stream">The stream containing JPEG data.</param>
        /// <returns>The loaded image object.</returns>
        public HpdfImage LoadJpegImage(Stream stream)
        {
            string localName = $"{GeneratedNamePrefix}Im{_xref.Entries.Count + 1}";
            return HpdfImage.LoadJpegImage(_xref, localName, stream);
        }

        /// <summary>
        /// Loads an ICC color profile file (HPDF_LoadIccProfileFromFile) to embed in the document: as the color space
        /// of images and of fill and stroke colors, or as the PDF/A output
        /// intent. The component count is read from the profile. Loading the same profile again returns the same
        /// object, which is embedded once.
        /// </summary>
        /// <param name="path">The path of the profile (.icc, .icm).</param>
        /// <returns>The profile.</returns>
        public HpdfIccProfile LoadIccProfile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The path cannot be null or empty");
            if (!File.Exists(path))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"File not found: {path}");
            return LoadIccProfile(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Loads an ICC color profile from memory (HPDF_ICC_LoadIccFromMem); see <see cref="LoadIccProfile(string)"/>.
        /// </summary>
        /// <param name="data">The profile data (copied).</param>
        /// <returns>The profile.</returns>
        /// <exception cref="HpdfException">The data is not an ICC profile of a gray, RGB or CMYK color space.</exception>
        public HpdfIccProfile LoadIccProfile(byte[] data)
        {
            if (data is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "ICC profile data cannot be null");
            return HpdfIccProfileRegistry.Of(_xref).GetOrAdd(data);
        }

        /// <summary>
        /// Gets the built-in sRGB profile (ICC v2, 456 bytes, CC0): the color space of ordinary screen colors and
        /// the profile of the PDF/A output intent. It is embedded when it is first asked for.
        /// </summary>
        public HpdfIccProfile SRgbProfile => HpdfIccProfileRegistry.Of(_xref).SRgb;

        /// <summary>
        /// Gets the ICC profiles embedded in the document, in the order they were loaded.
        /// </summary>
        public IReadOnlyList<HpdfIccProfile> IccProfiles => HpdfIccProfileRegistry.Of(_xref).Profiles;
    }
}
