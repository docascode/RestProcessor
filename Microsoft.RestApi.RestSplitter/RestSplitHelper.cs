namespace Microsoft.RestApi.RestSplitter
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.RestApi.RestSplitter.Generator;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class RestSplitHelper
    {
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
