using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace HousePushBoxSDK.Helpers
{
    public static class CryptographyHelper
    {
        public static string CreateHash(string apiKey, string secret, int unixTimestamp)
        {
            var content = apiKey + ":" + unixTimestamp;

            return Hmac(secret, content, MacAlgorithmNames.HmacSha1);
        }

        private static string Hmac(string secretKey, string value, string hmacAlgorithm)
        {
            var key = CryptographicBuffer.ConvertStringToBinary(secretKey, BinaryStringEncoding.Utf8);
            var msg = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);

            var objMacProv = MacAlgorithmProvider.OpenAlgorithm(hmacAlgorithm);
            var hash = objMacProv.CreateHash(key);
            hash.Append(msg);
            return CryptographicBuffer.EncodeToHexString(hash.GetValueAndReset());
        }
    }
}