namespace RestProcessor.Generator
{
    using Newtonsoft.Json.Linq;

    public class GeneratorFactory
    {
        public static IGenerator CreateGenerator(bool isGroupedByTag, JObject rootJObj, string targetDir, string filePath, OperationGroupMapping operationGroupMapping, bool isOperationLevel)
        {
            if (isGroupedByTag)
            {
                return new TagsGenerator(rootJObj, targetDir, filePath, isOperationLevel);
            }
            return new OperationGroupGenerator(rootJObj, targetDir, filePath, isOperationLevel, operationGroupMapping);
        }
    }
}
