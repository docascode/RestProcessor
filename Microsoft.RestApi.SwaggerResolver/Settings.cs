namespace Microsoft.RestApi.SwaggerResolver
{
    using Microsoft.RestApi.SwaggerResolver.Core.Utilities;

    public class Settings
    {
        private Settings()
        { 
        }

        static Settings()
        {
            FileSystem = new FileSystem();
        }

        public static IFileSystem FileSystem;
    }
}
