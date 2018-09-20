namespace Microsoft.RestApi.RestCI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
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
        private static OrgsMappingFile MappingFile;

        private static List<string> ErrorList = new List<string>();

        static int Main(string[] args)
        {
            Console.WriteLine("Processor begin at:" + DateTime.UtcNow);
            try
            {
                if (args.Length < 3)
                {
                    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_root_directory] [target_root_directory] [mappingfile.json] [output_directory]");
                    return 1;
                }

                if (!File.Exists(args[2]))
                {
                    throw new ArgumentException($"mappingFilePath '{ args[2]}' should exist.");
                }
                MappingFile = JsonUtility.ReadFromFile<OrgsMappingFile>(args[2]);
                if (MappingFile.ConvertYamlToJson)
                {
                    MappingFile = YamlConverter.ConvertYamls(args[0], MappingFile);
                }
                var outputDir = args.Length < 4 ? Path.Combine(args[1], MappingFile.TargetApiRootDir) : args[3];

                Console.WriteLine("Processor split begin at:" + DateTime.UtcNow);
                var restFileInfos = RestSpliter(args[0], args[1], MappingFile, outputDir);

                Console.WriteLine("Processor split end at:" + DateTime.UtcNow);
                Console.WriteLine("Processor transform start at:" + DateTime.UtcNow);

                if (MappingFile.UseYamlSchema)
                {
                    ExtractRestFiles(restFileInfos);
                    RestProcessor(restFileInfos);
                }
                Console.WriteLine("Processor transform end at:" + DateTime.UtcNow);
                if (ErrorList.Count > 0)
                {
                    foreach(var error in ErrorList)
                    {
                        Console.WriteLine($"Exception occurs: {error}");
                    }
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex}");
                return 1;
            }
            finally
            {
                Console.WriteLine("Processor end at:" + DateTime.UtcNow);
            }
        }

        public static IList<RestFileInfo> RestSpliter(string sourceRootDir, string targetRootDir, OrgsMappingFile mappingFile, string outputDir)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }
            if (string.IsNullOrEmpty(outputDir))
            {
                throw new ArgumentException($"{nameof(outputDir)} should not be null or empty.");
            }

            var splitter = new RestSplitter(sourceRootDir, targetRootDir, mappingFile, outputDir);
            var restFileInfos = splitter.Process();
            Console.WriteLine("Done processing all swagger files");
            return restFileInfos;
        }

        public static List<string> ExtractRestFiles(IList<RestFileInfo> restFileInfos)
        {
            var splitedFilePaths = new List<string>();
            foreach (var restFileInfo in restFileInfos)
            {
                foreach (var fileInfo in restFileInfo.FileNameInfos)
                {
                    ExtractRestFilesCore(fileInfo, splitedFilePaths);
                }
            }
            return splitedFilePaths;

        }

        public static void ExtractRestFilesCore(FileNameInfo fileNameInfo, List<string> splitedFilePaths)
        {
            if(fileNameInfo != null && !string.IsNullOrEmpty(fileNameInfo.FilePath) && File.Exists(fileNameInfo.FilePath))
            {
                splitedFilePaths.Add(fileNameInfo.FilePath);

                if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                {
                    foreach (var info in fileNameInfo.ChildrenFileNameInfo)
                    {
                        ExtractRestFilesCore(info, splitedFilePaths);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Warning: Can not find the file {fileNameInfo.FilePath}, please take attention!");
                if (fileNameInfo.ChildrenFileNameInfo != null && fileNameInfo.ChildrenFileNameInfo.Count > 0)
                {
                    foreach (var info in fileNameInfo.ChildrenFileNameInfo)
                    {
                        ExtractRestFilesCore(info, splitedFilePaths);
                    }
                }
            }
        }

        public static void RestProcessor(IList<RestFileInfo> restFileInfos)
        {
            var filePaths = ExtractRestFiles(restFileInfos);
            Parallel.ForEach(filePaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (filePath) =>
            {
                var folder = Path.GetDirectoryName(filePath);

                var swaggerModel = SwaggerJsonParser.Parse(filePath);
                var viewModel = SwaggerModelConverter.FromSwaggerModel(swaggerModel);

                var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}.yml");
                try
                {
                    RestTransformer.Process(ymlPath, swaggerModel, viewModel, folder, MappingFile.ProductUid);
                    Console.WriteLine($"Done generate yml model for {ymlPath}");
                }
                catch (Exception ex)
                {
                    ErrorList.Add($"Error generate yml files for {filePath}, details: {ex}");
                }
            });
        }
    }
}
