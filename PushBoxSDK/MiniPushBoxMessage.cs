using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode
{
    [DataContract]
    public struct MiniPushBoxMessage
    {
        [DataMember(Name = "i")]
        public int Id { get; set; }
        
        [DataMember(Name = "t")]
        public string Title { get; set; }

        [DataMember(Name = "m")]
        public string Message { get; set; }

        [DataMember(Name = "p")]
        public string Payload { get; set; }

        [DataMember(Name = "e")]
        public string ExpirationDate { get; set; }

        [DataMember(Name = "b")]
        public int Badge { get; set; }
    }
}
