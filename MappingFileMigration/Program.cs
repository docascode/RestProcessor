namespace MappingFileMigration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RestProcessor;

    public class Program
    {
        private static readonly string ConceptualFolder = "docs-ref-conceptual";
        private static readonly string IndexFolder = "docs-ref-index";

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("usage: MappingFileMigration.exe [old mapping file path] [expected new mapping file path].");
            }
            var oldMapping = Utility.ReadFromFile<MappingFile>(args[0]);
            var newMapping = TransformToNewMapping(oldMapping);
            Utility.Serialize(args[1], newMapping);
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
            services.Sort((a, b) => a.UrlGroup.CompareTo(b.UrlGroup));

            return new OrgsMappingFile
            {
                TargetApiRootDir = oldMapping.TargetApiRootDir,
                ApisPageOptions = new ApisPageOptions
                {
                    EnableAutoGenerate = false,
                    TargetFile = $"{IndexFolder}/index.md",
                    SummaryFile = $"{IndexFolder}/apis-page-summary.md",
                    ServiceDescriptionMetadata = "service_description"
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
