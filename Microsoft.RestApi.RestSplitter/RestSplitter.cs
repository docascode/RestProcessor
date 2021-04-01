namespace Microsoft.RestApi.RestSplitter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter.Model;

    public class RestSplitter
    {
        private readonly string _sourceRootDir;
        private readonly string _targetRootDir;
        private readonly string _outputDir;
        private readonly OrgsMappingFile _mappingFile;
        private static IList<RestFileInfo> _restFileInfos;

        protected const string TocFileName = "toc.md";
        protected static readonly Regex TocRegex = new Regex(@"^(?<headerLevel>#+)(( |\t)*)\[(?<tocTitle>.+)\]\((?<tocLink>(?!http[s]?://).*?)\)( |\t)*#*( |\t)*(\n|$)", RegexOptions.Compiled);
        protected const string SourceSwaggerMappingFileName = "sourceMapping.json";

        public RestSplitter(string sourceRootDir, string targetRootDir, OrgsMappingFile mappingFile, string outputDir)
        {
            Guard.ArgumentNotNullOrEmpty(sourceRootDir, nameof(sourceRootDir));
            Guard.ArgumentNotNullOrEmpty(targetRootDir, nameof(targetRootDir));
            Guard.ArgumentNotNull(mappingFile, nameof(mappingFile));
            Guard.ArgumentNotNullOrEmpty(outputDir, nameof(outputDir));

            _sourceRootDir = sourceRootDir;
            _targetRootDir = targetRootDir;
            _mappingFile = mappingFile;
            _outputDir = outputDir;
            _restFileInfos = new List<RestFileInfo>();
        }

        public IList<RestFileInfo> Process()
        {
            // Sort by org and service name
            SortOrgsMappingFile(_mappingFile);

            // Generate auto summary page
            GenerateAutoPage(_targetRootDir, _mappingFile);

            // Write toc structure from OrgsMappingFile
            var targetApiDir = GetOutputDirectory(_outputDir);

            if (_mappingFile.VersionList != null)
            {
                // Generate with version infos
                foreach (var version in _mappingFile.VersionList)
                {
                    var targetApiVersionDir = Path.Combine(targetApiDir, version);
                    Directory.CreateDirectory(targetApiVersionDir);
                    if (!targetApiVersionDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        targetApiVersionDir = targetApiVersionDir + Path.DirectorySeparatorChar;
                    }
                    WriteToc(_sourceRootDir, _targetRootDir, _mappingFile, targetApiVersionDir, version);
                }
            }
            else
            {
                // Generate with no version info
                WriteToc(_sourceRootDir, _targetRootDir, _mappingFile, targetApiDir, null);
            }

            return _restFileInfos;
        }

        #region Prviate Method

        private static void SortOrgsMappingFile(OrgsMappingFile orgsMappingFile)
        {
            orgsMappingFile.OrgInfos.Sort((x, y) => string.CompareOrdinal(x.OrgName, y.OrgName));
            foreach (var orgInfo in orgsMappingFile.OrgInfos)
            {
                orgInfo.Services.Sort((x, y) => string.CompareOrdinal(x.TocTitle, y.TocTitle));
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

        private static void WriteToc(string sourceRootDir, string targetRootDir, OrgsMappingFile orgsMappingFile, string targetApiVersionDir, string version)
        {
            var targetTocPath = Path.Combine(targetApiVersionDir, TocFileName);
            var lineNumberMappingFilePath = Path.Combine(targetRootDir, SourceSwaggerMappingFileName);

            Utility.ClearFile(lineNumberMappingFilePath);

            RepoFile repoFile = null;
            if (File.Exists(Path.Combine(targetRootDir, "repo.json")))
            {
                repoFile = JsonUtility.ReadFromFile<RepoFile>(Path.Combine(targetRootDir, "repo.json"));
            }

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
                    writer.WriteLine($"# [{orgsMappingFile.ApisPageOptions.TocTitle}]({FileUtility.GetRelativePath(targetIndexPath, targetApiVersionDir)})");
                }

                // Write organization info
                foreach (var orgInfo in orgsMappingFile.OrgInfos)
                {
                    if (version == null || string.Equals(orgInfo.Version, version))
                    {
                        // Deal with org name and index
                        var subTocPrefix = string.Empty;
                        if (!string.IsNullOrEmpty(orgInfo.OrgName))
                        {
                            // Write index
                            writer.WriteLine(!string.IsNullOrEmpty(orgInfo.OrgIndex)
                                ? $"# [{orgInfo.OrgName}]({GenerateIndexHRef(targetRootDir, orgInfo.OrgIndex, targetApiVersionDir)})"
                                : $"# {orgInfo.OrgName}");
                            subTocPrefix = "#";
                        }
                        else if (orgsMappingFile.ApisPageOptions?.EnableAutoGenerate != true && !string.IsNullOrEmpty(orgInfo.DefaultTocTitle) && !string.IsNullOrEmpty(orgInfo.OrgIndex))
                        {
                            writer.WriteLine($"# [{orgInfo.DefaultTocTitle}]({GenerateIndexHRef(targetRootDir, orgInfo.OrgIndex, targetApiVersionDir)})");
                        }

                        // Sort by service name
                        orgInfo.Services.Sort((a, b) => a.TocTitle.CompareTo(b.TocTitle));

                        // Write service info
                        foreach (var service in orgInfo.Services)
                        {
                            // 1. Top toc
                            Console.WriteLine($"Created conceptual toc item '{service.TocTitle}'");
                            writer.WriteLine(!string.IsNullOrEmpty(service.IndexFile)
                                ? $"{subTocPrefix}# [{service.TocTitle}]({GenerateIndexHRef(targetRootDir, service.IndexFile, targetApiVersionDir)})"
                                : $"{subTocPrefix}# {service.TocTitle}");

                            // 2. Parse and split REST swaggers
                            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
                            if (service.SwaggerInfo != null)
                            {
                                subTocDict = SplitSwaggers(sourceRootDir, targetApiVersionDir, service, orgsMappingFile, repoFile, version, lineNumberMappingFilePath);
                            }

                            // 3. Conceptual toc
                            List<string> tocLines = null;
                            if (!string.IsNullOrEmpty(service.TocFile))
                            {
                                tocLines = GenerateDocTocItems(targetRootDir, service.TocFile, targetApiVersionDir).Where(i => !string.IsNullOrEmpty(i)).ToList();
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
                            if (service.SwaggerInfo?.Count > 0)
                            {
                                var subRefTocPrefix = string.Empty;
                                if (tocLines?.Count > 0)
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

            TocConverter.GenerateOverviewNode(targetTocPath);

            if (orgsMappingFile.UserYamlToc)
            {
                TocConverter.Convert(targetTocPath);
                if (File.Exists(targetTocPath))
                {
                    File.Delete(targetTocPath);
                }
            }
        }

        private static SortedDictionary<string, List<SwaggerToc>> SplitSwaggers(string sourceRootDir, string targetApiVersionDir, ServiceInfo service, OrgsMappingFile orgsMappingFile, RepoFile repoFile, string version, string lineNumberMappingFilePath)
        {
            var subTocDict = new SortedDictionary<string, List<SwaggerToc>>();
            using (var resetAcrossSwaggerSplitter = new RestAcrossSwaggerSplitter(orgsMappingFile))
            { 
                foreach (var swagger in service.SwaggerInfo)
                {
                    var subGroupName = swagger.SubGroupTocTitle ?? string.Empty;
                    var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiVersionDir, service.UrlGroup, subGroupName.TrimSubGroupName()));
                    var sourceFile = Path.Combine(sourceRootDir, swagger.Source.TrimEnd());

                    var restFileInfo = RestSplitHelper.Split(targetDir, sourceFile, swagger.Source.TrimEnd(), orgsMappingFile.UseServiceUrlGroup ? service.UrlGroup : service.TocTitle, service.TocTitle, subGroupName, swagger.OperationGroupMapping, orgsMappingFile, repoFile, version, resetAcrossSwaggerSplitter);
                    var sourceSwaggerMappingDict = new Dictionary<string, string>();

                    if (restFileInfo == null)
                    {
                        continue;
                    }

                    restFileInfo.NeedPermission = swagger.NeedPermission;
                    _restFileInfos.Add(restFileInfo);

                    var tocTitle = Utility.ExtractPascalNameByRegex(restFileInfo.TocTitle, orgsMappingFile.NoSplitWords);
                    List<SwaggerToc> subTocList;
                    if (!subTocDict.TryGetValue(subGroupName, out subTocList))
                    {
                        subTocList = new List<SwaggerToc>();
                        subTocDict.Add(subGroupName, subTocList);
                    }

                    foreach (var fileNameInfo in restFileInfo.FileNameInfos)
                    {
                        var subTocTitle = fileNameInfo.TocName;
                        var filePath = FileUtility.NormalizePath(Path.Combine(service.UrlGroup, subGroupName.TrimSubGroupName(), fileNameInfo.FileName));

                        if (!orgsMappingFile.IsGroupdedByTag && subTocList.Any(toc => toc.Title == subTocTitle))
                        {
                            throw new InvalidOperationException($"Sub toc '{subTocTitle}' under '{tocTitle}' has been added into toc.md, please add operation group name mapping for file '{swagger.Source}' to avoid conflicting");
                        }

                        var childrenToc = new List<SwaggerToc>();
                        if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                        {
                            foreach (var nameInfo in fileNameInfo.ChildrenFileNameInfo)
                            {
                                childrenToc.Add(new SwaggerToc(nameInfo.TocName, FileUtility.NormalizePath(Path.Combine(service.UrlGroup, subGroupName.TrimSubGroupName(), nameInfo.FileName))));

                                var normalizedFileName = FileUtility.NormalizePath(nameInfo.FileName);

                                // Write into ref mapping dict
                                if (!sourceSwaggerMappingDict.ContainsKey(normalizedFileName))
                                {
                                    sourceSwaggerMappingDict.Add(normalizedFileName, nameInfo.SwaggerSourceUrl);
                                }
                            }
                        }

                        subTocList.Add(new SwaggerToc(subTocTitle, filePath, childrenToc));
                    }
                   
                    Console.WriteLine($"Done splitting swagger file from '{swagger.Source}' to '{service.UrlGroup}'");
                    // Write into source swagger mapping file
                    Utility.WriteDictToFile(lineNumberMappingFilePath, sourceSwaggerMappingDict);
                }

                resetAcrossSwaggerSplitter.Serialize();
            }

            return subTocDict;
        }

        private static string GetOutputDirectory(string outputRootDir)
        {
            Guard.ArgumentNotNullOrEmpty(outputRootDir, nameof(outputRootDir));

            if (Directory.Exists(outputRootDir))
            {
                // Clear last built output folder
                Directory.Delete(outputRootDir, true);
                Console.WriteLine($"Done cleaning previous existing {outputRootDir}");
            }
            Directory.CreateDirectory(outputRootDir);
            if (!outputRootDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outputRootDir = outputRootDir + Path.DirectorySeparatorChar;
            }
            return outputRootDir;
        }

        private static IEnumerable<string> GenerateDocTocItems(string targetRootDir, string tocRelativePath, string targetApiVersionDir)
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
            var tocRelativeDirectoryToApi = FileUtility.GetRelativePath(Path.GetDirectoryName(tocPath), targetApiVersionDir);

            foreach (var tocLine in File.ReadLines(tocPath))
            {
                var match = TocRegex.Match(tocLine);
                if (match.Success)
                {
                    var tocLink = match.Groups["tocLink"].Value;
                    if (string.IsNullOrEmpty(tocLink))
                    {
                        // Handle case like [Text]()
                        yield return tocLine;
                    }
                    else
                    {
                        var tocTitle = match.Groups["tocTitle"].Value;
                        var headerLevel = match.Groups["headerLevel"].Value.Length;
                        var tocLinkRelativePath = tocRelativeDirectoryToApi + "/" + tocLink;
                        var linkPath = Path.Combine(targetApiVersionDir, tocLinkRelativePath);
                        if (!File.Exists(linkPath))
                        {
                            throw new FileNotFoundException($"Link '{tocLinkRelativePath}' not exist in '{tocRelativePath}', when merging into '{TocFileName}' of '{targetApiVersionDir}'");
                        }
                        yield return $"{new string('#', headerLevel)} [{tocTitle}]({tocLinkRelativePath})";
                    }
                }
                else
                {
                    yield return tocLine;
                }
            }
        }

        private static string GenerateIndexHRef(string targetRootDir, string indexRelativePath, string targetApiVersionDir)
        {
            var indexPath = Path.Combine(targetRootDir, indexRelativePath);
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException($"Index file '{indexPath}' not exists.");
            }
            return FileUtility.GetRelativePath(indexPath, targetApiVersionDir);
        }

        private static string IncreaseSharpCharacter(string str)
        {
            return str + "#";
        }

        #endregion
    }
}
