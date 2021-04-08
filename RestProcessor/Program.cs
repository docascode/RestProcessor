namespace Microsoft.RestApi.RestCI
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
                    RestProcessor(restFileInfos, mappingFile);
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

        public static (List<FileNameInfo>, ConcurrentDictionary<string, ConcurrentBag<FileNameInfo>>) ExtractRestFiles(IList<RestFileInfo> restFileInfos,OrgsMappingFile orgsMappingFile)
        {
            var splitedGroupFiles = new List<FileNameInfo>();
            var splitedGroupOperationFiles = new ConcurrentDictionary<string, ConcurrentBag<FileNameInfo>>();

            foreach (var restFileInfo in restFileInfos)
            {
                foreach (var fileInfo in restFileInfo.FileNameInfos)
                {
                    if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.FilePath) && File.Exists(fileInfo.FilePath))
                    {
                        splitedGroupFiles.Add(fileInfo);

                        if (fileInfo.ChildrenFileNameInfo != null && fileInfo.ChildrenFileNameInfo.Count > 0)
                        {
                            foreach (var info in fileInfo.ChildrenFileNameInfo)
                            {
                                info.NeedPermission = restFileInfo.NeedPermission;
                                var operationFiles = splitedGroupOperationFiles.GetOrAdd(fileInfo.FilePath, new ConcurrentBag<FileNameInfo>());
                                operationFiles.Add(info);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Can not find the file {fileInfo.FilePath}, please take attention!");
                        var guidKey = Guid.NewGuid().ToString();
                        if (fileInfo.ChildrenFileNameInfo != null && fileInfo.ChildrenFileNameInfo.Count > 0)
                        {
                            foreach (var info in fileInfo.ChildrenFileNameInfo)
                            {
                                var operationFiles = splitedGroupOperationFiles.GetOrAdd(guidKey, new ConcurrentBag<FileNameInfo>());
                                operationFiles.Add(info);
                            }
                        }
                    }
                }
            }

            if (orgsMappingFile.IsGroupdedByTag)
            {
                splitedGroupFiles = MergeRestFile(splitedGroupFiles);
            }

            return (splitedGroupFiles, splitedGroupOperationFiles);

        }

        private static List<FileNameInfo> MergeRestFile(List<FileNameInfo> fileNameInfos)
        {
            var set = new HashSet<FileNameInfo>();
            foreach (var item in fileNameInfos)
            {
                var existFinleNameInfo= set.Where(fileNameInfo=> fileNameInfo.TocName== item.TocName).FirstOrDefault();
                if (existFinleNameInfo != null)
                {
                    existFinleNameInfo.ChildrenFileNameInfo.AddRange(item.ChildrenFileNameInfo);
                }
                else
                {
                    set.Add(item);
                }
            }

            return set.ToList();
        }

        private static void RestProcessorForOperation(string groupKey, FileNameInfo file, ConcurrentDictionary<string, ConcurrentBag<Operation>> groupOperations)
        {
            var folder = Path.GetDirectoryName(file.FilePath);
            var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(file.FilePath)}.yml");
            try
            {
                var operation = RestTransformer.ProcessOperation(groupKey, ymlPath, file.FilePath, file.NeedPermission);
                if (operation != null)
                {
                    var key = string.IsNullOrEmpty(file.Version) ? operation.GroupId : $"{file.Version}_{operation.GroupId}";
                    var operations = groupOperations.GetOrAdd(key, new ConcurrentBag<Operation>());
                    operations.Add(operation);
                }
                Console.WriteLine($"Done generate yml model for {file.FilePath}");
            }
            catch (Exception ex)
            {
                ErrorList.Add($"Error generate yml files for {file.FilePath}, details: {ex}");
            }
            finally 
            {
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }
            }
        }

        public static void RestProcessor(IList<RestFileInfo> restFileInfos, OrgsMappingFile orgsMappingFile)
        {
            var (groupFiles, groupOperationFiles) = ExtractRestFiles(restFileInfos,orgsMappingFile);
            var groupOperations = new ConcurrentDictionary<string, ConcurrentBag<Operation>>();

            //foreach (var groupOperationPath in groupOperationPaths)
            //{
            //    var firstOperationPath = groupOperationPath.Value.First();
            //    Console.WriteLine($"Start generate yml model for {firstOperationPath}");
            //    RestProcessorForOperation(groupOperationPath.Key, firstOperationPath, groupOperations);
            //    var otherOperationPaths = groupOperationPath.Value.Skip(1);
            //    foreach (var filePath in otherOperationPaths)
            //    {
            //        RestProcessorForOperation(groupOperationPath.Key, filePath, groupOperations);
            //    }
            //}
            //foreach (var filePath in groupPaths)
            //{
            //    var folder = Path.GetDirectoryName(filePath);
            //    var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}.yml");
            //    try
            //    {
            //        RestTransformer.ProcessGroup(ymlPath, filePath, groupOperations);
            //        Console.WriteLine($"Done generate yml model for {filePath}");
            //    }
            //    catch (Exception ex)
            //    {
            //        ErrorList.Add($"Error generate yml files for {filePath}, details: {ex}");
            //    }

            //    if (File.Exists(filePath))
            //    {
            //        File.Delete(filePath);
            //    }
            //}

            Parallel.ForEach(groupOperationFiles, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (groupOperationFile) =>
            {
                var firstOperationFile = groupOperationFile.Value.First();
                RestProcessorForOperation(groupOperationFile.Key, firstOperationFile, groupOperations);
                var otherOperationFiles = groupOperationFile.Value.Skip(1);
                Parallel.ForEach(otherOperationFiles, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (file) =>
                {
                    RestProcessorForOperation(groupOperationFile.Key, file, groupOperations);
                });
            });

            Parallel.ForEach(groupFiles, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (file) =>
            {
                var folder = Path.GetDirectoryName(file.FilePath);
                var ymlPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(file.FilePath)}.yml");

                try
                {
                    RestTransformer.ProcessGroup(ymlPath, file.FilePath, groupOperations, file.Version);
                    Console.WriteLine($"Done generate yml model for {file.FilePath}");
                }
                catch (Exception ex)
                {
                    ErrorList.Add($"Error generate yml files for {file.FilePath}, details: {ex}");
                }

                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }
            });
        }
    }
}
