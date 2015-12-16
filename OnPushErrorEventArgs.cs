using System;

namespace HouseOfCode.PushBoxSDK
{
    public class OnPushErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }

        public OnPushErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}