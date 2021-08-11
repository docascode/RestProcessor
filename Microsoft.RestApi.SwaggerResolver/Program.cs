using System;

namespace Microsoft.RestApi.SwaggerResolver
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Resolving begin at:" + DateTime.UtcNow);
            var paths = new string[] { @"D:\repos\apex\vstsrestapispecs\azure-rest-api-specs\specification\maps\data-plane\Creator\preview\2.0\wfs.json", @"D:\repos\apex\vstsrestapispecs\azure-rest-api-specs\specification\maps\data-plane\Creator\preview\2.0\alias.json" };

            try
            {
                foreach (var path in paths)
                {
                    Console.WriteLine($"Resolving swagger file by SwaggerResolver {path}");
                    var result = SwaggerParser.Resolver(path);
                    Settings.FileSystem.WriteAllText(path, result);
                    Console.WriteLine($"Done resolving swagger file by AutoRest{path}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex}");
                return 1;
            }
            finally
            {
                Console.WriteLine("Resolving end at:" + DateTime.UtcNow);
            }
        }
    }
}
