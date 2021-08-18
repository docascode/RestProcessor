namespace Microsoft.RestApi.SwaggerResolver
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public static class Runner
    {
        public static int Run(string[] paths)
        {
            Console.WriteLine("Resolving begin at:" + DateTime.UtcNow);
            try
            {
                Parallel.ForEach(paths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (path) =>
                {
                    var sb = new StringBuilder();
                    try
                    {
                        sb.AppendLine($"Resolving swagger file by SwaggerResolver {path}");
                        var formatPath = Path.GetFullPath(path);
                        var result = SwaggerParser.Resolver(formatPath);
                        Settings.FileSystem.WriteAllText(formatPath, result);
                        sb.AppendLine($"Done resolving swagger file by AutoRest{formatPath}");
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine(ex.Message);
                        throw ex;
                    }
                    finally
                    {
                        Console.WriteLine(sb.ToString());
                    }
                });

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
