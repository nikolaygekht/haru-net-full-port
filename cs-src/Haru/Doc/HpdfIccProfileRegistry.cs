/* The ICC profiles of a document: each distinct profile is embedded once, the built-in sRGB profile is loaded once. */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// The ICC profiles of the document of a cross-reference table, by their content: loading the same profile again
    /// (by the user or from the images of the document) returns the profile already embedded. Kept per
    /// cross-reference table, so the static image loaders, which know only the table, share it with the document.
    /// </summary>
    internal sealed class HpdfIccProfileRegistry
    {
        private const string SRgbResourceName = "Haru.Icc.sRGB-v2-micro.icc";

        private static readonly ConditionalWeakTable<HpdfXref, HpdfIccProfileRegistry> Registries = new();

        // The built-in profile is read from the assembly once per process
        private static readonly Lazy<byte[]> SRgbData = new(LoadSRgbData);
        private static readonly Lazy<string> SRgbKey = new(() => KeyOf(SRgbData.Value));

        private readonly HpdfXref _xref;
        private readonly Dictionary<string, HpdfIccProfile> _byContent = new(StringComparer.Ordinal);
        private readonly List<HpdfIccProfile> _profiles = new();

        private HpdfIccProfileRegistry(HpdfXref xref)
        {
            _xref = xref;
        }

        /// <summary>
        /// Gets the registry of the document of a cross-reference table.
        /// </summary>
        internal static HpdfIccProfileRegistry Of(HpdfXref xref) => Registries.GetValue(xref, x => new HpdfIccProfileRegistry(x));

        /// <summary>
        /// The profiles of the document, in the order they were embedded.
        /// </summary>
        internal IReadOnlyList<HpdfIccProfile> Profiles => _profiles;

        /// <summary>
        /// Gets the built-in sRGB profile (ICC v2, monitor class).
        /// </summary>
        internal HpdfIccProfile SRgb => GetOrAdd(SRgbData.Value);

        /// <summary>
        /// Whether a profile is the built-in sRGB profile, without embedding it.
        /// </summary>
        internal bool IsSRgb(HpdfIccProfile profile) =>
            _byContent.TryGetValue(SRgbKey.Value, out var srgb) && ReferenceEquals(srgb, profile);

        /// <summary>
        /// Gets the embedded profile with this content, embedding it on first use.
        /// </summary>
        /// <exception cref="HpdfException">The data is not an ICC profile of a gray, RGB or CMYK color space.</exception>
        internal HpdfIccProfile GetOrAdd(byte[] data)
        {
            HpdfIccProfile.ReadHeader(data);   // checked before anything is created
            string key = KeyOf(data);
            if (_byContent.TryGetValue(key, out var existing))
                return existing;

            var profile = HpdfIccProfile.Create(_xref, (byte[])data.Clone(),
                $"{HpdfDocument.GeneratedNamePrefix}CS{_profiles.Count + 1}");
            _byContent.Add(key, profile);
            _profiles.Add(profile);
            return profile;
        }

        private static string KeyOf(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

        private static byte[] LoadSRgbData()
        {
            using var stream = typeof(HpdfIccProfileRegistry).Assembly.GetManifestResourceStream(SRgbResourceName)
                ?? throw new InvalidOperationException($"The resource {SRgbResourceName} is missing");
            var data = new byte[stream.Length];
            stream.ReadExactly(data);
            return data;
        }
    }
}
