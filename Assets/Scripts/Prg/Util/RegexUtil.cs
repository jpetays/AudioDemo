using System.Text.RegularExpressions;

namespace Prg.Util
{
    public static class RegexUtil
    {
        private static readonly Regex
            TagsRegex = new(@"<[^>]*>", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex
            EmptyLines = new(@"^\s+$[\r\n|\n\r|\r|\n]*",
                RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static string RemoveAllTags(string text, string replacement = "") =>
            TagsRegex.Replace(text, string.Empty);

        public static string RemoveAllEmptyLines(string text) => EmptyLines.Replace(text, string.Empty);

        public static string ReplaceCrLf(string text, string replacement) =>
            Regex.Replace(text, @"\r\n|\n\r|\r|\n", replacement);
    }
}
