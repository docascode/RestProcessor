namespace RestProcessor
{
    using System;
    using System.Globalization;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class MsExamplesHanlder
    {
        public static void AddMapping(string sourceRootDir, string targetRootDir, string mappingFilePath)
        {
            var orgsMappingFile = Utility.ReadFromFile<OrgsMappingFile>(mappingFilePath);
            foreach (var orgInfo in orgsMappingFile.OrgInfos)
            {
                foreach (var service in orgInfo.Services)
                {
                    if (service.SwaggerInfo != null)
                    {
                        foreach (var swagger in service.SwaggerInfo)
                        {
                            var sourceFile = Path.Combine(sourceRootDir, swagger.Source.TrimEnd());
                            AddMappingCore(sourceFile);
                        }
                    }
                }
            }
        }

        private static void AddMappingCore(string sourceFile)
        {
            if (!File.Exists(sourceFile))
            {
                throw new ArgumentException($"{nameof(sourceFile)} '{sourceFile}' should exist.");
            }

            bool shouldUpdate = false;
            JObject rootJObj;
            using (var streamReader = File.OpenText(sourceFile))
            using (var reader = new JsonTextReader(streamReader))
            {
                var root = JToken.ReadFrom(reader);
                rootJObj = (JObject)root;

                RestHelper.ActionOnOperation(rootJObj, operation =>
                {
                    JToken examples;
                    if (operation.TryGetValue("x-ms-examples", out examples))
                    {
                        var mapping = new JObject();
                        var examplesJObj = (JObject)examples;
                        foreach (var pair in examplesJObj)
                        {
                            JToken refPath;
                            if (((JObject)pair.Value).TryGetValue("$ref", out refPath))
                            {
                                var wireFormatName = Path.GetFileNameWithoutExtension(((JValue)refPath).ToString(CultureInfo.InvariantCulture));
                                mapping.Add(pair.Key, wireFormatName);
                            }
                        }
                        if (mapping.Count > 0)
                        {
                            shouldUpdate = true;
                            operation.Add("x-internal-ms-examples-mapping", mapping);
                        }
                    }
                });
            }

            if (shouldUpdate)
            {
                var targetDir = Path.GetDirectoryName(sourceFile);
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                Utility.Serialize(targetDir, fileName, rootJObj);
            }
        }
    }
}
