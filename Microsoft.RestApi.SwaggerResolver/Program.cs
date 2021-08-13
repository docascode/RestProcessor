using System;

namespace Microsoft.RestApi.SwaggerResolver
{
    class Program
    {
        static int Main(string[] args)
        {
            var paths = new string[] { @"D:\repos\apex\vstsrestapispecs\azure-rest-api-specs\specification/apimanagement/resource-manager/Microsoft.ApiManagement/stable/2020-12-01/apimcontenttypes.json" };
            return Runner.Run(paths);
        }
    }
}
