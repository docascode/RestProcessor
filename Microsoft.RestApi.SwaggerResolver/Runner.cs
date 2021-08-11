namespace Microsoft.RestApi.SwaggerResolver
{
    using System;
    using System.IO;

    public static class Runner
    {
        public static int Run(string[] paths)
        {
            Console.WriteLine("Resolving begin at:" + DateTime.UtcNow);
            try
            {
                foreach (var path in paths)
                {
                    Console.WriteLine($"Resolving swagger file by SwaggerResolver {path}");
                    var formatPath = Path.GetFullPath(path);
                    var result = SwaggerParser.Resolver(formatPath);
                    Settings.FileSystem.WriteAllText(formatPath, result);
                    Console.WriteLine($"Done resolving swagger file by AutoRest{formatPath}");
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
