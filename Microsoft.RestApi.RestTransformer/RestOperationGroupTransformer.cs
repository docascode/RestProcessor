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

    public static class RestOperationGroupTransformer
    {
        public static OperationGroupEntity Transform(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel, string folder)
        {
            var serviceName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-name");
            var groupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var apiVersion = swaggerModel.Info.Version;

            var operations = new List<Operation>();
            var members = swaggerModel.Metadata.GetArrayFromMetaData<JObject>("x-internal-split-members");
            foreach (var member in members)
            {
                var memberDict = member.ToObject<Dictionary<string, string>>();
                if(memberDict.TryGetValue("relativePath", out var path))
                {
                    var childSwaggerModel = SwaggerJsonParser.Parse(Path.Combine(folder, path + ".json"));
                    var childViewModel = SwaggerModelConverter.FromSwaggerModel(childSwaggerModel);
                    var model = childViewModel.Children.FirstOrDefault();
                    var operationName = childSwaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-operation-name");
                    var operation = new Operation
                    {
                        Id = Utility.TrimWhiteSpace($"{swaggerModel.Host}.{serviceName}.{groupName}.{operationName}"),
                        Summary = Utility.GetSummary(model?.Summary, model?.Description)
                    };
                    operations.Add(operation);
                }
            }
            return new OperationGroupEntity
            {
                Id = Utility.TrimWhiteSpace($"{swaggerModel.Host}.{serviceName}.{groupName}"),
                ApiVersion = apiVersion,
                Name = groupName,
                Operations = operations,
                Service = serviceName,
                Summary = Utility.GetSummary(viewModel.Summary, viewModel.Description)
            };
        }
    }
}
