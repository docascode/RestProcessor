namespace Microsoft.RestApi.RestTransformer
{
    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;

    public class RestOperationGroupTransformer : RestGroupTransformer
    {
        protected override string GetSummary(SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel)
        {
            return Utility.GetSummary(viewModel.Summary, viewModel.Description);
        }
    }
}
