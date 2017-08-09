namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class MappingProcessor : MappingProcessorBase
    {
        public void Process(string sourceRootDir, string targetRootDir, MappingFile mappingFile)
        {
            var targetApiDir = GetApiDirectory(targetRootDir, mappingFile.TargetApiRootDir);

            // Mapping for group name and full toc title name
            var groupTocsMapping = new SortedDictionary<string, List<string>>();
            var tocDict = new Dictionary<string, List<SwaggerToc>>();
            foreach (var mappingItem in mappingFile.Mapping.ReferenceItems)
            {
                // Split rest files
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, mappingItem.TargetDir));
                var sourceFile = Path.Combine(sourceRootDir, mappingItem.SourceSwagger);
                var restFileInfo = RestSplitter.Process(targetDir, sourceFile, string.Empty, mappingItem.OperationGroupMapping, false, false);
                if (restFileInfo == null)
                {
                    continue;
                }

                // Extract top TOC title
                var tocTitle = string.IsNullOrEmpty(mappingItem.TocTitle)
                    ? Utility.ExtractPascalNameByRegex(restFileInfo.TocTitle)
                    : mappingItem.TocTitle;

                // Update toc dictionary
                List<SwaggerToc> subTocList;
                if (!tocDict.TryGetValue(tocTitle, out subTocList))
                {
                    subTocList = new List<SwaggerToc>();
                    tocDict.Add(tocTitle, subTocList);
                }

                // Sort sub TOC
                restFileInfo.FileNameInfos.Sort((a, b) => string.CompareOrdinal(a.FileName, b.FileName));

                // Generate sub TOC
                foreach (var fileNameInfo in restFileInfo.FileNameInfos)
                {
                    var subTocTitle = fileNameInfo.TocName;
                    var filePath = FileUtility.NormalizePath(Path.Combine(mappingItem.TargetDir, fileNameInfo.FileName));
                    if (subTocList.Any(toc => toc.Title == subTocTitle))
                    {
                        throw new InvalidOperationException($"Sub toc '{subTocTitle}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{mappingItem.SourceSwagger}' to avoid conflicting");
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

                // Add info to groupTocsMapping
                var groupServiceMapping = GroupServiceMapping.FromTocTitle(docItem.TocTitle);
                var groupKey = groupServiceMapping.Group ?? string.Empty;
                List<string> tocs;
                if (!groupTocsMapping.TryGetValue(groupKey, out tocs))
                {
                    tocs = new List<string>();
                    groupTocsMapping.Add(groupKey, tocs);
                }
                // Follow the sequence of documentation items
                tocs.Add(docItem.TocTitle);
            }

            Console.WriteLine("Start to generate toc.md");
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            using (var sw = new StreamWriter(targetTocPath))
            {
                foreach (var group in groupTocsMapping)
                {
                    var groupName = group.Key;
                    var subTocPrefix = string.Empty;
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        Console.WriteLine($"Created top grouped toc item '{groupName}'");
                        sw.WriteLine($"# {groupName}");
                        subTocPrefix = "#";
                    }

                    var fullTocTitles = group.Value;
                    foreach (var fullTocTitle in fullTocTitles)
                    {
                        DocumentationItem docItem;
                        if (!docDict.TryGetValue(fullTocTitle, out docItem))
                        {
                            throw new InvalidOperationException($"Toc title `{fullTocTitle}` should exist in documentation node of mapping.json.");
                        }
                        var groupServiceMapping = GroupServiceMapping.FromTocTitle(fullTocTitle);

                        // 1. Top toc
                        Console.WriteLine($"Created conceptual toc item '{groupServiceMapping.Service}'");
                        sw.WriteLine(!string.IsNullOrEmpty(docItem.SourceIndex)
                            ? $"{subTocPrefix}# [{groupServiceMapping.Service}]({GenerateIndexHRef(targetRootDir, docItem.SourceIndex, targetApiDir)})"
                            : $"{subTocPrefix}# {groupServiceMapping.Service}");

                        // 2. Conceptual toc
                        List<string> tocLines = null;
                        if (!string.IsNullOrEmpty(docItem.SourceToc))
                        {
                            tocLines = GenerateDocTocItems(targetRootDir, docItem.SourceToc, targetApiDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
                        }

                        if (tocLines != null && tocLines.Count > 0)
                        {
                            foreach (var tocLine in tocLines)
                            {
                                // Insert one heading before to make it sub toc
                                sw.WriteLine($"{subTocPrefix}#{tocLine}");
                            }
                            Console.WriteLine($"-- Created sub referenced toc items under conceptual toc item '{groupServiceMapping.Service}'");
                        }

                        // 3. REST toc
                        List<SwaggerToc> swaggerToc;
                        if (tocDict.TryGetValue(fullTocTitle, out swaggerToc))
                        {
                            swaggerToc.Sort((x, y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
                            // Only reference TOC with conceptual TOC should insert 'Reference' text
                            if (tocLines != null && tocLines.Count > 0)
                            {
                                sw.WriteLine($"{subTocPrefix}## Reference");
                                foreach (var subToc in swaggerToc)
                                {
                                    sw.WriteLine($"{subTocPrefix}### [{subToc.Title}]({subToc.FilePath})");
                                }
                            }
                            else
                            {
                                foreach (var subToc in swaggerToc)
                                {
                                    sw.WriteLine($"{subTocPrefix}## [{subToc.Title}]({subToc.FilePath})");
                                }
                            }
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
    }
}
