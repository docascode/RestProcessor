namespace RedirectSourceMapping
{
    using System;
    using System.IO;
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Processor begin at:" + DateTime.UtcNow);
                var legacy = new PublishFileExtractor(Path.Combine(Constants.WorkPath, "Legacy"));
                var modern = new PublishFileExtractor(Path.Combine(Constants.WorkPath, "Modern"));
                var redirect = new RedirectionExtractor(Path.Combine(Constants.WorkPath, "Redirection"));
                var generator = new Generator(legacy, modern, redirect);
                generator.Generate();
                Console.WriteLine("Processor END at:" + DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception" + ex.Message);
            }
            
        }
    }
}
