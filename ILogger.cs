using System;

namespace HouseOfCode.PushBoxSDK
{
    public enum LogLevel
    {
        Debug = 1,
        Warn = 2,
        Error = 3,
    }

    public interface ILogger
    {
        LogLevel Level { get; set; }

        void Debugf(string message, params object[] args);

        void Debug(string v);

        void Warnf(string v, params object[] p);
        void Warn(string v);
        void Warn(Exception e, string v);
    }
}
