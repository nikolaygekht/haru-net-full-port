using System;

namespace Haru.Annotations
{
    /// <summary>
    /// Annotation flags (/F): how a viewer shows, prints and lets the user change an annotation.
    /// </summary>
    [Flags]
    public enum HpdfAnnotationFlags
    {
        /// <summary>
        /// No flags: the annotation is shown on screen but not printed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Do not show an annotation of an unknown type with an icon (Invisible).
        /// </summary>
        Invisible = 1,

        /// <summary>
        /// Do not show or print the annotation, and do not let the user interact with it (Hidden).
        /// </summary>
        Hidden = 2,

        /// <summary>
        /// Print the annotation when the page is printed (Print).
        /// </summary>
        Print = 4,

        /// <summary>
        /// Do not scale the appearance with the page zoom (NoZoom).
        /// </summary>
        NoZoom = 8,

        /// <summary>
        /// Do not rotate the appearance with the page (NoRotate).
        /// </summary>
        NoRotate = 16,

        /// <summary>
        /// Do not show the annotation on screen (it may still be printed) (NoView).
        /// </summary>
        NoView = 32,

        /// <summary>
        /// Do not let the user interact with the annotation (ReadOnly).
        /// </summary>
        ReadOnly = 64,

        /// <summary>
        /// Do not let the user delete the annotation or change its properties, such as its position (Locked).
        /// </summary>
        Locked = 128,

        /// <summary>
        /// Invert <see cref="NoView"/> for certain events, such as the mouse over the annotation (ToggleNoView).
        /// </summary>
        ToggleNoView = 256,

        /// <summary>
        /// Do not let the user change the contents of the annotation (LockedContents, PDF 1.7).
        /// </summary>
        LockedContents = 512
    }
}
