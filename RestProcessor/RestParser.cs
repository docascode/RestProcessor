namespace RestProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public static class RestParser
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();
        private const string TocFileName = "toc.md";

        public static void Process(string sourceRootDir, string targetRootDir, string mappingFilePath)
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

            MappingFile mapping;
            using (var streamReader = File.OpenText(mappingFilePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                mapping = JsonSerializer.Deserialize<MappingFile>(reader);
            }

            ProcessCore(sourceRootDir, targetRootDir, mapping);
        }

        private static void ProcessCore(string sourceRootDir, string targetRootDir, MappingFile mappingFile)
        {
            var targetApiDir = Path.Combine(targetRootDir, mappingFile.TargetApiRootDir);
            if (Directory.Exists(targetApiDir))
            {
                // Clear last built target api folder
                Directory.Delete(targetApiDir, true);
                Console.WriteLine($"Done cleaning previous existing {targetApiDir}");
            }
            Directory.CreateDirectory(targetApiDir);

            var tocDict = new Dictionary<string, List<SubToc>>();
            foreach (var mappingItem in mappingFile.MappingItems)
            {
                // Split rest files
                var targetDir = FileUtility.CreateDirectoryIfNotExist(Path.Combine(targetApiDir, mappingItem.TargetDir));
                var sourceFile = Path.Combine(sourceRootDir, mappingItem.Source);
                var restFileInfo = RestSplitter.Process(targetDir, sourceFile);

                // Write top TOC
                var tocTitle = string.IsNullOrEmpty(mappingItem.TocTitle)
                    ? ExtractPascalName(restFileInfo.TocTitle)
                    : mappingItem.TocTitle;

                // Update toc dictionary
                List<SubToc> subTocList;
                if (!tocDict.TryGetValue(tocTitle, out subTocList))
                {
                    subTocList = new List<SubToc>();
                    tocDict.Add(tocTitle, subTocList);
                }

                // Sort sub TOC
                restFileInfo.FileNames.Sort();

                // Write sub TOC
                foreach (var fileName in restFileInfo.FileNames)
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var subTocTitle = ExtractPascalName(fileNameWithoutExt);
                    var filePath = FileUtility.NormalizePath(Path.Combine(mappingItem.TargetDir, fileName));
                    subTocList.Add(new SubToc(subTocTitle, filePath));
                }

                Console.WriteLine($"Done splitting swagger file from '{mappingItem.Source}' to '{mappingItem.TargetDir}'");
            }

            var targetTocPath = Path.Combine(targetApiDir, TocFileName);
            using (var sw = new StreamWriter(targetTocPath))
            {
                foreach (var toc in tocDict)
                {
                    sw.WriteLine($"# {toc.Key}");
                    toc.Value.Sort((x, y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
                    foreach (var subToc in toc.Value)
                    {
                        sw.WriteLine($"## [{subToc.Title}]({subToc.FilePath})");
                    }
                }
            }
        }

        private class SubToc
        {
            public string Title { get; }

            public string FilePath { get; }

            public SubToc(string title, string filePath)
            {
                Title = title;
                FilePath = filePath;
            }
        }

        private static string ExtractPascalName(string name)
        {
            var list = new HashSet<string> { "BI", "IP", "ML", "MAM", "VM", "VMs" };
            if (name.Contains(" "))
            {
                return name;
            }

            var result = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                // Exclude index = 0
                var c = name[i];
                if (i != 0 &&
                    char.IsUpper(c))
                {
                    var closestUpperCaseWord = GetClosestUpperCaseWord(name, i);
                    if (closestUpperCaseWord.Length > 0)
                    {
                        if (list.Contains(closestUpperCaseWord))
                        {
                            result.Append(" ");
                            result.Append(closestUpperCaseWord);
                            i = i + closestUpperCaseWord.Length - 1;
                            continue;
                        }

                        var closestCamelCaseWord = GetClosestCamelCaseWord(name, i);
                        if (list.Contains(closestCamelCaseWord))
                        {
                            result.Append(" ");
                            result.Append(closestCamelCaseWord);
                            i = i + closestCamelCaseWord.Length - 1;
                            continue;
                        }
                    }
                    result.Append(" ");
                }
                result.Append(c);
            }

            return result.ToString();
        }

        private static string GetClosestUpperCaseWord(string word, int index)
        {
            var result = new StringBuilder();
            for (var i = index; i < word.Length; i++)
            {
                var character = word[i];
                if (char.IsUpper(character))
                {
                    result.Append(character);
                }
                else
                {
                    break;
                }
            }

            if (result.Length == 0)
            {
                return string.Empty;
            }

            // Remove the last character, which is unlikely the continues upper case word.
            return result.ToString(0, result.Length - 1);
        }

        private static string GetClosestCamelCaseWord(string word, int index)
        {
            var result = new StringBuilder();
            var meetLowerCase = false;
            for (var i = index; i < word.Length; i++)
            {
                var character = word[i];
                if (char.IsUpper(character))
                {
                    if (meetLowerCase)
                    {
                        return result.ToString();
                    }
                }
                else
                {
                    meetLowerCase = true;
                }
                result.Append(character);
            }

            return result.ToString();
        }
    }
}
