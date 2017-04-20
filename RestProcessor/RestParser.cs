namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

            var tocDict = new Dictionary<string, List<SwaggerToc>>();
            foreach (var mappingItem in mappingFile.Mapping.ReferenceItems)
            {
                // Split rest files
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, mappingItem.TargetDir));
                var sourceFile = Path.Combine(sourceRootDir, mappingItem.SourceSwagger);
                var restFileInfo = RestSplitter.Process(targetDir, sourceFile, mappingItem.OperationGroupMapping);

                // Extract top TOC title
                var tocTitle = string.IsNullOrEmpty(mappingItem.TocTitle)
                    ? Utility.ExtractPascalName(restFileInfo.TocTitle)
                    : mappingItem.TocTitle;

                // Update toc dictionary
                List<SwaggerToc> subTocList;
                if (!tocDict.TryGetValue(tocTitle, out subTocList))
                {
                    subTocList = new List<SwaggerToc>();
                    tocDict.Add(tocTitle, subTocList);
                }

                // Sort sub TOC
                restFileInfo.FileNames.Sort();

                // Generate sub TOC
                foreach (var fileName in restFileInfo.FileNames)
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var subTocTitle = Utility.ExtractPascalName(fileNameWithoutExt);
                    var filePath = FileUtility.NormalizePath(Path.Combine(mappingItem.TargetDir, fileName));
                    if (subTocList.Any(toc => toc.Title == subTocTitle))
                    {
                        throw new InvalidOperationException($"Sub toc '{fileNameWithoutExt}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{mappingItem.SourceSwagger}' to avoid conflicting");
                    }

                    subTocList.Add(new SwaggerToc(subTocTitle, filePath));
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
                    throw new InvalidOperationException($"For {nameof(DocumentationItem)}, toc_title should not by null or empty, source_index is '{docItem.SourceIndex}', source_toc is '{docItem.SourceToc}'.");
                }
                if (docDict.ContainsKey(docItem.TocTitle))
                {
                    throw new InvalidOperationException($"For {nameof(DocumentationItem)}, toc_title should be unique, duplicate toc_title '{docItem.TocTitle}' occurred.");
                }
                docDict.Add(docItem.TocTitle, docItem);
            }

            Console.WriteLine("Start to generate toc.md");
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            using (var sw = new StreamWriter(targetTocPath))
            {
                // Follow the sequence of documentation items
                foreach (var docItem in docDict)
                {
                    // 1. Top toc
                    Console.WriteLine($"Created top conceptual toc item '{docItem.Key}'");
                    sw.WriteLine(!string.IsNullOrEmpty(docItem.Value.SourceIndex)
                        ? $"# [{docItem.Key}]({GenerateIndexHRef(targetRootDir, docItem.Value.SourceIndex, targetApiDir)})"
                        : $"# {docItem.Key}");

                    // 2. Conceptual toc
                    List<string> tocLines = null;
                    if (!string.IsNullOrEmpty(docItem.Value.SourceToc))
                    {
                        tocLines = GenerateDocTocItems(targetRootDir, docItem.Value.SourceToc, targetApiDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
                    }

                    if (tocLines != null)
                    {
                        foreach (var tocLine in tocLines)
                        {
                            // Insert one heading before to make it sub toc
                            sw.WriteLine($"#{tocLine}");
                        }
                        Console.WriteLine($"-- Created sub referenced toc items under conceptual toc item '{docItem.Key}'");
                    }

                    // 3. REST toc
                    List<SwaggerToc> swaggerToc;
                    if (tocDict.TryGetValue(docItem.Key, out swaggerToc))
                    {
                        swaggerToc.Sort((x, y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
                        // Only reference TOC with conceptual TOC should insert 'Reference' text
                        if (tocLines != null)
                        {
                            sw.WriteLine("## Reference");
                        }
                        foreach (var subToc in swaggerToc)
                        {
                            sw.WriteLine($"### [{subToc.Title}]({subToc.FilePath})");
                        }
                    }
                }

                // Write the toc of remaining REST
                foreach (var tocItem in tocDict.Where(i => !docDict.ContainsKey(i.Key)))
                {
                    // 1. Top toc
                    sw.WriteLine($"# {tocItem.Key}");

                    // 2. REST toc
                    tocItem.Value.Sort((x, y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
                    foreach (var subToc in tocItem.Value)
                    {
                        sw.WriteLine($"## [{subToc.Title}]({subToc.FilePath})");
                    }

                    Console.WriteLine($"Created top referenced toc item '{tocItem.Key}' which has no conceptual pages");
                }
            }
        }

        private class SwaggerToc
        {
            public string Title { get; }

            public string FilePath { get; }

            public SwaggerToc(string title, string filePath)
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
                    var tocLinkRelativePath = tocRelativeDirectoryToApi + "/" +tocLink;
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

        private static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiDir)
        {
            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return FileUtility.GetRelativePath(indexPath, targetApiDir);
        }
    }
}
