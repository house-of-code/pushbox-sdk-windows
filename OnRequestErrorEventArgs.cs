using System;

namespace HouseOfCode.PushBoxSDK
{
    public class OnRequestErrorEventArgs : EventArgs
    {
        public enum Type
        {
            AuthorizationError,
            ApiError,
        }

        public readonly Type ErrorType;
        public readonly string Message;
        public readonly string ApiMethod;
        public readonly Exception Exception;

        public OnRequestErrorEventArgs(Type errorType, string apiMethod, string message)
        {
            ErrorType = errorType;
            Message = message;
            ApiMethod = apiMethod;
        }

        public OnRequestErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}