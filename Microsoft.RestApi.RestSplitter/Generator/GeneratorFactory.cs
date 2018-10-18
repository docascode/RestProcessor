namespace Microsoft.RestApi.RestSplitter.Generator
{
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    public class GeneratorFactory
    {
        public static IGenerator CreateGenerator(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, OrgsMappingFile orgsMappingFile, IDictionary<string, int> lineNumberMappingDict, RepoFile repoFile, string swaggerSourcePath)
        {
            if (orgsMappingFile.IsGroupdedByTag)
            {
                return new TagsGenerator(rootJObj, targetDir, filePath, operationGroupMapping, orgsMappingFile, lineNumberMappingDict, repoFile, swaggerSourcePath);
            }
            return new OperationGroupGenerator(rootJObj, targetDir, filePath, operationGroupMapping, orgsMappingFile, lineNumberMappingDict, repoFile, swaggerSourcePath);
        }
    }
}
