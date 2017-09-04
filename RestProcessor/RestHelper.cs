namespace RestProcessor
{
    using System;

    using Newtonsoft.Json.Linq;

    public static class RestHelper
    {
        public static void ActionOnOperation(JObject jObject, Action<JObject> action)
        {
            foreach (var path in (JObject) jObject["paths"])
            {
                foreach (var item in (JObject) path.Value)
                {
                    // Skip for parameters
                    if (item.Key.Equals("parameters"))
                    {
                        continue;
                    }

                    action((JObject)item.Value);
                }
            }
        }
    }
}
