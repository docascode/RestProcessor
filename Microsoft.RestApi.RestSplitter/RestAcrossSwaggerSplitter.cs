using Microsoft.RestApi.RestSplitter.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.RestApi.RestSplitter
{
    public class RestAcrossSwaggerSplitter : IDisposable
    {
        private OrgsMappingFile orgsMappingFile;
        public RestAcrossSwaggerSplitter(OrgsMappingFile orgsMappingFile) {
            this.orgsMappingFile = orgsMappingFile;
        }
        private Dictionary<string, Tuple<JObject, string>> keyValuePairs = new Dictionary<string, Tuple<JObject, string>>();
        public void Merge(Dictionary<string, Tuple<JObject, string>> keyValues)
        {
            foreach(var item in keyValues)
            {
                if (keyValuePairs.TryGetValue(item.Key, out Tuple<JObject, string> keyValuePair))
                {
                    if (keyValuePair.Item1["x-internal-split-members"] != null)
                    {
                        keyValuePair.Item1["x-internal-split-members"]= JsonConvert.SerializeObject(keyValuePair.Item1["x-internal-split-members"].Union(item.Value.Item1["x-internal-split-members"]), Newtonsoft.Json.Formatting.Indented).ToString();
                    }
                    else
                    {
                        keyValuePair.Item1["x-internal-split-members"] = item.Value.Item1["x-internal-split-members"];
                    }

                }
                else
                {
                    keyValuePairs.Add(item.Key, item.Value);
                }
            }
        }
        public void Serialize()
        {
            if (orgsMappingFile.IsGroupdedByTag)
            {
                foreach (var item in keyValuePairs)
                {
                    var file = Utility.Serialize(item.Value.Item2, Utility.TryToFormalizeUrl(item.Key, orgsMappingFile.FormalizeUrl), item.Value.Item1);
                }
            }
        }
        public void Dispose()
        {
        }
    }
}
