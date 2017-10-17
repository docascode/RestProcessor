namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using RestProcessor.Model;

    public class OrgsMappingProcessor : MappingProcessorBase
    {
        public void Process(string sourceRootDir, string targetRootDir, OrgsMappingFile orgsMappingFile)
        {
            // Sort by org and service name
            SortOrgsMappingFile(orgsMappingFile);

            // Generate auto summary page
            GenerateAutoPage(targetRootDir, orgsMappingFile);

            // Write toc structure from OrgsMappingFile
            WriteToc(sourceRootDir, targetRootDir, orgsMappingFile);
        }

        private static void WriteToc(string sourceRootDir, string targetRootDir, OrgsMappingFile orgsMappingFile)
        {
            var targetApiDir = GetApiDirectory(targetRootDir, orgsMappingFile.TargetApiRootDir);
            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            var mappingConfig = new MappingConfig
            {
                IsOperationLevel = orgsMappingFile.IsOperationLevel,
                IsGroupedByTag = orgsMappingFile.IsGroupdedByTag,
                SplitOperationCountGreaterThan = orgsMappingFile.SplitOperationCountGreaterThan,
                NeedResolveXMsPaths = orgsMappingFile.NeedResolveXMsPaths
            };

            using (var writer = new StreamWriter(targetTocPath))
            {
                // Write auto generated apis page
                if (orgsMappingFile.ApisPageOptions?.EnableAutoGenerate == true)
                {
                    if (string.IsNullOrEmpty(orgsMappingFile.ApisPageOptions.TargetFile))
                    {
                        throw new InvalidOperationException("Target file of apis page options should not be null or empty.");
                    }
                    var targetIndexPath = Path.Combine(targetRootDir, orgsMappingFile.ApisPageOptions.TargetFile);
                    writer.WriteLine($"# [{orgsMappingFile.ApisPageOptions.TocTitle}]({FileUtility.GetRelativePath(targetIndexPath, targetApiDir)})");
                }

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
                    else if(orgsMappingFile.ApisPageOptions?.EnableAutoGenerate != true && !string.IsNullOrEmpty(orgInfo.DefaultTocTitle) && !string.IsNullOrEmpty(orgInfo.OrgIndex))
                    {
                        writer.WriteLine($"# [{orgInfo.DefaultTocTitle}]({GenerateIndexHRef(targetRootDir, orgInfo.OrgIndex, targetApiDir)})");
                    }

                    // Sort by service name
                    orgInfo.Services.Sort((a, b) => a.TocTitle.CompareTo(b.TocTitle));

                    // Write service info
                    foreach (var service in orgInfo.Services)
                    {
                        // 1. Top toc
                        Console.WriteLine($"Created conceptual toc item '{service.TocTitle}'");
                        writer.WriteLine(!string.IsNullOrEmpty(service.IndexFile)
                            ? $"{subTocPrefix}# [{service.TocTitle}]({GenerateIndexHRef(targetRootDir, service.IndexFile, targetApiDir)})"
                            : $"{subTocPrefix}# {service.TocTitle}");

                        // 2. Parse and split REST swaggers
                        var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
                        if (service.SwaggerInfo != null)
                        {
                            subTocDict = SplitSwaggers(sourceRootDir, targetApiDir, service, mappingConfig);
                        }

                        // 3. Conceptual toc
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

                        // 4. Write REST toc
                        if (service.SwaggerInfo != null)
                        {
                            var subRefTocPrefix = string.Empty;
                            if (tocLines != null && tocLines.Count > 0)
                            {
                                subRefTocPrefix = IncreaseSharpCharacter(subRefTocPrefix);
                                writer.WriteLine($"{subTocPrefix}#{subRefTocPrefix} Reference");
                            }

                            foreach (var pair in subTocDict)
                            {
                                var subGroupTocPrefix = subRefTocPrefix;
                                if (!string.IsNullOrEmpty(pair.Key))
                                {
                                    subGroupTocPrefix = IncreaseSharpCharacter(subRefTocPrefix);
                                    writer.WriteLine($"{subTocPrefix}#{subGroupTocPrefix} {pair.Key}");
                                }
                                var subTocList = pair.Value;
                                subTocList.Sort((x, y) => string.CompareOrdinal(x.Title, y.Title));
                                foreach (var subToc in subTocList)
                                {
                                    writer.WriteLine($"{subTocPrefix}##{subGroupTocPrefix} [{subToc.Title}]({subToc.FilePath})");
                                    if (subToc.ChildrenToc.Count > 0)
                                    {
                                        foreach (var child in subToc.ChildrenToc)
                                        {
                                            writer.WriteLine($"{subTocPrefix}###{subGroupTocPrefix} [{child.Title}]({child.FilePath})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateAutoPage(string targetRootDir, OrgsMappingFile orgsMappingFile)
        {
            if (orgsMappingFile.ApisPageOptions == null || !orgsMappingFile.ApisPageOptions.EnableAutoGenerate)
            {
                return;
            }

            var targetIndexPath = Path.Combine(targetRootDir, orgsMappingFile.ApisPageOptions.TargetFile);
            if (File.Exists(targetIndexPath))
            {
                Console.WriteLine($"Cleaning up previous existing {targetIndexPath}.");
                File.Delete(targetIndexPath);
            }

            using (var writer = new StreamWriter(targetIndexPath))
            {
                var summaryFile = Path.Combine(targetRootDir, orgsMappingFile.ApisPageOptions.SummaryFile);
                if (File.Exists(summaryFile))
                {
                    foreach (var line in File.ReadAllLines(summaryFile))
                    {
                        writer.WriteLine(line);
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("## All Product APIs");

                foreach (var orgInfo in orgsMappingFile.OrgInfos)
                {
                    // Org name as title
                    if (!string.IsNullOrWhiteSpace(orgInfo.OrgName))
                    {
                        writer.WriteLine($"### {orgInfo.OrgName}");
                        writer.WriteLine();
                    }

                    // Service table
                    if (orgInfo.Services.Count > 0)
                    {
                        writer.WriteLine("| Service | Description |");
                        writer.WriteLine("|---------|-------------|");
                        foreach (var service in orgInfo.Services)
                        {
                            if (string.IsNullOrWhiteSpace(service.IndexFile) && !File.Exists(service.IndexFile))
                            {
                                throw new InvalidOperationException($"Index file {service.IndexFile} of service {service.TocTitle} should exists.");
                            }
                            var summary = Utility.GetYamlHeaderByMeta(Path.Combine(targetRootDir, service.IndexFile), orgsMappingFile.ApisPageOptions.ServiceDescriptionMetadata);
                            writer.WriteLine($"| [{service.TocTitle}](~/{service.IndexFile}) | {summary ?? string.Empty} |");
                        }
                        writer.WriteLine();
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

        private static SortedDictionary<string, List<SwaggerToc>> SplitSwaggers(string sourceRootDir, string targetApiDir, ServiceInfo service, MappingConfig mappingConfig)
        {
            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();

            foreach (var swagger in service.SwaggerInfo)
            {
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, service.UrlGroup));
                var sourceFile = Path.Combine(sourceRootDir, swagger.Source.TrimEnd());

                var restFileInfo = RestSplitter.Split(targetDir, sourceFile, service.TocTitle, swagger.OperationGroupMapping, mappingConfig);

                if (restFileInfo == null)
                {
                    continue;
                }
                var tocTitle = Utility.ExtractPascalNameByRegex(restFileInfo.TocTitle);

                var subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                List<SwaggerToc> subTocList;
                if (!subTocDict.TryGetValue(subGroupName, out subTocList))
                {
                    subTocList = new List<SwaggerToc>();
                    subTocDict.Add(subGroupName, subTocList);
                }

                foreach (var fileNameInfo in restFileInfo.FileNameInfos)
                {
                    var subTocTitle = fileNameInfo.TocName;
                    var filePath = FileUtility.NormalizePath(Path.Combine(service.UrlGroup, fileNameInfo.FileName));

                    if (subTocList.Any(toc => toc.Title == subTocTitle))
                    {
                        throw new InvalidOperationException($"Sub toc '{subTocTitle}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{swagger.Source}' to avoid conflicting");
                    }

                    var childrenToc = new List<SwaggerToc>();
                    if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                    {
                        foreach (var nameInfo in fileNameInfo.ChildrenFileNameInfo)
                        {
                            childrenToc.Add(new SwaggerToc(nameInfo.TocName, FileUtility.NormalizePath(Path.Combine(service.UrlGroup, nameInfo.FileName))));
                        }
                    }

                    subTocList.Add(new SwaggerToc(subTocTitle, filePath, childrenToc));
                }
                Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
            }

            return subTocDict;
        }
    }
}
