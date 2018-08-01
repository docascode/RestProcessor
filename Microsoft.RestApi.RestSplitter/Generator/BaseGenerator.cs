namespace Microsoft.RestApi.RestSplitter.Generator
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json.Linq;
    using System.Linq;

    public abstract class BaseGenerator : IGenerator
    {
        protected JObject RootJObj { get; }

        protected string TargetDir { get; }

        protected string FilePath { get; }

        protected MappingConfig MappingConfig { get; }

        #region Constructors

        protected BaseGenerator(JObject rootJObj, string targetDir, string filePath, MappingConfig mappingConfig)
        {
            Guard.ArgumentNotNull(rootJObj, nameof(rootJObj));
            Guard.ArgumentNotNullOrEmpty(targetDir, nameof(targetDir));
            Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
            Guard.ArgumentNotNull(mappingConfig, nameof(mappingConfig));

            RootJObj = rootJObj;
            TargetDir = targetDir;
            FilePath = filePath;
            MappingConfig = mappingConfig;
        }


        #endregion

        #region Public Methods

        public abstract IEnumerable<FileNameInfo> Generate();

        protected abstract string GetOperationName(JObject operation);

        #endregion

        #region Protected Methods

        protected static void ClearKey(JObject jObject, string key)
        {
            JToken obj;
            if (jObject.TryGetValue(key, out obj))
            {
                jObject.Remove(key);
            }
        }

        protected IEnumerable<FileNameInfo> GenerateOperations(JObject rootJObj, JObject paths, string targetDir, string groupName)
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
                    var operationName = Utility.TrimSpacesInPath(GetOperationName(operationObj));
                    var operationTocName = Utility.ExtractPascalNameByRegex(RemoveTagFromOperationId(operationName, groupName));
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
                    rootJObj["x-internal-operation-name"] = operationTocName;
                    var operationFile = Utility.Serialize(Path.Combine(targetDir, groupName), RemoveTagFromOperationId(operationName, groupName), rootJObj);
                    ClearKey(rootJObj, "x-internal-split-type");
                    ClearKey(rootJObj, "x-internal-operation-name");

                    var fileName = Path.Combine(groupName, operationFile.Item1);
                    yield return new FileNameInfo
                    {
                        TocName = operationTocName,
                        FileName = MappingConfig.UseYamlSchema ? Path.ChangeExtension(fileName, "yml") : fileName,
                        FilePath = operationFile.Item2
                    };
                }
            }
        }

        protected void MergePathParametersToOperations(JObject filteredPaths, JToken pathParameters)
        {
            foreach (var path in filteredPaths)
            {
                foreach (var item in (JObject)path.Value)
                {
                    var operationObj = (JObject)item.Value;
                    JToken operationParameters;
                    if (operationObj.TryGetValue("parameters", out operationParameters))
                    {
                        JArray parameters = new JArray();
                        parameters.Merge(operationParameters);
                        parameters.Merge(pathParameters);
                        operationObj["parameters"] = DistinctParameters(parameters);
                    }
                    else
                    {
                        operationObj["parameters"] = pathParameters;
                    }
                }
            }
        }

        private JArray DistinctParameters(JArray parameters)
        {
            JArray distinctedParameters = new JArray();

            foreach (var p in parameters)
            {
                if (!distinctedParameters.Any(v => IsParameterEquals(v, p)))
                {
                    distinctedParameters.Add(p);
                }
            }

            return distinctedParameters;
        }

        private bool IsParameterEquals(JToken left, JToken right)
        {
            if (left["$ref"] != null && right["$ref"] != null)
            {
                return string.Equals(left["$ref"].ToString(), right["$ref"].ToString());
            }

            if (left["name"] != null && right["name"] != null && left["in"] != null && right["in"] != null)
            {
                return string.Equals(left["name"].ToString(), right["name"].ToString())
                    && string.Equals(left["in"].ToString(), right["in"].ToString());
            }
            return false;
        }

        private string RemoveTagFromOperationId(string operationName, string groupName)
        {
            Guard.ArgumentNotNullOrEmpty(operationName, nameof(operationName));
            Guard.ArgumentNotNullOrEmpty(groupName, nameof(groupName));

            var internalOperationName = operationName;
            if (MappingConfig.IsGroupedByTag && MappingConfig.RemoveTagFromOperationId)
            {
                if (operationName.StartsWith(groupName))
                {
                    internalOperationName = operationName.TrimStart(groupName.ToCharArray());
                }
                internalOperationName = internalOperationName.Trim('_').Trim(' ');
            }
            return internalOperationName;
        }

        #endregion
    }
}
