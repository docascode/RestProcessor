namespace Microsoft.RestApi.RestTransformer
{
    using System;

    public class RestGroupTransformerFactory
    {
        public static RestGroupTransformer CreateRestGroupTransformer(string type)
        {
            Console.WriteLine($"Info: operation group generate type: {type}");
            if (type == "TagGroup")
            {
                return new RestTagGroupTransformer();
            }
            return new RestOperationGroupTransformer();
        }
    }
}
