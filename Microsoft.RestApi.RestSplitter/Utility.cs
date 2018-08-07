namespace Microsoft.RestApi.RestSplitter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.RestApi.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class Utility
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static bool ShouldSplitToOperation(JObject root, int splitOperationCountGreaterThan)
        {
            var paths = ((JObject)root["paths"]);
            return paths.Count > splitOperationCountGreaterThan || (paths.Count == splitOperationCountGreaterThan && paths.Values().First().Values().Count() > splitOperationCountGreaterThan);
        }

        public static readonly Regex YamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(10));
        public static readonly YamlDotNet.Serialization.Deserializer YamlDeserializer = new YamlDotNet.Serialization.Deserializer();
        public static readonly YamlDotNet.Serialization.Serializer YamlSerializer = new YamlDotNet.Serialization.Serializer();
        public static readonly string Pattern = @"(?:{0}|[A-Z]+?(?={0}|[A-Z][a-z]|$)|[A-Z](?:[a-z]*?)(?={0}|[A-Z]|$)|(?:[a-z]+?)(?={0}|[A-Z]|$))";
        public static readonly HashSet<string> Keyword = new HashSet<string> { "BI", "IP", "ML", "MAM", "OS", "VMs", "VM", "APIM", "vCenters", "WANs", "WAN", "IDs", "ID" };

        public static Tuple<string, string> Serialize(string targetDir, string name, JObject root)
        {
            var fileName = $"{name}.json";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            using (var sw = new StreamWriter(Path.Combine(targetDir, fileName)))
            using (var writer = new JsonTextWriter(sw))
            {
                JsonSerializer.Serialize(writer, root);
            }
            return new Tuple<string, string>(fileName, Path.Combine(targetDir, fileName));
        }

        public static void Serialize(TextWriter writer, object obj)
        {
            JsonSerializer.Serialize(writer, obj);
        }

        public static object GetYamlHeaderByMeta(string filePath, string metaName)
        {
            var yamlHeader = GetYamlHeader(filePath);
            object result;
            if (yamlHeader != null && yamlHeader.TryGetValue(metaName, out result))
            {
                return result;
            }
            return null;
        }

        public static Dictionary<string, object> GetYamlHeader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File path {filePath} not exists when parsing yaml header.");
            }

            var markdown = File.ReadAllText(filePath);

            var match = YamlHeaderRegex.Match(markdown);
            if (match.Length == 0)
            {
                return null;
            }

            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                {
                    return YamlDeserializer.Deserialize<Dictionary<string, object>>(reader);
                }
            }
            catch (Exception)
            {
                Console.WriteLine();
                return null;
            }
        }

        public static T YamlDeserialize<T>(TextReader stream)
        {
            return YamlDeserializer.Deserialize<T>(stream);
        }

        public static void Serialize(string path, object obj)
        {
            using (var stream = File.Create(path))
            using (var writer = new StreamWriter(stream))
            {
                JsonSerializer.Serialize(writer, obj);
            }
        }

        public static T ReadFromFile<T>(string mappingFilePath)
        {
            using (var streamReader = File.OpenText(mappingFilePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                return JsonSerializer.Deserialize<T>(reader);
            }
        }

        public static string ExtractPascalNameByRegex(string name)
        {
            if (name.Contains(" "))
            {
                return name;
            }
            if (name.Contains("_") || name.Contains("-"))
            {
                return name.Replace('_', ' ').Replace('-', ' ');
            }

            var result = new List<string>();
            var p = string.Format(Pattern, string.Join("|", Keyword));
            while (name.Length > 0)
            {
                var m = Regex.Match(name, p);
                if (!m.Success)
                {
                    return name;
                }
                result.Add(m.Value);
                name = name.Substring(m.Length);
            }
            return string.Join(" ", result);
        }

        public static string ExtractPascalName(string name)
        {
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
                        if (Keyword.Contains(closestUpperCaseWord))
                        {
                            result.Append(" ");
                            result.Append(closestUpperCaseWord);
                            i = i + closestUpperCaseWord.Length - 1;
                            continue;
                        }

                        var closestCamelCaseWord = GetClosestCamelCaseWord(name, i);
                        if (Keyword.Contains(closestCamelCaseWord))
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

        public static string TrimSubGroupName(this string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return string.Empty;
            }
            return groupName.Replace(" ", "").Trim().ToLower();
        }

        public static string TryToFormalizeUrl(string path, bool isFormalized)
        {
            Guard.ArgumentNotNullOrEmpty(path, "FormalizedUrl");

            if (isFormalized)
            {
                return path
                    .Replace("%", "")
                    .Replace("\\", "")
                    .Replace("\"", "")
                    .Replace("^", "")
                    .Replace("`", "")
                    .Replace('<', '(')
                    .Replace('>', ')')
                    .Replace("{", "((")
                    .Replace("}", "))")
                    .Replace('|', '_')
                    .Replace(' ', '-');
            }
            else
            {
                return path;
            }
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
