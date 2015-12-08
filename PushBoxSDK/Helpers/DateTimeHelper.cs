using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.Helpers
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Convert datetime to iso 8601
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToIsoString(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture);
        }
        
    }
}
