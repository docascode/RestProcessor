namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using System;
    using System.Text.RegularExpressions;

    public static class Utilities
    {
        public static bool Double_str(string master, out string target)
        {
            Regex reg = new Regex(@"^\d+\.\d+$");
            if (reg.IsMatch(master))
            {
                target= Convert.ToString(Convert.ToDouble(master));
                return true;
            }
            else
            {
                target= master;
                return false;
            }
        }
    }
}
