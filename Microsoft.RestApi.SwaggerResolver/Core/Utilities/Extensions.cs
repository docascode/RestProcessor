namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class Extensions
    {
        public static bool EqualsIgnoreCase(this string s1, Fixable<string> s2) => ReferenceEquals(s1, s2.Value) || s1.Equals(s2.Value, StringComparison.OrdinalIgnoreCase);
        public static string AdjustGithubUrl(this string url) => Regex.Replace(url,
            @"^((http|https)\:\/\/)?github\.com\/(?<user>[^\/]+)\/(?<repo>[^\/]+)\/blob\/(?<branch>[^\/]+)\/(?<file>.+)$",
            @"https://raw.githubusercontent.com/${user}/${repo}/${branch}/${file}");
        private static string[] LFOnly = new[] { ".py", ".rb", ".ts", ".js", ".java", ".go" };
        public static bool IsFileLineFeedOnly(this string filename) => LFOnly.Any(each => filename.EndsWith(each, StringComparison.OrdinalIgnoreCase));
        public static string LineEnding(this string filename) => filename.IsFileLineFeedOnly() ? "\n" : "\r\n";
    }
}
