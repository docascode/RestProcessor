namespace Microsoft.RestApi.RestSplitter.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json.Linq;

    public class OperationGroupGenerator : BaseGenerator
    {
        protected OperationGroupMapping OperationGroupMapping { get; }

        #region Constructors
        public OperationGroupGenerator(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig) : base(rootJObj, targetDir, filePath, mappingConfig)
        {
            OperationGroupMapping = operationGroupMapping;
        }
        #endregion

        #region Public Methods

        #endregion

        #region Protected Methods

        public override IEnumerable<FileNameInfo> Generate()
        {
            var pathsJObj = (JObject)RootJObj["paths"];
            var operationGroups = GetOperationGroups(pathsJObj);
            if (operationGroups.Count == 0)
            {
                Console.WriteLine($"Operation groups is null or empty for file {FilePath}.");
            }
            else
            {
                foreach (var operationGroup in operationGroups)
                {
                    JToken pathParameters = null;
                    var filteredPaths = FindPathsByOperationGroup(pathsJObj, operationGroup, ref pathParameters);
                    if (filteredPaths.Count == 0)
                    {
                        throw new InvalidOperationException($"Operation group '{operationGroup}' could not be found in for {FileUtility.GetDirectoryName(TargetDir)}");
                    }
                    if(pathParameters != null)
                    {
                        MergePathParametersToOperations(filteredPaths, pathParameters);
                    }

                    // Get file name from operation group mapping
                    var fileNameInfo = new FileNameInfo();
                    var newOperationGroupName = Utility.TrimSpacesInPath(operationGroup);
                    
                    if (OperationGroupMapping != null && OperationGroupMapping.TryGetValueOrDefault(operationGroup, out newOperationGroupName, Utility.TrimSpacesInPath(operationGroup)))
                    {
                        // Trim space in new operation group name
                        newOperationGroupName = Utility.TrimSpacesInPath(newOperationGroupName);
                        fileNameInfo.TocName = newOperationGroupName;
                        RootJObj["x-internal-operation-group-name"] = newOperationGroupName;
                    }
                    else
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(newOperationGroupName);
                        fileNameInfo.TocName = Utility.ExtractPascalNameByRegex(fileNameWithoutExt);
                    }

                    // Reset paths to filtered paths
                    RootJObj["paths"] = filteredPaths;
                    RootJObj["x-internal-toc-name"] = fileNameInfo.TocName;

                    // Only split when the children count larger than MappingConfig.SplitOperationCountGreaterThan
                    if (MappingConfig.IsOperationLevel && Utility.ShouldSplitToOperation(RootJObj, MappingConfig.SplitOperationCountGreaterThan))
                    {
                        // Split operation group to operation
                        fileNameInfo.ChildrenFileNameInfo = new List<FileNameInfo>(GenerateOperations(RootJObj, (JObject)RootJObj["paths"], TargetDir, newOperationGroupName));

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
                        RootJObj["x-internal-split-type"] = SplitType.OperationGroup.ToString();
                    }
                    var file = Utility.Serialize(TargetDir, newOperationGroupName, RootJObj);
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

        protected override string GetOperationName(JObject operation)
        {
            return GetOperationGroupPerOperation(operation).Item2;
        }

        #endregion

        #region Private Methods

        private static JObject FindPathsByOperationGroup(JObject paths, string expectedOpGroup, ref JToken pathParameters)
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

        #endregion
    }
}
