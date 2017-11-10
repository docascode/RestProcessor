namespace Microsoft.RestApi.RestSplitter.Model
{
    using System.Collections.Generic;

    public class SwaggerToc
    {
        public string Title { get; }

        public string FilePath { get; }

        public List<SwaggerToc> ChildrenToc { get; }

        public SwaggerToc(string title, string filePath, List<SwaggerToc> childrenToc = null)
        {
            Title = title;
            FilePath = filePath;
            ChildrenToc = childrenToc;
        }
    }
}
