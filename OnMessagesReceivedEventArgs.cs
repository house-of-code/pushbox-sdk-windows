using System.Collections.Generic;

namespace HouseOfCode.PushBoxSDK
{
    public class OnMessagesReceivedEventArgs
    {
        public List<PushBoxMessage> Messages { get; set; }

        public OnMessagesReceivedEventArgs(List<PushBoxMessage> messages)
        {
            Messages = messages;
        }
    }
}