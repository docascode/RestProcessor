namespace RestProcessor.Generator
{
    using System.Collections.Generic;
    using System.IO;

    using RestProcessor.Model;

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

        public abstract IEnumerable<RestSplitter.FileNameInfo> Generate();

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

        protected IEnumerable<RestSplitter.FileNameInfo> GenerateOperations(JObject rootJObj, JObject paths, string targetDir, string tagName)
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
            // todo: compare as docfx. https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.Build.RestApi/SwaggerModelConverter.cs
            return string.Equals(left["$ref"]?.ToString(), right["$ref"]?.ToString());
        }

        #endregion
    }
}
