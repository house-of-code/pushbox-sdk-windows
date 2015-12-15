using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HouseOfCode.PushBoxSDK.Api
{
    [DataContract]
    public class Response
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "uid")]
        public string Uid { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "messages")]
        public List<PushBoxMessage> Messages { get; set; }

        public override string ToString()
        {
            return $"Response(Success={Success}, Uid={Uid}, Message={Message}, Messages={Messages})";
        }
    }
}
