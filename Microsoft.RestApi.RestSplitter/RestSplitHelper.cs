namespace Microsoft.RestApi.RestSplitter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.RestApi.RestSplitter.Generator;
    using Microsoft.RestApi.RestSplitter.Model;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class RestSplitHelper
    {
        public static RestFileInfo Split(string targetDir, string filePath, string swaggerRelativePath, string serviceId, string serviceName, OperationGroupMapping operationGroupMapping, MappingConfig mappingConfig, RepoFile repoFile, out string sourceSwaggerUrl)
        {
            
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"{nameof(targetDir)} '{targetDir}' should exist.");
            }
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"{nameof(filePath)} '{filePath}' should exist.");
            }

            var restFileInfo = new RestFileInfo();

            using (var streamReader = File.OpenText(filePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                var root = JToken.ReadFrom(reader);
                var rootJObj = (JObject)root;

                // Resolve $ref with json file instead of definition reference in the same swagger
                var refResolver = new RefResolver(rootJObj, filePath);
                refResolver.Resolve();

                if (mappingConfig.NeedResolveXMsPaths)
                {
                    var xMsPathsResolver = new XMsPathsResolver(rootJObj);
                    xMsPathsResolver.Resolve();
                }

                rootJObj["x-internal-service-id"] = serviceId;
                rootJObj["x-internal-service-name"] = serviceName;

                sourceSwaggerUrl = GetTheSwaggerSource(repoFile, swaggerRelativePath);
                if (sourceSwaggerUrl != null)
                {
                    rootJObj["x-internal-swagger-source-url"] = sourceSwaggerUrl;
                }

                var generator = GeneratorFactory.CreateGenerator(rootJObj, targetDir, filePath, operationGroupMapping, mappingConfig);
                var fileNameInfos = generator.Generate().ToList();

                if (fileNameInfos.Any())
                {
                    restFileInfo.FileNameInfos = fileNameInfos;
                }

                restFileInfo.TocTitle = GetInfoTitle(rootJObj);
            }
            return restFileInfo;
        }

        private static string GetInfoTitle(JObject root)
        {
            JToken info;
            if (root.TryGetValue("info", out info))
            {
                var infoJObj = (JObject)info;
                JToken title;
                if (infoJObj.TryGetValue("title", out title))
                {
                    return title.ToString();
                }
                throw new InvalidOperationException($"title is not defined in {infoJObj}");
            }
            throw new InvalidOperationException($"info is not defined in {root}");
        }

        private static string GetTheSwaggerSource(RepoFile repoFile, string swaggerRelativePath)
        {
            swaggerRelativePath = swaggerRelativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var paths = swaggerRelativePath.Split(Path.AltDirectorySeparatorChar);
            if (repoFile!= null && paths.Count() > 0)
            {
                var repo = repoFile.Repos.SingleOrDefault(r => string.Equals(r.Name, paths[0], StringComparison.OrdinalIgnoreCase));
                if (repo != null && repo.IsPublicRepo)
                {
                    if (repo.Url.Contains("github.com"))
                    {
                        return $"{repo.Url.TrimEnd('/')}/blob/{repo.Branch}/{swaggerRelativePath.Substring(paths[0].Length + 1)}";
                    }
                    else if(repo.Url.Contains("visualstudio.com"))
                    {
                        return $"{repo.Url.TrimEnd('/')}?path={swaggerRelativePath.Substring(paths[0].Length + 1)}&version=GB{repo.Branch}&a=contents";
                    }
                }
            }
            return null;
        }
    }
}
