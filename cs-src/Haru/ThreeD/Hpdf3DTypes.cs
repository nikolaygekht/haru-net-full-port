namespace Haru.ThreeD
{
    /// <summary>
    /// The format of a 3D model (/Subtype of the 3D stream).
    /// </summary>
    public enum Hpdf3DFormat
    {
        /// <summary>
        /// Universal 3D (ECMA-363), PDF 1.6.
        /// </summary>
        U3D = 0,

        /// <summary>
        /// Product Representation Compact (ISO 14739-1), Adobe ExtensionLevel 3.
        /// </summary>
        PRC
    }

    /// <summary>
    /// Lighting schemes of a 3D view (/LS /Subtype), in the order of the C library.
    /// </summary>
    public enum Hpdf3DLighting
    {
        /// <summary>The lights of the model file (Artwork).</summary>
        Artwork = 0,

        /// <summary>No lights (None).</summary>
        None,

        /// <summary>Three blue-grey infinite lights (White).</summary>
        White,

        /// <summary>Three light-grey infinite lights (Day).</summary>
        Day,

        /// <summary>One yellow, one aqua and one blue infinite light (Night).</summary>
        Night,

        /// <summary>Three grey infinite lights (Hard).</summary>
        Hard,

        /// <summary>One red, one green and one blue infinite light (Primary).</summary>
        Primary,

        /// <summary>Three blue infinite lights (Blue).</summary>
        Blue,

        /// <summary>Three red infinite lights (Red).</summary>
        Red,

        /// <summary>Six grey infinite lights along the axes (Cube).</summary>
        Cube,

        /// <summary>Three grey infinite lights and a light attached to the camera (CAD).</summary>
        CAD,

        /// <summary>A single light attached to the camera (Headlamp).</summary>
        Headlamp
    }

    /// <summary>
    /// When a 3D annotation is activated (/3DA /A).
    /// </summary>
    public enum Hpdf3DActivation
    {
        /// <summary>When the page that contains the annotation is opened (PO).</summary>
        PageOpen = 0,

        /// <summary>When the page that contains the annotation becomes visible (PV).</summary>
        PageVisible,

        /// <summary>When the user activates it, for example by clicking (XA).</summary>
        Explicit
    }

    /// <summary>
    /// When a 3D annotation is deactivated (/3DA /D).
    /// </summary>
    public enum Hpdf3DDeactivation
    {
        /// <summary>When the page that contains the annotation is closed (PC).</summary>
        PageClose = 0,

        /// <summary>When the page that contains the annotation is no longer visible (PI).</summary>
        PageInvisible,

        /// <summary>When the user deactivates it, for example from a context menu (XD).</summary>
        Explicit
    }

    /// <summary>
    /// The state of the 3D artwork of an annotation after activation or deactivation (/AIS, /DIS).
    /// </summary>
    public enum Hpdf3DArtworkState
    {
        /// <summary>Not instantiated: the annotation shows its appearance stream (U).</summary>
        Uninstantiated = 0,

        /// <summary>Instantiated, but not running scripts or animations (I).</summary>
        Instantiated,

        /// <summary>Instantiated and live: scripts and animations run, the user can interact (L).</summary>
        Live
    }
}
