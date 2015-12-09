using RestSharp.Deserializers;

namespace HouseOfCode.PushBoxSDK
{
    public struct PushBoxMessage
    {
        public PushBoxMessage(string title, string message, string payload) : this()
        {
            Title = title;
            Message = message;
            Payload = payload;
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Payload { get; set; }

        public int Badge { get; set; }

        [DeserializeAs(Name = "deliver_datetime")]
        public string DeliverDateTime { get; set; }

        [DeserializeAs(Name = "read_datetime")]
        public string ReadDateTime { get; set; }

        [DeserializeAs(Name = "handled_time")]
        public string HandledTime { get; set; }

        [DeserializeAs(Name = "expiration_date")]
        public string ExpirationDate { get; set; }
    }
}
