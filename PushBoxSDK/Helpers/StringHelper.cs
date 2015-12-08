using System;

namespace HouseOfCode.Helpers
{
    public static class StringHelper
    {
        public static bool EqualsCaseInsensitive(this string a, string b)
        {
            return 0 == String.Compare(
                     a,
                     b,
                     System.Globalization.CultureInfo.InvariantCulture,
                     System.Globalization.CompareOptions.IgnoreCase
                );
        }
    }
}
