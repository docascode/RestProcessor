namespace Microsoft.RestApi.RestCI
{
    using Microsoft.RestApi.RestSplitter;
    using System;
    using System.IO;

    public class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                {
                    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_root_directory] [target_root_directory] [mappingfile.json]");
                    return 1;
                }

                RestSpliter(args[0], args[1], args[2]);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex.Message}");
                return 1;
            }
        }

        public static void RestSpliter(string sourceRootDir, string targetRootDir, string mappingFilePath)
        {
            if (!Directory.Exists(sourceRootDir))
            {
                throw new ArgumentException($"{nameof(sourceRootDir)} '{sourceRootDir}' should exist.");
            }
            if (string.IsNullOrEmpty(targetRootDir))
            {
                throw new ArgumentException($"{nameof(targetRootDir)} should not be null or empty.");
            }
            if (!File.Exists(mappingFilePath))
            {
                throw new ArgumentException($"{nameof(mappingFilePath)} '{mappingFilePath}' should exist.");
            }

            var splitter = new RestSplitter(sourceRootDir, targetRootDir, mappingFilePath);
            splitter.Process();
            Console.WriteLine("Done processing all swagger files");
        }

        public static void RestProcessor()
        {
            //todo
        }

        public static void RestTransformer()
        {
            //todo
        }
    }
}
