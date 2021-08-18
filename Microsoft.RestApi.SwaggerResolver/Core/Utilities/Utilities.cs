namespace Microsoft.RestApi.SwaggerResolver.Core.Utilities
{
    using System;
    using System.Text.RegularExpressions;

    public static class Utilities
    {
        //Scientific counting method
        //private static Regex regsc = new Regex(@"([+-]?)((?<!0)\d\.\d{1,})E([-]?)(\d+)");
        //private static Regex reg = new Regex(@"^(-)?\d+\.\d+$");
        public static bool EnsureRemoveDecimalEndZero(double master)
        {
            //if (reg.IsMatch(master))
            //{
            //    target = Convert.ToString(Convert.ToDouble(master));
            //    return true;
            //}
            ////else if (regsc.IsMatch(master))
            ////{
            ////    target = master;
            ////    return true;
            ////}
            //else {
            //    target = master;
            //    return false;
            //}
            var mastertext = master.ToString();
            if (double.IsNaN(master) || double.IsInfinity(master) ||
            mastertext.IndexOf('.') != -1 || mastertext.IndexOf('E') != -1 ||
            mastertext.IndexOf('e') != -1)
            {
                return true;
            }

            return false;


        }
    }
}
