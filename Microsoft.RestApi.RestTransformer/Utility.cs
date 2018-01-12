namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestTransformer.Models;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class Utility
    {
        public static T GetValueFromMetaData<T>(this Dictionary<string, object> metadata, string key)
        {
            Guard.ArgumentNotNull(metadata, nameof(metadata));
            Guard.ArgumentNotNullOrEmpty(key, nameof(key));

            if (metadata.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"can not convert the metadata[{key}], detail: {ex}");
                    return default(T);
                }
            }
            return default(T);
        }

        public static T[] GetArrayFromMetaData<T>(this Dictionary<string, object> metadata, string key)
        {
            Guard.ArgumentNotNull(metadata, nameof(metadata));
            Guard.ArgumentNotNullOrEmpty(key, nameof(key));

            if (metadata.TryGetValue(key, out object value))
            {
                try
                {
                    return ((JArray)value).ToObject<T[]>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"can not convert the metadata[{key}], detail: {ex}");
                    return default(T[]);
                }
            }
            return default(T[]);
        }

        public static T GetDictionaryFromMetaData<T>(this Dictionary<string, object> metadata, string key)
        {
            Guard.ArgumentNotNull(metadata, nameof(metadata));
            Guard.ArgumentNotNullOrEmpty(key, nameof(key));

            if (metadata.TryGetValue(key, out object value))
            {
                try
                {
                    return ((JObject)value).ToObject<T>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"can not convert the metadata[{key}], detail: {ex}");
                    return default(T);
                }
            }
            return default(T);
        }

        public static string GetScheme(Dictionary<string, object> metadata)
        {
            var defaultScheme = string.Empty;
            var schemes = metadata.GetValueFromMetaData<JArray>("schemes");
            if(schemes != null)
            {
                var scheme = schemes.ToObject<string[]>()?.FirstOrDefault();
                if (!string.IsNullOrEmpty(scheme))
                {
                    return scheme;
                }
                
            }
            return defaultScheme;
        }

        public static Tuple<string, List<ParameterEntity>> GetHostWithParameters(string host, Dictionary<string, object> metadata, Dictionary<string, object> pathMetadata)
        {
            var hostParameterEntities = new List<ParameterEntity>();
            if (metadata.TryGetValue("x-ms-parameterized-host", out var jHost))
            {
                var parameterizedHost = ((JObject)jHost).ToObject<Dictionary<string, object>>();
                host = parameterizedHost.GetValueFromMetaData<string>("hostTemplate");
                var hostParameters = parameterizedHost.GetValueFromMetaData<JArray>("parameters");
                if (hostParameters != null)
                {
                    foreach (var hostParameter in hostParameters.ToObject<JObject[]>())
                    {
                        var entity = hostParameter.ToObject<ParameterEntity>();
                        entity.Types = new List<BaseParameterTypeEntity>
                        {
                            new BaseParameterTypeEntity
                            {
                                Id = (string)hostParameter.GetValue("type")
                            }
                        };
                        hostParameterEntities.Add(entity);
                    }
                }
            }
            if (pathMetadata.TryGetValue("x-ms-parameterized-host", out var pHost))
            {
                var parameterizedHost = ((JObject)pHost).ToObject<Dictionary<string, object>>();
                host = parameterizedHost.GetValueFromMetaData<string>("hostTemplate");
            }
            return Tuple.Create(string.IsNullOrEmpty(host) ? string.Empty : host, hostParameterEntities);
        }

        public static string GetSummary(string summary, string description)
        {
            var content = summary;
            if (!string.IsNullOrEmpty(description) && summary != description)
            {
                content = string.IsNullOrEmpty(summary) ? description : $"{summary} {description}";
            }
            return content;
        }

        public static string ContactDescription(string str1, string str2)
        {
            // two whitespaces and one enter, will parse to <br>
            var description = string.Concat(str1, "  \n", str2);
            return description.Trim('\n').Replace("\r\n", "\n");
        }

        public static string GetDefinitionPropertyDescription(DefinitionProperty definitionProperty)
        {
            string description = ContactDescription(definitionProperty.Title, definitionProperty.Description);
            if (definitionProperty.DefinitionObjectType != DefinitionObjectType.Array)
            {
                return description;
            }
            else
            {
                var subDescription = ContactDescription(definitionProperty.SubTitle, definitionProperty.SubDescription);
                if (string.IsNullOrEmpty(subDescription))
                {
                    return description;
                }
                return subDescription;
            }
        }

        public static string GetDefinitionDescription(Definition definition)
        {
            if (string.IsNullOrEmpty(definition.Title))
            {
                return definition.Description;
            }
            return definition.Title;
        }

        public static string GetStatusCodeString(string statusCode)
        {
            switch (statusCode)
            {
                case "200":
                    return "200 OK";
                case "201":
                    return "201 Created";
                case "202":
                    return "202 Accepted";
                case "204":
                    return "204 No Content";
                case "400":
                    return "400 Bad Request";
                default:
                    return "Other Status Codes";
            }
        }

        public static string TrimUId(this string value)
        {
            if(value == null)
            {
                return null;
            }
            return value.Replace(" ", "").Trim('.');
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
            {
                return null;
            }
            return str.Length > 1 ?char.ToUpper(str[0]) + str.Substring(1) : str.ToUpper();
        }

        public static string GetHostWithBasePath(string host, string basePath)
        {
            basePath = basePath?.Trim('/');
            if (string.IsNullOrEmpty(basePath))
            {
                return host;
            }
            return $"{host}/{basePath}";
        }

        public static string ResolveScheme(string scheme)
        {
            if (!string.IsNullOrEmpty(scheme))
            {
                return $"{scheme}://";
            }
            return string.Empty;
        }

        public static string GetHostWithBasePathUId(string host, string basePath)
        {
            basePath = basePath?.Trim('/');
            if (string.IsNullOrEmpty(basePath))
            {
                return host;
            }
            return $"{host}.{basePath}";
        }

        public static string FormatJsonString(object jsonValue)
        {
            if (jsonValue == null)
            {
                return null;
            }
            try
            {
                return JsonUtility.ToIndentedJsonString(jsonValue).Replace("\r\n", "\n");
            }
            catch
            {
                return null;
            }
        }

        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
