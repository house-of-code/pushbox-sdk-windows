using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.Api
{
    public struct Response
    {
        public bool Success { get; set; }
        public string Uid { get; set; }
        public string Message { get; set; }
        public List<PushBoxMessage> Messages { get; set; }

        public override string ToString()
        {
            return String.Format("Response(Success={0}, Uid={1}, Message={2}, Messages={3})", Success, Uid, Message, Messages);
        }
    }
}
