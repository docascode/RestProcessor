namespace Microsoft.RestApi.RestSplitter.Generator
{
    using System.Collections.Generic;

    using Microsoft.RestApi.RestSplitter.Model;

    public interface IGenerator
    {
        IEnumerable<FileNameInfo> Generate();
    }
}