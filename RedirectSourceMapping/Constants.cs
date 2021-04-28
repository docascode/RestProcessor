namespace RedirectSourceMapping
{
    using System.Configuration;
    public class Constants
    {
        public static string WorkPath;
        public static string SplitFolder;
        public static string PublishSuffix;
        public static string PublishPrefix;
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
