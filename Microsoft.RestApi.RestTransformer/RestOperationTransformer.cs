namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.RestApi.Common;
    using Microsoft.RestApi.RestTransformer.Models;

    using Newtonsoft.Json.Linq;
    using System.IO;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;

    public static class RestOperationTransformer
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static OperationEntity Transform(SwaggerModel swaggerModel, RestApiChildItemViewModel viewModel)
        {
            var scheme = Utility.GetScheme(swaggerModel.Metadata);
            var hostWithParameters = Utility.GetHostWithParameters(swaggerModel.Host, swaggerModel.Metadata);
            var host = hostWithParameters.Item1;
            var hostParameters = hostWithParameters.Item2;
            var apiVersion = swaggerModel.Info.Version;

            var parameterDefinitionObject = new DefinitionObject();
            var parameters = TransformParameters(hostParameters, viewModel, ref parameterDefinitionObject);
            var definitions = TransformDefinitions(parameterDefinitionObject);

            var responseDefinitionObject = new DefinitionObject();
            var responses = TransformResponses(viewModel, ref responseDefinitionObject);
            var responseDefinitions = TransformDefinitions(responseDefinitionObject, true);

            foreach (var definition in responseDefinitions)
            {
                if (!definitions.Any(d => d.Name == definition.Name))
                {
                    definitions.Add(definition);
                }
            }

            var paths = TransformPaths(viewModel, scheme, host, apiVersion, parameters);

            var serviceName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-name");
            var gourpName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var operationName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-operation-name");

            return new OperationEntity
            {
                Id = Utility.TrimWhiteSpace($"{swaggerModel.Host}.{serviceName}.{gourpName}.{operationName}"),
                Name = operationName,
                Service = serviceName,
                Summary = Utility.GetSummary(viewModel.Summary, viewModel.Description),
                ApiVersion = apiVersion,
                GroupName = gourpName,
                Responses = responses,
                Parameters = SortParameters(paths, parameters.Where(p => p.ParameterEntityType == ParameterEntityType.Query || p.ParameterEntityType == ParameterEntityType.Path).ToList()),
                RequestBodies = parameters.Where(p => p.ParameterEntityType == ParameterEntityType.Body).ToList(),
                RequestHeaders = parameters.Where(p => p.ParameterEntityType == ParameterEntityType.Header).ToList(),
                Paths = HandlePathsDefaultValues(paths, apiVersion),
                Produces = viewModel.Metadata.GetArrayFromMetaData<string>("produces"),
                Consumes = viewModel.Metadata.GetArrayFromMetaData<string>("consumes"),
                Examples = TransformExamples(viewModel, paths, parameters),
                Definitions = definitions
            };

        }

        private static string GetOptionalFullPath(IList<PathEntity> paths)
        {
            var pathContent = paths.Where(p => p.IsOptional).FirstOrDefault()?.Content;
            if (string.IsNullOrEmpty(pathContent))
            {
                pathContent = paths.First().Content;
            }
            return pathContent;
        }

        private static IList<ParameterEntity> SortParameters(IList<PathEntity> paths, IList<ParameterEntity> parameters)
        {
            var sortedParameters = new List<ParameterEntity>();
            var pathContent = GetOptionalFullPath(paths);
            var regex = new Regex("{.*?}");
            var matchResults = regex.Matches(pathContent);
            for (int i = 0; i < matchResults.Count; i++)
            {
                var parameter = parameters.SingleOrDefault(p => $"{{{p.Name}}}" == matchResults[i].Value);
                if (parameter != null)
                {
                    sortedParameters.Add(parameter);
                }
            }
            return sortedParameters;
        }

        private static IList<DefinitionEntity> TransformDefinitions(DefinitionObject definitionObject, bool includeRoot = false)
        {
            var definitions = new List<DefinitionEntity>();
            if (includeRoot && !string.IsNullOrEmpty(definitionObject.Type))
            {
                var parameterItems = GetRequestBody(definitionObject);
                var definition = new DefinitionEntity
                {
                    Name = definitionObject.Type,
                    Description = definitionObject.Description,
                    Kind = definitionObject.DefinitionObjectType.ToString(),
                    ParameterItems = parameterItems.Select(p => new DefinitionParameterEntity
                    {
                        Name = p.Name,
                        Description = p.Description,
                        IsReadOnly = p.IsReadOnly,
                        Types = p.Types,
                        TypesTitle = p.TypesTitle,
                        Pattern = p.Pattern,
                        Format = p.Format

                    }).ToList()
                };
                definitions.Add(definition);
            }

            foreach (var item in definitionObject.PropertyItems)
            {
                if (definitions.Any(d => d.Name == item.Type))
                {
                    continue;
                }
                if (item.DefinitionObjectType == DefinitionObjectType.Object || (item.DefinitionObjectType == DefinitionObjectType.Array && item.PropertyItems?.Count > 0))
                {
                    if (string.IsNullOrEmpty(item.AdditionalType))
                    {
                        var parameterItems = GetRequestBody(item);
                        var definition = new DefinitionEntity
                        {
                            Name = item.Type,
                            Description = item.Description,
                            Kind = "object",
                            ParameterItems = parameterItems.Select(p => new DefinitionParameterEntity
                            {
                                Name = p.Name,
                                Description = p.Description,
                                IsReadOnly = p.IsReadOnly,
                                Types = p.Types,
                                TypesTitle = p.TypesTitle,
                                Pattern = p.Pattern,
                                Format = p.Format

                            }).ToList()
                        };
                        definitions.Add(definition);
                    }

                    var tmpDefinitions = TransformDefinitions(item);
                    foreach (var tmpDefinition in tmpDefinitions)
                    {
                        if (!definitions.Any(d => d.Name == tmpDefinition.Name))
                        {
                            definitions.Add(tmpDefinition);
                        }
                    }
                }
                else if (item.DefinitionObjectType == DefinitionObjectType.Enum)
                {

                    var definition = new DefinitionEntity
                    {
                        Name = item.Type,
                        Description = item.Description,
                        Kind = "enum",
                        ParameterItems = item.EnumValues.Select(value => new DefinitionParameterEntity
                        {
                            Name = value,
                            Types = new List<BaseParameterTypeEntity> { new BaseParameterTypeEntity { Id = "string" } },

                        }).ToList()

                    };
                    definitions.Add(definition);
                }
            }

            foreach (var allOf in definitionObject.AllOfs)
            {
                var tmpDefinitions = TransformDefinitions(allOf);
                foreach (var tmpDefinition in tmpDefinitions)
                {
                    if (!definitions.Any(d => d.Name == tmpDefinition.Name))
                    {
                        definitions.Add(tmpDefinition);
                    }
                }
            }

            return definitions;
        }

        private static IList<PathEntity> HandlePathsDefaultValues(IList<PathEntity> paths, string apiVersion)
        {
            foreach (var path in paths)
            {
                path.Content = path.Content.Replace("{api-version}", apiVersion);
            }
            return paths;
        }

        private static void ResolveObject(string key, JObject nodeObject, DefinitionObject definitionObject, string[] requiredFields = null)
        {
            if (nodeObject.Type == JTokenType.Object)
            {
                var nodeObjectDict = nodeObject.ToObject<Dictionary<string, object>>();
                var refName = nodeObjectDict.GetValueFromMetaData<string>("x-internal-ref-name");
                var currentType = nodeObjectDict.GetValueFromMetaData<string>("type");
                definitionObject.Name = key ?? refName;
                definitionObject.Type = refName;
                definitionObject.Description = nodeObjectDict.GetValueFromMetaData<string>("description");
                definitionObject.IsReadOnly = nodeObjectDict.GetValueFromMetaData<bool>("readOnly");
                definitionObject.IsFlatten = nodeObjectDict.GetValueFromMetaData<bool>("x-ms-client-flatten");

                if (requiredFields != null && requiredFields.Any(v => v == definitionObject.Name))
                {
                    definitionObject.IsRequired = true;
                }

                var requiredProperties = nodeObjectDict.GetArrayFromMetaData<string>("required");

                var allOf = nodeObjectDict.GetArrayFromMetaData<JObject>("allOf");
                if (allOf != null && allOf.Count() > 0)
                {
                    definitionObject.DefinitionObjectType = DefinitionObjectType.Object;
                    foreach (var oneAllOf in allOf)
                    {
                        var childDefinitionObject = new DefinitionObject();
                        ResolveObject(string.Empty, oneAllOf, childDefinitionObject, requiredProperties);
                        definitionObject.AllOfs.Add(childDefinitionObject);
                    }
                }

                if (nodeObjectDict.GetValueFromMetaData<JObject>("properties") != null)
                {
                    definitionObject.DefinitionObjectType = DefinitionObjectType.Object;
                    var propertiesNode = nodeObjectDict.GetValueFromMetaData<JObject>("properties");
                    var properties = propertiesNode.ToObject<Dictionary<string, object>>();
                    foreach (var property in properties)
                    {
                        var childDefinitionObject = new DefinitionObject();

                        ResolveObject(property.Key, (JObject)property.Value, childDefinitionObject, requiredProperties);
                        definitionObject.PropertyItems.Add(childDefinitionObject);
                    }
                }
                else if (nodeObjectDict.GetValueFromMetaData<JObject>("additionalProperties") != null)
                {
                    definitionObject.DefinitionObjectType = DefinitionObjectType.Object;
                    var additionalPropertiesNode = nodeObjectDict.GetValueFromMetaData<JObject>("additionalProperties");
                    var additionalProperties = additionalPropertiesNode.ToObject<Dictionary<string, object>>();
                    var additionalType = additionalProperties.GetValueFromMetaData<string>("type");
                    var additionalPropertyProperties = additionalProperties.GetDictionaryFromMetaData<Dictionary<string, object>>("properties");

                    if (additionalType == "object" && additionalPropertyProperties != null)
                    {
                        var childDefinitionObject = new DefinitionObject();
                        definitionObject.AdditionalType = additionalProperties.GetValueFromMetaData<string>("x-internal-ref-name");
                        ResolveObject(string.Empty, additionalPropertiesNode, childDefinitionObject, requiredProperties);
                        definitionObject.PropertyItems.Add(childDefinitionObject);
                    }
                    else
                    {
                        definitionObject.DefinitionObjectType = DefinitionObjectType.Simple;
                        definitionObject.AdditionalType = additionalType;
                    }
                }
                else if (nodeObjectDict.GetValueFromMetaData<JObject>("items") != null)
                {
                    definitionObject.DefinitionObjectType = DefinitionObjectType.Array;
                    var itemsDefine = nodeObjectDict.GetValueFromMetaData<JObject>("items");

                    if(itemsDefine.TryGetValue("allOf", out var allOfsNode))
                    {
                        foreach (var oneAllOf in allOfsNode.ToObject<JArray>())
                        {
                            var childDefinitionObject = new DefinitionObject();
                            ResolveObject(string.Empty, (JObject)oneAllOf, childDefinitionObject, requiredProperties);
                            definitionObject.AllOfs.Add(childDefinitionObject);
                        }
                    }

                    if (itemsDefine.TryGetValue("properties", out var propertiesNode))
                    {
                        if (itemsDefine.TryGetValue("x-internal-ref-name", out var type))
                        {
                            definitionObject.Type = (string)type;
                        }
                        if (itemsDefine.TryGetValue("description", out var subDescription))
                        {
                            definitionObject.SubDescription = (string)subDescription;
                        }

                        var properties = propertiesNode.ToObject<Dictionary<string, object>>();
                        foreach (var property in properties)
                        {
                            var childDefinitionObject = new DefinitionObject();

                            ResolveObject(property.Key, (JObject)property.Value, childDefinitionObject, requiredProperties);
                            definitionObject.PropertyItems.Add(childDefinitionObject);
                        }
                    }
                    else if (itemsDefine.TryGetValue("type", out var itemType))
                    {
                        definitionObject.Type = (string)itemType;
                    }
                }
                else
                {
                    var enumNode = nodeObjectDict.GetDictionaryFromMetaData<Dictionary<string, object>>("x-ms-enum");
                    if (enumNode != null && enumNode.TryGetValue("name", out var enumName))
                    {

                        // x-ms-enum may be have enum{name, description}
                        definitionObject.DefinitionObjectType = DefinitionObjectType.Enum;
                        definitionObject.Type = (string)enumName;
                        definitionObject.EnumValues = nodeObjectDict.GetArrayFromMetaData<string>("enum");
                    }
                    else
                    {
                        definitionObject.DefinitionObjectType = DefinitionObjectType.Simple;
                        definitionObject.Type = currentType;
                    }
                }
            }
        }

        private static void ResolveDefinition(DefinitionObject definitionObject, DefinitionObject parentDefinitionObject = null)
        {
            foreach (var allOf in definitionObject.AllOfs)
            {
                ResolveDefinition(allOf, definitionObject);
            }

            foreach (var propertyItem in definitionObject.PropertyItems)
            {
                ResolveDefinition(propertyItem, definitionObject);
                if (propertyItem.IsFlatten)
                {
                    var items = new List<DefinitionObject>();
                    foreach (var item in definitionObject.PropertyItems)
                    {
                        if (!item.IsFlatten)
                        {
                            items.Add(item);
                        }
                    }

                    foreach (var item in propertyItem.PropertyItems)
                    {
                        items.Add(item);
                    }
                    definitionObject.PropertyItems = items;
                    propertyItem.PropertyItems = new List<DefinitionObject>();
                }
            }
        }

        private static DefinitionObject ResolveSchema(JObject nodeObject)
        {
            DefinitionObject definitionObject = new DefinitionObject();
            ResolveObject(string.Empty, nodeObject, definitionObject);
            ResolveDefinition(definitionObject);
            using (var sw = new StreamWriter("C:\\1.json"))
            using (var writer = new JsonTextWriter(sw))
            {
                JsonSerializer.Serialize(writer, definitionObject);
            }
            return definitionObject;
        }

        private static IList<ParameterEntity> GetRequestBody(DefinitionObject definitionObject)
        {
            var parameters = new List<ParameterEntity>();
            foreach (var property in definitionObject.PropertyItems)
            {
                var parameterTypeEntity = new BaseParameterTypeEntity
                {
                    Id = property.Type,
                };
                if (property.DefinitionObjectType == DefinitionObjectType.Array)
                {
                    parameterTypeEntity.IsArray = true;
                }
                if (!string.IsNullOrEmpty(property.AdditionalType))
                {
                    parameterTypeEntity.AdditionalTypes = new List<IdentifiableEntity>
                    {
                        new IdentifiableEntity{ Id = "string" },
                        new IdentifiableEntity{ Id = property.AdditionalType }
                    };
                }

                if (!parameters.Any(p => p.Name == property.Name))
                {
                    parameters.Add(new ParameterEntity
                    {
                        Name = property.Name,
                        Description = property.Description,
                        IsRequired = property.IsRequired,
                        IsReadOnly = property.IsReadOnly,
                        Types = new List<BaseParameterTypeEntity>()
                        {
                            parameterTypeEntity
                        },
                        In = "body",
                        ParameterEntityType = ParameterEntityType.Body,
                        Pattern = property.Pattern,
                        Format = property.Format
                    });
                }
            }

            foreach (var allOf in definitionObject.AllOfs)
            {
                var tmpParameters = GetRequestBody(allOf);
                foreach (var tmpParameter in tmpParameters)
                {
                    if (!parameters.Any(d => d.Name == tmpParameter.Name))
                    {
                        parameters.Add(tmpParameter);
                    }
                }
            }
            return parameters;
        }

        private static IList<ParameterEntity> TransformParameters(List<ParameterEntity> hostParameters, RestApiChildItemViewModel viewModel, ref DefinitionObject definitionObject)
        {
            var parameters = hostParameters == null ? new List<ParameterEntity>() : new List<ParameterEntity>(hostParameters);
            foreach (var parameter in viewModel.Parameters)
            {
                var inType = parameter.Metadata.GetValueFromMetaData<string>("in");
                if (inType != null && Enum.TryParse<ParameterEntityType>(inType, true, out var parameterEntityType))
                {
                    var isRequired = parameter.Metadata.GetValueFromMetaData<bool>("required");
                    if (parameter.Metadata.TryGetValue("x-ms-required", out var msRequired))
                    {
                        isRequired = (bool)msRequired;
                    }
                    var types = new List<BaseParameterTypeEntity>();
                    if (parameter.Metadata.TryGetValue("type", out var type))
                    {
                        types.Add(new BaseParameterTypeEntity
                        {
                            Id = (string)type
                        });

                        var parameterEntity = new ParameterEntity
                        {
                            Name = parameter.Name,
                            Description = parameter.Description,
                            IsRequired = isRequired,
                            Pattern = parameter.Metadata.GetValueFromMetaData<string>("pattern"),
                            Format = parameter.Metadata.GetValueFromMetaData<string>("format"),
                            In = inType,
                            ParameterEntityType = parameterEntityType,
                            Types = types,
                        };
                        parameters.Add(parameterEntity);
                    }
                    else if (parameter.Metadata.TryGetValue("schema", out var schema))
                    {
                        definitionObject = ResolveSchema((JObject)schema);
                        parameters.AddRange(GetRequestBody(definitionObject));
                    }
                }
            }
            return parameters;
        }

        private static string FormatPathQueryStrings(IEnumerable<ParameterEntity> queryParameters)
        {
            var queryStrings = queryParameters.Select(p =>
            {
                return $"{p.Name}={{{p.Name}}}";
            });
            return string.Join("&", queryStrings);
        }

        private static IList<PathEntity> TransformPaths(RestApiChildItemViewModel viewModel, string scheme, string host, string apiVersion, IList<ParameterEntity> parameters)
        {
            var pathEntities = new List<PathEntity>();

            // todo: do the enum, if the parameter is enum and the enum value only have one.
            var requiredQueryStrings = parameters.Where(p => p.IsRequired && p.In == "query");
            var requiredBasePath = viewModel.Path;
            if (requiredQueryStrings.Any())
            {
                requiredBasePath = requiredBasePath + "?" + FormatPathQueryStrings(requiredQueryStrings);
            }

            pathEntities.Add(new PathEntity
            {
                Content = $"{viewModel.OperationName.ToUpper()} {scheme}://{host}{requiredBasePath}",
                IsOptional = false
            });


            var allQueryStrings = parameters.Where(p => p.In == "query");
            var optionBasePath = viewModel.Path;
            if (!allQueryStrings.All(p => p.IsRequired))
            {
                optionBasePath = optionBasePath + "?" + FormatPathQueryStrings(allQueryStrings);

                pathEntities.Add(new PathEntity
                {
                    Content = $"{viewModel.OperationName.ToUpper()} {scheme}://{host}{optionBasePath}",
                    IsOptional = true
                });
            }
            return pathEntities;
        }

        private static IList<ResponseEntity> TransformResponses(RestApiChildItemViewModel child, ref DefinitionObject definitionObject)
        {
            var responses = new List<ResponseEntity>();
            foreach (var response in child.Responses)
            {
                var typesName = new List<BaseParameterTypeEntity>();
                var schema = response.Metadata.GetDictionaryFromMetaData<Dictionary<string, object>>("schema");
                if (schema != null)
                {
                    definitionObject = ResolveSchema(response.Metadata.GetValueFromMetaData<JObject>("schema"));

                    var schemaType = schema.GetValueFromMetaData<string>("type");
                    if ((schemaType != null && schemaType == "object") || schema.GetValueFromMetaData<JObject>("properties") != null)
                    {
                        typesName.Add(new BaseParameterTypeEntity
                        {
                            Id = schema.GetValueFromMetaData<string>("x-internal-ref-name")
                        });
                    }
                    if ((schemaType != null && schemaType == "array") || schema.GetValueFromMetaData<JObject>("items") != null)
                    {
                        var items = schema.GetDictionaryFromMetaData<Dictionary<string, object>>("items");
                        if (items != null)
                        {
                            typesName.Add(new BaseParameterTypeEntity
                            {
                                IsArray = true,
                                Id = items.GetValueFromMetaData<string>("x-internal-ref-name")
                            });
                        }
                    }
                }

                responses.Add(new ResponseEntity
                {
                    Name = Utility.GetStatusCodeString(response.HttpStatusCode),
                    Description = response.Description,
                    Types = typesName.Count == 0 ? null : typesName
                });
            }
            return responses;
        }

        private static string GetExampleRequest(IList<PathEntity> paths, Dictionary<string, object> parameters)
        {
            var pathContent = GetOptionalFullPath(paths);
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (pathContent != null && pathContent.Contains($"{{{parameter.Key}}}"))
                    {
                        pathContent = pathContent?.Replace($"{{{parameter.Key}}}", Convert.ToString(parameter.Value));
                    }
                }
            }

            return pathContent;
        }

        private static List<ExampleResponseEntity> GetExampleResponses(Dictionary<string, object> msExampleResponses)
        {
            var exampleResponses = new List<ExampleResponseEntity>();
            foreach (var msExampleResponse in msExampleResponses)
            {
                var msExampleResponseValue = ((JObject)msExampleResponse.Value).ToObject<Dictionary<string, object>>();
                string body = null;
                if (msExampleResponseValue.TryGetValue("body", out var msBody) && msBody != null)
                {
                    body = JsonUtility.ToJsonString(msBody);
                }
                else if (msExampleResponseValue.TryGetValue("value", out var msValue) && msValue != null)
                {
                    body = JsonUtility.ToJsonString(msExampleResponseValue);
                }

                string header = null;
                if (msExampleResponseValue.TryGetValue("headers", out var msHeader) && msHeader != null)
                {
                    header = JsonUtility.ToJsonString(msHeader);
                }

                var exampleResponse = new ExampleResponseEntity
                {
                    StatusCode = msExampleResponse.Key,
                    Body = body,
                    Headers = header
                };
                exampleResponses.Add(exampleResponse);
            }
            return exampleResponses;
        }

        private static string GetExampleRequestBody(Dictionary<string, object> msExampleParameters, IList<ParameterEntity> bodyParameters)
        {
            foreach (var bodyParameter in bodyParameters)
            {
                if (msExampleParameters != null)
                {
                    foreach (var msExampleParameter in msExampleParameters)
                    {
                        if (msExampleParameter.Key == bodyParameter.Name || msExampleParameter.Key == "parameters")
                        {
                            return JsonUtility.ToJsonString(msExampleParameter.Value);
                        }
                    }
                }
            }

            return null;
        }

        private static IList<ExampleEntity> TransformExamples(RestApiChildItemViewModel viewModel, IList<PathEntity> paths, IList<ParameterEntity> parameters)
        {
            var examples = new List<ExampleEntity>();
            var msExamples = viewModel.Metadata.GetDictionaryFromMetaData<Dictionary<string, object>>("x-ms-examples");
            if (msExamples != null)
            {
                foreach (var msExample in msExamples)
                {
                    var msExampleValue = ((JObject)msExample.Value).ToObject<Dictionary<string, object>>();
                    var msExampleParameters = msExampleValue.GetDictionaryFromMetaData<Dictionary<string, object>>("parameters");
                    var msExampleResponses = msExampleValue.GetDictionaryFromMetaData<Dictionary<string, object>>("responses");

                    var example = new ExampleEntity
                    {
                        Name = msExample.Key,
                        Request = GetExampleRequest(paths, msExampleParameters),
                        RequestBody = GetExampleRequestBody(msExampleParameters, parameters.Where(p => p.In == "body").ToList()),
                        ExampleResponses = GetExampleResponses(msExampleResponses)
                    };

                    examples.Add(example);
                }
            }
            return examples;
        }
    }
}
