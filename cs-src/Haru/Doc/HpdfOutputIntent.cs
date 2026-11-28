using Haru.Objects;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// The PDF/A output intent (ISO 19005-1, 6.2.2): an /OutputIntent dictionary of subtype GTS_PDFA1 that names the
    /// output condition and carries its ICC profile (/DestOutputProfile), which device colors of the document are
    /// interpreted with.
    /// </summary>
    internal sealed class HpdfOutputIntent
    {
        /// <summary>
        /// The output condition of the built-in sRGB profile, as registered in the ICC characterization data registry.
        /// </summary>
        internal const string SRgbCondition = "sRGB IEC61966-2.1";

        /// <summary>
        /// Gets the output intent dictionary, an indirect object.
        /// </summary>
        internal HpdfDict Dict { get; }

        /// <summary>
        /// Gets the profile of the intent.
        /// </summary>
        internal HpdfIccProfile Profile { get; }

        /// <summary>
        /// Gets the output condition of the intent.
        /// </summary>
        internal string Condition { get; }

        /// <param name="xref">The cross-reference table.</param>
        /// <param name="profile">The profile; checked by the caller.</param>
        /// <param name="outputCondition">The identifier and description of the output condition.</param>
        /// <param name="registered">Whether the condition is one of the ICC registry (http://www.color.org).</param>
        internal HpdfOutputIntent(HpdfXref xref, HpdfIccProfile profile, string outputCondition, bool registered)
        {
            Profile = profile;
            Condition = outputCondition;
            Dict = new HpdfDict();
            xref.Add(Dict);

            Dict.Add("Type", new HpdfName("OutputIntent"));
            Dict.Add("S", new HpdfName("GTS_PDFA1"));
            Dict.Add("OutputCondition", new HpdfString(outputCondition));
            Dict.Add("OutputConditionIdentifier", new HpdfString(outputCondition));
            Dict.Add("Info", new HpdfString(outputCondition));
            if (registered)
                Dict.Add("RegistryName", new HpdfString("http://www.color.org"));
            Dict.Add("DestOutputProfile", profile.StreamObject);
        }
    }
}
