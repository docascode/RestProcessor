namespace RestProcessor.Generator
{
    using RestProcessor.Model;

    using Newtonsoft.Json.Linq;

    public class GeneratorFactory
    {
        public static IGenerator CreateGenerator(JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig)
        {
            if (mappingConfig.IsGroupedByTag)
            {
                return new TagsGenerator(rootJObj, targetDir, filePath, mappingConfig);
            }
            return new OperationGroupGenerator(rootJObj, targetDir, filePath, operationGroupMapping, mappingConfig);
        }
    }
}
