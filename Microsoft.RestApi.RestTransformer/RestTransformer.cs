namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi;
    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.YamlSerialization;
    using Microsoft.RestApi.RestTransformer.Models;

    public class RestTransformer
    {
        public static readonly YamlSerializer YamlSerializer = new YamlSerializer();

        public static Operation ProcessOperation(string groupKey, string ymlPath, string filePath,bool needPermission=false)
        {
            var swaggerModel = SwaggerJsonParser.Parse(filePath);
            var viewModel = SwaggerModelConverter.FromSwaggerModel(swaggerModel);
            if (viewModel.Metadata.TryGetValue("x-internal-split-type", out var fileType))
            {
                string currentFileType = (string)fileType;
                if (string.Equals("Operation", currentFileType))
                {
                    if (viewModel.Children?.Count == 1)
                    {
                        var operationInfo = RestOperationTransformer.Transform(groupKey, swaggerModel, viewModel.Children.First(), needPermission);
                        if (operationInfo != null)
                        {
                            using (var writer = new StreamWriter(ymlPath))
                            {
                                writer.WriteLine("### YamlMime:RESTOperation");
                                YamlSerializer.Serialize(writer, operationInfo);
                            }
                            return new Operation
                            {
                                Id = operationInfo.Id,
                                GroupId = operationInfo.GroupId,
                                Summary = operationInfo.Summary
                            };
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Please make sure there is only 1 child here. the actual children number is : {viewModel.Children?.Count}");
                    }
                }
            }
            return null;
        }

        public static void ProcessGroup(string ymlPath, string filePath, ConcurrentDictionary<string, ConcurrentBag<Operation>> groupOperations, string version)
        {
            var swaggerModel = SwaggerJsonParser.Parse(filePath);
            var viewModel = SwaggerModelConverter.FromSwaggerModel(swaggerModel);
            if (viewModel.Metadata.TryGetValue("x-internal-split-type", out var fileType))
            {
                string currentFileType = (string)fileType;
                if (currentFileType == "OperationGroup" || currentFileType == "TagGroup")
                {
                    var restGroupTransformer = RestGroupTransformerFactory.CreateRestGroupTransformer(currentFileType);
                    var groupInfo = restGroupTransformer.Transform(swaggerModel, viewModel, groupOperations, version);
                    if (groupInfo != null)
                    {
                        using (var writer = new StreamWriter(ymlPath))
                        {
                            writer.WriteLine("### YamlMime:RESTOperationGroup");
                            YamlSerializer.Serialize(writer, groupInfo);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warining: the group has no members: {filePath}");
                    }
                }
            }
        }
    }
}
