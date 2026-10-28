using System.Text;

namespace KeyBrawler;

public static class AnswerText
{
    public static bool CanCorrect(string actual, string expected) => actual != expected && Normalize(actual) == expected;
    /// <summary>Normalize Turkish OCR letter variants without changing case, spacing or unrelated scripts.</summary>
    public static string Normalize(string text) => text.Normalize(NormalizationForm.FormC)
        .Replace('\u00e7', 'c').Replace('\u00c7', 'C')
        .Replace('\u011f', 'g').Replace('\u011e', 'G')
        .Replace('\u0131', 'i').Replace('\u0130', 'I')
        .Replace('\u00f6', 'o').Replace('\u00d6', 'O')
        .Replace('\u015f', 's').Replace('\u015e', 'S')
        .Replace('\u00fc', 'u').Replace('\u00dc', 'U');
}
