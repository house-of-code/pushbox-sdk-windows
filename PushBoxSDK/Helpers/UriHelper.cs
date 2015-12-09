using System;
using Windows.Foundation;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    public static class UriHelper
    {
        public static readonly string UriPrefix = "/_PushBoxSDK?p=";

        public static bool isPushBoxUri(this Uri uri)
        {
            return uri.ToString().StartsWith(UriPrefix);
        }
    }
}
