namespace Microsoft.RestApi.RestSplitter.Model
{
    using System.Collections.Generic;

    public class RestFileInfo
    {
        public List<FileNameInfo> FileNameInfos { get; set; } = new List<FileNameInfo>();

        public string TocTitle { get; set; }
    }

    public class FileNameInfo
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }

        public List<FileNameInfo> ChildrenFileNameInfo { get; set; }

        public string TocName { get; set; }

        public string SwaggerSourceUrl { get; set; }

        public string Version { get; set; }

    }
}
