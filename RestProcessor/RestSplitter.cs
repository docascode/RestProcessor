namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class RestSplitter
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };

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

                restFileInfo.TocTitle = GetInfoTitle(rootJObj);

                var pathsJObj = (JObject)rootJObj["paths"];
                var operationGroups = GetOperationGroups(pathsJObj);
                if (operationGroups.Count == 0)
                {
                    Console.WriteLine($"Operation groups is null or empty for file {filePath}.");
                    return null;
                }
                foreach (var operationGroup in operationGroups)
                {
                    var filteredPaths = FindPathsByOperationGroup(pathsJObj, operationGroup);
                    if (filteredPaths.Count == 0)
                    {
                        throw new InvalidOperationException($"Operation group '{operationGroup}' could not be found in for {FileUtility.GetDirectoryName(targetDir)}");
                    }

                    // Get file name from operation group mapping
                    var fileNameInfo = new FileNameInfo();
                    var fileName = operationGroup;
                    string newOperationGourpName;
                    if (operationGroupMapping != null && operationGroupMapping.TryGetValue(operationGroup, out newOperationGourpName))
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

                    // Reset paths to filtered paths
                    rootJObj["paths"] = filteredPaths;
                    rootJObj["x-internal-toc-name"] = fileNameInfo.TocName;

                    if (isOperationLevel)
                    {
                        // Split operation group to operation
                        fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(GenerateOperations(rootJObj, (JObject)rootJObj["paths"], targetDir, fileName));

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
                    }

                    rootJObj["x-internal-split-type"] = SplitType.OperationGroup.ToString();
                    fileNameInfo.FileName = Serialze(targetDir, fileName, rootJObj);
                    restFileInfo.FileNameInfos.Add(fileNameInfo);
                }
            }
            return restFileInfo;
        }

        private static IEnumerable<FileNameInfo> GenerateOperations(JObject rootJObj, JObject paths, string targetDir, string operationGroup)
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
                    var operationName = GetOperationGroupPerOperation(operationObj).Item2;
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
                    var operationFileName = Serialze(Path.Combine(targetDir, operationGroup), operationName, rootJObj);
                    rootJObj["x-internal-split-type"] = null;

                    yield return new FileNameInfo
                    {
                        TocName = operationTocName,
                        FileName = Path.Combine(operationGroup, operationFileName)
                    };
                }
            }
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
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
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
                    var opGroup = GetOperationGroupPerOperation((JObject)item.Value).Item1;
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
                    var operationGroupPerOperation = GetOperationGroupPerOperation((JObject)item.Value).Item1;
                    operationGroups.Add(operationGroupPerOperation);
                }
            }
            return operationGroups;
        }

        private static Tuple<string, string> GetOperationGroupPerOperation(JObject operation)
        {
            JToken value;
            if (operation.TryGetValue("operationId", out value) && value != null)
            {
                return GetOperationGroupFromOperationId(value.ToString());
            }
            throw new InvalidOperationException($"operationId is not defined in {operation}");
        }

        private static Tuple<string, string> GetOperationGroupFromOperationId(string operationId)
        {
            var result = operationId.Split('_');
            if (result.Length != 2)
            {
                // When the operation id doesn't contain '_', treat the whole operation id as Noun and Verb at the same time
                return Tuple.Create(result[0], result[0]);
            }
            if (result.Length > 2)
            {
                throw new InvalidOperationException($"Invalid operation id: {operationId}, it should be Noun_Verb format.");
            }
            return Tuple.Create(result[0], result[1]);
        }

        private enum SplitType
        {
            None,
            Operation,
            OperationGroup,
        }
    }
}
