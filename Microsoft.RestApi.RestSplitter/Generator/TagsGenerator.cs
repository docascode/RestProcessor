namespace Microsoft.RestApi.RestSplitter.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json.Linq;

    public class TagsGenerator : BaseGenerator
    {
        protected OperationGroupMapping OperationGroupMapping { get; }

        #region Constructors

        public TagsGenerator(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig, IDictionary<string, int> lineNumberMappingDict, RepoFile repoFile, string swaggerRelativePath) : base(rootJObj, targetDir, filePath, mappingConfig, lineNumberMappingDict, repoFile, swaggerRelativePath)
        {
            OperationGroupMapping = operationGroupMapping;
        }

        #endregion

        #region Public Methods

        public override IEnumerable<FileNameInfo> Generate()
        {
            var pathsJObj = (JObject)RootJObj["paths"];
            var tags = GetTags(pathsJObj);
            if (tags.Count == 0)
            {
                Console.WriteLine($"tags is null or empty for file {FilePath}.");
            }
            foreach (var tag in tags)
            {
                JToken pathParameters = null;
                var filteredPaths = FindPathsByTag(pathsJObj, tag, ref pathParameters);
                if(filteredPaths.Count > 0)
                {
                    if (pathParameters != null)
                    {
                        MergePathParametersToOperations(filteredPaths, pathParameters);
                    }

                    var fileNameInfo = new FileNameInfo
                    {
                        TocName = Utility.ExtractPascalNameByRegex(tag)
                    };
                    RootJObj["x-internal-operation-group-name"] = tag;

                    // Get file name from operation group mapping
                    string newTagName = tag;
                    if (OperationGroupMapping != null && OperationGroupMapping.TryGetValueOrDefault(tag, out newTagName, tag))
                    {
                        fileNameInfo.TocName = newTagName;
                        RootJObj["x-internal-operation-group-name"] = newTagName;
                    }

                    // Reset paths to filtered paths
                    RootJObj["paths"] = filteredPaths;
                    RootJObj["x-internal-toc-name"] = fileNameInfo.TocName;

                    // Only split when the children count larger than MappingConfig.SplitOperationCountGreaterThan
                    if (MappingConfig.IsOperationLevel && Utility.ShouldSplitToOperation(RootJObj, MappingConfig.SplitOperationCountGreaterThan))
                    {
                        // Split operation group to operation
                        fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(
                            GenerateOperations(
                                RootJObj, 
                                (JObject)RootJObj["paths"], 
                                TargetDir, 
                                newTagName
                            )
                        );

                        // Sort
                        fileNameInfo.ChildrenFileNameInfo.Sort((a, b) => string.CompareOrdinal(a.TocName, b.TocName));

                        // Clear up original paths in operation group
                        RootJObj["paths"] = new JObject();

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
                        RootJObj["x-internal-split-members"] = splitMembers;
                        RootJObj["x-internal-split-type"] = MappingConfig.UseYamlSchema ? SplitType.TagGroup.ToString() : SplitType.OperationGroup.ToString();
                    }
                    
                    var file = Utility.Serialize(TargetDir, Utility.TryToFormalizeUrl(newTagName, MappingConfig.FormalizeUrl), RootJObj);
                    fileNameInfo.FileName = MappingConfig.UseYamlSchema ? Path.ChangeExtension(file.Item1, "yml") : file.Item1;
                    fileNameInfo.FilePath = file.Item2;

                    // Clear up internal data
                    ClearKey(RootJObj, "x-internal-split-members");
                    ClearKey(RootJObj, "x-internal-split-type");
                    ClearKey(RootJObj, "x-internal-toc-name");

                    yield return fileNameInfo;
                }
            }
        }

        #endregion

        #region Protected Methods

        protected override string GetOperationName(JObject operation, out string operationId)
        {
            if (operation.TryGetValue("operationId", out JToken value) && value != null)
            {
                operationId = value.ToString();
                if (operation.TryGetValue("x-operationTitle", out JToken operationName) && operationName != null)
                {
                    return operationName.ToString();
                }
                return value.ToString();
            }
            throw new InvalidOperationException($"operationId is not defined in {operation}");
        }

        #endregion

        #region Private Methods

        private static JObject FindPathsByTag(JObject paths, string tag, ref JToken pathParameters)
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
                        pathParameters = item.Value;
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

        #endregion
    }
}
