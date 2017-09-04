namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

        public static RestFileInfo Process(string targetDir, string filePath, string serviceName, OperationGroupMapping operationGroupMapping, bool isOperationLevel, bool isGroupedByTag)
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

                IntegrateMsExamples(rootJObj, Path.GetDirectoryName(filePath));

                // Resolve $ref with json file instead of definition reference in the same swagger
                var refResolver = new RefResolver(rootJObj, filePath);
                refResolver.Resolve();

                rootJObj["x-internal-service-name"] = serviceName;
                var fileNameInfos = isGroupedByTag ?
                    TagsGenerator.Generate(rootJObj, targetDir, filePath, operationGroupMapping, isOperationLevel).ToList() :
                    OperationGroupGenerator.Generate(rootJObj, targetDir, filePath, operationGroupMapping, isOperationLevel).ToList();

                if (fileNameInfos.Any())
                {
                    restFileInfo.FileNameInfos = fileNameInfos;
                }

                restFileInfo.TocTitle = GetInfoTitle(rootJObj);
            }
            return restFileInfo;
        }

        private static void IntegrateMsExamples(JObject rootJObj, string directory)
        {
            RestHelper.ActionOnOperation(rootJObj, operation =>
            {
                JToken mapping;
                if (operation.TryGetValue(MsExamplesHanlder.InternalMsExamplesMappingKey, out mapping))
                {
                    JObject internalExamples = new JObject();
                    foreach (var pair in (JObject)mapping)
                    {
                        var exampleName = pair.Key;
                        var wireformatFileName = pair.Value;
                        var wireFormatPath = Path.Combine(directory, "wire-format", $"{wireformatFileName}.yml");
                        if (!File.Exists(wireFormatPath))
                        {
                            Console.WriteLine($"{wireFormatPath} not exist");
                            continue;
                        }
                        using (var reader = File.OpenText(wireFormatPath))
                        {
                            var msExample = Utility.YamlDeserialize<MsExample>(reader);
                            var stringBuilder = new StringBuilder();
                            using (var sw = new StringWriter(stringBuilder))
                            {
                                Utility.Serialize(sw, msExample);
                            }
                            internalExamples[exampleName] = JToken.Parse(stringBuilder.ToString());
                        }
                    }

                    if (internalExamples.Count > 0)
                    {
                        JToken originalExamples;
                        if (!operation.TryGetValue("x-internal-ms-examples", out originalExamples))
                        {
                            operation["x-internal-ms-examples"] = internalExamples;
                        }
                    }
                }
            });
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
