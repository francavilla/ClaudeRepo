namespace PasswordGen.Mobile.Services;

/// <summary>
/// Palette chiara e scura dell'app. I colori usati nei file XAML stanno nelle risorse di App.xaml (con valori chiari di partenza):
/// <see cref="Apply"/> li sostituisce in base al tema del telefono; i colori calcolati nei ViewModel li leggono da qui.
/// </summary>
public static class AppPalette
{
    public static bool IsDark { get; private set; }

    public static void Apply(ResourceDictionary resources, bool dark)
    {
        IsDark = dark;
        Set(resources, "Accent", dark ? "#3B82F6" : "#2563EB");
        Set(resources, "Header", dark ? "#111C3F" : "#1E3A8A");
        Set(resources, "Muted", dark ? "#94A3B8" : "#64748B");
        Set(resources, "PageBg", dark ? "#0F172A" : "#F1F5F9");
        Set(resources, "AccentSoft", dark ? "#1E3A5F" : "#DBEAFE");
        Set(resources, "CardBg", dark ? "#1E293B" : "#FFFFFF");
        Set(resources, "TextPrimary", dark ? "#E2E8F0" : "#0F172A");
        Set(resources, "Border", dark ? "#334155" : "#E2E8F0");
        Set(resources, "BorderStrong", dark ? "#475569" : "#CBD5E1");
        Set(resources, "Danger", dark ? "#F87171" : "#DC2626");
        Set(resources, "DangerBorder", dark ? "#7F1D1D" : "#FECACA");
        Set(resources, "WarnBg", dark ? "#3A2F12" : "#FEF3C7");
        Set(resources, "WarnSoft", dark ? "#3A2F12" : "#FFFBEB");
        Set(resources, "WarnBorder", dark ? "#78591B" : "#FDE68A");
        Set(resources, "WarnText", dark ? "#FBBF24" : "#B45309");
    }

    private static void Set(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = Color.FromArgb(hex);
    }

    // Colori calcolati nei ViewModel.
    public static Color Accent => Color.FromArgb(IsDark ? "#3B82F6" : "#2563EB");

    public static Color CardBackground => Color.FromArgb(IsDark ? "#1E293B" : "#FFFFFF");

    public static Color SelectedBackground => Color.FromArgb(IsDark ? "#1E3A5F" : "#E0E7FF");

    public static Color ReminderExpired => Color.FromArgb(IsDark ? "#3B1D22" : "#FEE2E2");

    public static Color ReminderDueSoon => Color.FromArgb(IsDark ? "#3A2F12" : "#FEF3C7");

    public static Color ReminderOk => Color.FromArgb(IsDark ? "#12301F" : "#DCFCE7");

    public static Color ReminderInfo => Color.FromArgb(IsDark ? "#172A4D" : "#DBEAFE");

    public static Color StrengthWeak => Color.FromArgb(IsDark ? "#F87171" : "#DC2626");

    public static Color StrengthFair => Color.FromArgb(IsDark ? "#FBBF24" : "#B45309");

    public static Color StrengthGood => Color.FromArgb(IsDark ? "#4ADE80" : "#15803D");
}
