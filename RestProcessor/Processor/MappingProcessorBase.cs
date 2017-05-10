namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using System.Text.RegularExpressions;

    public class MappingProcessorBase
    {
        protected const string TocFileName = "toc.md";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        protected static string GetApiDirectory(string rootDirectory, string targetApiRootDir)
        {
            var targetApiDir = Path.Combine(rootDirectory, targetApiRootDir);
            if (Directory.Exists(targetApiDir))
            {
                // Clear last built target api folder
                Directory.Delete(targetApiDir, true);
                Console.WriteLine($"Done cleaning previous existing {targetApiDir}");
            }
            Directory.CreateDirectory(targetApiDir);
            if (!targetApiDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                targetApiDir = targetApiDir + Path.DirectorySeparatorChar;
            }
            return targetApiDir;
        }

        protected static IEnumerable<string> GenerateDocTocItems(string targetRootDir, string tocRelativePath, string targetApiDir)
        {
            var tocPath = Path.Combine(targetRootDir, tocRelativePath);
            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException($"Toc file '{tocRelativePath}' not exists.");
            }
            var fileName = Path.GetFileName(tocPath);
            if (!fileName.Equals(TocFileName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Currently only '{TocFileName}' is supported as conceptual toc, please update the toc path '{tocRelativePath}'.");
            }
            var tocRelativeDirectoryToApi = FileUtility.GetRelativePath(Path.GetDirectoryName(tocPath), targetApiDir);

            foreach (var tocLine in File.ReadLines(tocPath))
            {
                var match = TocRegex.Match(tocLine);
                if (match.Success)
                {
                    var tocLink = match.Groups["tocLink"].Value;
                    var tocTitle = match.Groups["tocTitle"].Value;
                    var headerLevel = match.Groups["headerLevel"].Value.Length;
                    var tocLinkRelativePath = tocRelativeDirectoryToApi + "/" + tocLink;
                    var linkPath = Path.Combine(targetApiDir, tocLinkRelativePath);
                    if (!File.Exists(linkPath))
                    {
                        throw new FileNotFoundException($"Link '{tocLinkRelativePath}' not exist in '{tocRelativePath}', when merging into '{TocFileName}' of '{targetApiDir}'");
                    }
                    yield return $"{new string('#', headerLevel)} [{tocTitle}]({tocLinkRelativePath})";
                }
                else
                {
                    yield return tocLine;
                }
            }
        }

        protected static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiDir)
        {
            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return FileUtility.GetRelativePath(indexPath, targetApiDir);
        }

        protected class SwaggerToc
        {
            public string Title { get; }

            public string FilePath { get; }

            public SwaggerToc(string title, string filePath)
            {
                Title = title;
                FilePath = filePath;
            }
        }
    }
}
