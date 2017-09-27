namespace RestProcessor.Generator
{
    using System.Collections.Generic;

    public interface IGenerator
    {
        IEnumerable<RestSplitter.FileNameInfo> Generate();
    }
}