using System.Globalization;

namespace KeyboardAutoSwitcher;

/// <summary>
/// Represents a keyboard layout configuration with its culture and specific layout ID
/// </summary>
public class KeyboardLayoutConfig
{
    public string CultureName { get; }
    public int LayoutId { get; }
    public string DisplayName { get; }

    public KeyboardLayoutConfig(string cultureName, int layoutId, string displayName)
    {
        CultureName = cultureName;
        LayoutId = layoutId;
        DisplayName = displayName;
    }

    public CultureInfo GetCultureInfo()
    {
        return new CultureInfo(CultureName);
    }

    public int GetLanguageId()
    {
        return GetCultureInfo().LCID & 0xFFFF;
    }
}

/// <summary>
/// Predefined keyboard layouts
/// </summary>
public static class KeyboardLayouts
{
    // US-Dvorak layout
    // Common layout IDs for Dvorak:
    // - 0x00010409 (some systems)
    // - 0xF0020409 (other systems - variant ID F002)
    public static readonly KeyboardLayoutConfig UsDvorak = new(
        cultureName: "en-US",
        layoutId: unchecked((int)0xF0020409),  // Updated based on your system
        displayName: "English (United States) - Dvorak"
    );

    // French standard layout
    public static readonly KeyboardLayoutConfig FrenchStandard = new(
        cultureName: "fr-FR",
        layoutId: 0x040C040C,  // Updated based on your system
        displayName: "French (France)"
    );

    /// <summary>
    /// Get a keyboard layout by culture name
    /// </summary>
    public static KeyboardLayoutConfig? GetByCultureName(string cultureName)
    {
        return cultureName switch
        {
            "en-US" => UsDvorak,
            "fr-FR" => FrenchStandard,
            _ => null
        };
    }

    /// <summary>
    /// Get a keyboard layout by layout ID
    /// </summary>
    public static KeyboardLayoutConfig? GetByLayoutId(int layoutId)
    {
        if (layoutId == UsDvorak.LayoutId) return UsDvorak;
        if (layoutId == FrenchStandard.LayoutId) return FrenchStandard;

        return null;
    }

    /// <summary>
    /// Try to match a layout by language ID (lower 16 bits)
    /// This is a fallback when exact layout ID doesn't match
    /// NOTE: Disabled for en-US to avoid matching QWERTY instead of Dvorak
    /// </summary>
    public static KeyboardLayoutConfig? GetByLanguageId(int langId)
    {
        // Don't use language ID fallback for en-US to avoid matching QWERTY instead of Dvorak
        if (langId == UsDvorak.GetLanguageId()) return UsDvorak;
        if (langId == FrenchStandard.GetLanguageId()) return FrenchStandard;

        return null;
    }
}
