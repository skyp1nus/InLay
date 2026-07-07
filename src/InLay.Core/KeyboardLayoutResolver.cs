using Windows.Win32;

namespace InLay.Core;

/// <summary>
/// Maps a keyboard-layout handle (HKL) to a <see cref="LayoutInfo"/>. The bit extraction and label
/// derivation are pure (and unit-tested); <see cref="Resolve"/> is the only member that touches Win32
/// (<c>LCIDToLocaleName</c>) — the same pure/side-effecting split used by <see cref="AppPaths"/>.
/// </summary>
public static class KeyboardLayoutResolver
{
    /// <summary>LOCALE_NAME_MAX_LENGTH — max characters (including the null) LCIDToLocaleName may write.</summary>
    private const int LocaleNameMaxLength = 85;

    /// <summary>Extracts the language identifier (LANGID = the low word) from an HKL. Pure.</summary>
    public static ushort LangIdFromHkl(nint hkl) => (ushort)((ulong)(nuint)hkl & 0xFFFF);

    /// <summary>
    /// Derives the default two-letter label (e.g. <c>uk-UA</c> → <c>UK</c>) from a BCP-47 locale name,
    /// falling back to <c>"??"</c> when the name is missing. Pure. Custom labels are an M4 feature.
    /// </summary>
    public static string LabelFromLocaleName(string localeName)
    {
        if (string.IsNullOrWhiteSpace(localeName))
        {
            return "??";
        }

        int dash = localeName.IndexOf('-', StringComparison.Ordinal);
        string primary = dash >= 0 ? localeName[..dash] : localeName;
        string head = primary.Length >= 2 ? primary[..2] : primary;
        return head.ToUpperInvariant();
    }

    /// <summary>Resolves an HKL to a <see cref="LayoutInfo"/> via <c>LCIDToLocaleName</c>. Touches Win32.</summary>
    public static LayoutInfo Resolve(nint hkl)
    {
        ushort langId = LangIdFromHkl(hkl);
        string localeName = LocaleNameFromLangId(langId);
        return new LayoutInfo(langId, localeName, LabelFromLocaleName(localeName));
    }

    private static string LocaleNameFromLangId(ushort langId)
    {
        Span<char> buffer = stackalloc char[LocaleNameMaxLength];

        // LCIDToLocaleName writes the BCP-47 name and returns the character count including the
        // terminating null (0 on failure), so a valid name has length > 1.
        int length = PInvoke.LCIDToLocaleName(langId, buffer, 0);
        return length > 1 ? new string(buffer[..(length - 1)]) : string.Empty;
    }
}
