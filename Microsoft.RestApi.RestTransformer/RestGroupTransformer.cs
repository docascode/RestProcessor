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
        
        public OperationGroupEntity Transform(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel, ConcurrentDictionary<string, ConcurrentBag<Operation>> groupOperations, string version)
        {
            var serviceId = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-id");
            var serviceName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-service-name");
            var groupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var subgroupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-sub-group-name");
            var productUid = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-product-uid");

            var basePath = swaggerModel.BasePath;
            var apiVersion = swaggerModel.Info.Version;

            var groupId = Utility.TrimUId($"{Utility.GetHostWithBasePathUId(swaggerModel.Host, productUid, basePath)}.{serviceId}.{subgroupName}.{groupName}")?.ToLower();

            ConcurrentBag<Operation> operations;
            var key = string.IsNullOrEmpty(version) ? groupId : $"{version}_{groupId}";
            if (groupOperations.TryGetValue(key, out operations))
            {
                if (operations.Count() > 0)
                {
                    var summary = GetSummary(swaggerModel, viewModel);
                    var metadataDesc = Utility.ExtractMetaDataDescription(summary);
                    if (string.IsNullOrEmpty(metadataDesc)) {
                        metadataDesc = GenerateMetaDataDesc(operations);
                    }

                    return new OperationGroupEntity
                    {
                        Id = groupId,
                        ApiVersion = apiVersion,
                        Name = groupName,
                        Operations = operations.OrderBy(p => p.Id).ToList(),
                        Service = serviceName,
                        Summary = summary,
                        Metadata = new MetaDataEntity() {
                            Description = Utility.FormatMetaDataDescription(metadataDesc, serviceName)
                        }
                    };
                }
            }
            
            return null;
        }

        private string GenerateMetaDataDesc(ConcurrentBag<Operation> operations)
        {
            var names=operations.Select(p=>p.Name);
            return string.Join("|", names);
        }
    }
}
