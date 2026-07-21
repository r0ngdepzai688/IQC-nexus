using System.Text;
using System.Text.RegularExpressions;

namespace IqcQms.Application;

public static class MasterPlanBusinessKey
{
    private const char Separator = '\u001F';

    public static string NormalizeBasic(string? value) =>
        Regex.Replace((value ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim(), @"\s+", " ").ToUpperInvariant();

    public static string NormalizeCat(string? value) =>
        Regex.Replace((value ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim(), @"\s+", " ").ToUpperInvariant();

    public static bool TryCreate(string? basic, string? cat, out string basicKey, out string catKey, out string key)
    {
        basicKey = NormalizeBasic(basic);
        catKey = NormalizeCat(cat);
        key = basicKey.Length == 0 || catKey.Length == 0 ? string.Empty : $"{basicKey}{Separator}{catKey}";
        return key.Length > 0;
    }
}
