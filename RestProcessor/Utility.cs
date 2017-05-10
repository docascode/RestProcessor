namespace RestProcessor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    public static class Utility
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        public static T ReadFromFile<T>(string mappingFilePath)
        {
            using (var streamReader = File.OpenText(mappingFilePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                return JsonSerializer.Deserialize<T>(reader);
            }
        }

        public static string ExtractPascalName(string name)
        {
            var list = new HashSet<string> { "BI", "IP", "ML", "MAM", "OS", "VM", "VMs", "APIM" };
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
