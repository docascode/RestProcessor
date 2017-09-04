﻿namespace RestProcessor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    public static class TagsGenerator
    {
        public static IEnumerable<RestSplitter.FileNameInfo> Generate(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, bool isOperationLevel)
        {
            var pathsJObj = (JObject)rootJObj["paths"];
            var tags = GetTags(pathsJObj);
            foreach (var tag in tags)
            {
                var paths = FindPathsByTag(pathsJObj, tag);
                if (paths.Count > 0)
                {
                    rootJObj["paths"] = paths;
                    var fileNameInfo = new RestSplitter.FileNameInfo();
                    var fileName = tag;

                    // Get file name from operation group mapping
                    string newOperationGourpName;
                    if (operationGroupMapping != null && operationGroupMapping.TryGetValue(tag, out newOperationGourpName))
                    {
                        fileName = newOperationGourpName;
                        fileNameInfo.TocName = newOperationGourpName;
                        rootJObj["x-internal-operation-group-name"] = newOperationGourpName;
                    }
                    else
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        fileNameInfo.TocName = Utility.ExtractPascalNameByRegex(fileNameWithoutExt);
                    }

                    fileNameInfo.FileName = Utility.Serialize(targetDir, fileName, rootJObj);
                    yield return fileNameInfo;
                }
            }
        }

        private static JObject FindPathsByTag(JObject paths, string tag)
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
                    var tags = GetTagsPerOperation((JObject)item.Value);

                    // Only add into operations when the first tag of this operation equals expected.
                    var firstTag = tags.FirstOrDefault();
                    if (firstTag != null && firstTag == tag)
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

        public static HashSet<string> GetTags(JObject paths)
        {
            var tags = new HashSet<string>();
            foreach (var path in paths.Values())
            {
                foreach (var item in (JObject)path)
                {
                    // Skip find tag for parameters
                    if (item.Key.Equals("parameters"))
                    {
                        continue;
                    }
                    var tagsPerOperation = GetTagsPerOperation((JObject)item.Value);
                    tags.UnionWith(tagsPerOperation);
                }
            }
            return tags;
        }

        public static IEnumerable<string> GetTagsPerOperation(JObject operation)
        {
            JToken value;
            if (operation.TryGetValue("tags", out value) && value != null)
            {
                var tagsJArray = (JArray)value;
                foreach (var tagJToken in tagsJArray)
                {
                    yield return tagJToken.ToString();
                }
            }
        }
    }
}
