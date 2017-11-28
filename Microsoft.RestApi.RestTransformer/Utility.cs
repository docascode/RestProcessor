namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestTransformer.Models;

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
            var defaultScheme = "https";
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

        public static Tuple<string, List<ParameterEntity>> GetHostWithParameters(string host, Dictionary<string, object> metadata)
        {
            var hostParameterEntities = new List<ParameterEntity>();
            if (metadata.TryGetValue("x-ms-parameterized-host", out var jHost))
            {
                var parameterizedHost = ((JObject)jHost).ToObject<Dictionary<string, object>>();
                host = parameterizedHost.GetValueFromMetaData<string>("hostTemplate");
                var hostParameters = parameterizedHost.GetValueFromMetaData<JArray>("parameters");
                if(hostParameters != null)
                {
                    foreach (var hostParameter in hostParameters.ToObject<JObject[]>())
                    {
                        var entity = hostParameter.ToObject<ParameterEntity>();
                        entity.Types = new List<BaseParameterTypeEntity>
                        {
                            new BaseParameterTypeEntity
                            {
                                Id = (string)hostParameter.GetValue("type"),

                            }
                        };
                        hostParameterEntities.Add(entity);
                    }
                }
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

        public static string TrimWhiteSpace(this string value)
        {
            if(value == null)
            {
                return null;
            }
            return value.Replace(" ", "");
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
    }
}
