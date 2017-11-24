namespace Microsoft.RestApi.Common
{
    using System;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    public static class JsonUtility
    {
        /// <summary>
        /// Serialize object to json string
        /// </summary>
        /// <param name="value">object to be serialized</param>
        /// <param name="ignoreNullValue">whether to ignore null value</param>
        /// <returns>Json string</returns>
        /// <remarks>string type will not be serialized, e.g. the output of 'abc' will be 'abc', not '"abc"'.</remarks>
        /// <remarks>Enum will be serialized to string.</remarks>
        public static string ToJsonString(object value, bool ignoreNullValue = false)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            var setting = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new StringEnumConverter() },
                NullValueHandling = ignoreNullValue ? NullValueHandling.Ignore : NullValueHandling.Include
            };

            // Note that when JsonConvert simple types, quotes will be added to the value
            return JsonConvert.SerializeObject(value, setting);
        }

        /// <summary>
        /// Serialize object to json string with indent
        /// </summary>
        /// <param name="value">object to be serialized</param>
        /// <param name="ignoreNullValue">whether to ignore null value</param>
        /// <returns>Json string</returns>
        /// <remarks>By default, null value will be ignored.</remarks>
        /// <remarks>Enum will be serialized to string.</remarks>
        public static string ToIndentedJsonString(object value, bool ignoreNullValue = true)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            var setting = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = ignoreNullValue? NullValueHandling.Ignore : NullValueHandling.Include
            };

            // Note that when JsonConvert simple types, quotes will be added to the value
            return JsonConvert.SerializeObject(value, setting);
        }

        /// <summary>
        /// deserialize input string to type T. Throw ArgumentException if input is not in json format or not a valid json format of type T (i.e. missing member).
        /// </summary>
        /// <typeparam name="T">type deserialized to</typeparam>
        /// <param name="value">string to be deserialized</param>
        /// <returns>deserialized object of type T</returns>
        public static T FromJsonStringStrict<T>(string value)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            try
            {
                return JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid json string", jre);
            }
            catch (JsonSerializationException jse)
            {
                throw new ArgumentException($"Invalid json string for type {typeof(T)}.", jse);
            }
        }

        /// <summary>
        /// deserialize input string to type T. Throw ArgumentException if input is not in json format or not a valid json format of type T
        /// </summary>
        /// <typeparam name="T">type deserialized to</typeparam>
        /// <param name="value">string to be deserialized</param>
        /// <returns>deserialized object of type T</returns>
        public static T FromJsonString<T>(string value)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid json string", jre);
            }
            catch (JsonSerializationException jse)
            {
                throw new ArgumentException($"Invalid json string for type {typeof(T)}.", jse);
            }
        }

        /// <summary>
        /// Convert a json deserialized object to specific type T
        /// </summary>
        /// <typeparam name="T">type to convert to</typeparam>
        /// <param name="obj">json deserialized object</param>
        /// <param name="ignoreError">whether to ignore conversion error, default is false</param>
        /// <returns>converted object in type <typeparamref name="T"/></returns>
        public static T ToSpecificType<T>(object obj, bool ignoreError = false)
        {
            Guard.ArgumentNotNull(obj, nameof(obj));

            try
            {
                return JObject.FromObject(obj).ToObject<T>();
            }
            catch (Exception e) when (ignoreError && (e is JsonReaderException || e is JsonSerializationException))
            {
                return default(T);
            }
        }

        /// <summary>
        /// Serialize object to json stream.
        /// </summary>
        /// <param name="value">object to be serialized</param>
        /// <param name="ignoreNullValue">whether to ignore null value</param>
        /// <returns>Json stream</returns>
        public static Stream ToJsonStream(object value, bool ignoreNullValue = false)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            var jsonSerializer = new JsonSerializer { NullValueHandling = ignoreNullValue ? NullValueHandling.Ignore : NullValueHandling.Include };

            jsonSerializer.Serialize(streamWriter, value);

            streamWriter.Flush();
            memoryStream.Position = 0;

            return memoryStream;
        }

        /// <summary>
        /// deserialize input stream to type T. Throw ArgumentException if input is not in json format or not a valid json format of type T
        /// </summary>
        /// <typeparam name="T">type deserialized to</typeparam>
        /// <param name="stream">stream to be deserialized</param>
        /// <returns>deserialized object of type T</returns>
        public static T FromJsonStream<T>(Stream stream)
        {
            Guard.ArgumentNotNull(stream, nameof(stream));

            try
            {
                using (var sr = new StreamReader(stream))
                using (var reader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(reader);
                }
            }
            catch (JsonReaderException jre)
            {
                throw new ArgumentException("Invalid json string", jre);
            }
            catch (JsonSerializationException jse)
            {
                throw new ArgumentException($"Invalid json string for type {typeof(T)}.", jse);
            }
        }

        public static T ReadFromFile<T>(string mappingFilePath)
        {
            using (var streamReader = File.OpenText(mappingFilePath))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        /// <summary>
        /// Converter for converting Uri to string and vica versa
        /// </summary>
        public sealed class UriJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Uri);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                {
                    return null;
                }

                return new Uri((string)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteValue(((Uri)value).OriginalString);
            }
        }
    }
}
