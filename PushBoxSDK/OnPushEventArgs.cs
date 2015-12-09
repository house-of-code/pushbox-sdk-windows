using System;

namespace HouseOfCode.PushBoxSDK
{
    public class OnPushEventArgs : EventArgs
    {
        public readonly PushBoxMessage Message;
       
        public OnPushEventArgs(PushBoxMessage message)
        {
            Message = message;
        }
    }
}