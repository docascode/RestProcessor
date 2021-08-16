namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using System;
    using System.Text.RegularExpressions;

    public static class Utilities
    {
        public static string Double_str(string master)
        {
            Regex reg = new Regex(@"^\d+\.\d+$");
            if (reg.IsMatch(master))
            {
                return Convert.ToString(Convert.ToDouble(master)); 
            }
            else
            {
                return master;
            }
        }
    }
}
