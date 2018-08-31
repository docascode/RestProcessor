namespace Microsoft.RestApi.RestSplitter.Generator
{
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    public class GeneratorFactory
    {
        public static IGenerator CreateGenerator(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig, IDictionary<string, int> lineNumberMappingDict, RepoFile repoFile, string swaggerSourcePath)
        {
            if (mappingConfig.IsGroupedByTag)
            {
                return new TagsGenerator(rootJObj, targetDir, filePath, operationGroupMapping, mappingConfig, lineNumberMappingDict, repoFile, swaggerSourcePath);
            }
            return new OperationGroupGenerator(rootJObj, targetDir, filePath, operationGroupMapping, mappingConfig, lineNumberMappingDict, repoFile, swaggerSourcePath);
        }
    }
}
