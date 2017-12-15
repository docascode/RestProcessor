namespace Microsoft.RestApi.RestCI
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json;

    public class YamlConverter
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static readonly YamlDeserializer YamlDeserializer = new YamlDeserializer();

        public static OrgsMappingFile ConvertYamls(string sourceRootDir, OrgsMappingFile orgsMappingFile)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            Guard.ArgumentNotNull(orgsMappingFile, nameof(orgsMappingFile));

            foreach (var orgInfo in orgsMappingFile.OrgInfos)
            {
                foreach (var service in orgInfo.Services)
                {
                    if (service.SwaggerInfo != null)
                    {
                        foreach (var swagger in service.SwaggerInfo)
                        {
                            var sourceFile = Path.Combine(sourceRootDir, swagger.Source.TrimEnd());
                            if (Path.GetExtension(sourceFile) == ".yaml")
                            {
                                ConvertYamlToJson(sourceFile, Path.ChangeExtension(sourceFile, ".json"));
                                swagger.Source = Path.ChangeExtension(swagger.Source, ".json");
                            }
                        }
                    }
                }
            }
            return orgsMappingFile;
        }

        private static void ConvertYamlToJson(string yamlFilePath, string jsonFilePath)
        {
            using (var reader = new StreamReader(yamlFilePath))
            {
                var yamlObject = YamlDeserializer.Deserialize(reader);
                using (var writer = new StreamWriter(jsonFilePath))
                {
                    JsonSerializer.Serialize(writer, yamlObject);
                }
            }
        }
    }
}
