namespace Microsoft.RestApi.SwaggerResolver.Core.Parsing
{
    using Microsoft.RestApi.SwaggerResolver.Core.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using YamlDotNet.RepresentationModel;
    using YamlDotNet.Serialization;

    public static class YamlExtensions
    {
        private static Deserializer YamlDeserializer
        {
            get
            {
                var d = new Deserializer();
                //d.NodeDeserializers.Insert(0, new YamlBoolDeserializer());
                return d;
            }
        }

        private static JsonSerializer JsonSerializer => new JsonSerializer();
        public static bool IsYaml(this string text) => text.ParseYaml() != null;
        /// <summary>
        /// Gets the YAML syntax tree from given string. Returns null on failure.
        /// </summary>
        public static YamlNode ParseYaml(this string yaml)
        {
            YamlNode doc = null;
            try
            {
                var yamlStream = new YamlStream();
                using (var reader = new StringReader(yaml))
                {
                    yamlStream.Load(reader);
                }
                doc = yamlStream.Documents[0].RootNode;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return doc;
        }

        public static YamlMappingNode MergeWith(this YamlMappingNode self, YamlMappingNode other)
           => MergeYamlObjects(self, other);
        /// <summary>
        /// Merges to Yaml mapping nodes (~ dictionaries) as follows:
        /// - members existing only in one node will be present in the result
        /// - members existing in both nodes will
        ///     - be present in the result, if they are identical
        ///     - be merged if they are mapping or sequence nodes
        ///     - THROW an exception otherwise
        /// </summary>
        public static T MergeYamlObjects<T>(T a, T b, ObjectPath path = null) where T : YamlNode
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }
            if (path == null)
            {
                path = ObjectPath.Empty;
            }

            // trivial case
            if (a.Equals(b))
            {
                return a;
            }

            // mapping nodes
            var aMapping = a as YamlMappingNode;
            var bMapping = b as YamlMappingNode;
            if (aMapping != null && bMapping != null)
            {
                // iterate all members
                var result = new YamlMappingNode();
                var keys = aMapping.Children.Keys.Concat(bMapping.Children.Keys).Distinct();
                foreach (var key in keys)
                {
                    var subpath = path.AppendProperty(key.ToString());

                    // forward if only present in one of the nodes
                    if (!aMapping.Children.ContainsKey(key))
                    {
                        result.Children.Add(key, bMapping.Children[key]);
                        continue;
                    }
                    if (!bMapping.Children.ContainsKey(key))
                    {
                        result.Children.Add(key, aMapping.Children[key]);
                        continue;
                    }

                    // try merge objects otherwise
                    var aMember = aMapping.Children[key];
                    var bMember = bMapping.Children[key];
                    result.Children.Add(key, MergeYamlObjects(aMember, bMember, subpath));
                }
                return result as T;
            }

            // sequence nodes
            var aSequence = a as YamlSequenceNode;
            var bSequence = b as YamlSequenceNode;
            if (aSequence != null && bSequence != null)
            {
                return new YamlSequenceNode(aSequence.Children.Concat(bSequence.Children).Distinct()) as T;
            }

            // nothing worked
            throw new Exception($"{path.JsonPath} has incomaptible values ({a}, {b}).");
        }

        /// <summary>
        /// Converts the YAML document to JSON.
        /// </summary>
        public static string EnsureYamlIsJson(this string text)
        {
            using (var reader = new StringReader(text))
            {
                using (var writer = new StringWriter(CultureInfo.CurrentCulture))
                {
                    var obj = YamlDeserializer.Deserialize(reader);
                    JsonSerializer.Serialize(writer, obj);
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Gets the YAML syntax tree from given string. Returns null on failure.
        /// </summary>
        public static string Serialize(this YamlNode node)
        {
            using (var writer = new StringWriter())
            {
                new YamlStream(new YamlDocument(node)).Save(writer, false);
                return writer.ToString();
            }
        }
    }
}
