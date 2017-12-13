namespace Microsoft.RestApi.RestCI
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Build.RestApi;
    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter;
    using Microsoft.RestApi.RestSplitter.Model;
    using Microsoft.RestApi.RestTransformer;
    using Newtonsoft.Json;

    public class Program
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                {
                    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_root_directory] [target_root_directory] [mappingfile.json]");
                    return 1;
                }
                if (!File.Exists(args[2]))
                {
                    throw new ArgumentException($"mappingFilePath '{ args[2]}' should exist.");
                }
                var mappingFile = JsonUtility.ReadFromFile<OrgsMappingFile>(args[2]);
                if (mappingFile.ConvertYamlToJson)
                {
                    mappingFile = YamlConverter.ConvertYamls(args[0], mappingFile);
                }

                var restFileInfos = RestSpliter(args[0], args[1], mappingFile);
               
                if (mappingFile.UseYamlSchema)
                {
                    RestProcessor(restFileInfos);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex.Message}");
                return 1;
            }
        }

        public static IList<RestFileInfo> RestSpliter(string sourceRootDir, string targetRootDir, OrgsMappingFile mappingFile)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }

            var splitter = new RestSplitter(sourceRootDir, targetRootDir, mappingFile);
            var restFileInfos = splitter.Process();
            Console.WriteLine("Done processing all swagger files");
            return restFileInfos;
        }

        public static void RestProcessor(IList<RestFileInfo> restFileInfos)
        {
            foreach(var restFileInfo in restFileInfos)
            {
                foreach(var fileInfo in restFileInfo.FileNameInfos)
                {
                    RestTransformerWrapper(fileInfo);
                }
            }
        }

        private static void RestTransformerWrapper(FileNameInfo fileNameInfo)
        {
            if(fileNameInfo != null && !string.IsNullOrEmpty(fileNameInfo.FilePath) && File.Exists(fileNameInfo.FilePath))
            {
                if (fileNameInfo.FilePath == "C:\\Code\\RestRepos\\azure-docs-rest-apis\\docs-ref-autogen\\time-series-insights-management\\EventSources\\CreateOrUpdate.json")
                {
                    var folder = Path.GetDirectoryName(fileNameInfo.FilePath);

                    var swaggerModel = SwaggerJsonParser.Parse(fileNameInfo.FilePath);
                    var viewModel = SwaggerModelConverter.FromSwaggerModel(swaggerModel);

                    //var fileName = $"{Path.GetFileNameWithoutExtension(fileNameInfo.FilePath)}.raw.json";
                    //using (var sw = new StreamWriter(Path.Combine(folder, fileName)))
                    //using (var writer = new JsonTextWriter(sw))
                    //{
                    //    JsonSerializer.Serialize(writer, swaggerModel);
                    //}
                    //Console.WriteLine($"Done generate view model for {fileName}");

                    var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(fileNameInfo.FilePath)}.yml");
                    try
                    {
                        RestTransformer.Process(ymlPath, swaggerModel, viewModel, folder);
                        Console.WriteLine($"Done generate yml model for {ymlPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error generate yml files for {fileNameInfo.FilePath}, details: {ex}");
                    }
                }
                
                if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                {
                    foreach(var info in fileNameInfo.ChildrenFileNameInfo)
                    {
                        RestTransformerWrapper(info);
                    }
                }
            }
        }
    }
}
