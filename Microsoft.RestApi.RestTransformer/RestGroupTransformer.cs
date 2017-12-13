namespace Microsoft.RestApi.RestTransformer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi;
    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.RestApi.RestTransformer.Models;

    using Newtonsoft.Json.Linq;

    public abstract class RestGroupTransformer
    {
        protected abstract string GetSummary(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel);
        
        public OperationGroupEntity Transform(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel, string folder)
        {
            var serviceName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-name");
            var groupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var basePath = swaggerModel.BasePath;
            var apiVersion = swaggerModel.Info.Version;

            var members = swaggerModel.Metadata.GetArrayFromMetaData<JObject>("x-internal-split-members");
            if (members != null && members.Count() > 0)
            {
                var operations = new List<Operation>();
                if (members[0].TryGetValue("relativePath", out var relativePath))
                {
                    var directoryName = Path.GetDirectoryName(Path.Combine(folder, (string)relativePath + ".json"));
                    var operationPaths = Directory.GetFiles(directoryName, "*.json");

                    foreach (var operationPath in operationPaths)
                    {
                        var childSwaggerModel = SwaggerJsonParser.Parse(operationPath);
                        var childViewModel = SwaggerModelConverter.FromSwaggerModel(childSwaggerModel);

                        var model = childViewModel.Children.FirstOrDefault();
                        var operationName = childSwaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-operation-name");
                        var operation = new Operation
                        {
                            Id = Utility.TrimUId($"{Utility.GetHostWithBasePathUId(swaggerModel.Host, basePath)}.{serviceName}.{groupName}.{operationName}")?.ToLower(),
                            Summary = Utility.GetSummary(model?.Summary, model?.Description)
                        };
                        operations.Add(operation);
                    }
                    return new OperationGroupEntity
                    {
                        Id = Utility.TrimUId($"{Utility.GetHostWithBasePathUId(swaggerModel.Host, basePath)}.{serviceName}.{groupName}")?.ToLower(),
                        ApiVersion = apiVersion,
                        Name = groupName,
                        Operations = operations,
                        Service = serviceName,
                        Summary = GetSummary(swaggerModel, viewModel)
                    };
                }
            }
            return null;
        }
    }
}
