namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestTransformer.Models;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class Utility
    {
        public static T GetValueFromMetaData<T>(this Dictionary<string, object> metadata, string key, T defaultValue=default(T))
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
                    return defaultValue;
                }
            }
            return defaultValue;
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

        public static string GetApiVersion(RestApiChildItemViewModel child, string defaultApiVersion)
        {
            string apiVersion = child.Metadata.GetValueFromMetaData<string>("x-ms-docs-override-version");
            return string.IsNullOrEmpty(apiVersion) ? defaultApiVersion : apiVersion;
        }

        public static ParameterizedHost GetHostWithParameters(string host, Dictionary<string, object> metadata, Dictionary<string, object> pathMetadata)
        {
            var hostParameterEntities = new List<ParameterEntity>();
            var useSchemePrefix = true;
            if (metadata.TryGetValue("x-ms-parameterized-host", out var jHost))
            {
                var parameterizedHost = ((JObject)jHost).ToObject<Dictionary<string, object>>();
                host = parameterizedHost.GetValueFromMetaData<string>("hostTemplate");
                useSchemePrefix = parameterizedHost.GetValueFromMetaData("useSchemePrefix", true);
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
            return new ParameterizedHost
            {
                Host = string.IsNullOrEmpty(host) ? string.Empty : host,
                Parameters = hostParameterEntities,
                UseSchemePrefix = useSchemePrefix
            };
        }

        public static string FormatMetaDataDescription(string desc, string serviceName)
        {
            if (string.IsNullOrEmpty(desc) || desc.Length <= 100)
            {
                const string cannedSuffixedFormat = "Learn more about <{0}> service - {1}";
                return string.Format(cannedSuffixedFormat, serviceName, desc);
            }

            return desc;
        }

        public static string ExtractMetaDataDescription(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= 100)
            {
                return str;
            }
   
            var metaDateDescription = "";
            while (metaDateDescription.Length <= 100 && str.Length> 0)
            {
                var index = str.IndexOf('.');
                if (index == -1)
                {
                    metaDateDescription += str;
                    break;
                }

                var desc = str.Substring(0, index+1);
                metaDateDescription += desc;
                str = str.Substring(desc.Length);
            }

            return metaDateDescription;
        }
        public static string GetSummary(string summary, string description)
        {
            var content = summary;
            if (!string.IsNullOrEmpty(description) && summary != description)
            {
                content = string.IsNullOrEmpty(summary) ? description : ContactDescription(summary, description);
            }
            return content;
        }

        public static string ContactDescription(string str1, string str2)
        {
            var description = string.Empty;
            if (string.IsNullOrEmpty(str1))
            {
                description = str2;
            }
            else if (string.IsNullOrEmpty(str2))
            {
                description = str1;
            }
            else
            {
                // two whitespaces and one enter, will parse to <br>
                description = string.IsNullOrEmpty(str1) ? str2 : string.Concat(str1, "  \n", str2);
            }
           
            return description?.Trim('\n')?.Replace("\r\n", "\n");
        }

        public static string GetDefinitionPropertyDescription(DefinitionProperty definitionProperty)
        {
            string description = ContactDescription(definitionProperty.Title, definitionProperty.Description);
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            else if (definitionProperty.DefinitionObjectType == DefinitionObjectType.Array)
            {
                var subDescription = ContactDescription(definitionProperty.SubTitle, definitionProperty.SubDescription);
                if (!string.IsNullOrEmpty(subDescription))
                {
                    return subDescription;
                }
            }
            return string.Empty;
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
                case "401":
                    return "401 Unauthorized";
                case "403":
                    return "403 Forbidden";
                case "404":
                    return "404 Not Found";
                case "500":
                    return "500 Internal Server Error";
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

            var splitedValue = value.Replace("/", ".").Replace("\\", ".").Replace(" ", "").Trim('.').Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(".", splitedValue);
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

        public static string GetHostWithBasePathUId(string host, string productId, string basePath)
        {
            basePath = basePath?.Trim('/');
            if (!string.IsNullOrEmpty(productId))
            {
                host = productId;
            }
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
