namespace DocuMind.Application.Common;

/// <summary>
/// Picks the English or Vietnamese form of a message shown to the user.
/// The WPF layer sets <see cref="Vietnamese"/> from the EN / VI switch (UiText), so status lines,
/// error messages and validation messages follow the same language as the rest of the window.
/// </summary>
public static class AppText
{
    private static volatile bool _vietnamese;

    public static bool Vietnamese
    {
        get => _vietnamese;
        set => _vietnamese = value;
    }

    public static string T(string english, string vietnamese) => Vietnamese ? vietnamese : english;
}
