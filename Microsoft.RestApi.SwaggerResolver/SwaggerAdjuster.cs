namespace Microsoft.RestApi.SwaggerResolver
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    public static class SwaggerAdjuster
    {
        public static void Travel(JObject jObj)
        {
            AdjustAdditionalProperties(jObj);
            AdjustConsumesAndProduces(jObj);
        }
        private static void AdjustAdditionalProperties(JObject jObj)
        {
            IEnumerable<JToken> additionalPropertiesList = jObj.SelectTokens("$..additionalProperties").Where(p => ((object)p).ToString()=="True"/* || ((object)p).ToString() == "False"*/);
            var count =0;
            while (count< additionalPropertiesList.Count())
            {
                var additionalProperties = additionalPropertiesList.ElementAt(count);
                foreach (var props in additionalProperties.Parent?.Parent?.Parent?.Children<JObject>())
                {
                    JProperty typeProp = props.Property("type");
                    JProperty additionalPropertiesProp = props.Property("additionalProperties");
                    if (additionalPropertiesProp != null)
                    {
                        if (typeProp?.Value?.ToString() == "object")
                        {
                            typeProp.Remove();
                        }

                        additionalPropertiesProp.Remove();
                        var prop = (JObject)JsonConvert.DeserializeObject("{'type':'object'}");
                        props.Add("additionalProperties", prop);
                    }
                    else {
                        count++;
                    }
                }
            }
        }

        private static void AdjustConsumesAndProduces(JObject jObj)
        {
            var consumes = jObj.SelectToken("consumes")?.DeepClone();
            var produces = jObj.SelectToken("produces")?.DeepClone();

            if (consumes == null && produces != null)
            {
                return;
            }
            if (consumes != null)
            {
                jObj.Property("consumes").Remove();
            }
            if (produces != null)
            {
                jObj.Property("produces").Remove();
            }

            AdjustConsumesAndProduces(jObj, "paths", consumes, produces);
            AdjustConsumesAndProduces(jObj, "x-ms-paths", consumes, produces);
        }
        private static void AdjustConsumesAndProduces(JObject jObj,string pathName, JToken consumes, JToken produces)
        {
            if (string.IsNullOrEmpty(pathName) || jObj == null)
            {
                return;
            }
            var paths = jObj.SelectToken(pathName);
            if (paths == null)
            {
                return;
            }
            foreach (var path in paths.Children())
            {
                foreach (var action in path.Children())
                {
                    foreach (var item in action.Children())
                    {
                        var keyValues = JObject.Parse("{" + item.ToString() + "}");
                        if (keyValues.ContainsKey("post") || keyValues.ContainsKey("get") || keyValues.ContainsKey("put") || keyValues.ContainsKey("delete") || keyValues.ContainsKey("patch") || keyValues.ContainsKey("head"))
                        {
                            foreach (var child in item.Children())
                            {
                                var count = child.Children().Count();
                                if (count > 0)
                                {
                                    var token = child.ElementAt(count - 1);
                                    if (null == child["consumes"] && consumes != null)
                                    {
                                        token.AddAfterSelf(new JProperty("consumes", consumes));
                                    }
                                    if (null == child["produces"] && produces != null)
                                    {
                                        token.AddAfterSelf(new JProperty("produces", produces));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
