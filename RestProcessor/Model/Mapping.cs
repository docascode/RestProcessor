namespace RestProcessor
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class Mapping
    {
        [JsonProperty("documentation")]
        public List<DocumentationItem> DocumentationItems { get; set; }

        [JsonProperty("reference")]
        public List<ReferenceItem> ReferenceItems { get; set; }
    }
}
