namespace RestProcessor
{
    using System;
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
            if (tags.Count == 0)
            {
                Console.WriteLine($"tags is null or empty for file {filePath}.");
            }
            foreach (var tag in tags)
            {
                var paths = FindPathsByTag(pathsJObj, tag);
                var filteredPaths = FindPathsByTag(pathsJObj, tag);
                if(filteredPaths.Count > 0)
                {
                    var fileNameInfo = new RestSplitter.FileNameInfo
                    {
                        TocName = tag
                    };
                   
                    // Reset paths to filtered paths
                    rootJObj["paths"] = filteredPaths;
                    rootJObj["x-internal-toc-name"] = fileNameInfo.TocName;

                    // Only split when the children count larger than 1
                    if (isOperationLevel && Utility.ShouldSplitToOperation(rootJObj))
                    {
                        // Split operation group to operation
                        fileNameInfo.ChildrenFileNameInfo = new List<RestSplitter.FileNameInfo>(GenerateOperations(rootJObj, (JObject)rootJObj["paths"], targetDir, tag));

                        // Sort
                        fileNameInfo.ChildrenFileNameInfo.Sort((a, b) => string.CompareOrdinal(a.TocName, b.TocName));

                        // Clear up original paths in operation group
                        rootJObj["paths"] = new JObject();

                        // Add split members into operation group
                        var splitMembers = new JArray();

                        foreach (var childInfo in fileNameInfo.ChildrenFileNameInfo)
                        {
                            var relativePath = FileUtility.NormalizePath(childInfo.FileName);
                            var dotIndex = relativePath.LastIndexOf('.');
                            var relativePathWithoutExt = relativePath;
                            if (dotIndex > 0)
                            {
                                // Remove ".json"
                                relativePathWithoutExt = relativePath.Remove(dotIndex);
                            }
                            splitMembers.Add(new JObject
                            {
                                { "displayName", childInfo.TocName },
                                { "relativePath", relativePathWithoutExt },
                            });
                        }
                        rootJObj["x-internal-split-members"] = splitMembers;
                        rootJObj["x-internal-split-type"] = SplitType.TagOperation.ToString();
                    }

                    fileNameInfo.FileName = Utility.Serialize(targetDir, tag, rootJObj);

                    // Clear up internal data
                    ClearKey(rootJObj, "x-internal-split-members");
                    ClearKey(rootJObj, "x-internal-split-type");
                    ClearKey(rootJObj, "x-internal-toc-name");

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

        private static IEnumerable<RestSplitter.FileNameInfo> GenerateOperations(JObject rootJObj, JObject paths, string targetDir, string tagName)
        {
            foreach (var path in paths)
            {
                foreach (var item in (JObject)path.Value)
                {
                    // Skip for parameters
                    if (item.Key.Equals("parameters"))
                    {
                        continue;
                    }

                    var operationObj = (JObject)item.Value;
                    var operationName = GetOperationName(operationObj);
                    var operationTocName = Utility.ExtractPascalNameByRegex(operationName);
                    operationObj["x-internal-toc-name"] = operationTocName;

                    // Reuse the root object, to reuse the other properties
                    rootJObj["paths"] = new JObject
                    {
                        {
                            path.Key, new JObject
                            {
                                { item.Key, operationObj }
                            }
                        }
                    };

                    rootJObj["x-internal-split-type"] = SplitType.Operation.ToString();
                    var operationFileName = Utility.Serialize(Path.Combine(targetDir, tagName), operationName, rootJObj);
                    ClearKey(rootJObj, "x-internal-split-type");

                    yield return new RestSplitter.FileNameInfo
                    {
                        TocName = operationTocName,
                        FileName = Path.Combine(tagName, operationFileName)
                    };
                }
            }
        }

        private static string GetOperationName(JObject operation)
        {
            JToken value;
            if (operation.TryGetValue("operationId", out value) && value != null)
            {
                return value.ToString();
            }
            throw new InvalidOperationException($"operationId is not defined in {operation}");
        }

        private static void ClearKey(JObject jObject, string key)
        {
            JToken obj;
            if (jObject.TryGetValue(key, out obj))
            {
                jObject.Remove(key);
            }
        }
    }
}
