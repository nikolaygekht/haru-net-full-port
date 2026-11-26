using System;

namespace Haru.Doc
{
    /// <summary>
    /// Viewer preferences of the document (/ViewerPreferences in the catalog): how a viewer
    /// presents the document window. The values match the HPDF_HIDE_TOOLBAR ... HPDF_PRINT_SCALING_NONE
    /// constants of the C library and <see cref="Types.HpdfConst"/>, and can be combined.
    /// </summary>
    [Flags]
    public enum HpdfViewerPreferences
    {
        /// <summary>
        /// No preferences: the viewer uses its defaults.
        /// </summary>
        None = 0,

        /// <summary>
        /// Hide the viewer's tool bars when the document is active.
        /// </summary>
        HideToolbar = 1,

        /// <summary>
        /// Hide the viewer's menu bar when the document is active.
        /// </summary>
        HideMenubar = 2,

        /// <summary>
        /// Hide user interface elements in the document window (scroll bars, navigation controls),
        /// leaving only the contents displayed.
        /// </summary>
        HideWindowUI = 4,

        /// <summary>
        /// Resize the document window to fit the size of the first displayed page.
        /// </summary>
        FitWindow = 8,

        /// <summary>
        /// Position the document window in the center of the screen.
        /// </summary>
        CenterWindow = 16,

        /// <summary>
        /// Preselect "no scaling" (actual size) in the print dialog (/PrintScaling /None, PDF 1.6).
        /// </summary>
        PrintScalingNone = 32
    }
}
