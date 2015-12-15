using System;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    public static class StringHelper
    {
        public static bool EqualsCaseInsensitive(this string a, string b)
        {
            return 0 == String.Compare(
                     a,
                     b,
                     StringComparison.OrdinalIgnoreCase
                );
        }
    }
}
