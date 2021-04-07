namespace Microsoft.RestApi.RestSplitter.Generator
{
    using System;
    using System.Collections.Generic;

    using Microsoft.RestApi.RestSplitter.Model;
    using Newtonsoft.Json.Linq;

    public interface IGenerator
    {
        IEnumerable<FileNameInfo> Generate();
        Dictionary<string, Tuple<JObject, string>> GetStoreInfo();
    }
}