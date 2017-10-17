namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using RestProcessor.Generator;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestProcessor.Model;

    public static class RestSplitter
    {
        public class RestFileInfo
        {
            public List<FileNameInfo> FileNameInfos { get; set; } = new List<FileNameInfo>();

            public string TocTitle { get; set; }
        }

        public class FileNameInfo
        {
            public string FileName { get; set; }

            public List<FileNameInfo> ChildrenFileNameInfo { get; set; }

            public string TocName { get; set; }
        }

        public static RestFileInfo Split(string targetDir, string filePath, string serviceName, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig)
        {
            var restFileInfo = new RestFileInfo();
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"{nameof(targetDir)} '{targetDir}' should exist.");
            }
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{nameof(filePath)} '{filePath}' should exist.");
            }

            using (var streamReader = File.OpenText(filePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                var root = JToken.ReadFrom(reader);
                var rootJObj = (JObject)root;

                // Resolve $ref with json file instead of definition reference in the same swagger
                var refResolver = new RefResolver(rootJObj, filePath);
                refResolver.Resolve();

                // Resolve x-ms-paths to paths
                if (mappingConfig.NeedResolveXMsPaths)
                {
                    var xMsPathsResolver = new XMsPathsResolver(rootJObj);
                    xMsPathsResolver.Resolve();
                }

                rootJObj["x-internal-service-name"] = serviceName;
                var generator = GeneratorFactory.CreateGenerator(rootJObj, targetDir, filePath, operationGroupMapping, mappingConfig);
                var fileNameInfos = generator.Generate().ToList();

                if (fileNameInfos.Any())
                {
                    restFileInfo.FileNameInfos = fileNameInfos;
                }

                restFileInfo.TocTitle = GetInfoTitle(rootJObj);
            }
            return restFileInfo;
        }

        private static string GetInfoTitle(JObject root)
        {
            JToken info;
            if (root.TryGetValue("info", out info))
            {
                var infoJObj = (JObject)info;
                JToken title;
                if (infoJObj.TryGetValue("title", out title))
                {
                    return title.ToString();
                }
                throw new InvalidOperationException($"title is not defined in {infoJObj}");
            }
            throw new InvalidOperationException($"info is not defined in {root}");
        }
    }
}
