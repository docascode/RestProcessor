namespace RedirectSourceMapping.Model
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    public class Redirections
    {
        [JsonProperty("redirections")]
        public HashSet<Redirection> List { get; set; }
    }

    public class Redirection
    {
        [JsonProperty("source_path", Order = 1)]
        public string Source_path { get; set; }
        [JsonProperty("redirect_url",Order =2)]
        public string Redirect_url { get; set; }
        [JsonProperty("redirect_document_id",Order =3)]
        public bool Redirect_document_id { get; set; }
    }
}
