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
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };

        public class RestFileInfo
        {
            public List<string> FileNames { get; set; } = new List<string>();

            public string TocTitle { get; set; }
        }

        public static RestFileInfo Process(string targetDir, string filePath)
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

                restFileInfo.TocTitle = GetInfoTitle(rootJObj);

                var pathsJObj = (JObject)rootJObj["paths"];
                var operationGroups = GetOperationGroups(pathsJObj);
                if (operationGroups.Count == 0)
                {
                    throw new InvalidOperationException("Operation groups should not be null or empty.");
                }
                foreach (var operationGroup in operationGroups)
                {
                    var filteredPaths = FindPathsByOperationGroup(pathsJObj, operationGroup);
                    if (filteredPaths.Count == 0)
                    {
                        throw new InvalidOperationException($"Operation group '{operationGroup}' could not be found in for {FileUtility.GetDirectoryName(targetDir)}");
                    }

                    // Reset paths to filtered paths
                    rootJObj["paths"] = filteredPaths;
                    restFileInfo.FileNames.Add(Serialze(targetDir, operationGroup, rootJObj));
                }
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

        private static string Serialze(string targetDir, string name, JObject root)
        {
            var fileName = $"{name}.json";
            using (var sw = new StreamWriter(Path.Combine(targetDir, fileName)))
            using (var writer = new JsonTextWriter(sw))
            {
                JsonSerializer.Serialize(writer, root);
            }
            return fileName;
        }

        private static JObject FindPathsByOperationGroup(JObject paths, string expectedOpGroup)
        {
            var filteredPaths = new JObject();
            foreach (var path in paths)
            {
                var pathUrl = path.Key;
                foreach (var item in (JObject)path.Value)
                {
                    // Skip find tag for parameters
                    if (item.Key.Equals("parameters"))
                    {
                        continue;
                    }
                    var opGroup = GetOperationGroupPerOperation((JObject)item.Value);
                    if (expectedOpGroup == opGroup)
                    {
                        if (filteredPaths[pathUrl] == null)
                        {
                            // New added
                            var operations = new JObject { { item.Key, item.Value } };
                            filteredPaths[pathUrl] = operations;
                        }
                        else
                        {
                            // Modified
                            var operations = (JObject)filteredPaths[pathUrl];
                            operations.Add(item.Key, item.Value);
                        }
                    }
                }
            }
            return filteredPaths;
        }

        private static HashSet<string> GetOperationGroups(JObject paths)
        {
            var operationGroups = new HashSet<string>();
            foreach (var path in paths.Values())
            {
                foreach (var item in (JObject)path)
                {
                    // Skip find operation group for parameters
                    if (item.Key.Equals("parameters"))
                    {
                        continue;
                    }
                    var operationGroupPerOperation = GetOperationGroupPerOperation((JObject)item.Value);
                    operationGroups.Add(operationGroupPerOperation);
                }
            }
            return operationGroups;
        }

        private static string GetOperationGroupPerOperation(JObject operation)
        {
            JToken value;
            if (operation.TryGetValue("operationId", out value) && value != null)
            {
                return GetOperationGroupFromOperationId(value.ToString());
            }
            throw new InvalidOperationException($"operationId is not defined in {operation}");
        }

        private static string GetOperationGroupFromOperationId(string operationId)
        {
            return operationId.Split('_')[0];
        }
    }
}
