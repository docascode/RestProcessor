namespace RestProcessor
{
    using System;

    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 3 || args.Length > 4)
                {
                    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_root_directory] [target_root_directory] [mappingfile.json] [boolean:generateMsExamplesMapping]");
                    return 1;
                }

                if (args.Length == 4 && args[3] != null)
                {
                    bool generateMsExamplesMapping;
                    if (bool.TryParse(args[3], out generateMsExamplesMapping) && generateMsExamplesMapping)
                    {
                        MsExamplesHanlder.AddMapping(args[0], args[1], args[2]);
                    }
                }
                else
                {
                    RestParser.Process(args[0], args[1], args[2]);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurs: {ex.Message}");
                return 1;
            }
        }
    }
}
