using System;
using System.Runtime.Serialization;

namespace HouseOfCode.PushBoxSDK
{
    [DataContract]
    public struct PushBoxMessage
    {
        public PushBoxMessage(string title, string message, string payload) : this()
        {
            Title = title;
            Message = message;
            Payload = payload;
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "payload")]
        public string Payload { get; set; }

        [DataMember(Name = "badge")]
        public int Badge { get; set; }

        [DataMember(Name = "deliver_datetime")]
        public string DeliverDateTime { get; set; }

        [DataMember(Name = "read_datetime")]
        public string ReadDateTime { get; set; }

        [DataMember(Name = "handled_time")]
        public string HandledTime { get; set; }

        [DataMember(Name = "expiration_date")]
        public string ExpirationDate { get; set; }

        public override string ToString()
        {
            return
                $@" # {Title}

message ---
{Message}
---

payload ---
{Payload}
---

badge: {Badge}
deliver_datetime: {DeliverDateTime}
read_datetime: {ReadDateTime}
handled_time: {HandledTime}
expiration_date: {ExpirationDate}
";
        }
    }
}
