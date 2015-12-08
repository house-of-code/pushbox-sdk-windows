using System;

namespace HouseOfCode
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

        public OnRequestErrorEventArgs(Type errorType, string message, string apiMethod)
        {
            ErrorType = errorType;
            Message = message;
            ApiMethod = apiMethod;
        }
    }
}