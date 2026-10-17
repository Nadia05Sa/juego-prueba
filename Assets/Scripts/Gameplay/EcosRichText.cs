using System.Text.RegularExpressions;

/// <summary>
/// Convierte comillas tipográficas / dobles en negrita de Unity UI (rich text).
/// </summary>
public static class EcosRichText
{
    private static readonly Regex Guillemets = new Regex(@"«([^»]*)»", RegexOptions.Compiled);
    private static readonly Regex DoubleAngles = new Regex(@"<<([^>]*)>>", RegexOptions.Compiled);

    public static string Format(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = Guillemets.Replace(text, "<b>$1</b>");
        text = DoubleAngles.Replace(text, "<b>$1</b>");
        return text;
    }
}
