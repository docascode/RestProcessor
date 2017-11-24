namespace Microsoft.RestApi.RestTransformer
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;
    using Microsoft.DocAsCode.DataContracts.RestApi;

    using Newtonsoft.Json;

    public class RestTransformer
    {
        public static readonly YamlDotNet.Serialization.Serializer YamlSerializer = new YamlDotNet.Serialization.Serializer();

        public static void Process(string filePath, SwaggerModel swaggerModel, RestApiRootItemViewModel viewModel)
        {
            if(viewModel.Metadata.TryGetValue("x-internal-split-type", out var fileType))
            {
                string currentFileType = (string)fileType;
                if (currentFileType == "OperationGroup")
                {

                }
                else if (currentFileType == "Operation")
                {
                    if (viewModel.Children?.Count == 1)
                    {
                        using (var writer = new StreamWriter(filePath))
                        {
                            writer.WriteLine("### YamlMime:RESTOperation");
                            YamlSerializer.Serialize(writer, RestOperationTransformer.Transform(swaggerModel, viewModel.Children.First()));
                        }
                        File.Delete(Path.ChangeExtension(filePath, ".json"));
                    }
                    else
                    {
                        Console.WriteLine($"Please make sure there is only 1 child here. the actual children number is : {viewModel.Children?.Count}");
                    }
                }
            }
        }
    }
}
