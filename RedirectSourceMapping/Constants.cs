namespace RedirectSourceMapping
{
    using System.Configuration;
    public class Constants
    {
        public static string WorkPath;//= @"D:\repos\apex\RedirectSourceMapping\RedirectSourceMapping\Source";
        public static string SplitFolder;//= "docs-ref-autogen";
        public static string PublishSuffix;// = "?branch=master";
        public static string PublishPrefix;// = "https://review.docs.microsoft.com/en-us";
        static Constants()
        {
            WorkPath = ConfigurationManager.AppSettings["WorkPath"];
            SplitFolder = ConfigurationManager.AppSettings["SplitFolder"];
            PublishSuffix = ConfigurationManager.AppSettings["PublishSuffix"];
            PublishPrefix = ConfigurationManager.AppSettings["PublishPrefix"];
        }
        private Constants()
        { 
        
        }
    }
}
