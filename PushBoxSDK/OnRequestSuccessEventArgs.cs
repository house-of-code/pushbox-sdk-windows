using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.PushBoxSDK
{
    public class OnRequestSuccessEventArgs
    {
        public readonly string ApiMethod;
        public readonly string Message;

        public OnRequestSuccessEventArgs(string apiMethod, string message)
        {
            ApiMethod = apiMethod;
            Message = message;
        }
    }
}
