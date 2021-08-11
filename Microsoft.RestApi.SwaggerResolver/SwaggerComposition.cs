namespace Microsoft.RestApi.SwaggerResolver
{
    using Newtonsoft.Json.Linq;
    using System.Linq;

    public static class SwaggerComposition
    {
        public static void Travel(JObject jObj)
        {
            var consumes = jObj.SelectToken("consumes").DeepClone();
            var produces = jObj.SelectToken("produces").DeepClone();
            jObj.Property("consumes").Remove();
            jObj.Property("produces").Remove();
            var paths = jObj.SelectToken("paths");
            foreach (var path in paths.Children())
            {
                foreach (var action in path.Children())
                {
                    foreach (var item in action.Children())
                    {
                        foreach (var child in item.Children())
                        {
                            var count = child.Children().Count();

                            if (count > 0)
                            {
                                var token = child.ElementAt(count - 1);
                                if (null == child["consumes"])
                                {
                                    token.AddAfterSelf(new JProperty("consumes", consumes));
                                }
                                if (null == child["produces"])
                                {
                                    token.AddAfterSelf(new JProperty("produces", produces)); ;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
