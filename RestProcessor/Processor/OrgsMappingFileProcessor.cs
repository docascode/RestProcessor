namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class OrgsMappingProcessor : MappingProcessorBase
    {
        public void Process(string sourceRootDir, string targetRootDir, OrgsMappingFile orgsMappingFile)
        {
            // Sort by org and service name
            SortOrgsMappingFile(orgsMappingFile);

            // Write toc structure from OrgsMappingFile
            WriteToc(sourceRootDir, targetRootDir, orgsMappingFile);
        }

        private static void WriteToc(string sourceRootDir, string targetRootDir, OrgsMappingFile orgsMappingFile)
        {
            var targetApiDir = GetApiDirectory(targetRootDir, orgsMappingFile.TargetApiRootDir);
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            using (var writer = new StreamWriter(targetTocPath))
            {
                // Write organization info
                foreach (var orgInfo in orgsMappingFile.OrgInfos)
                {
                    // Deal with org name and index
                    var subTocPrefix = string.Empty;
                    if (!string.IsNullOrEmpty(orgInfo.OrgName))
                    {
                        // Write index
                        writer.WriteLine(!string.IsNullOrEmpty(orgInfo.OrgIndex)
                            ? $"# [{orgInfo.OrgName}]({GenerateIndexHRef(targetRootDir, orgInfo.OrgIndex, targetApiDir)})"
                            : $"# {orgInfo.OrgName}");
                        subTocPrefix = "#";
                    }
                    else if(!string.IsNullOrEmpty(orgInfo.DefaultTocTitle) && !string.IsNullOrEmpty(orgInfo.OrgIndex))
                    {
                        writer.WriteLine($"# [{orgInfo.DefaultTocTitle}]({GenerateIndexHRef(targetRootDir, orgInfo.OrgIndex, targetApiDir)})");
                    }

                    // Write service info
                    foreach (var service in orgInfo.Services)
                    {
                        // 1. Top toc
                        Console.WriteLine($"Created conceptual toc item '{service.TocTitle}'");
                        writer.WriteLine(!string.IsNullOrEmpty(service.IndexFile)
                            ? $"{subTocPrefix}# [{service.TocTitle}]({GenerateIndexHRef(targetRootDir, service.IndexFile, targetApiDir)})"
                            : $"{subTocPrefix}# {service.TocTitle}");

                        // 2. Conceptual toc
                        List<string> tocLines = null;
                        if (!string.IsNullOrEmpty(service.TocFile))
                        {
                            tocLines = GenerateDocTocItems(targetRootDir, service.TocFile, targetApiDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
                            if (tocLines.Any())
                            {
                                foreach (var tocLine in tocLines)
                                {
                                    // Insert one heading before to make it sub toc
                                    writer.WriteLine($"{subTocPrefix}#{tocLine}");
                                }
                                Console.WriteLine($"-- Created sub referenced toc items under conceptual toc item '{service.TocTitle}'");
                            }
                        }

                        // 3. REST toc
                        var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
                        foreach (var swagger in service.SwaggerInfo)
                        {
                            var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, service.UrlGroup));
                            var sourceFile = Path.Combine(sourceRootDir, swagger.Source);
                            var restFileInfo = RestSplitter.Process(targetDir, sourceFile, swagger.OperationGroupMapping);
                            var tocTitle = Utility.ExtractPascalName(restFileInfo.TocTitle);

                            var subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                            List<SwaggerToc> subTocList;
                            if (!subTocDict.TryGetValue(subGroupName, out subTocList))
                            {
                                subTocList = new List<SwaggerToc>();
                                subTocDict.Add(subGroupName, subTocList);
                            }

                            foreach (var fileName in restFileInfo.FileNames)
                            {
                                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                                var subTocTitle = Utility.ExtractPascalName(fileNameWithoutExt);
                                var filePath = FileUtility.NormalizePath(Path.Combine(service.UrlGroup, fileName));


                                if (subTocList.Any(toc => toc.Title == subTocTitle))
                                {
                                    throw new InvalidOperationException($"Sub toc '{fileNameWithoutExt}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{swagger.Source}' to avoid conflicting");
                                }

                                subTocList.Add(new SwaggerToc(subTocTitle, filePath));
                            }
                            Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
                        }

                        var subRefTocPrefix = string.Empty;
                        if (tocLines != null && tocLines.Count > 0)
                        {
                            writer.WriteLine($"{subTocPrefix}#{subRefTocPrefix} Reference");
                            IncreaseSharpCharacter(subRefTocPrefix);
                        }

                        foreach (var pair in subTocDict)
                        {
                            if (!string.IsNullOrEmpty(pair.Key))
                            {
                                writer.WriteLine($"{subTocPrefix}##{subRefTocPrefix} {pair.Key}");
                                IncreaseSharpCharacter(subRefTocPrefix);
                            }
                            var subTocList = pair.Value;
                            subTocList.Sort((x, y) => string.CompareOrdinal(x.Title, y.Title));
                            foreach (var subToc in subTocList)
                            {
                                writer.WriteLine($"{subTocPrefix}###{subRefTocPrefix} [{subToc.Title}]({subToc.FilePath})");
                            }
                        }
                    }
                }
            }
        }

        private static void SortOrgsMappingFile(OrgsMappingFile orgsMappingFile)
        {
            orgsMappingFile.OrgInfos.Sort((x, y) => string.CompareOrdinal(x.OrgName, y.OrgName));
            foreach (var orgInfo in orgsMappingFile.OrgInfos)
            {
                orgInfo.Services.Sort((x, y) => string.CompareOrdinal(x.TocTitle, y.TocTitle));
            }
        }
    }
}
