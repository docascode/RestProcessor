namespace Microsoft.RestApi.RestCI
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestSplitter;
    using Microsoft.RestApi.RestSplitter.Model;
    using Microsoft.RestApi.RestTransformer;
    using Microsoft.RestApi.RestTransformer.Models;

    using Newtonsoft.Json;

    public class Program
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

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
                var mappingFile = JsonUtility.ReadFromFile<OrgsMappingFile>(args[2]);
                if (mappingFile.ConvertYamlToJson)
                {
                    mappingFile = YamlConverter.ConvertYamls(args[0], mappingFile);
                }
                var outputDir = args.Length < 4 ? Path.Combine(args[1], mappingFile.TargetApiRootDir) : args[3];

                Console.WriteLine("Processor split begin at:" + DateTime.UtcNow);
                var restFileInfos = RestSpliter(args[0], args[1], mappingFile, outputDir);

                Console.WriteLine("Processor split end at:" + DateTime.UtcNow);
                Console.WriteLine("Processor transform start at:" + DateTime.UtcNow);

                if (mappingFile.UseYamlSchema)
                {
                    RestProcessor(restFileInfos);
                }

                Console.WriteLine("Processor transform end at:" + DateTime.UtcNow);
                if (ErrorList.Count > 0)
                {
                    foreach (var error in ErrorList)
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

        public static (List<string>, List<string>) ExtractRestFiles(IList<RestFileInfo> restFileInfos)
        {
            var splitedOperationPaths = new List<string>();
            var splitedGroupPaths = new List<string>();

            foreach (var restFileInfo in restFileInfos)
            {
                foreach (var fileInfo in restFileInfo.FileNameInfos)
                {
                    if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.FilePath) && File.Exists(fileInfo.FilePath))
                    {
                        splitedGroupPaths.Add(fileInfo.FilePath);

                        if (fileInfo.ChildrenFileNameInfo != null && fileInfo.ChildrenFileNameInfo.Count > 0)
                        {
                            foreach (var info in fileInfo.ChildrenFileNameInfo)
                            {
                                splitedOperationPaths.Add(info.FilePath);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Can not find the file {fileInfo.FilePath}, please take attention!");
                        if (fileInfo.ChildrenFileNameInfo != null && fileInfo.ChildrenFileNameInfo.Count > 0)
                        {
                            foreach (var info in fileInfo.ChildrenFileNameInfo)
                            {
                                splitedOperationPaths.Add(info.FilePath);
                            }
                        }
                    }
                }
            }
            return (splitedGroupPaths, splitedOperationPaths);

        }

        public static void RestProcessor(IList<RestFileInfo> restFileInfos)
        {
            var (groupPaths, operationPaths) = ExtractRestFiles(restFileInfos);

            var groupOperations = new ConcurrentDictionary<string, ConcurrentBag<Operation>>();
            Parallel.ForEach(operationPaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (filePath) =>
            {
                var folder = Path.GetDirectoryName(filePath);
                var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}.yml");
                try
                {
                    var operation = RestTransformer.ProcessOperation(ymlPath, filePath);
                    if (operation != null)
                    {
                        var operations = groupOperations.GetOrAdd(operation.GroupId, new ConcurrentBag<Operation>());
                        operations.Add(operation);
                    }
                    Console.WriteLine($"Done generate yml model for {filePath}");
                }
                catch (Exception ex)
                {
                    ErrorList.Add($"Error generate yml files for {filePath}, details: {ex}");
                }
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });

            Parallel.ForEach(groupPaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (filePath) =>
            {
                var folder = Path.GetDirectoryName(filePath);
                var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}.yml");
                try
                {
                    RestTransformer.ProcessGroup(ymlPath, filePath, groupOperations);
                    Console.WriteLine($"Done generate yml model for {filePath}");
                }
                catch (Exception ex)
                {
                    ErrorList.Add($"Error generate yml files for {filePath}, details: {ex}");
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }
    }
}
