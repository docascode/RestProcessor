namespace Microsoft.RestApi.RestTransformer.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ResponseEntity : BaseParameterEntity
    {
        [YamlMember(Alias = "headers")]
        [JsonProperty("headers")]
        public IList<ResponseHeader> ResponseHeaders { get; set; }
    }
}
