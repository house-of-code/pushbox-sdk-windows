using System;

namespace HouseOfCode
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