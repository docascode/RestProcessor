namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;

    public class FloatValueJsonConverter: JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JObject) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonException();
            }

            JObject jObject = JObject.Load(reader); ;
            reader = new JObject(jObject).CreateReader();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Float:
                        var obj = jObject.SelectToken(reader.Path);
                        var keyValue = obj?.Parent?.Parent?.Parent;
                        foreach (var item in keyValue?.Children<JObject>())
                        {
                            var props = item.Properties();
                            var count = 0;
                            while (count < props.Count())
                            {
                                var jProperty = props.ElementAt(count);
                                var name = jProperty.Name;
                                var value = jProperty.Value;
                                if(string.Compare(reader.Path,jProperty.Path,true)==0 && !Utilities.EnsureRemoveDecimalEndZero(value.Value<double>()))
                                {
                                    try
                                    {
                                        var convert = Convert.ToInt32(value.Value<string>());
                                        jProperty.Remove();
                                        item.Add(name, convert);
                                    }
                                    catch
                                    { 
                                    }
                                }
                                count++;
                            }
                        }
                        break;
                }
            }

            return jObject;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

        }
    }
}
