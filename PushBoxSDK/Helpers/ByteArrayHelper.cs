using System.Text;

namespace HouseOfCode.Helpers
{
    public static class ByteArrayHelper
    {
        public static string ToHexString(this byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                // hex.AppendFormat("{0:X2}", b & 0x00FF);
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}
