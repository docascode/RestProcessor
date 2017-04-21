namespace RestProcessor
{
    using System;
    using System.Linq;

    public class GroupServiceMapping
    {
        public string Group { get; private set; }

        public string Service { get; private set; }

        public string TocTitle { get; private set; }

        public static GroupServiceMapping FromTocTitle(string tocTitle)
        {
            var splitResult = SplitTocTitle(tocTitle);
            return new GroupServiceMapping
            {
                Group = splitResult.Length == 2 ? splitResult[0] : null,
                Service = splitResult.Length == 2 ? splitResult[1] : splitResult[0],
                TocTitle = tocTitle
            };
        }

        private static string[] SplitTocTitle(string tocTitle)
        {
            var splitResult = tocTitle?.Split('/');
            if (splitResult == null || splitResult.Count() > 2)
            {
                throw new InvalidOperationException($"Cannot parse group name and service from toc title `{tocTitle}`, please make sure this toc title contains no more than 1 '/'.");
            }
            return splitResult;
        }
    }
}
