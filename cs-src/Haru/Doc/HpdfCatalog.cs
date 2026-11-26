using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// Represents the document catalog (root object) of a PDF document.
    /// The catalog is the entry point for accessing the document's page tree and other document-level structures.
    /// </summary>
    public class HpdfCatalog
    {
        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;

        /// <summary>
        /// Gets the underlying dictionary object for this catalog.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets or sets the page layout to be used when the document is opened.
        /// </summary>
        public HpdfPageLayout PageLayout
        {
            get
            {
                if (!_dict.TryGetValue("PageLayout", out var layoutObj))
                return HpdfPageLayout.SinglePage;
            var layoutName = layoutObj as HpdfName;
                if (layoutName is null)
                    return HpdfPageLayout.SinglePage;

                return layoutName.Value switch
                {
                    "SinglePage" => HpdfPageLayout.SinglePage,
                    "OneColumn" => HpdfPageLayout.OneColumn,
                    "TwoColumnLeft" => HpdfPageLayout.TwoColumnLeft,
                    "TwoColumnRight" => HpdfPageLayout.TwoColumnRight,
                    "TwoPageLeft" => HpdfPageLayout.TwoPageLeft,
                    "TwoPageRight" => HpdfPageLayout.TwoPageRight,
                    _ => HpdfPageLayout.SinglePage
                };
            }
            set
            {
                var layoutName = value switch
                {
                    HpdfPageLayout.SinglePage => "SinglePage",
                    HpdfPageLayout.OneColumn => "OneColumn",
                    HpdfPageLayout.TwoColumnLeft => "TwoColumnLeft",
                    HpdfPageLayout.TwoColumnRight => "TwoColumnRight",
                    HpdfPageLayout.TwoPageLeft => "TwoPageLeft",
                    HpdfPageLayout.TwoPageRight => "TwoPageRight",
                    _ => "SinglePage"
                };
                _dict["PageLayout"] = new HpdfName(layoutName);

                // The two-page layouts were introduced in PDF 1.5
                if (value == HpdfPageLayout.TwoPageLeft || value == HpdfPageLayout.TwoPageRight)
                    _xref.RequireVersion(HpdfVersion.Version15);
            }
        }

        /// <summary>
        /// Gets or sets the page mode to be used when the document is opened.
        /// </summary>
        public HpdfPageMode PageMode
        {
            get
            {
                if (!_dict.TryGetValue("PageMode", out var modeObj))
                return HpdfPageMode.UseNone;
            var modeName = modeObj as HpdfName;
                if (modeName is null)
                    return HpdfPageMode.UseNone;

                return modeName.Value switch
                {
                    "UseNone" => HpdfPageMode.UseNone,
                    "UseOutlines" => HpdfPageMode.UseOutlines,
                    "UseThumbs" => HpdfPageMode.UseThumbs,
                    "FullScreen" => HpdfPageMode.FullScreen,
                    "UseOC" => HpdfPageMode.UseOC,
                    "UseAttachments" => HpdfPageMode.UseAttachments,
                    _ => HpdfPageMode.UseNone
                };
            }
            set
            {
                var modeName = value switch
                {
                    HpdfPageMode.UseNone => "UseNone",
                    HpdfPageMode.UseOutlines => "UseOutlines",
                    HpdfPageMode.UseThumbs => "UseThumbs",
                    HpdfPageMode.FullScreen => "FullScreen",
                    HpdfPageMode.UseOC => "UseOC",
                    HpdfPageMode.UseAttachments => "UseAttachments",
                    _ => "UseNone"
                };
                _dict["PageMode"] = new HpdfName(modeName);

                // Optional content (UseOC) appeared in PDF 1.5, the attachments panel in PDF 1.6
                if (value == HpdfPageMode.UseOC)
                    _xref.RequireVersion(HpdfVersion.Version15);
                else if (value == HpdfPageMode.UseAttachments)
                    _xref.RequireVersion(HpdfVersion.Version16);
            }
        }

        /// <summary>
        /// Gets or sets the viewer preferences (/ViewerPreferences): how a viewer presents the
        /// document window. <see cref="HpdfViewerPreferences.None"/> removes the dictionary.
        /// <see cref="HpdfViewerPreferences.PrintScalingNone"/> requires PDF 1.6, which the document
        /// version is raised to.
        /// </summary>
        public HpdfViewerPreferences ViewerPreferences
        {
            get
            {
                if (!_dict.TryGetValue("ViewerPreferences", out var preferencesObj) || preferencesObj is not HpdfDict preferences)
                    return HpdfViewerPreferences.None;

                var result = HpdfViewerPreferences.None;
                foreach (var (key, flag) in ViewerPreferenceKeys)
                {
                    if (preferences.TryGetValue(key, out var value) && value is HpdfBoolean { Value: true })
                        result |= flag;
                }

                if (preferences.TryGetValue("PrintScaling", out var scaling) && scaling is HpdfName { Value: "None" })
                    result |= HpdfViewerPreferences.PrintScalingNone;

                return result;
            }
            set
            {
                _dict.Remove("ViewerPreferences");
                if (value == HpdfViewerPreferences.None)
                    return;

                var preferences = new HpdfDict();
                foreach (var (key, flag) in ViewerPreferenceKeys)
                {
                    if ((value & flag) != 0)
                        preferences.Add(key, new HpdfBoolean(true));
                }

                if ((value & HpdfViewerPreferences.PrintScalingNone) != 0)
                {
                    preferences.Add("PrintScaling", new HpdfName("None"));
                    _xref.RequireVersion(HpdfVersion.Version16);
                }

                _dict.Add("ViewerPreferences", preferences);
            }
        }

        /// <summary>
        /// Sets the destination shown when the document is opened (/OpenAction),
        /// for example a page other than the first one or a specific zoom.
        /// </summary>
        /// <param name="destination">
        /// The destination, with its view set (<see cref="HpdfDestination.SetXYZ"/>, <see cref="HpdfDestination.SetFit"/>, ...);
        /// null removes the open action.
        /// </param>
        public void SetOpenAction(HpdfDestination? destination)
        {
            if (destination is null)
            {
                _dict.Remove("OpenAction");
                return;
            }

            if (destination.DestArray.Count < 2)
                throw new HpdfException(HpdfErrorCode.InvalidDestination,
                    "The destination has no view: call SetXYZ, SetFit or another Set method first");

            _dict["OpenAction"] = destination.DestArray;
        }

        /// <summary>
        /// Gets the destination array of the open action, or null if none is set.
        /// </summary>
        /// <returns>The destination array, or null.</returns>
        public HpdfArray? GetOpenAction()
        {
            if (_dict.TryGetValue("OpenAction", out var openAction))
                return openAction as HpdfArray;
            return null;
        }

        private static readonly (string Key, HpdfViewerPreferences Flag)[] ViewerPreferenceKeys =
        {
            ("HideToolbar", HpdfViewerPreferences.HideToolbar),
            ("HideMenubar", HpdfViewerPreferences.HideMenubar),
            ("HideWindowUI", HpdfViewerPreferences.HideWindowUI),
            ("FitWindow", HpdfViewerPreferences.FitWindow),
            ("CenterWindow", HpdfViewerPreferences.CenterWindow)
        };

        /// <summary>
        /// Sets the page mode (convenience method for API compatibility).
        /// </summary>
        /// <param name="mode">The page mode to set.</param>
        public void SetPageMode(HpdfPageMode mode)
        {
            PageMode = mode;
        }

        /// <summary>
        /// Creates a new catalog with a root pages object.
        /// </summary>
        /// <param name="xref">The cross-reference table to add the catalog to.</param>
        /// <param name="rootPages">The root pages object.</param>
        public HpdfCatalog(HpdfXref xref, HpdfPages rootPages)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            if (rootPages is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Root pages cannot be null");

            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("Catalog"));
            _dict.Add("Pages", rootPages.Dict);

            // Add catalog to xref
            xref.Add(_dict);
        }

        /// <summary>
        /// Gets the root pages object from the catalog.
        /// </summary>
        /// <returns>The root pages dictionary, or null if not found.</returns>
        public HpdfDict? GetRootPages()
        {
            if (_dict.TryGetValue("Pages", out var pages))
                return pages as HpdfDict;
            return null;
        }

        /// <summary>
        /// Sets the names dictionary for the catalog.
        /// </summary>
        /// <param name="namesDict">The names dictionary.</param>
        public void SetNames(HpdfDict namesDict)
        {
            if (namesDict is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Names dictionary cannot be null");

            _dict["Names"] = namesDict;
        }

        /// <summary>
        /// Gets the names dictionary from the catalog.
        /// </summary>
        /// <returns>The names dictionary, or null if not set.</returns>
        public HpdfDict? GetNames()
        {
            if (_dict.TryGetValue("Names", out var names))
                return names as HpdfDict;
            return null;
        }

        /// <summary>
        /// Sets the page labels dictionary for the catalog.
        /// Page labels control how page numbers are displayed in PDF viewers.
        /// </summary>
        /// <param name="pageLabelsDict">The page labels dictionary containing the number tree.</param>
        public void SetPageLabels(HpdfDict pageLabelsDict)
        {
            if (pageLabelsDict is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "PageLabels dictionary cannot be null");

            _dict["PageLabels"] = pageLabelsDict;
        }

        /// <summary>
        /// Gets the page labels dictionary from the catalog.
        /// </summary>
        /// <returns>The page labels dictionary, or null if not set.</returns>
        public HpdfDict? GetPageLabels()
        {
            if (_dict.TryGetValue("PageLabels", out var pageLabels))
                return pageLabels as HpdfDict;
            return null;
        }

        /// <summary>
        /// Sets the AcroForm dictionary for the catalog.
        /// This enables interactive forms in the PDF document.
        /// </summary>
        /// <param name="acroForm">The AcroForm dictionary.</param>
        public void SetAcroForm(HpdfDict acroForm)
        {
            if (acroForm is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "AcroForm dictionary cannot be null");

            _dict["AcroForm"] = acroForm;
        }

        /// <summary>
        /// Gets the AcroForm dictionary from the catalog.
        /// </summary>
        /// <returns>The AcroForm dictionary, or null if not set.</returns>
        public HpdfDict? GetAcroForm()
        {
            if (_dict.TryGetValue("AcroForm", out var acroForm))
                return acroForm as HpdfDict;
            return null;
        }
    }
}
