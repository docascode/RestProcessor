namespace MappingFileMigration
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    using RestProcessor;

    public class Program
    {
        private static readonly string ConceptualFolder = "docs-ref-conceptual";
        private static readonly string IndexFolder = "docs-ref-index";
        private static readonly string ServiceDescriptionMeta = "service_description";
        private static readonly string ServiceDescriptionContent = "To be added";

        public static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length >3)
            {
                throw new ArgumentException("usage: MappingFileMigration.exe [old mapping file path] [expected new mapping file path] [optional: root rest repo path].");
            }
            var oldMapping = Utility.ReadFromFile<MappingFile>(args[0]);
            var newMapping = TransformToNewMapping(oldMapping);
            Utility.Serialize(args[1], newMapping);

            var repoPath = args[2];
            if (!string.IsNullOrEmpty(repoPath))
            {
                AddServiceDescriptionMeta(repoPath, oldMapping);
            }
        }

        private static void AddServiceDescriptionMeta(string repoPath, MappingFile mappingFile)
        {
            foreach (var item in mappingFile.Mapping.DocumentationItems)
            {
                if (!item.SourceIndex.StartsWith(ConceptualFolder))
                {
                    continue;
                }

                var indexFilePath = Path.Combine(repoPath, item.SourceIndex);
                var dict = Utility.GetYamlHeader(indexFilePath);
                if (dict != null)
                {
                    dict[ServiceDescriptionMeta] = ServiceDescriptionContent;
                    var text = File.ReadAllText(indexFilePath);
                    var body = Utility.YamlHeaderRegex.Replace(text, string.Empty);
                    using (var writer = new StreamWriter(indexFilePath))
                    {
                        if (dict != null && dict.Count > 0)
                        {
                            var header = Utility.YamlSerializer.Serialize(dict);
                            var lastIndexOfNewLine = header.LastIndexOf("\r\n");
                            header = header.Remove(lastIndexOfNewLine);
                            writer.WriteLine("---");
                            writer.WriteLine(header);
                            writer.WriteLine("---");
                            writer.Write(body);
                        }
                    }
                }
            }
        }

        private static OrgsMappingFile TransformToNewMapping(MappingFile oldMapping)
        {
            var services = new List<ServiceInfo>();

            // Services which contains swagger files
            var serviceGroup = oldMapping.Mapping.ReferenceItems.GroupBy(i => i.TargetDir);
            foreach (var service in serviceGroup)
            {
                var item0 = service.First();
                var conceptualFolder = $"{ConceptualFolder}/{item0.TargetDir}";
                services.Add(new ServiceInfo
                {
                    TocTitle = item0.TocTitle,
                    UrlGroup = item0.TargetDir,
                    IndexFile = $"{conceptualFolder}/index.md",
                    TocFile = $"{conceptualFolder}/toc.md",
                    SwaggerInfo = service.Select(item => new SwaggerInfo
                    {
                        Source = item.SourceSwagger,
                        OperationGroupMapping = item.OperationGroupMapping
                    }).ToList()
                });
            }

            // None service index and toc
            var serviceTocTitles = serviceGroup.Select(i => i.First().TocTitle);
            var noneServiceGroup = oldMapping.Mapping.DocumentationItems.Where(i => !serviceTocTitles.Contains(i.TocTitle));
            foreach (var noneService in noneServiceGroup)
            {
                services.Add(new ServiceInfo
                {
                    TocTitle = noneService.TocTitle,
                    UrlGroup = GetUrlGroupFromIndex(noneService.SourceIndex),
                    IndexFile = noneService.SourceIndex,
                    TocFile = noneService.SourceToc,
                });
            }

            // sort services
            services.Sort((a, b) => a.TocTitle.CompareTo(b.TocTitle));

            return new OrgsMappingFile
            {
                TargetApiRootDir = oldMapping.TargetApiRootDir,
                ApisPageOptions = new ApisPageOptions
                {
                    EnableAutoGenerate = false,
                    TargetFile = $"{IndexFolder}/index.md",
                    SummaryFile = $"{IndexFolder}/apis-page-summary.md",
                    ServiceDescriptionMetadata = ServiceDescriptionMeta
                },
                OrgInfos = new List<OrgInfo> { new OrgInfo
                {
                    OrgName = string.Empty,
                    // TODO
                    Services = services }
                }
            };
        }

        private static string GetUrlGroupFromIndex(string str)
        {
            var result = str.Split('/');
            if (result.Length != 3 || result.First() != ConceptualFolder)
            {
                throw new InvalidOperationException($"Could not parse service name from {str}");
            }
            return result[1];
        }
    }
}
