using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Linq;

namespace HouseOfCode.PushBoxSDK.Helpers
{
    internal class DataContractJsonHelper
    {
        private ILogger Logger;

        public DataContractJsonHelper(ILogger logger)
        {
            Logger = logger;
        }
    }
}