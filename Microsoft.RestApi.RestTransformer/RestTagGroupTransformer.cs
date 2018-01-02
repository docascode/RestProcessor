namespace Microsoft.RestApi.RestTransformer
{
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;

    public class RestTagGroupTransformer : RestGroupTransformer
    {
        protected override string GetSummary(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel)
        {
            var groupName = swaggerModel.Metadata.GetValueFromMetaData<string>("x-internal-toc-name");
            var tag = viewModel.Tags?.FirstOrDefault(t => t.Name == groupName);
            if (!string.IsNullOrEmpty(tag?.Description))
            {
                return tag?.Description;
            }
            return swaggerModel.Info?.PatternedObjects?.GetValueFromMetaData<string>("description");
        }
    }
}
