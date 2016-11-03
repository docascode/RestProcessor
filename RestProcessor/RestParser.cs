namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    public static class RestParser
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();
        private const string TocFileName = "toc.md";
        public static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);

        public static void Process(string sourceRootDir, string targetRootDir, string mappingFilePath)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }
            if (!File.Exists(mappingFilePath))
            {
                throw new ArgumentException($"{nameof(mappingFilePath)} '{mappingFilePath}' should exist.");
            }

            MappingFile mapping;
            using (var streamReader = File.OpenText(mappingFilePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                mapping = JsonSerializer.Deserialize<MappingFile>(reader);
            }

            ProcessCore(sourceRootDir, targetRootDir, mapping);
            Console.WriteLine("Done processing all swagger files");
        }

        private static void ProcessCore(string sourceRootDir, string targetRootDir, MappingFile mappingFile)
        {
            var targetApiDir = Path.Combine(targetRootDir, mappingFile.TargetApiRootDir);
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

            var tocDict = new Dictionary<string, List<SubToc>>();
            foreach (var mappingItem in mappingFile.Mapping.ReferenceItems)
            {
                // Split rest files
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, mappingItem.TargetDir));
                var sourceFile = Path.Combine(sourceRootDir, mappingItem.SourceSwagger);
                var restFileInfo = RestSplitter.Process(targetDir, sourceFile, mappingItem.OperationGroupMapping);

                // Extract top TOC title
                var tocTitle = string.IsNullOrEmpty(mappingItem.TocTitle)
                    ? ExtractPascalName(restFileInfo.TocTitle)
                    : mappingItem.TocTitle;

                // Update toc dictionary
                List<SubToc> subTocList;
                if (!tocDict.TryGetValue(tocTitle, out subTocList))
                {
                    subTocList = new List<SubToc>();
                    tocDict.Add(tocTitle, subTocList);
                }

                // Sort sub TOC
                restFileInfo.FileNames.Sort();

                // Generate sub TOC
                foreach (var fileName in restFileInfo.FileNames)
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var subTocTitle = ExtractPascalName(fileNameWithoutExt);
                    var filePath = FileUtility.NormalizePath(Path.Combine(mappingItem.TargetDir, fileName));
                    if (subTocList.Any(toc => toc.Title == subTocTitle))
                    {
                        throw new InvalidOperationException($"Sub toc {subTocTitle} under {tocTitle} has been added into toc.md, please add operation group name mapping to avoid conflicting");
                    }

                    subTocList.Add(new SubToc(subTocTitle, filePath));
                }

                Console.WriteLine($"Done splitting swagger file from '{mappingItem.SourceSwagger}' to '{mappingItem.TargetDir}'");
            }

            // Extract dictionary for documentation toc and index
            Console.WriteLine("Start to extract documentation toc and index");
            var docDict = new Dictionary<string, DocumentationItem>();
            foreach (var docItem in mappingFile.Mapping.DocumentationItems)
            {
                if (string.IsNullOrEmpty(docItem.TocTitle))
                {
                    throw new InvalidOperationException($"For {nameof(DocumentationItem)}, toc_title should not by null or empty, source_index is {docItem.SourceIndex}, source_toc is {docItem.SourceToc}.");
                }
                if (docDict.ContainsKey(docItem.TocTitle))
                {
                    throw new InvalidOperationException($"For {nameof(DocumentationItem)}, toc_title should be unique, duplicate toc_title {docItem.TocTitle} occured.");
                }
                docDict.Add(docItem.TocTitle, docItem);
            }

            Console.WriteLine("Start to generate toc.md");
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            using (var sw = new StreamWriter(targetTocPath))
            {
                foreach (var toc in tocDict)
                {
                    List<string> tocLines = null;
                    DocumentationItem docItem;
                    if (docDict.TryGetValue(toc.Key, out docItem) && !string.IsNullOrEmpty(docItem.SourceToc))
                    {
                        tocLines = GenerateDocTocItems(targetRootDir, docItem.SourceToc, targetApiDir).ToList();
                    }

                    // 1. Top toc
                    sw.WriteLine(!string.IsNullOrEmpty(docItem?.SourceIndex)
                        ? $"# [{toc.Key}]({GenerateIndexHRef(targetRootDir, docItem.SourceIndex, targetApiDir)})"
                        : $"# {toc.Key}");

                    // 2. Conceptual toc
                    if (tocLines != null)
                    {
                        foreach (var tocLine in tocLines)
                        {
                            // Insert one heading before to make it sub toc
                            sw.WriteLine($"#{tocLine}");
                        }
                    }

                    // 3. REST toc
                    toc.Value.Sort((x, y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
                    foreach (var subToc in toc.Value)
                    {
                        sw.WriteLine($"## [{subToc.Title}]({subToc.FilePath})");
                    }
                }
            }
        }

        private class SubToc
        {
            public string Title { get; }

            public string FilePath { get; }

            public SubToc(string title, string filePath)
            {
                Title = title;
                FilePath = filePath;
            }
        }

        private static IEnumerable<string> GenerateDocTocItems(string targetRootDir, string tocRelativePath, string targetApiDir)
        {
            var tocPath = Path.Combine(targetRootDir, tocRelativePath);
            if (!File.Exists(tocPath))
            {
                throw new FileNotFoundException($"Toc file '{tocRelativePath}' not exists.");
            }
            var tocRelativeDirectoryToApi = GetRelativePath(Path.GetDirectoryName(tocPath), targetApiDir);

            foreach (var tocLine in File.ReadLines(tocPath))
            {
                var match = TocRegex.Match(tocLine);
                if (match.Success)
                {
                    var tocLink = match.Groups["tocLink"].Value;
                    var tocTitle = match.Groups["tocTitle"].Value;
                    var headerLevel = match.Groups["headerLevel"].Value.Length;
                    var tocLinkRelativePath = tocRelativeDirectoryToApi + "/" +tocLink;
                    yield return $"{new string('#', headerLevel)} [{tocTitle}]({tocLinkRelativePath})";
                }
                else
                {
                    yield return tocLine;
                }
            }
        }

        private static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiDir)
        {
            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return GetRelativePath(indexPath, targetApiDir);
        }

        private static string GetRelativePath(string path, string basePath)
        {
            var pathUri = new Uri(path);
            var basePathUri = new Uri(basePath);
            var relativePathToBaseUri = basePathUri.MakeRelativeUri(pathUri);
            return relativePathToBaseUri.OriginalString.Length == 0 ? Path.GetFileName(path) : relativePathToBaseUri.OriginalString;
        }

        private static string ExtractPascalName(string name)
        {
            var list = new HashSet<string> { "BI", "IP", "ML", "MAM", "VM", "VMs" };
            if (name.Contains(" "))
            {
                return name;
            }

            var result = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                // Exclude index = 0
                var c = name[i];
                if (i != 0 &&
                    char.IsUpper(c))
                {
                    var closestUpperCaseWord = GetClosestUpperCaseWord(name, i);
                    if (closestUpperCaseWord.Length > 0)
                    {
                        if (list.Contains(closestUpperCaseWord))
                        {
                            result.Append(" ");
                            result.Append(closestUpperCaseWord);
                            i = i + closestUpperCaseWord.Length - 1;
                            continue;
                        }

                        var closestCamelCaseWord = GetClosestCamelCaseWord(name, i);
                        if (list.Contains(closestCamelCaseWord))
                        {
                            result.Append(" ");
                            result.Append(closestCamelCaseWord);
                            i = i + closestCamelCaseWord.Length - 1;
                            continue;
                        }
                    }
                    result.Append(" ");
                }
                result.Append(c);
            }

            return result.ToString();
        }

        private static string GetClosestUpperCaseWord(string word, int index)
        {
            var result = new StringBuilder();
            for (var i = index; i < word.Length; i++)
            {
                var character = word[i];
                if (char.IsUpper(character))
                {
                    result.Append(character);
                }
                else
                {
                    break;
                }
            }

            if (result.Length == 0)
            {
                return string.Empty;
            }

            // Remove the last character, which is unlikely the continues upper case word.
            return result.ToString(0, result.Length - 1);
        }

        private static string GetClosestCamelCaseWord(string word, int index)
        {
            var result = new StringBuilder();
            var meetLowerCase = false;
            for (var i = index; i < word.Length; i++)
            {
                var character = word[i];
                if (char.IsUpper(character))
                {
                    if (meetLowerCase)
                    {
                        return result.ToString();
                    }
                }
                else
                {
                    meetLowerCase = true;
                }
                result.Append(character);
            }

            return result.ToString();
        }
    }
}
