namespace Microsoft.RestApi.RestTransformer
{
    using System.Collections.Concurrent;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.RestApi.RestTransformer.Models;

    public abstract class RestGroupTransformer
    {
        protected abstract string GetSummary(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel);
        
        public OperationGroupEntity Transform(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel, ConcurrentBag<Operation> allOperations)
        {
            var serviceId = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-id");
            var serviceName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-name");
            var groupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var productUid = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-product-uid");

            var basePath = swaggerModel.BasePath;
            var apiVersion = swaggerModel.Info.Version;

            var groupId = Utility.TrimUId($"{Utility.GetHostWithBasePathUId(swaggerModel.Host, productUid, basePath)}.{serviceId}.{groupName}")?.ToLower();
            var operations = allOperations.Where(o => o.GroupId == groupId);
            if (operations.Count() > 0)
            {
                return new OperationGroupEntity
                {
                    Id = groupId,
                    ApiVersion = apiVersion,
                    Name = groupName,
                    Operations = operations.ToList(),
                    Service = serviceName,
                    Summary = GetSummary(swaggerModel, viewModel)
                };
            }
            return null;
        }
    }
}
