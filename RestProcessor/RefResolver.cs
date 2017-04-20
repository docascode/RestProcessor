namespace RestProcessor
{
    using System;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class RefResolver
    {
        private const string DefinitionKey = "definitions";
        private readonly string _swaggerDir;
        private readonly JObject _root;
        private readonly JObject _definitionsJObject;

        public RefResolver(JObject root, string swaggerPath)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (swaggerPath == null)
            {
                throw new ArgumentNullException(nameof(swaggerPath));
            }

            _root = root;
            _swaggerDir = Path.GetDirectoryName(swaggerPath);
            if (string.IsNullOrEmpty(_swaggerDir))
            {
                throw new InvalidOperationException($"Directory of {_swaggerDir} should not be null or empty");
            }

            // Set up definitions
            JToken defValue;
            if (root.TryGetValue(DefinitionKey, out defValue))
            {
                if (defValue.Type != JTokenType.Object)
                {
                    throw new InvalidOperationException($"Type of 'definitions' should be {nameof(JObject)}.");
                }
                _definitionsJObject = (JObject)defValue;
            }
            else
            {
                _definitionsJObject = new JObject();
                root[DefinitionKey] = _definitionsJObject;
            }
        }

        public void Resolve()
        {
            ResolveRecursive(_root);
        }

        private void ResolveRecursive(JToken jToken)
        {
            var jArray = jToken as JArray;
            if (jArray != null)
            {
                foreach (var item in jArray)
                {
                    ResolveRecursive(item);
                }
            }

            var jObject = jToken as JObject;
            if (jObject != null)
            {
                foreach (var pair in jObject)
                {
                    if (pair.Key.Equals("$ref", StringComparison.OrdinalIgnoreCase))
                    {
                        var jValue = pair.Value as JValue;
                        if (jValue != null && jValue.Type == JTokenType.String)
                        {
                            var stringValue = (string)jValue;
                            if (stringValue.EndsWith(".json"))
                            {
                                var fullPath = Path.Combine(_swaggerDir, stringValue);
                                using (var streamReader = File.OpenText(fullPath))
                                using (var reader = new JsonTextReader(streamReader))
                                {
                                    var root = JToken.ReadFrom(reader);
                                    var fileName = Path.GetFileNameWithoutExtension(fullPath);
                                    const int counter = 0;
                                    var definitionName = AddDefinitions(fileName, root, counter);
                                    jObject[pair.Key] = JToken.FromObject($"#/{DefinitionKey}/{definitionName}");
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Value of {pair.Key} should be string type, but actually its type is {pair.Value.Type} with path {pair.Value.Path}.");
                        }
                    }
                    ResolveRecursive(jObject[pair.Key]);
                }
            }
        }

        private string AddDefinitions(string fileName, JToken root, int counter)
        {
            JToken jToken;
            if (_definitionsJObject.TryGetValue(fileName, out jToken))
            {
                counter++;
                var randomFileName = $"{fileName}-{counter}";
                return AddDefinitions(randomFileName, root, counter);
            }
            _definitionsJObject[fileName] = root;
            return fileName;
        }
    }
}
