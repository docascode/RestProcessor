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
    using System.Threading.Tasks;

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

                var restFileInfos = RestSpliter(args[0], args[1], args[2]);

                if (!File.Exists(args[2]))
                {
                    throw new ArgumentException($"mappingFilePath '{ args[2]}' should exist.");
                }
                var config = JsonUtility.ReadFromFile<RestCIConfig>(args[2]);
                if (config.UseYamlSchema)
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

        public static IList<RestFileInfo> RestSpliter(string sourceRootDir, string targetRootDir, string mappingFilePath)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }
            if (!File.Exists(mappingFilePath))
            {
                throw new ArgumentException($"{nameof(mappingFilePath)} '{mappingFilePath}' should exist.");
            }

            var splitter = new RestSplitter(sourceRootDir, targetRootDir, mappingFilePath);
            var restFileInfos = splitter.Process();
            Console.WriteLine("Done processing all swagger files");
            return restFileInfos;
        }

        public static void RestProcessor(IList<RestFileInfo> restFileInfos)
        {
            Parallel.ForEach(restFileInfos, new ParallelOptions { MaxDegreeOfParallelism = 8 }, restFileInfo => {
                Parallel.ForEach(restFileInfo.FileNameInfos, new ParallelOptions { MaxDegreeOfParallelism = 8 }, fileInfo => {
                    RestTransformerWrapper(fileInfo);
                });
            });
        }

        private static void RestTransformerWrapper(FileNameInfo fileNameInfo)
        {
            if(fileNameInfo != null && !string.IsNullOrEmpty(fileNameInfo.FilePath) && File.Exists(fileNameInfo.FilePath))
            {
                var folder = Path.GetDirectoryName(fileNameInfo.FilePath);

                var swaggerModel = SwaggerJsonParser.Parse(fileNameInfo.FilePath);
                var viewModel = SwaggerModelConverter.FromSwaggerModel(swaggerModel);

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

                if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                {
                    Parallel.ForEach(fileNameInfo.ChildrenFileNameInfo, new ParallelOptions { MaxDegreeOfParallelism = 8 }, info => {
                        RestTransformerWrapper(info);
                    });
                }
            }
        }
    }
}
