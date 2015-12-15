using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    internal static class StreamHelper
    {
        internal static string ReadAll(this Stream stream)
        {
            var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
