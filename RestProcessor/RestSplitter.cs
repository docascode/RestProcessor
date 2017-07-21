namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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

        public static RestFileInfo Process(string targetDir, string filePath, OperationGroupMapping operationGroupMapping, bool isOperationLevel = false)
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

                var pathsJObj = (JObject)rootJObj["paths"];

                var fileNameInfos = OperationGroupGenerator.Generate(pathsJObj, rootJObj, targetDir, filePath, operationGroupMapping, isOperationLevel);

                restFileInfo.FileNameInfos = fileNameInfos.ToList();
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
